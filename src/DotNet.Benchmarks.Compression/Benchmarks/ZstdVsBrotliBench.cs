using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace DotNet.Benchmarks.Compression.Benchmarks;

/// <summary>
/// Definitive Benchmark: GZip vs Brotli vs Zstandard (.NET 11)
///
/// - Realistic Data: API JSON (simulates real microservice payloads)
/// - Large Sizes: 1 MB, 5 MB, 10 MB, 100 MB
/// - Compression AND Decompression
/// - Levels: Fastest and Optimal
/// - Compression Ratios reported
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ZstdVsBrotliBench
{
    private byte[] _data = null!;

    private byte[] _gzipCompressed = null!;
    private byte[] _brotliCompressed = null!;
    private byte[] _zstdCompressed = null!;

    private MemoryStream _output = null!;

    [Params(1, 5, 10, 100)]
    public int SizeMB { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = GenerateRealisticJson(SizeMB * 1024 * 1024);
        _output = new MemoryStream(_data.Length);

        // Pre-compress for decompression tests (Optimal)
        _gzipCompressed = CompressToArray(s => new GZipStream(s, CompressionLevel.Optimal, true));
        _brotliCompressed = CompressToArray(s => new BrotliStream(s, CompressionLevel.Optimal, true));
        _zstdCompressed = CompressToArray(s => new ZstandardStream(s, CompressionLevel.Optimal, true));

        // Compressed sizes report
        Console.WriteLine();
        Console.WriteLine($"  ╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"  ║  Payload: {SizeMB} MB ({_data.Length:N0} bytes) — Realistic JSON         ║");
        Console.WriteLine($"  ╠══════════════════════════════════════════════════════════════╣");

        foreach (var level in new[] { CompressionLevel.Fastest, CompressionLevel.Optimal })
        {
            var gzBuf = CompressToArray(s => new GZipStream(s, level, true));
            var brBuf = CompressToArray(s => new BrotliStream(s, level, true));
            var zsBuf = CompressToArray(s => new ZstandardStream(s, level, true));

            Console.WriteLine($"  ║  [{level,-8}]                                                  ║");
            Console.WriteLine($"  ║    GZip:      {gzBuf.Length,12:N0} bytes  (ratio {(double)gzBuf.Length / _data.Length:P1})     ║");
            Console.WriteLine($"  ║    Brotli:    {brBuf.Length,12:N0} bytes  (ratio {(double)brBuf.Length / _data.Length:P1})     ║");
            Console.WriteLine($"  ║    Zstandard: {zsBuf.Length,12:N0} bytes  (ratio {(double)zsBuf.Length / _data.Length:P1})     ║");
        }

        Console.WriteLine($"  ╠══════════════════════════════════════════════════════════════╣");
        Console.WriteLine($"  ║  Decompression uses pre-compressed buffers in Optimal       ║");
        Console.WriteLine($"  ║    GZip:      {_gzipCompressed.Length,12:N0} bytes                         ║");
        Console.WriteLine($"  ║    Brotli:    {_brotliCompressed.Length,12:N0} bytes                         ║");
        Console.WriteLine($"  ║    Zstandard: {_zstdCompressed.Length,12:N0} bytes                         ║");
        Console.WriteLine($"  ╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    /// <summary>
    /// Generates realistic JSON simulating REST API / microservice payloads.
    /// Includes varied fields: strings, numbers, dates, arrays, nested objects.
    /// Natural entropy — not repetitive text or pure random noise.
    /// </summary>
    private static byte[] GenerateRealisticJson(int targetBytes)
    {
        var rng = new Random(42); // fixed seed for reproducibility
        var records = new List<object>();

        // Names and data kept localized to simulate regional e-commerce data (Brazil)
        string[] firstNames = ["João", "Maria", "Pedro", "Ana", "Carlos", "Fernanda", "Lucas", "Juliana",
            "Rafael", "Beatriz", "Gustavo", "Camila", "Thiago", "Larissa", "Diego", "Amanda"];
        string[] lastNames = ["Silva", "Santos", "Oliveira", "Souza", "Rodrigues", "Ferreira", "Almeida",
            "Nascimento", "Lima", "Araújo", "Pereira", "Costa", "Carvalho", "Melo"];
        string[] cities = ["São Paulo", "Rio de Janeiro", "Belo Horizonte", "Curitiba", "Porto Alegre",
            "Salvador", "Brasília", "Fortaleza", "Recife", "Manaus", "Goiânia", "Belém"];
        string[] states = ["SP", "RJ", "MG", "PR", "RS", "BA", "DF", "CE", "PE", "AM", "GO", "PA"];
        string[] products = ["Notebook Dell Inspiron 15", "iPhone 15 Pro Max 256GB", "Samsung Galaxy S24 Ultra",
            "LG UltraWide Monitor 34\"", "Redragon Mechanical Keyboard", "Logitech MX Master 3S Mouse",
            "HyperX Cloud III Headset", "Samsung 990 Pro 2TB NVMe SSD", "Logitech C920 Webcam",
            "ThunderX3 Gamer Chair", "RTX 4070 Ti Graphics Card", "Ryzen 7 7800X3D Processor"];
        string[] statuses = ["pending", "processing", "shipped", "delivered", "cancelled", "refunded"];
        string[] methods = ["credit_card", "debit_card", "pix", "boleto", "wallet"];
        string[] carriers = ["DHL Express", "FedEx", "UPS", "Local Courier", "Amazon Logistics"];
        string[] logLevels = ["INFO", "WARN", "ERROR", "DEBUG"];
        string[] endpoints = ["/api/v1/orders", "/api/v1/users", "/api/v1/products", "/api/v1/payments",
            "/api/v1/shipping", "/api/v1/inventory", "/api/v1/reports", "/api/v1/notifications"];
        string[] userAgents = [
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/131.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_0) AppleWebKit/605.1.15 Safari/17.0",
            "Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 Chrome/131.0 Mobile",
            "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0) AppleWebKit/605.1.15 Mobile/15E148"
        ];

        int id = 1;
        while (true)
        {
            // Simulates an e-commerce order record with log entries
            var orderDate = new DateTime(2024, 1, 1).AddMinutes(rng.Next(525600));
            var itemCount = rng.Next(1, 6);
            var items = new List<object>();
            decimal total = 0;

            for (int i = 0; i < itemCount; i++)
            {
                var price = Math.Round((decimal)(rng.NextDouble() * 5000 + 50), 2);
                var qty = rng.Next(1, 4);
                total += price * qty;
                items.Add(new
                {
                    sku = $"SKU-{rng.Next(10000, 99999)}",
                    name = products[rng.Next(products.Length)],
                    quantity = qty,
                    unit_price = price,
                    subtotal = price * qty
                });
            }

            var firstName = firstNames[rng.Next(firstNames.Length)];
            var lastName = lastNames[rng.Next(lastNames.Length)];
            var cityIdx = rng.Next(cities.Length);

            var record = new
            {
                order_id = $"ORD-{id:D8}",
                created_at = orderDate.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                updated_at = orderDate.AddHours(rng.Next(1, 72)).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                status = statuses[rng.Next(statuses.Length)],
                customer = new
                {
                    id = $"USR-{rng.Next(100000, 999999)}",
                    name = $"{firstName} {lastName}",
                    email = $"{firstName.ToLower()}.{lastName.ToLower()}{rng.Next(100)}@email.com",
                    phone = $"+55 {rng.Next(11, 99)} 9{rng.Next(1000, 9999)}-{rng.Next(1000, 9999)}",
                    document = $"{rng.Next(100, 999)}.{rng.Next(100, 999)}.{rng.Next(100, 999)}-{rng.Next(10, 99)}"
                },
                shipping = new
                {
                    address = $"Street {lastNames[rng.Next(lastNames.Length)]}, {rng.Next(1, 9999)}",
                    complement = rng.Next(3) == 0 ? $"Apt {rng.Next(1, 200)}" : null,
                    city = cities[cityIdx],
                    state = states[cityIdx],
                    zip_code = $"{rng.Next(10000, 99999)}-{rng.Next(100, 999)}",
                    carrier = carriers[rng.Next(carriers.Length)],
                    tracking_code = $"TRK{rng.Next(100000000, 999999999)}",
                    estimated_days = rng.Next(2, 15)
                },
                payment = new
                {
                    method = methods[rng.Next(methods.Length)],
                    installments = rng.Next(1, 13),
                    total,
                    discount = Math.Round(total * (decimal)(rng.NextDouble() * 0.15), 2),
                    transaction_id = Guid.NewGuid().ToString()
                },
                items,
                metadata = new
                {
                    ip_address = $"{rng.Next(1, 255)}.{rng.Next(0, 255)}.{rng.Next(0, 255)}.{rng.Next(1, 255)}",
                    user_agent = userAgents[rng.Next(userAgents.Length)],
                    session_id = Guid.NewGuid().ToString(),
                    utm_source = rng.Next(3) == 0 ? "google_ads" : rng.Next(2) == 0 ? "instagram" : "organic",
                    request_log = new
                    {
                        level = logLevels[rng.Next(logLevels.Length)],
                        endpoint = endpoints[rng.Next(endpoints.Length)],
                        response_time_ms = rng.Next(5, 2000),
                        status_code = rng.Next(10) == 0 ? 500 : rng.Next(5) == 0 ? 404 : 200
                    }
                }
            };

            records.Add(record);
            id++;

            // Check size periodically (every 50 records)
            if (id % 50 == 0)
            {
                var currentBytes = JsonSerializer.SerializeToUtf8Bytes(new { data = records, total_records = records.Count });
                if (currentBytes.Length >= targetBytes)
                {
                    return currentBytes;
                }
            }
        }
    }

    private byte[] CompressToArray(Func<MemoryStream, Stream> factory)
    {
        using var ms = new MemoryStream();
        using (var compressor = factory(ms))
            compressor.Write(_data);
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════
    //  COMPRESSION — Fastest
    // ═══════════════════════════════════════════════════

    [BenchmarkCategory("Compress-Fastest"), Benchmark(Baseline = true)]
    public void GZip_Compress_Fastest()
    {
        _output.SetLength(0);
        using var gz = new GZipStream(_output, CompressionLevel.Fastest, leaveOpen: true);
        gz.Write(_data);
    }

    [BenchmarkCategory("Compress-Fastest"), Benchmark]
    public void Brotli_Compress_Fastest()
    {
        _output.SetLength(0);
        using var br = new BrotliStream(_output, CompressionLevel.Fastest, leaveOpen: true);
        br.Write(_data);
    }

    [BenchmarkCategory("Compress-Fastest"), Benchmark]
    public void Zstd_Compress_Fastest()
    {
        _output.SetLength(0);
        using var zstd = new ZstandardStream(_output, CompressionLevel.Fastest, leaveOpen: true);
        zstd.Write(_data);
    }

    // ═══════════════════════════════════════════════════
    //  COMPRESSION — Optimal
    // ═══════════════════════════════════════════════════

    [BenchmarkCategory("Compress-Optimal"), Benchmark(Baseline = true)]
    public void GZip_Compress_Optimal()
    {
        _output.SetLength(0);
        using var gz = new GZipStream(_output, CompressionLevel.Optimal, leaveOpen: true);
        gz.Write(_data);
    }

    [BenchmarkCategory("Compress-Optimal"), Benchmark]
    public void Brotli_Compress_Optimal()
    {
        _output.SetLength(0);
        using var br = new BrotliStream(_output, CompressionLevel.Optimal, leaveOpen: true);
        br.Write(_data);
    }

    [BenchmarkCategory("Compress-Optimal"), Benchmark]
    public void Zstd_Compress_Optimal()
    {
        _output.SetLength(0);
        using var zstd = new ZstandardStream(_output, CompressionLevel.Optimal, leaveOpen: true);
        zstd.Write(_data);
    }

    // ═══════════════════════════════════════════════════
    //  DECOMPRESSION (Pre-compressed Optimal data)
    // ═══════════════════════════════════════════════════

    [BenchmarkCategory("Decompress"), Benchmark(Baseline = true)]
    public void GZip_Decompress()
    {
        _output.SetLength(0);
        using var input = new MemoryStream(_gzipCompressed);
        using var gz = new GZipStream(input, CompressionMode.Decompress);
        gz.CopyTo(_output);
    }

    [BenchmarkCategory("Decompress"), Benchmark]
    public void Brotli_Decompress()
    {
        _output.SetLength(0);
        using var input = new MemoryStream(_brotliCompressed);
        using var br = new BrotliStream(input, CompressionMode.Decompress);
        br.CopyTo(_output);
    }

    [BenchmarkCategory("Decompress"), Benchmark]
    public void Zstd_Decompress()
    {
        _output.SetLength(0);
        using var input = new MemoryStream(_zstdCompressed);
        using var zstd = new ZstandardStream(input, CompressionMode.Decompress);
        zstd.CopyTo(_output);
    }
}
