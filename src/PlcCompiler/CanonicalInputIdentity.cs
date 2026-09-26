using System.Security.Cryptography;
using System.Text;
using TiaAutomationFactory.PlcCompiler;

namespace TiaAutomationFactory.PlcCompiler;

public static class CanonicalInputIdentity
{
    public static string Compute(PlcIrDataType dataType)
    {
        ArgumentNullException.ThrowIfNull(dataType);

        using var sha256 = SHA256.Create();
        var canonical = BuildCanonicalRepresentation(dataType);
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string BuildCanonicalRepresentation(PlcIrDataType dataType)
    {
        var sb = new StringBuilder();
        sb.Append(dataType.Name);
        sb.Append('|');
        foreach (var field in dataType.Fields)
        {
            sb.Append(field.Name);
            sb.Append(':');
            sb.Append(field.Type.ToString());
            sb.Append('|');
        }
        return sb.ToString();
    }
}