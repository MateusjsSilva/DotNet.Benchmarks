# GUID v4 → v7: Migration Benchmark (Part 2)

**Scenario:** legacy database with 100,000 GUID v4 rows (fragmented index).

**Strategies tested when inserting new rows:**

| Strategy | Description |
|---|---|
| V4 Baseline | Continue generating v4 — fragmentation grows over time |
| V7 Hybrid | Switch to v7 immediately — inherited fragmentation remains, new inserts are sequential |
| V7 + Vacuum | Compact the index before inserting v7 (analogous to `ALTER INDEX REBUILD` / `VACUUM FULL`) |
| V7 Full Rebuild | All IDs migrated to v7 — ideal post-migration state |

## 10.000 new rows (base: 100.000 v4)

| Strategy | Time (ms) | Rows/s | Gain vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 108,3 | 92.353 | — | 0% | 151,6% slower |
| V7 Hybrid | 20,9 | 477.929 | +80,7% | 100,0% | -51,4% slower |
| V7 + Vacuum | 27,4 | 365.462 | +74,7% | 100,0% | -36,4% slower |
| V7 Full Rebuild | 43,0 | 232.350 | +60,3% | 100% | — |

## 25.000 new rows (base: 100.000 v4)

| Strategy | Time (ms) | Rows/s | Gain vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 803,4 | 31.119 | — | 0% | 755,3% slower |
| V7 Hybrid | 142,8 | 175.046 | +82,2% | 61,2% | 52,1% slower |
| V7 + Vacuum | 127,7 | 195.833 | +84,1% | 70,1% | 35,9% slower |
| V7 Full Rebuild | 93,9 | 266.159 | +88,3% | 100% | — |

## 50.000 new rows (base: 100.000 v4)

| Strategy | Time (ms) | Rows/s | Gain vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 844,3 | 59.221 | — | 0% | 544,8% slower |
| V7 Hybrid | 78,5 | 636.643 | +90,7% | 100,0% | -40,0% slower |
| V7 + Vacuum | 173,0 | 288.959 | +79,5% | 71,2% | 32,2% slower |
| V7 Full Rebuild | 130,9 | 381.869 | +84,5% | 100% | — |

## 100.000 new rows (base: 100.000 v4)

| Strategy | Time (ms) | Rows/s | Gain vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 1.803,9 | 55.437 | — | 0% | 391,2% slower |
| V7 Hybrid | 466,8 | 214.243 | +74,1% | 73,2% | 27,1% slower |
| V7 + Vacuum | 378,7 | 264.036 | +79,0% | 96,2% | 3,1% slower |
| V7 Full Rebuild | 367,3 | 272.285 | +79,6% | 100% | — |

## 250.000 new rows (base: 100.000 v4)

| Strategy | Time (ms) | Rows/s | Gain vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 6.015,1 | 41.562 | — | 0% | 371,0% slower |
| V7 Hybrid | 1.278,8 | 195.491 | +78,7% | 99,8% | 0,1% slower |
| V7 + Vacuum | 1.100,2 | 227.231 | +81,7% | 100,0% | -13,9% slower |
| V7 Full Rebuild | 1.277,2 | 195.745 | +78,8% | 100% | — |

## 500.000 new rows (base: 100.000 v4)

| Strategy | Time (ms) | Rows/s | Gain vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 13.691,7 | 36.518 | — | 0% | 888,9% slower |
| V7 Hybrid | 1.561,7 | 320.168 | +88,6% | 87,4% | 12,8% slower |
| V7 + Vacuum | 1.695,4 | 294.912 | +87,6% | 79,6% | 22,5% slower |
| V7 Full Rebuild | 1.384,6 | 361.123 | +89,9% | 100% | — |

## Interpretation Guide

- **Gain vs V4**: how much faster compared to staying on v4
- **Recovery Index**: % of the maximum possible gain already recovered — 100% = equals Full Rebuild
  - Formula: `(V7_Hybrid_throughput − V4_throughput) / (V7_Full_throughput − V4_throughput)`
- **Gap vs Ideal**: how far the approach is from Full Rebuild

## Conclusions

- **V7 Hybrid** provides immediate gains even on a fragmented v4 base
- Gains increase as the share of new v7 records grows
- **V7 + Vacuum** recovers most of the ideal gain with a single command
- For teams that cannot rebuild all IDs, periodic `REORGANIZE`/`VACUUM`
  compacts old pages while v7 ensures new inserts do not fragment

> **Note:** `VACUUM` in SQLite ≈ `ALTER INDEX REBUILD` (SQL Server) ≈ `VACUUM FULL + REINDEX` (Postgres)
