using BenchmarkDotNet.Running;
using DotNet.Benchmarks.Compression.Benchmarks;

BenchmarkRunner.Run<ZstdVsBrotliBench>();
