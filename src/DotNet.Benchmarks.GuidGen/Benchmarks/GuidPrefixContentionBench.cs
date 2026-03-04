using BenchmarkDotNet.Attributes;

namespace DotNet.Benchmarks.GuidGen.Benchmarks;

[MemoryDiagnoser]
public class GuidPrefixContentionBench
{
    [Params(100_000)]
    public int Records;

    [Benchmark(Baseline = true)]
    public double GuidV4_MaxPrefixBucketShare() => CalculateMaxPrefixBucketShare(static () => Guid.NewGuid());

    [Benchmark]
    public double GuidV7_MaxPrefixBucketShare() => CalculateMaxPrefixBucketShare(static () => Guid.CreateVersion7());

    private double CalculateMaxPrefixBucketShare(Func<Guid> guidFactory)
    {
        var buckets = new Dictionary<ushort, int>();

        for (var i = 0; i < Records; i++)
        {
            var key = GuidKey.From(guidFactory());
            var prefix = (ushort)(key.High >> 48);

            buckets.TryGetValue(prefix, out var count);
            buckets[prefix] = count + 1;
        }

        var maxBucket = buckets.Values.Max();
        return (double)maxBucket / Records;
    }
}
