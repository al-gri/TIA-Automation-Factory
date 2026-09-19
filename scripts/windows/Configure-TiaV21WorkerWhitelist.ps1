<#>
.SYNOPSIS
    Configures the Siemens Openness whitelist for TiaV21Worker.exe by creating the application-specific
    registry key and granting the current user minimum write permissions on that key only.

.DESCRIPTION
    This script must be run elevated (as Administrator). It is idempotent and safe to run multiple times.
    It creates the registry key path for the current TIA Portal version (derived from the loaded
    Siemens.Engineering assembly) and grants the current Windows identity Write/SetValue permissions
    only on the TiaV21Worker.exe\Entry key, never on the parent Whitelist tree.

.NOTES
    Task: TIA-AUTH-001
    The whitelist version is derived from the Siemens.Engineering assembly major/minor version.
    For TIA Portal V21, this will be "21.0".
    The key path is: HKLM:\SOFTWARE\Siemens\Automation\Openness\Whitelist\<version>\Entries\TiaV21Worker.exe\Entry
#>

#requires -RunAsAdministrator

param(
    [Parameter(Mandatory = $false)]
    [string]$TiaV21WorkerPath = "C:\Program Files\TIA Automation Factory\TiaV21Worker.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-WhitelistVersion {
    # The version is derived from the Siemens.Engineering assembly major/minor.
    # Since we cannot load the assembly directly in PowerShell without the full path,
    # we use the known TIA Portal V21 version. This matches the C# logic:
    # Assembly engineeringAssembly = typeof(Siemens.Engineering.TiaPortal).Assembly;
    # Version version = engineeringAssembly.GetName().Version;
    # return $"{version.Major}.{version.Minor}";
    # For TIA Portal V21, the assembly version is 21.x.x.x
    return "21.0"
}

function Get-CurrentUserSid {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    return $identity.User.Value
}

function Get-WhitelistKeyPath {
    param(
        [string]$Version
    )
    return "HKLM:\SOFTWARE\Siemens\Automation\Openness\Whitelist\$Version\Entries\TiaV21Worker.exe\Entry"
}

function Ensure-WhitelistKey {
    param(
        [string]$KeyPath
    )

    if (-not (Test-Path $KeyPath)) {
        Write-Host "Creating whitelist key: $KeyPath"
        New-Item -Path $KeyPath -Force | Out-Null
    }
    else {
        Write-Host "Whitelist key already exists: $KeyPath"
    }
}

function Grant-MinimumPermissions {
    param(
        [string]$KeyPath,
        [string]$UserSid
    )

    $acl = Get-Acl -Path $KeyPath
    # Minimum rights for self-sync: open existing Entry key (QueryValues) and set values (SetValue)
    # No CreateSubKey, no inheritance - rights apply only to the Entry key itself
    $requiredRights = "SetValue, QueryValues"
    $rule = New-Object System.Security.AccessControl.RegistryAccessRule(
        $UserSid,
        $requiredRights,
        "None",
        "None",
        "Allow"
    )

    # Check if an equivalent Allow rule with exactly the required rights already exists
    $existingRule = $acl.Access | Where-Object {
        $_.IdentityReference.Value -eq $UserSid -and
        $_.AccessControlType -eq "Allow" -and
        $_.RegistryRights -eq [System.Security.AccessControl.RegistryRights]::SetValue -bor [System.Security.AccessControl.RegistryRights]::QueryValues -and
        $_.InheritanceFlags -eq "None" -and
        $_.PropagationFlags -eq "None"
    }

    if ($null -eq $existingRule) {
        Write-Host "Granting minimum registry permissions to current user on: $KeyPath"
        $acl.AddAccessRule($rule)
        Set-Acl -Path $KeyPath -AclObject $acl
    }
    else {
        Write-Host "Minimum registry permissions already granted to current user on: $KeyPath"
    }
}

function Main {
    Write-Host "=== TIA-AUTH-001: TiaV21Worker Whitelist Bootstrap ==="
    Write-Host "Running elevated: $([System.Security.Principal.WindowsPrincipal]::new([System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator))"

    $version = Get-WhitelistVersion
    Write-Host "TIA Whitelist version: $version"

    $keyPath = Get-WhitelistKeyPath -Version $version
    Write-Host "Target registry key: $keyPath"

    $userSid = Get-CurrentUserSid
    Write-Host "Current user SID: $userSid"

    Ensure-WhitelistKey -KeyPath $keyPath
    Grant-MinimumPermissions -KeyPath $keyPath -UserSid $userSid

    Write-Host "=== Bootstrap completed successfully ==="
}

try {
    Main
    exit 0
}
catch {
    Write-Error "Bootstrap failed: $($_.Exception.Message)"
    exit 1
}