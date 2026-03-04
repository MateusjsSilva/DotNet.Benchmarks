using BenchmarkDotNet.Attributes;

namespace DotNet.Benchmarks.GuidGen.Benchmarks;

[MemoryDiagnoser]
public class GuidGenerationBench
{
    [Benchmark(Baseline = true)]
    public Guid GuidV4_NewGuid() => Guid.NewGuid();

    [Benchmark]
    public Guid GuidV7_CreateVersion7() => Guid.CreateVersion7();
}
