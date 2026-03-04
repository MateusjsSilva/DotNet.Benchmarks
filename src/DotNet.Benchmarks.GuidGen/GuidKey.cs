using System.Buffers.Binary;

namespace DotNet.Benchmarks.GuidGen;

internal readonly struct GuidKey : IComparable<GuidKey>
{
    public ulong High { get; }
    public ulong Low { get; }

    private GuidKey(ulong high, ulong low)
    {
        High = high;
        Low = low;
    }

    public static GuidKey From(Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        guid.TryWriteBytes(bytes, bigEndian: true, out _);

        var high = BinaryPrimitives.ReadUInt64BigEndian(bytes[..8]);
        var low = BinaryPrimitives.ReadUInt64BigEndian(bytes[8..]);
        return new GuidKey(high, low);
    }

    public int CompareTo(GuidKey other)
    {
        var highCompare = High.CompareTo(other.High);
        return highCompare != 0 ? highCompare : Low.CompareTo(other.Low);
    }
}
