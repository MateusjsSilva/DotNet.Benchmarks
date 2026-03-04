using BenchmarkDotNet.Attributes;

namespace DotNet.Benchmarks.GuidGen.Benchmarks;

[MemoryDiagnoser]
public class GuidInsertLocalityBench
{
    [Params(20_000)]
    public int Records;

    [Benchmark(Baseline = true)]
    public long GuidV4_ShiftWork() => SimulateInsertShiftWork(static () => Guid.NewGuid());

    [Benchmark]
    public long GuidV7_ShiftWork() => SimulateInsertShiftWork(static () => Guid.CreateVersion7());

    private long SimulateInsertShiftWork(Func<Guid> guidFactory)
    {
        var keys = new List<GuidKey>(Records);
        long shiftWork = 0;

        for (var i = 0; i < Records; i++)
        {
            var key = GuidKey.From(guidFactory());
            var index = keys.BinarySearch(key);
            if (index < 0)
            {
                index = ~index;
            }

            shiftWork += keys.Count - index;
            keys.Insert(index, key);
        }

        return shiftWork;
    }
}
