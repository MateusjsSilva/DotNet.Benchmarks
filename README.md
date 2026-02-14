# DotNet.Benchmarks

High-performance benchmarks for .NET features using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Projects

| Project | Description | Target |
|---|---|---|
| **DotNet.Benchmarks.Compression** | GZip vs Brotli vs Zstandard (native .NET 11) | `net11.0` |

## Structure

```
DotNet.Benchmarks/
├── src/
│   ├── DotNet.Benchmarks.Compression/
│   │   ├── Benchmarks/
│   │   │   └── ZstdVsBrotliBench.cs
│   │   ├── Program.cs
│   │   └── DotNet.Benchmarks.Compression.csproj
├── results/
│   ├── BenchmarkDashboard.html (Visual Charts)
│   └── CompressionResults.md
├── docs/
├── DotNet.Benchmarks.slnx
└── README.md
```

## Benchmark Results: .NET 11 Compression

**Environment**: `.NET 11.0 Preview` on `Linux (Ubuntu 24.04)`, AMD EPYC Processor.

### Summary
- **Zstandard (Zstd)** is significantly faster than GZip and Brotli in **Optimal** mode (up to 4x faster).
- **Zstandard** also wins in **Decompression** speed across all sizes.
- **Brotli** provides good compression but is much slower to compress in Optimal mode.

### Highlights (100 MB Payload)

| Algorithm | Mode | Time (ms) | Throughput |
|---|---|---:|---:|
| **Zstandard** | Optimal | **591 ms** | **~169 MB/s** |
| GZip | Optimal | 1073 ms | ~93 MB/s |
| Brotli | Optimal | 2549 ms | ~39 MB/s |

*Lower time is better.*

You can view the full interactive charts in [results/BenchmarkDashboard.html](results/BenchmarkDashboard.html).

## Prerequisites

- [.NET 11 Preview SDK](https://dotnet.microsoft.com/download/dotnet/11.0) (for Compression project)

## How to Run

> **Important:** Benchmarks must always be run in **Release** mode for reliable results.

### Compression Benchmark

```bash
dotnet run --project src/DotNet.Benchmarks.Compression -c Release
```

The results will appear in the console with columns for **Mean** (average time), **Error**, **StdDev**, **Ratio**, and **Allocated** (memory).

### Quick Mode (Smoke Test)

```bash
dotnet run --project src/DotNet.Benchmarks.Compression -c Release -- --job short
```

## License

See [LICENSE.txt](LICENSE.txt).