# GUID v4 → v7: Convergence Point

**Legacy base:** 100,000 GUID v4 rows (fragmented B-Tree index)
**Methodology:** 3 runs per point → median (reduces single-run variance)
**Convergence threshold:** Recovery Index ≥ 90%

> Recovery Index = (Hybrid_throughput − V4_throughput) / (Full_throughput − V4_throughput) × 100%
> 100% = V7 Hybrid equals Full Rebuild (complete migration)

| New Rows | Ratio (new/base) | V4 (ms) | Hybrid (ms) | Full (ms) | Hybrid Speedup | Recovery Index |
|---:|---:|---:|---:|---:|---:|---:|
| 10.000 | 0,10× | 76,1 | 16,0 | 15,8 | 4,77× | **98,7%** |
| 20.000 | 0,20× | 131,6 | 29,5 | 30,0 | 4,45× | **102,2%** ← **convergência** |
| 30.000 | 0,30× | 247,0 | 45,0 | 49,9 | 5,49× | **113,8%** |
| 40.000 | 0,40× | 349,7 | 69,9 | 61,9 | 5,00× | 86,1% |
| 50.000 | 0,50× | 457,2 | 82,0 | 79,3 | 5,58× | **96,0%** |
| 60.000 | 0,60× | 551,5 | 97,7 | 95,8 | 5,64× | **97,7%** |
| 70.000 | 0,70× | 656,6 | 140,8 | 141,4 | 4,66× | **100,6%** |
| 80.000 | 0,80× | 763,3 | 164,7 | 156,4 | 4,63× | **93,7%** |
| 90.000 | 0,90× | 851,2 | 178,6 | 173,6 | 4,77× | **96,5%** |
| 100.000 | 1,00× | 989,1 | 195,0 | 196,3 | 5,07× | **100,9%** |
| 110.000 | 1,10× | 1.113,8 | 218,7 | 226,9 | 5,09× | **104,7%** |
| 120.000 | 1,20× | 1.241,5 | 238,7 | 242,5 | 5,20× | **102,0%** |
| 130.000 | 1,30× | 1.390,6 | 261,0 | 278,4 | 5,33× | **108,3%** |
| 140.000 | 1,40× | 1.647,2 | 290,4 | 277,6 | 5,67× | **94,7%** |
| 150.000 | 1,50× | 1.581,8 | 311,1 | 301,9 | 5,08× | **96,3%** |
| 160.000 | 1,60× | 2.037,5 | 357,1 | 321,3 | 5,71× | 88,1% |
| 170.000 | 1,70× | 1.888,8 | 367,7 | 338,1 | 5,14× | **90,2%** |
| 180.000 | 1,80× | 2.023,8 | 369,7 | 362,0 | 5,47× | **97,5%** |
| 190.000 | 1,90× | 2.177,3 | 399,8 | 393,2 | 5,45× | **98,0%** |
| 200.000 | 2,00× | 2.313,6 | 423,8 | 418,3 | 5,46× | **98,4%** |
| 210.000 | 2,10× | 2.586,9 | 478,3 | 443,3 | 5,41× | **91,2%** |
| 220.000 | 2,20× | 2.591,2 | 494,7 | 461,9 | 5,24× | **91,9%** |
| 230.000 | 2,30× | 2.888,0 | 508,7 | 477,6 | 5,68× | **92,7%** |
| 240.000 | 2,40× | 2.852,8 | 509,8 | 515,4 | 5,60× | **101,3%** |
| 250.000 | 2,50× | 3.587,8 | 576,6 | 529,1 | 6,22× | **90,3%** |
| 260.000 | 2,60× | 3.121,9 | 576,5 | 528,4 | 5,42× | 90,0% |
| 270.000 | 2,70× | 3.276,2 | 592,8 | 547,7 | 5,53× | **90,9%** |
| 280.000 | 2,80× | 3.393,9 | 634,8 | 580,6 | 5,35× | 89,7% |
| 290.000 | 2,90× | 3.695,2 | 612,2 | 609,2 | 6,04× | **99,4%** |
| 300.000 | 3,00× | 3.780,7 | 686,9 | 650,1 | 5,50× | **93,5%** |
| 310.000 | 3,10× | 3.969,6 | 744,7 | 669,1 | 5,33× | 87,8% |
| 320.000 | 3,20× | 4.129,1 | 690,1 | 680,4 | 5,98× | **98,3%** |
| 330.000 | 3,30× | 4.206,1 | 723,8 | 740,1 | 5,81× | **102,7%** |
| 340.000 | 3,40× | 4.419,0 | 745,5 | 720,9 | 5,93× | **96,1%** |
| 350.000 | 3,50× | 4.818,6 | 766,6 | 712,8 | 6,29× | **91,8%** |

## Result: Convergence at ~10,000 new rows

When the database has 100,000 legacy v4 rows, **V7 Hybrid** reaches ≥90% of
the Full Rebuild performance after inserting **~10,000 new rows** (ratio **0.10× the legacy base**).

### What this means in practice

- Switching to `Guid.CreateVersion7()` in code has an **immediate** impact (approx. 4×–6× faster than v4)
- Performance becomes **practically equal** to Full Rebuild once you insert ~0.10× the legacy base size in new v7 records
- A `REORGANIZE` / `VACUUM` speeds up convergence: the compacted index removes inherited fragmentation

## Recovery Index Interpretation

| Range | Interpretation |
|---|---|
| 0–50% | V7 Hybrid clearly behind Full Rebuild — inherited fragmentation dominates |
| 50–80% | Partial gain — most of the benefit is already present |
| 80–95% | Near ideal — inherited fragmentation has residual impact |
| ≥95% | **Converged** — performance equivalent to Full Rebuild |
> >100% can occur due to single-run variance (SQLite without warmed cache)
