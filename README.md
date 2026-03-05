# DotNet.Benchmarks

High-performance benchmarks for .NET features using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Projects

| Project | Description | Target |
|---|---|---|
| **DotNet.Benchmarks.Compression** | GZip vs Brotli vs Zstandard (native .NET 11) | `net11.0` |
| **DotNet.Benchmarks.GuidGen** | GUID v4 vs GUID v7 (generation, index locality, SQLite, hotspots) | `net11.0` |

## Structure

```
DotNet.Benchmarks/
├── src/
│   ├── DotNet.Benchmarks.Compression/
│   │   ├── Benchmarks/
│   │   │   └── ZstdVsBrotliBench.cs
│   │   ├── Program.cs
│   │   └── DotNet.Benchmarks.Compression.csproj
│   └── DotNet.Benchmarks.GuidGen/
│       ├── Benchmarks/
│       ├── Scenarios/
│       ├── Program.cs
│       └── DotNet.Benchmarks.GuidGen.csproj
├── results/
│   ├── Compression/
│   │   ├── artifacts/
│   │   ├── BenchmarkDashboard.html
│   │   └── CompressionResults.md
│   └── GuidGen/
│       ├── artifacts/
│       └── GuidScenarioReport.md
├── docs/
├── DotNet.Benchmarks.slnx
└── README.md
```

## Benchmark Results: .NET 11 Compression

**Environment**:
- **OS**: Linux (Ubuntu 24.04)
- **Framework**: .NET 11.0 Preview
- **Hardware**: Cloud VPS (12 vCPUs, 48 GB RAM, SSD)

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

You can view the full interactive charts in [results/BenchmarkDashboard.html](results/Compression/BenchmarkDashboard.html).

## Prerequisites

- [.NET 11 Preview SDK](https://dotnet.microsoft.com/download/dotnet/11.0)
  - The project auto-resolves SDKs via `global.json`.
  - If using a user-local installation, ensure `DOTNET_ROOT` points to it:
    ```powershell
    $env:DOTNET_ROOT = "$env:USERPROFILE\.dotnet"
    $env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
    ```

## How to Run

> **Important:** Benchmarks must always be run in **Release** mode for reliable results.

### Verify Setup

```bash
dotnet --version
# Expected output: 11.0.100-preview.1... or similar

dotnet --list-sdks
# Must show 11.0.100-preview.1.26104.118
```
---
### Compression Benchmark

**Full run** (statistical analysis with multiple payload sizes):

```bash
dotnet run --project src/DotNet.Benchmarks.Compression -c Release
```

The results will appear in the console with columns for **Mean** (average time), **Error**, **StdDev**, **Ratio**, and **Allocated** (memory).

Artifacts path: `results/Compression/artifacts/`

**Quick mode** (smoke test, shorter execution):

```bash
dotnet run --project src/DotNet.Benchmarks.Compression -c Release -- --job short
```
---
### GUID Benchmark (v4 vs v7)

#### Option A: Full BenchmarkDotNet run (comprehensive, slower)

```bash
dotnet run --project src/DotNet.Benchmarks.GuidGen -c Release
```

Outputs detailed benchmarks with statistical analysis.
Artifacts: `results/GuidGen/artifacts/`

#### Option B: Scenario Report (quick consolidated table, recommended for demos)

```bash
dotnet run --project src/DotNet.Benchmarks.GuidGen -c Release -- --scenarios
```

This generates a practical table comparing v4 vs v7 across 4 scenarios (generation, locality, SQLite insert, hotspot risk).

**Output:**
- BenchmarkDotNet artifacts: `results/GuidGen/artifacts/`
- Scenario report: `results/GuidGen/GuidScenarioReport.md`

### Example Session

```bash
# Clone and enter repo
cd DotNet.Benchmarks

# Verify SDK
dotnet --version

# Run compression (quick)
dotnet run --project src/DotNet.Benchmarks.Compression -c Release -- --job short
# Check results/Compression/artifacts/ for HTML dashboard

# Run GUID scenarios (fast, informative)
dotnet run --project src/DotNet.Benchmarks.GuidGen -c Release -- --scenarios
# Check results/GuidGen/GuidScenarioReport.md for consolidated table

# Run full GUID benchmarks (comprehensive, slower ~1-2 min)
dotnet run --project src/DotNet.Benchmarks.GuidGen -c Release
# Check results/GuidGen/artifacts/ for statistical analysis
```

## License

See [LICENSE.txt](LICENSE.txt).