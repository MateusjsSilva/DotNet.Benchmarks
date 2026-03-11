# GUID v4 → v7: Benchmark de Migração (Parte 2)

**Cenário:** banco legado com 100.000 linhas de GUID v4 (índice fragmentado).

**Estratégias testadas ao inserir novas linhas:**

| Estratégia | Descrição |
|---|---|
| V4 Baseline | Continua gerando v4 — fragmentação cresce indefinidamente |
| V7 Hybrid | Troca para v7 imediatamente — fragmentação herdada persiste, novas inserções são sequenciais |
| V7 + Vacuum | Compacta o índice antes de inserir v7 (análogo a `ALTER INDEX REBUILD` / `VACUUM FULL`) |
| V7 Full Rebuild | Todos os IDs foram migrados para v7 — estado ideal pós-migração completa |

## 10.000 novas linhas (base: 100.000 v4)

| Estratégia | Tempo (ms) | Rows/s | Ganho vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 108,3 | 92.353 | — | 0% | 151,6% mais lento |
| V7 Hybrid | 20,9 | 477.929 | +80,7% | 100,0% | -51,4% mais lento |
| V7 + Vacuum | 27,4 | 365.462 | +74,7% | 100,0% | -36,4% mais lento |
| V7 Full Rebuild | 43,0 | 232.350 | +60,3% | 100% | — |

## 25.000 novas linhas (base: 100.000 v4)

| Estratégia | Tempo (ms) | Rows/s | Ganho vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 803,4 | 31.119 | — | 0% | 755,3% mais lento |
| V7 Hybrid | 142,8 | 175.046 | +82,2% | 61,2% | 52,1% mais lento |
| V7 + Vacuum | 127,7 | 195.833 | +84,1% | 70,1% | 35,9% mais lento |
| V7 Full Rebuild | 93,9 | 266.159 | +88,3% | 100% | — |

## 50.000 novas linhas (base: 100.000 v4)

| Estratégia | Tempo (ms) | Rows/s | Ganho vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 844,3 | 59.221 | — | 0% | 544,8% mais lento |
| V7 Hybrid | 78,5 | 636.643 | +90,7% | 100,0% | -40,0% mais lento |
| V7 + Vacuum | 173,0 | 288.959 | +79,5% | 71,2% | 32,2% mais lento |
| V7 Full Rebuild | 130,9 | 381.869 | +84,5% | 100% | — |

## 100.000 novas linhas (base: 100.000 v4)

| Estratégia | Tempo (ms) | Rows/s | Ganho vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 1.803,9 | 55.437 | — | 0% | 391,2% mais lento |
| V7 Hybrid | 466,8 | 214.243 | +74,1% | 73,2% | 27,1% mais lento |
| V7 + Vacuum | 378,7 | 264.036 | +79,0% | 96,2% | 3,1% mais lento |
| V7 Full Rebuild | 367,3 | 272.285 | +79,6% | 100% | — |

## 250.000 novas linhas (base: 100.000 v4)

| Estratégia | Tempo (ms) | Rows/s | Ganho vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 6.015,1 | 41.562 | — | 0% | 371,0% mais lento |
| V7 Hybrid | 1.278,8 | 195.491 | +78,7% | 99,8% | 0,1% mais lento |
| V7 + Vacuum | 1.100,2 | 227.231 | +81,7% | 100,0% | -13,9% mais lento |
| V7 Full Rebuild | 1.277,2 | 195.745 | +78,8% | 100% | — |

## 500.000 novas linhas (base: 100.000 v4)

| Estratégia | Tempo (ms) | Rows/s | Ganho vs V4 | Recovery Index | Gap vs Ideal |
|---|---:|---:|---:|---:|---:|
| V4 Baseline | 13.691,7 | 36.518 | — | 0% | 888,9% mais lento |
| V7 Hybrid | 1.561,7 | 320.168 | +88,6% | 87,4% | 12,8% mais lento |
| V7 + Vacuum | 1.695,4 | 294.912 | +87,6% | 79,6% | 22,5% mais lento |
| V7 Full Rebuild | 1.384,6 | 361.123 | +89,9% | 100% | — |

## Guia de Interpretação

- **Ganho vs V4**: quanto mais rápido que continuar com v4 puro
- **Recovery Index**: % do ganho máximo possível já recuperado — 100% = equivale ao Full Rebuild
  - Fórmula: `(V7_Hybrid_throughput − V4_throughput) / (V7_Full_throughput − V4_throughput)`
- **Gap vs Ideal**: quanto a abordagem ainda está atrás do Full Rebuild

## Conclusões

- **V7 Hybrid** oferece ganho imediato mesmo sobre uma base v4 fragmentada
- O ganho cresce à medida que a proporção de novos registros v7 aumenta
- **V7 + Vacuum** recupera a maior parte do ganho ideal com um único comando
- Para quem não pode rebuildar todos os IDs, um `REORGANIZE`/`VACUUM` periódico
  compacta as páginas antigas enquanto o v7 garante que as novas não fragmentem

> **Nota:** `VACUUM` no SQLite ≈ `ALTER INDEX REBUILD` (SQL Server) ≈ `VACUUM FULL + REINDEX` (Postgres)
