using System.Security.Cryptography;
using System.Text;
using TiaAutomationFactory.Domain;

namespace TiaAutomationFactory.PlcCompiler;

public static class CanonicalInputIdentity
{
    private static readonly byte[] FormatMagic = Encoding.ASCII.GetBytes("TAF-CANONICAL-INPUT-V1");

    public static string Compute(PlcIrDataType dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);

        using var sha256 = SHA256.Create();
        var canonical = BuildCanonicalRepresentation(dataType);
        var hash = sha256.ComputeHash(canonical);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static byte[] BuildCanonicalRepresentation(PlcIrDataType dataType)
    {
        using var stream = new MemoryStream();

        WriteBytes(stream, FormatMagic);
        WriteString(stream, dataType.Name);
        WriteInt32BigEndian(stream, dataType.Fields.Count);

        foreach (var field in dataType.Fields)
        {
            WriteString(stream, field.Name);
            WriteString(stream, GetCanonicalTypeToken(field.Type));
        }

        return stream.ToArray();
    }

    private static void WriteString(Stream stream, string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteInt32BigEndian(stream, bytes.Length);
        WriteBytes(stream, bytes);
    }

    private static void WriteInt32BigEndian(Stream stream, int value)
    {
        stream.WriteByte((byte)((value >> 24) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)(value & 0xFF));
    }

    private static void WriteBytes(Stream stream, byte[] bytes)
    {
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string GetCanonicalTypeToken(AutomationType type) => type switch
    {
        AutomationType.Bool => "Bool",
        AutomationType.Int => "Int",
        AutomationType.DInt => "DInt",
        AutomationType.Real => "Real",
        AutomationType.String => "String",
        AutomationType.Time => "Time",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
