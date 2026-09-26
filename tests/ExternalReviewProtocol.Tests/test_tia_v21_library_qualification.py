import json
import os
import shutil
import signal
import subprocess
import tempfile
import textwrap
import unittest
from copy import deepcopy
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORKFLOW_PATH = ROOT / ".github" / "workflows" / "tia-v21-library-qualification.yml"
WORKFLOW = WORKFLOW_PATH.read_text(encoding="utf-8")
EXPECTED_SOURCE_SHA256 = "a" * 64
EXPECTED_TIA_BUILD = "Siemens.Engineering v21.0.0.0 (file: 2100.0.121.1)"


def between(start: str, end: str) -> str:
    start_index = WORKFLOW.index(start)
    return WORKFLOW[start_index : WORKFLOW.index(end, start_index)]


def qualification_script() -> str:
    block = between(
        "      - name: Run qualification twice and create sanitized evidence",
        "      - name: Show sanitized evidence",
    )
    return textwrap.dedent(block.split("        run: |\n", 1)[1])


def harness_functions() -> str:
    script = qualification_script()
    start_marker = "# OLQ-HARNESS-FUNCTIONS-BEGIN"
    end_marker = "# OLQ-HARNESS-FUNCTIONS-END"
    start = script.index(start_marker) + len(start_marker)
    end = script.index(end_marker, start)
    return script[start:end].strip() + "\n"


def invocation_functions() -> str:
    script = qualification_script()
    start = script.index("function Invoke-Qualification")
    end = script.index("function Stop-Evidence", start)
    return harness_functions() + script[start:end].strip() + "\n"


def manifest(*, is_reuse: bool, native_reopen: bool) -> dict[str, object]:
    return {
        "success": True,
        "isReuse": is_reuse,
        "nativeReopenSuccess": native_reopen,
        "sourceArchiveSha256": EXPECTED_SOURCE_SHA256,
        "tiaBuildIdentity": EXPECTED_TIA_BUILD,
        "qualificationIdentity": "olq-v21-txn-002-v1",
        "qualifiedArchiveName": "OpenLibrary-qualified.zal21",
        "qualifiedArchiveSha256": "b" * 64,
        "originalProvenanceRunId": "trusted-run-123",
        "originalCompletedAtUtc": "2026-09-25T17:00:00.1234567Z",
    }


def run_pwsh(
    script_text: str,
    *,
    environment: dict[str, str] | None = None,
    timeout_seconds: float | None = None,
) -> subprocess.CompletedProcess[str]:
    with tempfile.TemporaryDirectory() as directory:
        script_path = Path(directory) / "test.ps1"
        script_path.write_text(script_text, encoding="utf-8")
        env = os.environ.copy()
        if environment:
            env.update(environment)

        args = ["pwsh", "-NoProfile", "-NonInteractive", "-File", str(script_path)]
        if timeout_seconds is None:
            return subprocess.run(
                args,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                env=env,
            )
        if timeout_seconds <= 0:
            raise ValueError("timeout_seconds must be positive")

        process = subprocess.Popen(
            args,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            env=env,
            start_new_session=True,
        )
        try:
            stdout, _ = process.communicate(timeout=timeout_seconds)
        except subprocess.TimeoutExpired as timeout_error:
            try:
                if os.name == "posix":
                    os.killpg(process.pid, signal.SIGKILL)
                else:
                    process.kill()
            except ProcessLookupError:
                pass

            try:
                teardown_stdout, _ = process.communicate(timeout=5)
            except subprocess.TimeoutExpired as teardown_error:
                if process.stdout is not None:
                    process.stdout.close()
                try:
                    process.wait(timeout=1)
                except subprocess.TimeoutExpired:
                    pass
                raise AssertionError(
                    "Timed-out PowerShell process group could not be reaped "
                    "within the bounded teardown window."
                ) from teardown_error

            raise AssertionError(
                f"PowerShell process group exceeded {timeout_seconds} seconds; "
                f"captured output={teardown_stdout!r}"
            ) from timeout_error

        return subprocess.CompletedProcess(args, process.returncode, stdout, None)


def run_production_pair_cases(cases: list[dict[str, object]]) -> dict[str, dict[str, object]]:
    with tempfile.TemporaryDirectory() as directory:
        cases_path = Path(directory) / "cases.json"
        cases_path.write_text(json.dumps(cases), encoding="utf-8")
        script = harness_functions() + r'''
$caseJson = Get-Content -LiteralPath $env:OLQ_CASES_PATH -Raw
$convertFromJson = Get-Command ConvertFrom-Json
if ($convertFromJson.Parameters.ContainsKey('DateKind')) {
  $cases = $caseJson | ConvertFrom-Json -DateKind String
}
else {
  $cases = $caseJson | ConvertFrom-Json
}
$results = @()
foreach ($case in @($cases)) {
  $result = Test-QualificationPair `
    -Run1 $case.run1 `
    -Run2 $case.run2 `
    -Run1ExitCode ([int]$case.run1ExitCode) `
    -Run2ExitCode ([int]$case.run2ExitCode) `
    -ExpectedSourceArchiveSha256 ([string]$case.expectedSourceArchiveSha256)

  $results += [pscustomobject]@{
    name = [string]$case.name
    isValid = [bool]$result.IsValid
    failureCode = $result.FailureCode
  }
}
$results | ConvertTo-Json -Depth 8 -Compress
'''
        completed = run_pwsh(
            script,
            environment={"OLQ_CASES_PATH": str(cases_path)},
        )
        if completed.returncode != 0:
            raise AssertionError(completed.stdout)
        parsed = json.loads(completed.stdout.strip())
        if isinstance(parsed, dict):
            parsed = [parsed]
        return {str(item["name"]): item for item in parsed}


class TiaV21LibraryQualificationWorkflowTests(unittest.TestCase):
    def case(
        self,
        name: str,
        run1: dict[str, object],
        run2: dict[str, object],
        *,
        run1_exit: int = 0,
        run2_exit: int = 0,
        expected_source: str = EXPECTED_SOURCE_SHA256,
    ) -> dict[str, object]:
        return {
            "name": name,
            "run1": run1,
            "run2": run2,
            "run1ExitCode": run1_exit,
            "run2ExitCode": run2_exit,
            "expectedSourceArchiveSha256": expected_source,
        }

    def test_trusted_main_only_boundary_is_preserved(self) -> None:
        self.assertIn("if: github.ref == 'refs/heads/main'", WORKFLOW)
        self.assertIn("ref: main", WORKFLOW)
        self.assertIn("persist-credentials: false", WORKFLOW)
        self.assertIn("Trusted checkout is not current origin/main.", WORKFLOW)
        self.assertNotIn("pull_request_target", WORKFLOW)

    @unittest.skipUnless(shutil.which("pwsh"), "pwsh is unavailable")
    def test_production_pair_gate_accepts_fresh_then_reuse_and_repeated_reuse(self) -> None:
        fresh = manifest(is_reuse=False, native_reopen=True)
        reuse = manifest(is_reuse=True, native_reopen=False)
        cases = [
            self.case("fresh-reuse", fresh, reuse),
            self.case("reuse-reuse", reuse, deepcopy(reuse)),
        ]
        results = run_production_pair_cases(cases)
        self.assertTrue(results["fresh-reuse"]["isValid"], results)
        self.assertTrue(results["reuse-reuse"]["isValid"], results)

    @unittest.skipUnless(shutil.which("pwsh"), "pwsh is unavailable")
    def test_production_pair_gate_rejects_old_mode_exit_and_success_claims(self) -> None:
        fresh = manifest(is_reuse=False, native_reopen=True)
        reuse = manifest(is_reuse=True, native_reopen=False)

        old_gate = deepcopy(reuse)
        old_gate["nativeReopenSuccess"] = True

        run1_failed = deepcopy(fresh)
        run1_failed["success"] = False

        run2_failed = deepcopy(reuse)
        run2_failed["success"] = False

        cases = [
            self.case("old-run2-fresh-reopen", fresh, old_gate),
            self.case("run1-exit", fresh, reuse, run1_exit=7),
            self.case("run2-exit", fresh, reuse, run2_exit=8),
            self.case("run1-success", run1_failed, reuse),
            self.case("run2-success", fresh, run2_failed),
        ]
        results = run_production_pair_cases(cases)
        for name in (case["name"] for case in cases):
            self.assertFalse(results[str(name)]["isValid"], (name, results))

    @unittest.skipUnless(shutil.which("pwsh"), "pwsh is unavailable")
    def test_production_pair_gate_rejects_missing_malformed_or_changed_provenance(self) -> None:
        fresh = manifest(is_reuse=False, native_reopen=True)
        reuse = manifest(is_reuse=True, native_reopen=False)
        mutations = [
            ("missing-run-id", "originalProvenanceRunId", None),
            ("path-run-id", "originalProvenanceRunId", r"DOMAIN\user"),
            ("changed-run-id", "originalProvenanceRunId", "trusted-run-456"),
            ("offset-time", "originalCompletedAtUtc", "2026-09-25T19:00:00+02:00"),
            ("malformed-time", "originalCompletedAtUtc", "not-a-time"),
            ("changed-time", "originalCompletedAtUtc", "2026-09-25T17:00:01Z"),
            ("typed-is-reuse", "isReuse", "true"),
            ("missing-reopen", "nativeReopenSuccess", None),
        ]
        cases = []
        for name, field, value in mutations:
            bad = deepcopy(reuse)
            bad[field] = value
            cases.append(self.case(name, fresh, bad))

        results = run_production_pair_cases(cases)
        for name, _, _ in mutations:
            self.assertFalse(results[name]["isValid"], (name, results))

    @unittest.skipUnless(shutil.which("pwsh"), "pwsh is unavailable")
    def test_production_pair_gate_rejects_paths_users_exception_text_and_bad_identities(self) -> None:
        fresh = manifest(is_reuse=False, native_reopen=True)
        reuse = manifest(is_reuse=True, native_reopen=False)
        bad_builds = [
            r"C:\Users\alice\Siemens.Engineering v21.0.0.0",
            r"\\server\share\Siemens.Engineering v21.0.0.0",
            r"DOMAIN\alice Siemens.Engineering v21.0.0.0",
            "EngineeringTargetInvocationException hresult:0x80131500",
            EXPECTED_TIA_BUILD + "\t",
        ]
        cases = []
        for index, value in enumerate(bad_builds):
            bad = deepcopy(reuse)
            bad["tiaBuildIdentity"] = value
            cases.append(self.case(f"build-{index}", fresh, bad))

        bad_hash = deepcopy(reuse)
        bad_hash["qualifiedArchiveSha256"] = "not-a-hash"
        cases.append(self.case("archive-hash", fresh, bad_hash))

        bad_name = deepcopy(reuse)
        bad_name["qualifiedArchiveName"] = r"C:\private\archive.zal21"
        cases.append(self.case("archive-path", fresh, bad_name))

        changed_identity = deepcopy(reuse)
        changed_identity["qualificationIdentity"] = "olq-v21-txn-002-v2"
        cases.append(self.case("identity-mismatch", fresh, changed_identity))

        wrong_source = deepcopy(reuse)
        wrong_source["sourceArchiveSha256"] = "c" * 64
        cases.append(self.case("source-mismatch", fresh, wrong_source))

        results = run_production_pair_cases(cases)
        for case in cases:
            name = str(case["name"])
            self.assertFalse(results[name]["isValid"], (name, results))

    @unittest.skipUnless(shutil.which("pwsh"), "pwsh is unavailable")
    def test_cleanup_function_deletes_and_fails_closed_when_deletion_is_suppressed(self) -> None:
        script = harness_functions() + r'''
$deletedRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("olq-clean-" + [guid]::NewGuid().ToString("N"))
$failedRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("olq-fail-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $deletedRoot, $failedRoot | Out-Null
Set-Content -LiteralPath (Join-Path $deletedRoot "raw.txt") -Value "raw"
Set-Content -LiteralPath (Join-Path $failedRoot "raw.txt") -Value "raw"

Remove-RawEvidence -Path $deletedRoot
$normalDeleted = -not (Test-Path -LiteralPath $deletedRoot)
$failureCaught = $false
try {
  Remove-RawEvidence -Path $failedRoot -RemoveAction { param($Target) }
}
catch {
  $failureCaught = $true
}
$failedRootStillExists = Test-Path -LiteralPath $failedRoot
Remove-Item -LiteralPath $failedRoot -Recurse -Force -ErrorAction Stop

[pscustomobject]@{
  normalDeleted = $normalDeleted
  failureCaught = $failureCaught
  failedRootStillExists = $failedRootStillExists
} | ConvertTo-Json -Compress
'''
        completed = run_pwsh(script)
        self.assertEqual(0, completed.returncode, completed.stdout)
        result = json.loads(completed.stdout.strip())
        self.assertTrue(result["normalDeleted"], result)
        self.assertTrue(result["failureCaught"], result)
        self.assertTrue(result["failedRootStillExists"], result)

    @unittest.skipUnless(shutil.which("pwsh"), "pwsh is unavailable")
    def test_job_object_helper_csharp_compiles_when_pwsh_is_available(self) -> None:
        script = harness_functions() + r'''
Initialize-QualificationJobObjectTypes
$type = 'TiaAutomationFactory.Qualification.ContainedProcess' -as [type]
if ($null -eq $type) {
  throw 'ContainedProcess type was not created.'
}
[pscustomobject]@{ typeName = $type.FullName } | ConvertTo-Json -Compress
'''
        completed = run_pwsh(script)
        self.assertEqual(0, completed.returncode, completed.stdout)
        result = json.loads(completed.stdout.strip())
        self.assertEqual(
            "TiaAutomationFactory.Qualification.ContainedProcess",
            result["typeName"],
        )

    @unittest.skipUnless(shutil.which("pwsh"), "pwsh is unavailable")
    def test_timeout_race_waits_for_surviving_child_absence_before_return(self) -> None:
        script = invocation_functions() + r'''
$rawRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("olq-timeout-race-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $rawRoot | Out-Null
$parentScript = Join-Path $rawRoot 'parent.ps1'
$childScript = Join-Path $rawRoot 'child.ps1'
$childPidPath = Join-Path $rawRoot 'child.pid'
$exitGatePath = Join-Path $rawRoot 'exit.gate'
$parentStdoutPath = Join-Path $rawRoot 'parent.stdout.txt'
$parentStderrPath = Join-Path $rawRoot 'parent.stderr.txt'
$childStdoutPath = Join-Path $rawRoot 'child.stdout.txt'
$childStderrPath = Join-Path $rawRoot 'child.stderr.txt'
$resultPath = $env:OLQ_RACE_RESULT_PATH
if ([string]::IsNullOrWhiteSpace($resultPath)) {
  throw 'Race regression result path is not configured.'
}

$childSource = @"
`$childPidPath = `$env:OLQ_RACE_CHILD_PID_PATH
if ([string]::IsNullOrWhiteSpace(`$childPidPath)) {
  throw 'Synthetic race child PID path environment variable is not configured.'
}
`$childPidTempPath = `$childPidPath + '.tmp'
[System.IO.File]::WriteAllText(
  `$childPidTempPath,
  [string]`$PID,
  [System.Text.UTF8Encoding]::new(`$false))
[System.IO.File]::Move(`$childPidTempPath, `$childPidPath)
Start-Sleep -Seconds 60
"@
$childSource | Set-Content -LiteralPath $childScript -Encoding UTF8

$parentSource = @"
param(
  [string]`$ChildScriptPath,
  [string]`$ExitGatePath,
  [string]`$ChildStdoutPath,
  [string]`$ChildStderrPath
)
`$pwsh = `$env:OLQ_RACE_PWSH_PATH
if ([string]::IsNullOrWhiteSpace(`$pwsh) -or
    -not [System.IO.Path]::IsPathRooted(`$pwsh) -or
    -not (Test-Path -LiteralPath `$pwsh -PathType Leaf)) {
  throw 'Synthetic race pwsh path environment variable is invalid.'
}
foreach (`$requiredPath in @(`$ChildScriptPath, `$ChildStdoutPath, `$ChildStderrPath)) {
  if ([string]::IsNullOrWhiteSpace([string]`$requiredPath)) {
    throw 'Synthetic race nested launch path is empty.'
  }
}
if (-not (Test-Path -LiteralPath `$ChildScriptPath -PathType Leaf)) {
  throw 'Synthetic race child script path is invalid.'
}
`$childStartParameters = @{
  FilePath = [string]`$pwsh
  ArgumentList = @(
    '-NoProfile',
    '-NonInteractive',
    '-File',
    [string]`$ChildScriptPath
  )
  RedirectStandardOutput = [string]`$ChildStdoutPath
  RedirectStandardError = [string]`$ChildStderrPath
  PassThru = `$true
}
`$child = Start-Process @childStartParameters
while (-not (Test-Path -LiteralPath `$ExitGatePath)) {
  Start-Sleep -Milliseconds 20
}
exit 0
"@
$parentSource | Set-Content -LiteralPath $parentScript -Encoding UTF8

$env:OLQ_RACE_CHILD_PID_PATH = $childPidPath
if ([string]::IsNullOrWhiteSpace($env:OLQ_RACE_CHILD_PID_PATH)) {
  throw 'Synthetic race child PID path environment variable is not configured.'
}

$pwsh = [string][System.Diagnostics.Process]::GetCurrentProcess().MainModule.FileName
if ([string]::IsNullOrWhiteSpace($pwsh) -or
    -not [System.IO.Path]::IsPathRooted($pwsh) -or
    -not (Test-Path -LiteralPath $pwsh -PathType Leaf)) {
  throw 'Outer race regression could not resolve its current pwsh executable path.'
}
$env:OLQ_RACE_PWSH_PATH = $pwsh

$parent = Start-Process `
  -FilePath $pwsh `
  -ArgumentList @(
    '-NoProfile',
    '-NonInteractive',
    '-File',
    $parentScript,
    '-ChildScriptPath',
    $childScript,
    '-ExitGatePath',
    $exitGatePath,
    '-ChildStdoutPath',
    $childStdoutPath,
    '-ChildStderrPath',
    $childStderrPath
  ) `
  -RedirectStandardOutput $parentStdoutPath `
  -RedirectStandardError $parentStderrPath `
  -PassThru
$parentId = [int]$parent.Id
if ($parentId -le 0 -or $parentId -eq [int]$PID) {
  throw 'Race regression parent PID is invalid or matches the outer PowerShell PID.'
}

function Send-TestSinglePidKill {
  param(
    [Parameter(Mandatory = $true)]
    [System.Diagnostics.Process]$Process,
    [Parameter(Mandatory = $true)]
    [int]$TargetId,
    [Parameter(Mandatory = $true)]
    [string]$Name
  )

  if ($TargetId -le 0) {
    throw ("{0} has an invalid cached PID." -f $Name)
  }
  if ($TargetId -eq [int]$PID) {
    throw ("{0} cached PID unexpectedly matches the outer PowerShell PID." -f $Name)
  }
  if ($Process.HasExited) {
    return
  }
  if (-not (Test-Path -LiteralPath '/bin/kill' -PathType Leaf)) {
    throw 'Required single-PID Linux kill utility is unavailable.'
  }

  & /bin/kill -KILL -- $TargetId 1>$null 2>$null
  $signalExitCode = [int]$LASTEXITCODE
  if ($signalExitCode -ne 0 -and -not $Process.HasExited) {
    throw ("Single-PID kill failed for {0} while the exact process object remained alive." -f $Name)
  }
}

$fake = [pscustomobject]@{
  Process = $parent
  ParentId = $parentId
  ChildProcess = $null
  ChildId = 0
  ChildPidPath = $childPidPath
  ExitGatePath = $exitGatePath
  ChildSurvivedParentExit = $false
  VerifiedEmpty = $false
  Disposed = $false
}
$fake | Add-Member -MemberType ScriptMethod -Name TerminateAndWait -Value {
  param([int]$WaitMilliseconds)
  if ($WaitMilliseconds -le 0) {
    throw 'Test containment wait must be positive.'
  }

  $trackedProcesses = @(
    [pscustomobject]@{ Process = $this.Process; TargetId = [int]$this.ParentId; Name = 'Parent' }
    if ($null -ne $this.ChildProcess) {
      [pscustomobject]@{ Process = $this.ChildProcess; TargetId = [int]$this.ChildId; Name = 'Long-lived child' }
    }
  )
  $deadline = [DateTime]::UtcNow.AddMilliseconds($WaitMilliseconds)

  foreach ($tracked in $trackedProcesses) {
    if ($tracked.TargetId -le 0 -or $tracked.TargetId -eq [int]$PID) {
      throw ("{0} has an invalid cached PID." -f $tracked.Name)
    }
    if (-not $tracked.Process.HasExited) {
      Send-TestSinglePidKill -Process $tracked.Process -TargetId $tracked.TargetId -Name $tracked.Name
    }
  }

  foreach ($tracked in $trackedProcesses) {
    $remainingMilliseconds = [int]($deadline - [DateTime]::UtcNow).TotalMilliseconds
    if ($remainingMilliseconds -le 0 -or
        -not $tracked.Process.WaitForExit($remainingMilliseconds)) {
      throw 'Test containment did not become empty before its deadline.'
    }
    if (-not $tracked.Process.HasExited) {
      throw 'Tracked process remained alive after bounded wait.'
    }
  }

  $this.VerifiedEmpty = $true
}
$fake | Add-Member -MemberType ScriptMethod -Name Dispose -Value {
  $this.Disposed = $true
}

$startAction = {
  param($HostPath, $ScriptPath, $InvocationPath, $WorkingDirectory)
  return $script:fake
}
$waitAction = {
  param($ContainedProcess, $WaitMilliseconds)
  $deadline = [DateTime]::UtcNow.AddSeconds(10)
  while (-not (Test-Path -LiteralPath $ContainedProcess.ChildPidPath)) {
    if ([DateTime]::UtcNow -ge $deadline) {
      $diagnostics = @()
      foreach ($diagnosticPath in @($parentStderrPath, $childStderrPath)) {
        if (Test-Path -LiteralPath $diagnosticPath) {
          $excerpt = Get-Content -LiteralPath $diagnosticPath -Raw -ErrorAction SilentlyContinue
          if ($null -ne $excerpt) {
            $excerpt = [System.Text.RegularExpressions.Regex]::Replace(
              [string]$excerpt,
              '[\r\n]+',
              ' ')
            if ($excerpt.Length -gt 512) { $excerpt = $excerpt.Substring(0, 512) }
            if (-not [string]::IsNullOrWhiteSpace($excerpt)) {
              $diagnostics += $excerpt
            }
          }
        }
      }
      $suffix = if ($diagnostics.Count -gt 0) {
        ' diagnostics=' + ($diagnostics -join ' | ')
      }
      else {
        ''
      }
      throw ('Child did not publish the long-lived child PID.' + $suffix)
    }
    Start-Sleep -Milliseconds 20
  }

  $childId = [int](
    Get-Content -LiteralPath $ContainedProcess.ChildPidPath -Raw)
  if ($childId -le 0 -or $childId -eq [int]$PID) {
    throw 'Race regression child PID is invalid or matches the outer PowerShell PID.'
  }
  if ([int]$ContainedProcess.ParentId -eq $childId) {
    throw 'Race regression parent and child process identities collided.'
  }
  $ContainedProcess.ChildProcess = [System.Diagnostics.Process]::GetProcessById($childId)
  if ($ContainedProcess.ChildProcess.HasExited) {
    throw 'Long-lived child was not alive at the timeout boundary.'
  }
  $ContainedProcess.ChildId = $childId

  Set-Content -LiteralPath $ContainedProcess.ExitGatePath -Value 'exit'
  if (-not $ContainedProcess.Process.WaitForExit(10000)) {
    throw 'Parent did not exit at the timeout boundary.'
  }
  if ($ContainedProcess.ChildProcess.HasExited) {
    throw 'Long-lived child did not survive the parent exit race.'
  }
  $ContainedProcess.ChildSurvivedParentExit = $true

  return $false
}

function Stop-TestProcessBounded {
  param(
    [System.Diagnostics.Process]$Process,
    [int]$TargetId,
    [string]$Name,
    [int]$WaitMilliseconds = 5000
  )

  if ($null -eq $Process) { return }
  if ($TargetId -le 0 -or $TargetId -eq [int]$PID) {
    throw ("{0} has an invalid cached PID during cleanup." -f $Name)
  }
  if (-not $Process.HasExited) {
    Send-TestSinglePidKill -Process $Process -TargetId $TargetId -Name $Name
  }
  if (-not $Process.WaitForExit($WaitMilliseconds) -or -not $Process.HasExited) {
    throw ("{0} did not exit during bounded cleanup." -f $Name)
  }
}

try {
  $result = Invoke-Qualification `
    -Phase 'race' `
    -Executable 'unused-by-test' `
    -SourceArchive 'unused-by-test' `
    -OutputRoot 'unused-by-test' `
    -ManifestPath (Join-Path $rawRoot 'manifest.json') `
    -WorkPath (Join-Path $rawRoot 'work') `
    -TimeoutMinutes 1 `
    -StartContainedProcessAction $startAction `
    -WaitForExitAction $waitAction

  $childExitedAfterReturn = [bool]$fake.ChildProcess.HasExited
  $raceResult = [pscustomobject]@{
    timedOut = [bool]$result.TimedOut
    childSurvivedParentExit = [bool]$fake.ChildSurvivedParentExit
    verifiedEmpty = [bool]$fake.VerifiedEmpty
    childExitedAfterReturn = $childExitedAfterReturn
    parentExited = [bool]$fake.Process.HasExited
    disposed = [bool]$fake.Disposed
  }
  $raceJson = $raceResult | ConvertTo-Json -Compress
  [System.IO.File]::WriteAllText(
    $resultPath,
    $raceJson,
    [System.Text.UTF8Encoding]::new($false))
}
finally {
  try {
    Stop-TestProcessBounded -Process $fake.ChildProcess -TargetId ([int]$fake.ChildId) -Name 'Long-lived child'
  }
  finally {
    try {
      Stop-TestProcessBounded -Process $parent -TargetId ([int]$fake.ParentId) -Name 'Parent'
    }
    finally {
      if ($null -ne $fake.ChildProcess) {
        $fake.ChildProcess.Dispose()
      }
      $parent.Dispose()
      Remove-Item -LiteralPath $rawRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
  }
}
'''
        with tempfile.TemporaryDirectory() as directory:
            result_path = Path(directory) / "race-result.json"
            completed = run_pwsh(
                script,
                environment={"OLQ_RACE_RESULT_PATH": str(result_path)},
                timeout_seconds=30,
            )
            self.assertEqual(0, completed.returncode, completed.stdout)
            self.assertTrue(result_path.is_file(), completed.stdout)
            payload = result_path.read_text(encoding="utf-8").strip()
            self.assertTrue(payload, completed.stdout)
            try:
                result = json.loads(payload)
            except json.JSONDecodeError as error:
                self.fail(
                    f"Race regression result is not valid JSON: {error}; "
                    f"payload={payload!r}; pwsh={completed.stdout!r}"
                )

        self.assertIsInstance(result, dict, result)
        self.assertTrue(result["timedOut"], result)
        self.assertTrue(result["childSurvivedParentExit"], result)
        self.assertTrue(result["verifiedEmpty"], result)
        self.assertTrue(result["childExitedAfterReturn"], result)
        self.assertTrue(result["parentExited"], result)
        self.assertTrue(result["disposed"], result)

    def test_cleanup_is_independent_of_evidence_writes_and_cannot_leave_pass(self) -> None:
        script = qualification_script()
        cleanup_index = script.rindex("Remove-RawEvidence -Path $rawRoot")
        evidence_write_index = script.rindex("$evidence |")
        output_write_index = script.rindex('"status=$($evidence.status)"')

        self.assertLess(cleanup_index, evidence_write_index)
        self.assertLess(cleanup_index, output_write_index)
        cleanup_block = between(
            "          function Remove-RawEvidence",
            "          # OLQ-HARNESS-FUNCTIONS-END",
        )
        self.assertIn("-ErrorAction Stop", cleanup_block)
        self.assertNotIn("-ErrorAction SilentlyContinue", cleanup_block)
        self.assertIn("Raw qualification evidence cleanup could not be verified.", cleanup_block)
        self.assertIn("$evidence.status = 'FAIL'", script)
        self.assertIn("$evidence.deterministicSecondRun = $false", script)
        self.assertIn("if ($null -ne $cleanupError)", script)

    def test_timeout_source_uses_job_object_before_resume_and_verifies_empty_job(self) -> None:
        invocation = between(
            "          function Invoke-Qualification",
            "          function Stop-Evidence",
        )
        start_method = between(
            "        public static ContainedProcess Start(",
            "        public void TerminateAndWait(",
        )
        termination_method = between(
            "        public void TerminateAndWait(",
            "        public void Dispose()",
        )

        self.assertIn("JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE", WORKFLOW)
        self.assertNotIn("JOB_OBJECT_LIMIT_BREAKAWAY_OK", WORKFLOW)
        self.assertNotIn("JOB_OBJECT_LIMIT_SILENT_BREAKAWAY_OK", WORKFLOW)
        self.assertIn("NativeMethods.CREATE_SUSPENDED", start_method)
        self.assertIn("AssignProcessToJobObject(job, processInformation.hProcess)", start_method)
        self.assertIn("ResumeThread(processInformation.hThread)", start_method)
        self.assertLess(
            start_method.index("AssignProcessToJobObject(job, processInformation.hProcess)"),
            start_method.index("ResumeThread(processInformation.hThread)"),
        )
        self.assertIn("TerminateJobObject(this.jobHandle, 1)", termination_method)
        self.assertIn("QueryActiveProcessCount(this.jobHandle)", termination_method)
        self.assertIn("activeProcesses == 0 && rootExited", termination_method)
        self.assertIn("Start-QualificationContainedProcess", invocation)
        self.assertIn("Stop-QualificationContainedProcess", invocation)
        self.assertLess(
            invocation.index("$contained = & $StartContainedProcessAction"),
            invocation.index("$completedBeforeTimeout = & $WaitForExitAction"),
        )
        self.assertLess(
            invocation.index("Stop-QualificationContainedProcess"),
            invocation.index("TimedOut = $true"),
        )
        self.assertNotIn("taskkill.exe", WORKFLOW)
        self.assertNotIn("Get-QualificationProcessTreeIds", WORKFLOW)
        self.assertNotIn("if ($Process.HasExited) { return }", WORKFLOW)

    def test_public_evidence_uses_exact_tia_identity_and_never_copies_worker_failure(self) -> None:
        functions = harness_functions()
        self.assertIn(EXPECTED_TIA_BUILD, functions)
        self.assertIn("Test-ExactTiaBuildIdentity", functions)
        self.assertNotIn("[string]$run1.failure", WORKFLOW)
        self.assertNotIn("[string]$run2.failure", WORKFLOW)
        self.assertIn(
            "Qualification harness failed before valid public evidence could be completed.",
            WORKFLOW,
        )
        publication = between(
            "      - name: Publish sanitized result to qualification issue",
            "      - name: Enforce qualification result",
        )
        self.assertNotIn("stdout", publication.lower())
        self.assertNotIn("stderr", publication.lower())
        self.assertIn("String(value).replace(/[\\r\\n`]/g, ' ').slice(0, 512)", publication)

    @unittest.skipUnless(shutil.which("pwsh"), "pwsh is unavailable")
    def test_embedded_powershell_parses_when_pwsh_is_available(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            script = Path(directory) / "qualification.ps1"
            script.write_text(qualification_script(), encoding="utf-8")
            parser = (
                "$tokens=$null; $errors=$null; "
                "[System.Management.Automation.Language.Parser]::ParseFile("
                "$env:POWERSHELL_PARSE_TARGET,[ref]$tokens,[ref]$errors) | Out-Null; "
                "if ($errors.Count) { $errors | % { Write-Error $_.Message }; exit 1 }"
            )
            completed = subprocess.run(
                ["pwsh", "-NoProfile", "-NonInteractive", "-Command", parser],
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
                env={**os.environ, "POWERSHELL_PARSE_TARGET": str(script)},
            )
            self.assertEqual(0, completed.returncode, completed.stdout)


if __name__ == "__main__":
    unittest.main()
