# GUID v4 vs GUID v7 - Scenario Report

## 1) ID Generation Throughput

| Strategy | IDs | Time (ms) | IDs/s | ns/op | Max same-ms burst |
|---|---:|---:|---:|---:|---:|
| V4 | 2.000.000 | 378,8 | 5.279.660 | 189,4 | 8.085 |
| V7 | 2.000.000 | 409,0 | 4.889.728 | 204,5 | 6.116 |

## 2) Insert Locality Simulation (Ordered Index)

| Strategy | IDs | Mid-index inserts | Mid-index % | Shift work (items moved) |
|---|---:|---:|---:|---:|
| V4 | 100.000 | 99.987 | 99,99% | 2.499.837.238 |
| V7 | 100.000 | 99.595 | 99,60% | 50.666.039 |

## 3) SQLite Insert Scenario (Primary Key BLOB)

| Strategy | Rows | Time (ms) | Rows/s | DB Size (KB) | Page Count |
|---|---:|---:|---:|---:|---:|
| V4 | 100.000 | 812,8 | 123.034 | 6.416,0 | 1.604 |
| V7 | 100.000 | 429,7 | 232.714 | 6.504,0 | 1.626 |

## 4) Hotspot Risk (Prefix concentration)

| Strategy | IDs | Distinct prefixes | Max bucket share |
|---|---:|---:|---:|
| V4 | 100.000 | 51.395 | 0,01% |
| V7 | 100.000 | 1 | 100,00% |

> Nota: GUID v7 tende a reduzir custo de escrita em índices ordenados, mas pode concentrar tráfego em intervalos curtos de tempo (hotspot) dependendo do padrão de shard/partição.
