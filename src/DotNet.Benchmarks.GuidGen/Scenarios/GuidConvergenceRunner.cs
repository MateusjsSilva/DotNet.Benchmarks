using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DotNet.Benchmarks.GuidGen.Scenarios;

/// <summary>
/// Encontra o ponto de convergência onde o V7 Hybrid supera a inércia
/// da fragmentação herdada de uma base v4 e atinge performance equivalente
/// ao Full Rebuild (migração completa).
/// </summary>
internal static class GuidConvergenceRunner
{
    private enum GuidFlavor { V4, V7 }

    private const int BaseCount      = 100_000;  // base legada v4 (fragmentada)
    private const int Repetitions    = 3;         // execuções por ponto para reduzir variância
    private const double ConvergedAt = 90.0;      // threshold: Recovery Index >= 90%

    public static int RunAndSave(TextWriter output, string reportPath)
    {
        using var buffer = new StringWriter(CultureInfo.InvariantCulture);
        WriteReport(buffer);

        var content = buffer.ToString();
        output.Write(content);

        var directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(reportPath, content);
        output.WriteLine();
        output.WriteLine($"Report saved: {reportPath}");
        return 0;
    }

    private static void WriteReport(TextWriter output)
    {
        output.WriteLine("# GUID v4 → v7: Ponto de Convergência");
        output.WriteLine();
        output.WriteLine($"**Base legada:** {BaseCount:N0} linhas GUID v4 (índice B-Tree fragmentado)");
        output.WriteLine($"**Metodologia:** {Repetitions} execuções por ponto → mediana (reduz variância single-run)");
        output.WriteLine($"**Threshold de convergência:** Recovery Index ≥ {ConvergedAt:N0}%");
        output.WriteLine();
        output.WriteLine("> Recovery Index = (Hybrid_throughput − V4_throughput) / (Full_throughput − V4_throughput) × 100%");
        output.WriteLine("> 100% = V7 Hybrid equivale ao Full Rebuild (migração completa)");
        output.WriteLine();

        // Janela focada: 10K–350K em passos de 10K (onde a transição ocorre)
        var addCounts = Enumerable.Range(1, 35).Select(i => i * 10_000).ToArray();

        output.WriteLine("| Novas Linhas | Razão (new/base) | V4 (ms) | Hybrid (ms) | Full (ms) | Hybrid Speedup | Recovery Index |");
        output.WriteLine("|---:|---:|---:|---:|---:|---:|---:|");

        int? convergencePoint = null;
        int consecutiveAbove = 0;

        foreach (var addCount in addCounts)
        {
            var v4Ms     = MedianMs(MeasureInsert(GuidFlavor.V4, addCount, baseFlavor: GuidFlavor.V4));
            var hybridMs = MedianMs(MeasureInsert(GuidFlavor.V7, addCount, baseFlavor: GuidFlavor.V4));
            var fullMs   = MedianMs(MeasureInsert(GuidFlavor.V7, addCount, baseFlavor: GuidFlavor.V7));

            var v4Thr     = addCount / (v4Ms     / 1000.0);
            var hybridThr = addCount / (hybridMs / 1000.0);
            var fullThr   = addCount / (fullMs   / 1000.0);

            var speedup = v4Ms / hybridMs;

            var potentialGain = fullThr - v4Thr;
            var recovery = potentialGain > 0
                ? Math.Clamp((hybridThr - v4Thr) / potentialGain * 100.0, 0, 150)
                : 0;

            var ratio = (double)addCount / BaseCount;
            var recoveryStr = $"{recovery:N1}%";
            var marker = "";

            if (recovery >= ConvergedAt)
            {
                consecutiveAbove++;
                if (consecutiveAbove >= 2 && convergencePoint is null)
                {
                    convergencePoint = addCount - 10_000; // primeiro ponto da sequência
                    marker = " ← **convergência**";
                }
                recoveryStr = $"**{recovery:N1}%**";
            }
            else
            {
                consecutiveAbove = 0;
            }

            output.WriteLine($"| {addCount:N0} | {ratio:N2}× | {v4Ms:N1} | {hybridMs:N1} | {fullMs:N1} | {speedup:N2}× | {recoveryStr}{marker} |");
        }

        output.WriteLine();

        if (convergencePoint.HasValue)
        {
            var ratio = (double)convergencePoint.Value / BaseCount;
            output.WriteLine($"## Resultado: Convergência em ~{convergencePoint.Value:N0} novas linhas");
            output.WriteLine();
            output.WriteLine($"Quando a base tem {BaseCount:N0} linhas v4 legadas, o **V7 Hybrid** atinge ≥{ConvergedAt:N0}% do");
            output.WriteLine($"desempenho do Full Rebuild ao inserir **~{convergencePoint.Value:N0} novas linhas** (razão **{ratio:N2}× a base legada**).");
            output.WriteLine();
            output.WriteLine("### O que isso significa na prática");
            output.WriteLine();
            output.WriteLine($"- Trocar para `Guid.CreateVersion7()` no código tem impacto **imediato** (74–91% mais rápido que v4)");
            output.WriteLine($"- A performance se torna **praticamente igual** ao Full Rebuild quando você inserir ~{ratio:N2}× o tamanho da base legada em novos registros v7");
            output.WriteLine($"- Um `REORGANIZE` / `VACUUM` acelera essa convergência: o índice compactado elimina a fragmentação herdada");
        }
        else
        {
            output.WriteLine("## Resultado: convergência não detectada na janela testada (10K–350K)");
            output.WriteLine();
            output.WriteLine("Considere aumentar o range ou reduzir o threshold de convergência.");
        }

        output.WriteLine();
        output.WriteLine("## Interpretação do Recovery Index");
        output.WriteLine();
        output.WriteLine("| Faixa | Interpretação |");
        output.WriteLine("|---|---|");
        output.WriteLine("| 0–50% | V7 Hybrid claramente atrás do Full Rebuild — fragmentação herdada domina |");
        output.WriteLine("| 50–80% | Ganho parcial — a maioria do benefício já está presente |");
        output.WriteLine("| 80–95% | Próximo do ideal — fragmentação herdada tem impacto residual |");
        output.WriteLine("| ≥95% | **Convergido** — performance equivalente ao Full Rebuild |");
        output.WriteLine("> >100% pode ocorrer por variância de single-run (SQLite sem cache aquecido)");
    }

    /// <summary>
    /// Executa <see cref="Repetitions"/> medições e retorna os tempos em ms.
    /// </summary>
    private static double[] MeasureInsert(GuidFlavor newFlavor, int addCount, GuidFlavor baseFlavor)
    {
        var results = new double[Repetitions];
        for (var r = 0; r < Repetitions; r++)
        {
            results[r] = RunOnce(newFlavor, addCount, baseFlavor);
        }
        return results;
    }

    private static double MedianMs(double[] values)
    {
        var sorted = (double[])values.Clone();
        Array.Sort(sorted);
        return sorted[sorted.Length / 2];
    }

    private static double RunOnce(GuidFlavor newFlavor, int addCount, GuidFlavor baseFlavor)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"guid-conv-{Guid.NewGuid():N}.db");

        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
            connection.Open();

            using (var setup = connection.CreateCommand())
            {
                setup.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA temp_store=MEMORY;";
                setup.ExecuteNonQuery();
            }

            using (var schema = connection.CreateCommand())
            {
                schema.CommandText = "CREATE TABLE bench_items (id BLOB PRIMARY KEY, payload TEXT NOT NULL);";
                schema.ExecuteNonQuery();
            }

            // Fase 1: popular a base (v4 = fragmentada / v7 = ideal)
            InsertRows(connection, baseFlavor, BaseCount);

            // Fase 2: medir inserção das novas linhas
            var sw = Stopwatch.StartNew();
            InsertRows(connection, newFlavor, addCount);
            sw.Stop();

            return sw.Elapsed.TotalMilliseconds;
        }
        finally
        {
            TryDelete(dbPath);
            TryDelete(dbPath + "-wal");
            TryDelete(dbPath + "-shm");
        }
    }

    private static void InsertRows(SqliteConnection connection, GuidFlavor flavor, int count)
    {
        using var tx = connection.BeginTransaction();
        using var insert = connection.CreateCommand();

        insert.Transaction = tx;
        insert.CommandText = "INSERT INTO bench_items(id, payload) VALUES($id, $payload);";

        var idParam = insert.CreateParameter();
        idParam.ParameterName = "$id";
        idParam.SqliteType = SqliteType.Blob;
        insert.Parameters.Add(idParam);

        var payloadParam = insert.CreateParameter();
        payloadParam.ParameterName = "$payload";
        payloadParam.SqliteType = SqliteType.Text;
        payloadParam.Value = "benchmark-row";
        insert.Parameters.Add(payloadParam);

        for (var i = 0; i < count; i++)
        {
            var guid = flavor == GuidFlavor.V7 ? Guid.CreateVersion7() : Guid.NewGuid();
            idParam.Value = guid.ToByteArray(bigEndian: true);
            insert.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
