using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DotNet.Benchmarks.GuidGen.Scenarios;

internal static class GuidMigrationScenarioRunner
{
    private enum GuidFlavor { V4, V7 }

    private enum MigrationStrategy
    {
        V4Baseline,
        V7Hybrid,
        V7AfterVacuum,
        V7FullRebuild
    }

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
        const int baseCount = 100_000;

        output.WriteLine("# GUID v4 → v7: Benchmark de Migração (Parte 2)");
        output.WriteLine();
        output.WriteLine($"**Cenário:** banco legado com {baseCount:N0} linhas de GUID v4 (índice fragmentado).");
        output.WriteLine();
        output.WriteLine("**Estratégias testadas ao inserir novas linhas:**");
        output.WriteLine();
        output.WriteLine("| Estratégia | Descrição |");
        output.WriteLine("|---|---|");
        output.WriteLine("| V4 Baseline | Continua gerando v4 — fragmentação cresce indefinidamente |");
        output.WriteLine("| V7 Hybrid | Troca para v7 imediatamente — fragmentação herdada persiste, novas inserções são sequenciais |");
        output.WriteLine("| V7 + Vacuum | Compacta o índice antes de inserir v7 (análogo a `ALTER INDEX REBUILD` / `VACUUM FULL`) |");
        output.WriteLine("| V7 Full Rebuild | Todos os IDs foram migrados para v7 — estado ideal pós-migração completa |");
        output.WriteLine();

        var addCounts = new[] { 10_000, 25_000, 50_000, 100_000, 250_000, 500_000 };

        foreach (var addCount in addCounts)
        {
            var v4 = RunMigrationScenario(MigrationStrategy.V4Baseline, baseCount, addCount);
            var v7Hybrid = RunMigrationScenario(MigrationStrategy.V7Hybrid, baseCount, addCount);
            var v7Vacuum = RunMigrationScenario(MigrationStrategy.V7AfterVacuum, baseCount, addCount);
            var v7Full = RunMigrationScenario(MigrationStrategy.V7FullRebuild, baseCount, addCount);

            output.WriteLine($"## {addCount:N0} novas linhas (base: {baseCount:N0} v4)");
            output.WriteLine();
            output.WriteLine("| Estratégia | Tempo (ms) | Rows/s | Ganho vs V4 | Recovery Index | Gap vs Ideal |");
            output.WriteLine("|---|---:|---:|---:|---:|---:|");

            PrintRow(output, v4, v4, v7Full);
            PrintRow(output, v7Hybrid, v4, v7Full);
            PrintRow(output, v7Vacuum, v4, v7Full);
            PrintRow(output, v7Full, v4, v7Full);

            output.WriteLine();
        }

        output.WriteLine("## Guia de Interpretação");
        output.WriteLine();
        output.WriteLine("- **Ganho vs V4**: quanto mais rápido que continuar com v4 puro");
        output.WriteLine("- **Recovery Index**: % do ganho máximo possível já recuperado — 100% = equivale ao Full Rebuild");
        output.WriteLine("  - Fórmula: `(V7_Hybrid_throughput − V4_throughput) / (V7_Full_throughput − V4_throughput)`");
        output.WriteLine("- **Gap vs Ideal**: quanto a abordagem ainda está atrás do Full Rebuild");
        output.WriteLine();
        output.WriteLine("## Conclusões");
        output.WriteLine();
        output.WriteLine("- **V7 Hybrid** oferece ganho imediato mesmo sobre uma base v4 fragmentada");
        output.WriteLine("- O ganho cresce à medida que a proporção de novos registros v7 aumenta");
        output.WriteLine("- **V7 + Vacuum** recupera a maior parte do ganho ideal com um único comando");
        output.WriteLine("- Para quem não pode rebuildar todos os IDs, um `REORGANIZE`/`VACUUM` periódico");
        output.WriteLine("  compacta as páginas antigas enquanto o v7 garante que as novas não fragmentem");
        output.WriteLine();
        output.WriteLine("> **Nota:** `VACUUM` no SQLite ≈ `ALTER INDEX REBUILD` (SQL Server) ≈ `VACUUM FULL + REINDEX` (Postgres)");
    }

    private static void PrintRow(TextWriter output, MigrationResult result, MigrationResult baseline, MigrationResult ideal)
    {
        var gainVsV4 = result.Strategy == MigrationStrategy.V4Baseline
            ? "—"
            : $"+{(baseline.ElapsedMs - result.ElapsedMs) / baseline.ElapsedMs * 100:N1}%";

        // Recovery Index: how much of the ideal gain (Full Rebuild) has been recovered
        // 0% = no improvement vs V4; 100% = matches Full Rebuild
        var recoveryIndex = result.Strategy == MigrationStrategy.V4Baseline
            ? "0%"
            : result.Strategy == MigrationStrategy.V7FullRebuild
                ? "100%"
                : ComputeRecovery(result.RowsPerSec, baseline.RowsPerSec, ideal.RowsPerSec);

        var gapVsIdeal = result.Strategy == MigrationStrategy.V7FullRebuild
            ? "—"
            : $"{(result.ElapsedMs - ideal.ElapsedMs) / ideal.ElapsedMs * 100:N1}% mais lento";

        var name = result.Strategy switch
        {
            MigrationStrategy.V4Baseline => "V4 Baseline",
            MigrationStrategy.V7Hybrid => "V7 Hybrid",
            MigrationStrategy.V7AfterVacuum => "V7 + Vacuum",
            MigrationStrategy.V7FullRebuild => "V7 Full Rebuild",
            _ => result.Strategy.ToString()
        };

        output.WriteLine($"| {name} | {result.ElapsedMs:N1} | {result.RowsPerSec:N0} | {gainVsV4} | {recoveryIndex} | {gapVsIdeal} |");
    }

    private static string ComputeRecovery(double actual, double baseline, double ideal)
    {
        var potentialGain = ideal - baseline;
        if (potentialGain <= 0) return "N/A";
        var recovered = Math.Clamp((actual - baseline) / potentialGain * 100, 0, 100);
        return $"{recovered:N1}%";
    }

    private static MigrationResult RunMigrationScenario(MigrationStrategy strategy, int baseCount, int addCount)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"guid-migration-{strategy}-{Guid.NewGuid():N}.db");

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

            // Fase 1: popular a base legada
            // V4Baseline, V7Hybrid, V7AfterVacuum → base v4 (fragmentada)
            // V7FullRebuild → base v7 (todos os IDs migrados, índice limpo)
            var baseFlavor = strategy == MigrationStrategy.V7FullRebuild ? GuidFlavor.V7 : GuidFlavor.V4;
            InsertRows(connection, baseFlavor, baseCount);

            // Fase 2: compactar o índice (apenas para V7AfterVacuum)
            // VACUUM reconstrói o banco eliminando fragmentação — análogo ao ALTER INDEX REBUILD
            if (strategy == MigrationStrategy.V7AfterVacuum)
            {
                using var vacuum = connection.CreateCommand();
                vacuum.CommandText = "VACUUM;";
                vacuum.ExecuteNonQuery();
            }

            // Fase 3: inserção cronometrada das novas linhas
            var newFlavor = strategy == MigrationStrategy.V4Baseline ? GuidFlavor.V4 : GuidFlavor.V7;

            var sw = Stopwatch.StartNew();
            InsertRows(connection, newFlavor, addCount);
            sw.Stop();

            var rowsPerSec = addCount / sw.Elapsed.TotalSeconds;
            var pageCount = ExecuteScalarInt(connection, "PRAGMA page_count;");
            connection.Close();
            var fileBytes = new FileInfo(dbPath).Length;

            return new MigrationResult(strategy, baseCount, addCount, sw.Elapsed.TotalMilliseconds, rowsPerSec, fileBytes, pageCount);
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
            // Big-endian: garante que a ordem de bytes == ordem temporal para o v7
            idParam.Value = guid.ToByteArray(bigEndian: true);
            insert.ExecuteNonQuery();
        }

        tx.Commit();
    }

    private static int ExecuteScalarInt(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }

    private sealed record MigrationResult(
        MigrationStrategy Strategy,
        int BaseCount,
        int AddCount,
        double ElapsedMs,
        double RowsPerSec,
        long FileBytes,
        int PageCount);
}
