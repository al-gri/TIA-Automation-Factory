<#>
.SYNOPSIS
    Configures the Siemens Openness AllowList for TiaV21Worker.exe by creating the application-specific
    registry key and granting the current user minimum write permissions on that key only.

.DESCRIPTION
    This script must be run elevated (as Administrator). It is idempotent and safe to run multiple times.
    It creates the version-independent registry key path for TIA Portal V21 AllowList and grants the current
    Windows identity Write/SetValue permissions only on the TiaV21Worker.exe\Entry key, never on the parent
    AllowList tree. Existing-rule detection normalizes IdentityReference to SecurityIdentifier for true
    idempotence across NTAccount/SID representation differences.

.NOTES
    Task: TIA-AUTH-V21-ALLOWLIST-001
    For TIA Portal V21, the key path is: HKLM:\SOFTWARE\Siemens\Automation\Openness\AllowList\TiaV21Worker.exe\Entry
#>

#requires -RunAsAdministrator

param(
    [Parameter(Mandatory = $false)]
    [string]$TiaV21WorkerPath = "C:\Program Files\TIA Automation Factory\TiaV21Worker.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-AllowListKeyPath {
    return "HKLM:\SOFTWARE\Siemens\Automation\Openness\AllowList\TiaV21Worker.exe\Entry"
}

function Get-CurrentUserSid {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    return [System.Security.Principal.SecurityIdentifier]::new($identity.User.Value)
}

function Ensure-AllowListKey {
    param(
        [string]$KeyPath
    )

    if (-not (Test-Path $KeyPath)) {
        Write-Host "Creating AllowList key: $KeyPath"
        New-Item -Path $KeyPath -Force | Out-Null
    }
    else {
        Write-Host "AllowList key already exists: $KeyPath"
    }
}

function Grant-MinimumPermissions {
    param(
        [string]$KeyPath,
        [System.Security.Principal.SecurityIdentifier]$UserSid
    )

    $acl = Get-Acl -Path $KeyPath
    # Minimum rights for self-sync: open existing Entry key (QueryValues) and set values (SetValue)
    # No CreateSubKey, no inheritance - rights apply only to the Entry key itself
    $requiredRights = [System.Security.AccessControl.RegistryRights]::SetValue -bor [System.Security.AccessControl.RegistryRights]::QueryValues
    $rule = New-Object System.Security.AccessControl.RegistryAccessRule(
        $UserSid,
        $requiredRights,
        "None",
        "None",
        "Allow"
    )

    # Check if an equivalent Allow rule with exactly the required rights already exists.
    # Normalize each IdentityReference to SecurityIdentifier for comparison so that
    # an NTAccount representation of the same principal does not cause duplicate rules.
    $existingRule = $acl.Access | Where-Object {
        $_.AccessControlType -eq "Allow" -and
        $_.RegistryRights -eq $requiredRights -and
        $_.InheritanceFlags -eq "None" -and
        $_.PropagationFlags -eq "None" -and
        (TryNormalizeSid($_.IdentityReference) -eq $UserSid.Value)
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

function TryNormalizeSid {
    param(
        [System.Security.Principal.IdentityReference]$IdentityRef
    )

    try {
        $sid = $IdentityRef.Translate([System.Security.Principal.SecurityIdentifier])
        return $sid.Value
    }
    catch {
        # Non-translatable identity (e.g., well-known group, deleted account) - return original value
        # to avoid false-positive matches, but do not broaden rights.
        return $IdentityRef.Value
    }
}

function Main {
    Write-Host "=== TIA-AUTH-V21-ALLOWLIST-001: TiaV21Worker AllowList Bootstrap ==="
    Write-Host "Running elevated: $([System.Security.Principal.WindowsPrincipal]::new([System.Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator))"

    $keyPath = Get-AllowListKeyPath
    Write-Host "Target registry key: $keyPath"

    $userSid = Get-CurrentUserSid
    Write-Host "Current user SID: $userSid"

    Ensure-AllowListKey -KeyPath $keyPath
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