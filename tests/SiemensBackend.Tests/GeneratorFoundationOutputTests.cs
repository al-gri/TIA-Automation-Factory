using System.Text;
using System.Text.Json;
using TiaAutomationFactory.GeneratorCli;
using Xunit;

namespace SiemensBackend.Tests;

public sealed class GeneratorFoundationOutputTests
{
    private const string MotorJson = """
    {
      "name": "Motor",
      "fields": [
        { "name": "Start", "type": "Bool" },
        { "name": "Stop", "type": "Bool" },
        { "name": "Running", "type": "Bool" },
        { "name": "Fault", "type": "Bool" },
        { "name": "Speed", "type": "Real" }
      ]
    }
    """;

    [Fact]
    public async Task Profile_mode_is_byte_deterministic_and_hashes_exact_artifact_bytes()
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("motor.json", MotorJson);
        var outputA = Path.Combine(temp.Path, "out-a");
        var outputB = Path.Combine(temp.Path, "out-b");

        Assert.Equal(0, await Run(input, outputA, "profile-A"));
        Assert.Equal(0, await Run(input, outputB, "profile-A"));

        var artifactA = await File.ReadAllBytesAsync(Path.Combine(outputA, "UDT_Motor.scl"));
        var artifactB = await File.ReadAllBytesAsync(Path.Combine(outputB, "UDT_Motor.scl"));
        var manifestA = await File.ReadAllBytesAsync(Path.Combine(outputA, GeneratorManifest.ManifestFileName));
        var manifestB = await File.ReadAllBytesAsync(Path.Combine(outputB, GeneratorManifest.ManifestFileName));

        Assert.Equal(artifactA, artifactB);
        Assert.Equal(manifestA, manifestB);

        using var json = JsonDocument.Parse(manifestA);
        var root = json.RootElement;
        Assert.Equal(GeneratorManifest.GeneratorIdentity, root.GetProperty("generatorIdentity").GetString());
        Assert.Equal("profile-A", root.GetProperty("profileIdentity").GetString());

        var artifacts = root.GetProperty("artifacts");
        Assert.Equal(1, artifacts.GetArrayLength());
        Assert.Equal("UDT_Motor.scl", artifacts[0].GetProperty("path").GetString());
        Assert.Equal(
            GeneratorManifest.Sha256Hex(artifactA),
            artifacts[0].GetProperty("sha256").GetString());

        var manifestText = Encoding.UTF8.GetString(manifestA);
        Assert.DoesNotContain(temp.Path, manifestText, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.UserName, manifestText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("timestamp", manifestText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Changing_only_profile_changes_binding_not_input_identity_or_artifact_hash()
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("motor.json", MotorJson);
        var outputA = Path.Combine(temp.Path, "profile-a");
        var outputB = Path.Combine(temp.Path, "profile-b");

        Assert.Equal(0, await Run(input, outputA, "profile-A"));
        Assert.Equal(0, await Run(input, outputB, "profile-B"));

        using var manifestA = JsonDocument.Parse(await File.ReadAllBytesAsync(
            Path.Combine(outputA, GeneratorManifest.ManifestFileName)));
        using var manifestB = JsonDocument.Parse(await File.ReadAllBytesAsync(
            Path.Combine(outputB, GeneratorManifest.ManifestFileName)));

        Assert.Equal(
            manifestA.RootElement.GetProperty("inputIdentity").GetString(),
            manifestB.RootElement.GetProperty("inputIdentity").GetString());
        Assert.NotEqual(
            manifestA.RootElement.GetProperty("profileIdentity").GetString(),
            manifestB.RootElement.GetProperty("profileIdentity").GetString());
        Assert.Equal(
            manifestA.RootElement.GetProperty("artifacts")[0].GetProperty("sha256").GetString(),
            manifestB.RootElement.GetProperty("artifacts")[0].GetProperty("sha256").GetString());
    }

    [Fact]
    public async Task Legacy_two_argument_motor_flow_remains_compatible_without_manifest()
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("motor.json", MotorJson);
        var output = Path.Combine(temp.Path, "legacy");

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var exit = await GeneratorCliApp.RunAsync(new[] { input, output }, stdout, stderr);

        Assert.Equal(0, exit);
        Assert.True(File.Exists(Path.Combine(output, "UDT_Motor.scl")));
        Assert.False(File.Exists(Path.Combine(output, GeneratorManifest.ManifestFileName)));
        Assert.Contains("UDT_Motor.scl", stdout.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(temp.Path, stdout.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, stderr.ToString());
    }

    [Theory]
    [InlineData("""{"name":"Motor","fields":[{"name":"Start"}]}""")]
    [InlineData("""{"name":"Motor","fields":[{"name":"Start","type":0}]}""")]
    [InlineData("""{"name":"Motor","fields":[{"name":"Start","type":"0"}]}""")]
    [InlineData("""{"name":"Motor","fields":[{"name":"Start","type":"Bogus"}]}""")]
    public async Task Missing_numeric_or_unsupported_type_fails_closed(string json)
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("input.json", json);
        var output = Path.Combine(temp.Path, "out");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await GeneratorCliApp.RunAsync(new[] { input, output }, stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(GeneratorCliApp.InputTypeDiagnostic + Environment.NewLine, stderr.ToString());
        Assert.False(Directory.Exists(output));
    }

    [Theory]
    [InlineData("""{"name":"Motor","fields":[{"name":"Start","type":"Bool","TYPE":0}]}""")]
    [InlineData("""{"name":"Motor","fields":[{"name":"Start","TYPE":0,"type":"Bool"}]}""")]
    [InlineData("""{"name":"Motor","fields":[{"name":"Start","type":"Bool","TYPE":"Bogus"}]}""")]
    [InlineData("""{"name":"Motor","fields":[{"name":"Start","TYPE":"Bogus","type":"Bool"}]}""")]
    public async Task Duplicate_type_properties_fail_closed_in_both_orders(string json)
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("input.json", json);
        var output = Path.Combine(temp.Path, "out");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await GeneratorCliApp.RunAsync(new[] { input, output }, stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(GeneratorCliApp.InputTypeDiagnostic + Environment.NewLine, stderr.ToString());
        Assert.False(Directory.Exists(output));
    }

    [Theory]
    [InlineData("""{"name":"Motor","NAME":"Other","fields":[]}""")]
    [InlineData("""{"name":"Motor","fields":[],"FIELDS":[]}""")]
    [InlineData("""{"name":"Motor","fields":[{"name":"Start","NAME":"Other","type":"Bool"}]}""")]
    public async Task Duplicate_non_type_semantic_properties_fail_as_model_errors(string json)
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("input.json", json);
        var output = Path.Combine(temp.Path, "out");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await GeneratorCliApp.RunAsync(new[] { input, output }, stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(GeneratorCliApp.InputModelDiagnostic + Environment.NewLine, stderr.ToString());
        Assert.False(Directory.Exists(output));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("line\nbreak")]
    public async Task Invalid_profile_identity_fails_before_any_write(string profile)
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("motor.json", MotorJson);
        var output = Path.Combine(temp.Path, "out");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exit = await GeneratorCliApp.RunAsync(new[] { input, output, profile }, stdout, stderr);

        Assert.Equal(2, exit);
        Assert.Equal(GeneratorCliApp.ProfileDiagnostic + Environment.NewLine, stderr.ToString());
        Assert.False(Directory.Exists(output));
    }

    [Fact]
    public void Output_containment_rejects_absolute_parent_and_sibling_prefix_escapes()
    {
        using var temp = new TempDirectory();
        var root = Path.Combine(temp.Path, "root");
        var sibling = Path.Combine(temp.Path, "root-sibling", "escape.scl");

        Assert.Throws<GeneratorCliException>(() =>
            OutputContainment.ResolveContainedPath(root, "../root-sibling/escape.scl"));
        Assert.Throws<GeneratorCliException>(() =>
            OutputContainment.ResolveContainedPath(root, "..\\root-sibling\\escape.scl"));
        Assert.Throws<GeneratorCliException>(() =>
            OutputContainment.ResolveContainedPath(root, Path.Combine(temp.Path, "absolute.scl")));
        Assert.Throws<GeneratorCliException>(() =>
            OutputContainment.ResolveContainedPath(root, @"C:\outside\escape.scl"));

        Assert.False(File.Exists(sibling));
    }

    [Fact]
    public async Task Existing_artifact_symlink_fails_closed_without_modifying_outside_target()
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("motor.json", MotorJson);
        var output = Path.Combine(temp.Path, "out");
        Directory.CreateDirectory(output);

        var outside = temp.WriteFile("outside-artifact.scl", "sentinel");
        File.CreateSymbolicLink(Path.Combine(output, "UDT_Motor.scl"), outside);

        var stderr = new StringWriter();
        var exit = await GeneratorCliApp.RunAsync(
            new[] { input, output },
            new StringWriter(),
            stderr);

        Assert.Equal(2, exit);
        Assert.Equal(OutputContainment.DiagnosticCode + Environment.NewLine, stderr.ToString());
        Assert.Equal("sentinel", await File.ReadAllTextAsync(outside));
    }

    [Fact]
    public async Task Existing_manifest_symlink_fails_before_any_output_write()
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("motor.json", MotorJson);
        var output = Path.Combine(temp.Path, "out");
        Directory.CreateDirectory(output);

        var outside = temp.WriteFile("outside-manifest.json", "sentinel");
        File.CreateSymbolicLink(
            Path.Combine(output, GeneratorManifest.ManifestFileName),
            outside);

        var stderr = new StringWriter();
        var exit = await GeneratorCliApp.RunAsync(
            new[] { input, output, "profile-A" },
            new StringWriter(),
            stderr);

        Assert.Equal(2, exit);
        Assert.Equal(OutputContainment.DiagnosticCode + Environment.NewLine, stderr.ToString());
        Assert.Equal("sentinel", await File.ReadAllTextAsync(outside));
        Assert.False(File.Exists(Path.Combine(output, "UDT_Motor.scl")));
    }

    [Fact]
    public async Task Redirecting_output_component_fails_closed_without_writing_through_link()
    {
        using var temp = new TempDirectory();
        var input = temp.WriteFile("motor.json", MotorJson);
        var outsideRoot = Path.Combine(temp.Path, "outside-root");
        Directory.CreateDirectory(outsideRoot);

        var rootParent = Path.Combine(temp.Path, "root-parent");
        Directory.CreateDirectory(rootParent);
        var redirect = Path.Combine(rootParent, "redirect");
        Directory.CreateSymbolicLink(redirect, outsideRoot);

        var output = Path.Combine(redirect, "nested");
        var stderr = new StringWriter();
        var exit = await GeneratorCliApp.RunAsync(
            new[] { input, output },
            new StringWriter(),
            stderr);

        Assert.Equal(2, exit);
        Assert.Equal(OutputContainment.DiagnosticCode + Environment.NewLine, stderr.ToString());
        Assert.False(File.Exists(Path.Combine(outsideRoot, "nested", "UDT_Motor.scl")));
    }

    [Fact]
    public void Output_containment_accepts_a_relative_child_and_normalizes_it_under_root()
    {
        using var temp = new TempDirectory();
        var root = Path.Combine(temp.Path, "root");

        var resolved = OutputContainment.ResolveContainedPath(root, "nested/artifact.scl");
        var relative = Path.GetRelativePath(Path.GetFullPath(root), resolved);

        Assert.False(Path.IsPathRooted(relative));
        Assert.Equal(Path.Combine("nested", "artifact.scl"), relative);
    }

    private static async Task<int> Run(string input, string output, string profile)
    {
        return await GeneratorCliApp.RunAsync(
            new[] { input, output, profile },
            new StringWriter(),
            new StringWriter());
    }

    private sealed class TempDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "taf-output-tests-" + Guid.NewGuid().ToString("N"));

        public TempDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public string WriteFile(string name, string content)
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content, new UTF8Encoding(false));
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
