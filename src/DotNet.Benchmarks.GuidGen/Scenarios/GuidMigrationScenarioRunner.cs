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

        output.WriteLine("# GUID v4 → v7: Migration Benchmark (Part 2)");
        output.WriteLine();
        output.WriteLine($"**Scenario:** legacy database with {baseCount:N0} GUID v4 rows (fragmented index).");
        output.WriteLine();
        output.WriteLine("**Strategies tested when inserting new rows:");
        output.WriteLine();
        output.WriteLine("| Strategy | Description |");
        output.WriteLine("|---|---|");
        output.WriteLine("| V4 Baseline | Continue generating v4 — fragmentation grows over time |");
        output.WriteLine("| V7 Hybrid | Switch to v7 immediately — inherited fragmentation remains, new inserts are sequential |");
        output.WriteLine("| V7 + Vacuum | Compact the index before inserting v7 (analogous to `ALTER INDEX REBUILD` / `VACUUM FULL`) |");
        output.WriteLine("| V7 Full Rebuild | All IDs migrated to v7 — ideal post-migration state |");
        output.WriteLine();

        var addCounts = new[] { 10_000, 25_000, 50_000, 100_000, 250_000, 500_000 };

        foreach (var addCount in addCounts)
        {
            var v4 = RunMigrationScenario(MigrationStrategy.V4Baseline, baseCount, addCount);
            var v7Hybrid = RunMigrationScenario(MigrationStrategy.V7Hybrid, baseCount, addCount);
            var v7Vacuum = RunMigrationScenario(MigrationStrategy.V7AfterVacuum, baseCount, addCount);
            var v7Full = RunMigrationScenario(MigrationStrategy.V7FullRebuild, baseCount, addCount);

            output.WriteLine($"## {addCount:N0} new rows (base: {baseCount:N0} v4)");
            output.WriteLine();
            output.WriteLine("| Strategy | Time (ms) | Rows/s | Gain vs V4 | Recovery Index | Gap vs Ideal |");
            output.WriteLine("|---|---:|---:|---:|---:|---:|");

            PrintRow(output, v4, v4, v7Full);
            PrintRow(output, v7Hybrid, v4, v7Full);
            PrintRow(output, v7Vacuum, v4, v7Full);
            PrintRow(output, v7Full, v4, v7Full);

            output.WriteLine();
        }

            output.WriteLine("## Interpretation Guide");
        output.WriteLine();
            output.WriteLine("- **Gain vs V4**: how much faster compared to staying on v4");
            output.WriteLine("- **Recovery Index**: % of the maximum possible gain already recovered — 100% = equals Full Rebuild");
            output.WriteLine("  - Formula: `(V7_Hybrid_throughput − V4_throughput) / (V7_Full_throughput − V4_throughput)`");
            output.WriteLine("- **Gap vs Ideal**: how far the approach is from Full Rebuild");
        output.WriteLine();
            output.WriteLine("## Conclusions");
        output.WriteLine();
            output.WriteLine("- **V7 Hybrid** provides immediate gains even on a fragmented v4 base");
            output.WriteLine("- Gains increase as the share of new v7 records grows");
            output.WriteLine("- **V7 + Vacuum** recovers most of the ideal gain with a single command");
            output.WriteLine("- For teams that cannot rebuild all IDs, periodic `REORGANIZE`/`VACUUM`");
            output.WriteLine("  compacts old pages while v7 ensures new inserts do not fragment");
        output.WriteLine();
            output.WriteLine("> **Note:** `VACUUM` in SQLite ≈ `ALTER INDEX REBUILD` (SQL Server) ≈ `VACUUM FULL + REINDEX` (Postgres)");
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
            : $"{(result.ElapsedMs - ideal.ElapsedMs) / ideal.ElapsedMs * 100:N1}% slower";

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

            // Phase 1: populate the legacy base
            // V4Baseline, V7Hybrid, V7AfterVacuum -> base v4 (fragmented)
            // V7FullRebuild -> base v7 (all IDs migrated, clean index)
            var baseFlavor = strategy == MigrationStrategy.V7FullRebuild ? GuidFlavor.V7 : GuidFlavor.V4;
            InsertRows(connection, baseFlavor, baseCount);

            // Phase 2: compact the index (only for V7AfterVacuum)
            // VACUUM rebuilds the database to remove fragmentation — analogous to ALTER INDEX REBUILD
            if (strategy == MigrationStrategy.V7AfterVacuum)
            {
                using var vacuum = connection.CreateCommand();
                vacuum.CommandText = "VACUUM;";
                vacuum.ExecuteNonQuery();
            }

            // Phase 3: timed insertion of new rows
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
            // Big-endian: ensures byte order == temporal order for v7
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
