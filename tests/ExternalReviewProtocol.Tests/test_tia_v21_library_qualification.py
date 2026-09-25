import json
import os
import shutil
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


def run_pwsh(script_text: str, *, environment: dict[str, str] | None = None) -> subprocess.CompletedProcess[str]:
    with tempfile.TemporaryDirectory() as directory:
        script_path = Path(directory) / "test.ps1"
        script_path.write_text(script_text, encoding="utf-8")
        env = os.environ.copy()
        if environment:
            env.update(environment)
        return subprocess.run(
            ["pwsh", "-NoProfile", "-NonInteractive", "-File", str(script_path)],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
            env=env,
        )


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
    def test_timeout_termination_waits_until_process_exit(self) -> None:
        script = harness_functions() + r'''
$pwsh = (Get-Command pwsh).Source
$child = Start-Process `
  -FilePath $pwsh `
  -ArgumentList '-NoProfile -NonInteractive -Command "Start-Sleep -Seconds 60"' `
  -PassThru
try {
  Stop-QualificationProcessTree `
    -Process $child `
    -ProcessTreeIds @($child.Id) `
    -WaitMilliseconds 10000 `
    -KillTreeAction {
      param($Target)
      Stop-Process -Id $Target.Id -Force -ErrorAction Stop
    }

  [pscustomobject]@{ hasExited = [bool]$child.HasExited } |
    ConvertTo-Json -Compress
}
finally {
  if (-not $child.HasExited) {
    Stop-Process -Id $child.Id -Force -ErrorAction SilentlyContinue
  }
}
'''
        completed = run_pwsh(script)
        self.assertEqual(0, completed.returncode, completed.stdout)
        result = json.loads(completed.stdout.strip())
        self.assertTrue(result["hasExited"], result)

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

    def test_timeout_source_uses_verified_process_termination(self) -> None:
        invocation = between(
            "          function Invoke-Qualification",
            "          function Stop-Evidence",
        )
        stop_function = between(
            "          function Stop-QualificationProcessTree",
            "          function Remove-RawEvidence",
        )
        self.assertIn("Stop-QualificationProcessTree -Process $process", invocation)
        self.assertIn("WaitForExit($WaitMilliseconds)", stop_function)
        self.assertIn("$Process.HasExited", stop_function)
        self.assertIn("taskkill.exe", stop_function)
        self.assertIn("Get-QualificationProcessTreeIds", stop_function)
        self.assertIn("Get-Process -Id $_", stop_function)
        self.assertIn("$remaining.Count -ne 0", stop_function)

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
