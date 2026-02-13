# DotNet.Benchmarks

Benchmarks de alto desempenho para recursos do .NET usando [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Projetos

| Projeto | Descrição | Target |
|---|---|---|
| **DotNet.Benchmarks.Compression** | GZip vs Brotli vs Zstandard (nativo .NET 11) | `net11.0` |
| **DotNet.Benchmarks.Runtime** | Benchmarks de runtime | `net10.0` |
| **DotNet.Benchmarks.Threading** | Benchmarks de threading | `net10.0` |

## Estrutura

```
DotNet.Benchmarks/
├── src/
│   ├── DotNet.Benchmarks.Compression/
│   │   ├── Benchmarks/
│   │   │   └── ZstdVsBrotliBench.cs
│   │   ├── Program.cs
│   │   └── DotNet.Benchmarks.Compression.csproj
│   ├── DotNet.Benchmarks.Runtime/
│   └── DotNet.Benchmarks.Threading/
├── results/
│   └── CompressionResults.md
├── docs/
├── DotNet.Benchmarks.slnx
└── README.md
```

## Pré-requisitos

- [.NET 11 Preview SDK](https://dotnet.microsoft.com/download/dotnet/11.0) (para o projeto de Compression)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (para os demais projetos)

## Como rodar

> **Importante:** Benchmarks devem sempre rodar em modo **Release** para resultados confiáveis.

### Compression Benchmark

```bash
dotnet run --project src/DotNet.Benchmarks.Compression -c Release
```

Os resultados aparecerão no terminal com as colunas **Mean** (tempo médio), **Error**, **StdDev**, **Ratio** e **Allocated** (memória).

### Modo rápido (smoke test)

```bash
dotnet run --project src/DotNet.Benchmarks.Compression -c Release -- --job short
```

## Licença

Veja [LICENSE.txt](LICENSE.txt).