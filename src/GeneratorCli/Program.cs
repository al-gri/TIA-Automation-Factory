using System.Text;
using System.Text.Json;
using TiaAutomationFactory.Domain;
using TiaAutomationFactory.PlcCompiler;
using TiaAutomationFactory.SiemensBackend;

namespace TiaAutomationFactory.GeneratorCli;

public static class Program
{
    public static Task<int> Main(string[] args)
        => GeneratorCliApp.RunAsync(args, Console.Out, Console.Error);
}

public sealed class GeneratorCliException : Exception
{
    public string Code { get; }

    public GeneratorCliException(string code)
        : base(code)
    {
        Code = code;
    }
}

public static class GeneratorCliApp
{
    public const string UsageDiagnostic = "GEN-CLI-USAGE";
    public const string InputJsonDiagnostic = "GEN-CLI-INPUT-JSON";
    public const string InputModelDiagnostic = "GEN-CLI-INPUT-MODEL";
    public const string InputTypeDiagnostic = "GEN-CLI-INPUT-TYPE";
    public const string ProfileDiagnostic = "GEN-CLI-PROFILE";
    public const string SiemensNameDiagnostic = "GEN-CLI-SIEMENS-NAME";
    public const string IoDiagnostic = "GEN-CLI-IO";
    public const string UnexpectedDiagnostic = "GEN-CLI-UNEXPECTED";

    public static async Task<int> RunAsync(
        string[] arguments,
        TextWriter stdout,
        TextWriter stderr)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (arguments.Length is < 2 or > 3)
        {
            await stderr.WriteLineAsync(UsageDiagnostic);
            return 2;
        }

        try
        {
            var json = await File.ReadAllTextAsync(arguments[0]);
            var device = ParseDevice(json);
            var ir = AutomationCompiler.Compile(device);
            var scl = SclDataTypeGenerator.Generate(ir);
            var artifactBytes = new UTF8Encoding(false).GetBytes(scl);
            var artifactRelativePath = $"UDT_{ir.Name}.scl";

            var artifactPath = OutputContainment.ResolveContainedPath(
                arguments[1],
                artifactRelativePath);

            string? manifestPath = null;
            byte[]? manifestBytes = null;

            if (arguments.Length == 3)
            {
                var profileIdentity = ValidateProfileIdentity(arguments[2]);
                var inputIdentity = CanonicalInputIdentity.Compute(ir);
                var artifact = GeneratorManifest.ArtifactFromBytes(
                    artifactRelativePath,
                    artifactBytes);

                manifestBytes = GeneratorManifest.Serialize(
                    inputIdentity,
                    profileIdentity,
                    new[] { artifact });

                manifestPath = OutputContainment.ResolveContainedPath(
                    arguments[1],
                    GeneratorManifest.ManifestFileName);
            }

            Directory.CreateDirectory(Path.GetFullPath(arguments[1]));
            await File.WriteAllBytesAsync(artifactPath, artifactBytes);

            if (manifestPath is not null && manifestBytes is not null)
                await File.WriteAllBytesAsync(manifestPath, manifestBytes);

            await stdout.WriteLineAsync(artifactRelativePath);
            if (manifestPath is not null)
                await stdout.WriteLineAsync(GeneratorManifest.ManifestFileName);

            return 0;
        }
        catch (GeneratorCliException exception)
        {
            await stderr.WriteLineAsync(exception.Code);
            return 2;
        }
        catch (JsonException)
        {
            await stderr.WriteLineAsync(InputJsonDiagnostic);
            return 2;
        }
        catch (InvalidOperationException)
        {
            await stderr.WriteLineAsync(InputModelDiagnostic);
            return 2;
        }
        catch (SiemensNamePolicyException)
        {
            await stderr.WriteLineAsync(SiemensNameDiagnostic);
            return 2;
        }
        catch (IOException)
        {
            await stderr.WriteLineAsync(IoDiagnostic);
            return 2;
        }
        catch (UnauthorizedAccessException)
        {
            await stderr.WriteLineAsync(IoDiagnostic);
            return 2;
        }
        catch
        {
            await stderr.WriteLineAsync(UnexpectedDiagnostic);
            return 2;
        }
    }

    public static AutomationDevice ParseDevice(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            throw new GeneratorCliException(InputModelDiagnostic);

        if (!TryGetPropertyIgnoreCase(root, "name", out var nameElement)
            || nameElement.ValueKind != JsonValueKind.String)
        {
            throw new GeneratorCliException(InputModelDiagnostic);
        }

        if (!TryGetPropertyIgnoreCase(root, "fields", out var fieldsElement)
            || fieldsElement.ValueKind != JsonValueKind.Array)
        {
            throw new GeneratorCliException(InputModelDiagnostic);
        }

        var fields = new List<AutomationField>();
        foreach (var fieldElement in fieldsElement.EnumerateArray())
        {
            if (fieldElement.ValueKind != JsonValueKind.Object)
                throw new GeneratorCliException(InputModelDiagnostic);

            if (!TryGetPropertyIgnoreCase(fieldElement, "name", out var fieldNameElement)
                || fieldNameElement.ValueKind != JsonValueKind.String)
            {
                throw new GeneratorCliException(InputModelDiagnostic);
            }

            if (!TryGetPropertyIgnoreCase(fieldElement, "type", out var typeElement)
                || typeElement.ValueKind != JsonValueKind.String)
            {
                throw new GeneratorCliException(InputTypeDiagnostic);
            }

            var typeToken = typeElement.GetString();
            if (string.IsNullOrWhiteSpace(typeToken)
                || !Enum.TryParse<AutomationType>(typeToken, ignoreCase: true, out var type)
                || !Enum.IsDefined(type))
            {
                throw new GeneratorCliException(InputTypeDiagnostic);
            }

            fields.Add(new AutomationField(fieldNameElement.GetString()!, type));
        }

        return new AutomationDevice(nameElement.GetString()!, fields);
    }

    public static string ValidateProfileIdentity(string profileIdentity)
    {
        if (string.IsNullOrWhiteSpace(profileIdentity)
            || profileIdentity.Length > 128
            || profileIdentity.Any(character => char.IsControl(character) || char.IsSurrogate(character)))
        {
            throw new GeneratorCliException(ProfileDiagnostic);
        }

        return profileIdentity;
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
