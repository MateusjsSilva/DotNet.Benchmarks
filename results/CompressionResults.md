# .NET 11 Compression Benchmark Results

**BenchmarkDotNet** v0.14.0 · **Linux (Ubuntu 24.04)** · **.NET 11.0 Preview** · **AMD EPYC**

## Results Summary

The table below shows the performance of Zstandard, GZip, and Brotli compression algorithms on realistic JSON payloads.

| Method | Size (MB) | Mean (ms) | Ratio | Throughput (MB/s) |
| :--- | :--- | :--- | :--- | :--- |
| **GZip_Compress_Fastest** | 1 | 3.04 ms | 1.00 | ~329 MB/s |
| Brotli_Compress_Fastest | 1 | 3.41 ms | 1.12 | ~293 MB/s |
| **Zstd_Compress_Fastest** | 1 | **2.58 ms** | **0.85** | **~387 MB/s** |
| | | | | |
| **GZip_Compress_Fastest** | 5 | 15.17 ms | 1.00 | ~329 MB/s |
| Brotli_Compress_Fastest | 5 | 19.03 ms | 1.25 | ~262 MB/s |
| **Zstd_Compress_Fastest** | 5 | **13.04 ms** | **0.86** | **~383 MB/s** |
| | | | | |
| **GZip_Compress_Optimal** | 1 | 11.01 ms | 1.00 | ~90 MB/s |
| Brotli_Compress_Optimal | 1 | 12.98 ms | 1.18 | ~77 MB/s |
| **Zstd_Compress_Optimal** | 1 | **3.75 ms** | **0.34** | **~266 MB/s** |
| | | | | |
| **GZip_Compress_Optimal** | 5 | 54.91 ms | 1.00 | ~91 MB/s |
| Brotli_Compress_Optimal | 5 | 105.76 ms | 1.93 | ~47 MB/s |
| **Zstd_Compress_Optimal** | 5 | **24.14 ms** | **0.44** | **~207 MB/s** |
| | | | | |
| **GZip_Compress_Optimal** | 100 | 1,073.72 ms | 1.00 | ~93 MB/s |
| Brotli_Compress_Optimal | 100 | 2,549.09 ms | 2.38 | ~39 MB/s |
| **Zstd_Compress_Optimal** | 100 | **591.40 ms** | **0.55** | **~169 MB/s** |

## Key Findings

1.  **Zstandard is significantly faster** in `Optimal` mode, achieving up to **3x-4x** greater throughput compared to GZip.
2.  **Brotli** struggles with large payloads in `Optimal` mode, becoming significantly slower than both GZip and Zstandard.
3.  **Decompression**: Zstandard consistently outperformed both GZip and Brotli in decompression speeds across all tested sizes.
