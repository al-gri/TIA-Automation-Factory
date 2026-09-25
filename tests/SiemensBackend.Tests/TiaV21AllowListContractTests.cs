using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace TiaAutomationFactory.SiemensBackend.Tests;

public sealed class TiaV21AllowListContractTests
{
    private const string ExpectedEntryKeyPath =
        @"SOFTWARE\Siemens\Automation\Openness\AllowList\TiaV21Worker.exe\Entry";

    private const string ExpectedBasePath =
        @"SOFTWARE\Siemens\Automation\Openness\AllowList";

    // Legacy layout was Openness\Whitelist\<version>\Entries\TiaV21Worker.exe\Entry.
    // These markers deliberately target the legacy registry-path segments, never the bare
    // word "Whitelist", which is a legitimate part of production type/method names
    // (WhitelistManager, SynchronizeWhitelist, WhitelistSyncResult).
    private const string LegacyBasePathMarker = @"Openness\Whitelist";
    private const string LegacyWhitelistSegment = "Whitelist\\";
    private const string LegacyEntriesSegment = "\\Entries\\";

    [Fact]
    public void WhitelistManager_TargetsVersionIndependentV21AllowListEntry()
    {
        string source = ReadRepoFile("src/TiaV21Worker/WhitelistManager.cs");

        Assert.Contains(ExpectedEntryKeyPath, source);
        Assert.Contains(ExpectedBasePath, source);
        Assert.DoesNotContain(LegacyBasePathMarker, source);
        Assert.DoesNotContain(LegacyWhitelistSegment, source);
        Assert.DoesNotContain(LegacyEntriesSegment, source);
        Assert.DoesNotContain("GetWhitelistVersion", source);
        Assert.DoesNotContain("WhitelistBasePath", source);
    }

    [Fact]
    public void WhitelistManager_WritesExactAllowListValueContract()
    {
        string source = ReadRepoFile("src/TiaV21Worker/WhitelistManager.cs");

        Assert.Contains("entryKey.SetValue(\"Path\", executablePath, RegistryValueKind.String);", source);
        Assert.Contains("entryKey.SetValue(\"DateModified\", dateModified, RegistryValueKind.String);", source);
        Assert.Contains("entryKey.SetValue(\"FileHash\", fileHash, RegistryValueKind.String);", source);
        Assert.Contains("File.GetLastWriteTimeUtc(filePath)", source);
        Assert.Contains("sha256.ComputeHash(stream)", source);
        Assert.Contains("Convert.ToBase64String(hash)", source);
    }

    [Fact]
    public void WhitelistManager_DateModifiedFormat_MatchesRequiredUtcMillisecondFormat()
    {
        string source = ReadRepoFile("src/TiaV21Worker/WhitelistManager.cs");

        Match match = Regex.Match(
            source,
            @"ToString\(""(?<format>[^""]+)"",\s*CultureInfo\.InvariantCulture\)");

        Assert.True(match.Success, "WhitelistManager must format DateModified with CultureInfo.InvariantCulture.");

        string format = match.Groups["format"].Value;
        var sample = new DateTime(2026, 9, 21, 14, 30, 45, 123, DateTimeKind.Utc);

        Assert.Equal("2026/09/21 14:30:45.123", sample.ToString(format, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void WhitelistManager_OpensRegistry64LocalMachineWithMinimumRights()
    {
        string source = ReadRepoFile("src/TiaV21Worker/WhitelistManager.cs");

        Assert.Contains("RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)", source);
        Assert.Contains("RegistryRights.SetValue | RegistryRights.QueryValues", source);
    }

    [Fact]
    public void WhitelistManager_FailsClosedBeforeOpennessWhenEntryIsUnavailable()
    {
        string source = ReadRepoFile("src/TiaV21Worker/WhitelistManager.cs");

        Assert.Contains("if (entryKey == null)", source);
        Assert.Contains("catch (UnauthorizedAccessException)", source);

        int failClosedPaths = Regex.Matches(source, "CreateBootstrapRequired\\(\\)").Count;
        Assert.True(
            failClosedPaths >= 3,
            $"Expected fail-closed bootstrap-required paths for null/unauthorized/unexpected failures, found {failClosedPaths}.");
    }

    [Fact]
    public void BootstrapScript_TargetsExactV21AllowListEntryAndDropsLegacyLayout()
    {
        string script = ReadRepoFile("scripts/windows/Configure-TiaV21WorkerWhitelist.ps1");

        Assert.Contains(@"HKLM:\SOFTWARE\Siemens\Automation\Openness\AllowList\TiaV21Worker.exe\Entry", script);
        Assert.DoesNotContain(LegacyBasePathMarker, script);
        Assert.DoesNotContain(LegacyWhitelistSegment, script);
        Assert.DoesNotContain(LegacyEntriesSegment, script);
        Assert.DoesNotContain("Get-WhitelistVersion", script);
    }

    [Fact]
    public void BootstrapScript_RequiresElevationAndGrantsOnlyNarrowExactRule()
    {
        string script = ReadRepoFile("scripts/windows/Configure-TiaV21WorkerWhitelist.ps1");

        Assert.Contains("#requires -RunAsAdministrator", script);
        Assert.Contains(
            "[System.Security.AccessControl.RegistryRights]::SetValue -bor [System.Security.AccessControl.RegistryRights]::QueryValues",
            script);
        Assert.Contains("$_.AccessControlType -eq \"Allow\"", script);
        Assert.Contains("$_.RegistryRights -eq $requiredRights", script);
        Assert.Contains("$_.InheritanceFlags -eq \"None\"", script);
        Assert.Contains("$_.PropagationFlags -eq \"None\"", script);
        Assert.Contains("$acl.AddAccessRule($rule)", script);
        Assert.Contains("Set-Acl -Path $KeyPath -AclObject $acl", script);
    }

    [Fact]
    public void BootstrapScript_NormalizesExistingRuleIdentityToSidBeforeComparison()
    {
        string script = ReadRepoFile("scripts/windows/Configure-TiaV21WorkerWhitelist.ps1");

        Assert.Contains("function TryNormalizeSid", script);
        Assert.Contains("$IdentityRef.Translate([System.Security.Principal.SecurityIdentifier])", script);
        Assert.Contains("return $sid.Value", script);
        Assert.Contains("return $IdentityRef.Value", script);
        Assert.Contains("(TryNormalizeSid($_.IdentityReference) -eq $UserSid.Value)", script);
    }

    private static string ReadRepoFile(string relativePath, [CallerFilePath] string testFilePath = "")
    {
        // testFilePath is bound at compile time to tests/SiemensBackend.Tests/TiaV21AllowListContractTests.cs.
        // Walking up three directories yields the repository root on both Linux and Windows.
        string repoRoot = Path.GetFullPath(Path.Combine(testFilePath, "..", "..", ".."));
        string fullPath = Path.Combine(repoRoot, relativePath);

        Assert.True(File.Exists(fullPath), $"Expected repository file '{relativePath}' at '{fullPath}'.");

        return File.ReadAllText(fullPath);
    }
}
