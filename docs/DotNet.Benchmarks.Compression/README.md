# DotNet.Benchmarks.Compression

## Overview

Quick summary:

- **Purpose**: Compare GZip, Brotli, and Zstandard (Zstd) across compression/decompression scenarios using BenchmarkDotNet.
- **Target**: `net11.0` (leverages .NET 11 preview APIs and optimizations).
- **Data**: Realistic JSON (REST API / microservice payloads) in sizes: 1 MB, 5 MB, 10 MB, 100 MB.
- **Metrics**: Throughput, compression ratio (Fastest vs Optimal), memory allocation.

## Key Results (Example)

| Algorithm | Mode | Time (ms) | Throughput |
|---|---|---:|---:|
| Zstandard | Optimal | 591 ms | ~169 MB/s |
| GZip | Optimal | 1073 ms | ~93 MB/s |
| Brotli | Optimal | 2549 ms | ~39 MB/s |

## How to Run

### Full Benchmark

Best for complete statistical analysis (~2-5 min depending on hardware):

```bash
dotnet run --project src/DotNet.Benchmarks.Compression -c Release
```

**Output**:
- Console: tables with statistics per algorithm/size (Mean, Error, StdDev, Ratio, Allocated)
- Artifacts: HTML dashboard + JSON results in `results/Compression/artifacts/`

### Quick Mode (Smoke Test)

Fast validation (~10-20 sec, great for CI/demos):

```bash
dotnet run --project src/DotNet.Benchmarks.Compression -c Release -- --job short
```

**Output**:
- Console: summary with pre-compressed sizes and ratios
- Artifacts: `results/Compression/artifacts/` (abbreviated)

## Interpretation

- **Zstandard** is significantly faster than GZip and Brotli in Optimal mode (up to 4x).
- **Zstandard** also wins in decompression speed.
- **Brotli** offers better compression (~50% reduction vs Zstd) but is much slower to compress.
- **GZip** offers adequate balance for universal compatibility.

## Structure

- `Benchmarks/ZstdVsBrotliBench.cs` — main class with all benchmarks
- `Program.cs` — entry point (auto-detects `results/` root)
- `DotNet.Benchmarks.Compression.csproj` — BenchmarkDotNet references

## Standardized Output

**Artifacts directory**: `results/Compression/artifacts/`

- `BenchmarkRun-*.json` — raw results
- `*.html` — interactive dashboard (open in browser)
- `*.md` — markdown summary

## Troubleshooting

- **"Timeout"**: reduce payload size in `GenerateRealisticJson()` if needed.
- **"Memory exceeded"**: use `--job short` or `--job tiny` for quick validation.
