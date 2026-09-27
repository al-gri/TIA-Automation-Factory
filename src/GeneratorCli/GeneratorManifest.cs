using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace TiaAutomationFactory.GeneratorCli;

public sealed record GeneratorArtifact(string RelativePath, string Sha256);

public static class GeneratorManifest
{
    public const string GeneratorIdentity = "tia-automation-factory-generator-v1";
    public const string ManifestFileName = "generator-manifest.json";

    public static GeneratorArtifact ArtifactFromBytes(string relativePath, byte[] bytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(bytes);

        return new GeneratorArtifact(relativePath.Replace('\\', '/'), Sha256Hex(bytes));
    }

    public static byte[] Serialize(
        string inputIdentity,
        string profileIdentity,
        IEnumerable<GeneratorArtifact> artifacts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileIdentity);
        ArgumentNullException.ThrowIfNull(artifacts);

        var ordered = artifacts
            .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
            .ToArray();

        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(
            buffer,
            new JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.Default
            }))
        {
            writer.WriteStartObject();
            writer.WriteString("generatorIdentity", GeneratorIdentity);
            writer.WriteString("inputIdentity", inputIdentity);
            writer.WriteString("profileIdentity", profileIdentity);
            writer.WritePropertyName("artifacts");
            writer.WriteStartArray();

            foreach (var artifact in ordered)
            {
                writer.WriteStartObject();
                writer.WriteString("path", artifact.RelativePath);
                writer.WriteString("sha256", artifact.Sha256);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return buffer.ToArray();
    }

    public static string Sha256Hex(byte[] bytes)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(bytes)).ToLowerInvariant();
    }
}
