import re
import shutil
import subprocess
import tempfile
import textwrap
import unittest
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = (ROOT / ".github" / "workflows" / "tia-v21-library-qualification.yml").read_text(encoding="utf-8")
IDENTITY_FIELDS = (
    "sourceArchiveSha256",
    "tiaBuildIdentity",
    "qualificationIdentity",
    "qualifiedArchiveName",
    "qualifiedArchiveSha256",
)


def between(start: str, end: str) -> str:
    start_index = WORKFLOW.index(start)
    return WORKFLOW[start_index : WORKFLOW.index(end, start_index)]


def truthful_mode(is_reuse: object, native_reopen: object, *, require_reuse: bool) -> bool:
    if type(is_reuse) is not bool or type(native_reopen) is not bool:
        return False
    return is_reuse and not native_reopen if require_reuse else (
        (is_reuse and not native_reopen) or (not is_reuse and native_reopen)
    )


def canonical_utc(value: object) -> bool:
    if not isinstance(value, str) or re.fullmatch(
        r"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?Z", value
    ) is None:
        return False
    try:
        return datetime.fromisoformat(value[:-1] + "+00:00").tzinfo == timezone.utc
    except ValueError:
        return False


def safe_provenance(value: object) -> bool:
    return isinstance(value, str) and re.fullmatch(
        r"[A-Za-z0-9][A-Za-z0-9._:-]{0,127}", value
    ) is not None


def valid_identity(manifest: dict[str, object]) -> bool:
    source_hash = manifest.get("sourceArchiveSha256")
    archive_hash = manifest.get("qualifiedArchiveSha256")
    build = manifest.get("tiaBuildIdentity")
    qualification = manifest.get("qualificationIdentity")
    archive_name = manifest.get("qualifiedArchiveName")
    return (
        isinstance(source_hash, str)
        and re.fullmatch(r"[0-9a-f]{64}", source_hash) is not None
        and isinstance(archive_hash, str)
        and re.fullmatch(r"[0-9a-f]{64}", archive_hash) is not None
        and isinstance(build, str)
        and 0 < len(build) <= 256
        and re.search(r"[\r\n`]", build) is None
        and safe_provenance(qualification)
        and isinstance(archive_name, str)
        and 0 < len(archive_name) <= 255
        and re.search(r"[\r\n`/\\]", archive_name) is None
        and archive_name.lower().endswith(".zal21")
    )


def valid_manifest(manifest: dict[str, object], *, require_reuse: bool) -> bool:
    return (
        manifest.get("success") is True
        and truthful_mode(
            manifest.get("isReuse"),
            manifest.get("nativeReopenSuccess"),
            require_reuse=require_reuse,
        )
        and safe_provenance(manifest.get("originalProvenanceRunId"))
        and canonical_utc(manifest.get("originalCompletedAtUtc"))
        and valid_identity(manifest)
    )


def valid_pair(run1: dict[str, object], run2: dict[str, object]) -> bool:
    return (
        valid_manifest(run1, require_reuse=False)
        and valid_manifest(run2, require_reuse=True)
        and all(run1.get(field) == run2.get(field) for field in IDENTITY_FIELDS)
        and run1.get("originalProvenanceRunId") == run2.get("originalProvenanceRunId")
        and run1.get("originalCompletedAtUtc") == run2.get("originalCompletedAtUtc")
    )


def manifest(*, is_reuse: bool, native_reopen: bool) -> dict[str, object]:
    return {
        "success": True,
        "isReuse": is_reuse,
        "nativeReopenSuccess": native_reopen,
        "sourceArchiveSha256": "a" * 64,
        "tiaBuildIdentity": "Siemens.Engineering v21.0.0.0 (file: 2100.0.121.1)",
        "qualificationIdentity": "olq-v21-txn-002-v1",
        "qualifiedArchiveName": "OpenLibrary-qualified.zal21",
        "qualifiedArchiveSha256": "b" * 64,
        "originalProvenanceRunId": "trusted-run-123",
        "originalCompletedAtUtc": "2026-09-25T17:00:00.1234567Z",
    }


def qualification_script() -> str:
    block = between(
        "      - name: Run qualification twice and create sanitized evidence",
        "      - name: Show sanitized evidence",
    )
    return textwrap.dedent(block.split("        run: |\n", 1)[1])


class TiaV21LibraryQualificationWorkflowTests(unittest.TestCase):
    def test_trusted_main_only_boundary_is_preserved(self) -> None:
        self.assertIn("if: github.ref == 'refs/heads/main'", WORKFLOW)
        self.assertIn("ref: main", WORKFLOW)
        self.assertIn("persist-credentials: false", WORKFLOW)
        self.assertIn("Trusted checkout is not current origin/main.", WORKFLOW)
        self.assertNotIn("pull_request_target", WORKFLOW)

    def test_public_evidence_has_explicit_per_run_mode_and_provenance(self) -> None:
        self.assertIn("schema = 'olq-001-public-evidence-v2'", WORKFLOW)
        for field in (
            "run1IsReuse",
            "run1NativeReopenSuccess",
            "run2IsReuse",
            "run2NativeReopenSuccess",
            "originalProvenanceRunId",
            "originalCompletedAtUtc",
        ):
            self.assertIn(f"{field} =", WORKFLOW)
            self.assertIn(f"e.{field}", WORKFLOW)

    def test_fresh_then_reuse_and_repeated_reuse_are_truthful(self) -> None:
        fresh = manifest(is_reuse=False, native_reopen=True)
        reuse = manifest(is_reuse=True, native_reopen=False)
        self.assertTrue(valid_pair(fresh, reuse))
        self.assertTrue(valid_pair(reuse, reuse.copy()))

        old_gate_shape = reuse.copy()
        old_gate_shape["nativeReopenSuccess"] = True
        self.assertFalse(valid_pair(fresh, old_gate_shape))

    def test_missing_malformed_or_changed_provenance_fails_closed(self) -> None:
        run1 = manifest(is_reuse=False, native_reopen=True)
        run2 = manifest(is_reuse=True, native_reopen=False)
        for field, value in (
            ("originalProvenanceRunId", None),
            ("originalProvenanceRunId", "unsafe path\\name"),
            ("originalCompletedAtUtc", "2026-09-25T17:00:00+02:00"),
            ("originalCompletedAtUtc", "not-a-time"),
            ("isReuse", "true"),
            ("nativeReopenSuccess", None),
        ):
            bad = run2.copy()
            bad[field] = value
            self.assertFalse(valid_pair(run1, bad), (field, value))

        for field, value in (
            ("originalProvenanceRunId", "trusted-run-456"),
            ("originalCompletedAtUtc", "2026-09-25T17:00:01Z"),
        ):
            bad = run2.copy()
            bad[field] = value
            self.assertFalse(valid_pair(run1, bad), (field, value))

    def test_invalid_or_changed_identity_fails_closed(self) -> None:
        run1 = manifest(is_reuse=False, native_reopen=True)
        run2 = manifest(is_reuse=True, native_reopen=False)
        for field, value in (
            ("sourceArchiveSha256", "A" * 64),
            ("qualifiedArchiveSha256", "not-a-hash"),
            ("tiaBuildIdentity", "x" * 257),
            ("qualificationIdentity", "bad identity"),
            ("qualifiedArchiveName", r"C:\private\archive.zal21"),
            ("qualifiedArchiveName", "archive.zip"),
        ):
            bad = run2.copy()
            bad[field] = value
            self.assertFalse(valid_pair(run1, bad), (field, value))

        changed = run2.copy()
        changed["qualifiedArchiveSha256"] = "c" * 64
        self.assertFalse(valid_pair(run1, changed))

    def test_source_gate_requires_reuse_without_fresh_reopen(self) -> None:
        mode = between("          function Truthful-Mode", "          $identityFields")
        self.assertIn("Bool $Manifest 'isReuse'", mode)
        self.assertIn("Bool $Manifest 'nativeReopenSuccess'", mode)
        self.assertIn("if ($RequireReuse)", mode)
        self.assertIn("$isReuse -and -not $reopen", mode)
        self.assertIn("-not $isReuse -and $reopen", mode)

        gate = between("            $same = (", "            $evidence.deterministicSecondRun")
        self.assertIn("Truthful-Mode $r2 $true", gate)
        self.assertIn("Identity-Same $r1 $r2", gate)
        self.assertIn("$runId, $run2Id", gate)
        self.assertIn("$completed, $run2Completed", gate)
        self.assertNotIn("[bool]$r2.nativeReopenSuccess", gate)

    def test_public_fields_are_bounded_and_raw_diagnostics_are_not_published(self) -> None:
        identity = between("          function Safe-PublicText", "          function Identity-Same")
        self.assertIn("$Value.Length -le $MaxLength", identity)
        self.assertIn("^[0-9a-f]{64}$", identity)
        self.assertIn("Safe-PublicText (Text $Manifest 'tiaBuildIdentity') 256", identity)
        self.assertIn("Safe-ArchiveName (Text $Manifest 'qualifiedArchiveName')", identity)
        self.assertIn("-ieq '.zal21'", identity)
        self.assertIn("$actualSourceSha256", WORKFLOW)
        self.assertIn("source identity did not match the runner-local archive", WORKFLOW)

        publication = between(
            "      - name: Publish sanitized result to qualification issue",
            "      - name: Enforce qualification result",
        )
        self.assertNotIn("stdout", publication.lower())
        self.assertNotIn("stderr", publication.lower())
        self.assertIn("String(value).replace(/[\\r\\n`]/g, ' ').slice(0, 512)", publication)
        self.assertIn(
            "Only sanitized hashes/identities/reuse/provenance/status are published",
            publication,
        )
        self.assertNotIn("Public-Failure", WORKFLOW)
        self.assertNotIn("[string]$r1.failure", WORKFLOW)
        self.assertNotIn("[string]$r2.failure", WORKFLOW)
        self.assertIn("Qualification harness failed before valid public evidence", WORKFLOW)

    @unittest.skipUnless(shutil.which("pwsh"), "pwsh is unavailable")
    def test_embedded_powershell_parses_when_pwsh_is_available(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            script = Path(directory) / "qualification.ps1"
            script.write_text(qualification_script(), encoding="utf-8")
            parser = (
                "$tokens=$null; $errors=$null; "
                "[System.Management.Automation.Language.Parser]::ParseFile("
                "$args[0],[ref]$tokens,[ref]$errors) | Out-Null; "
                "if ($errors.Count) { $errors | % { Write-Error $_.Message }; exit 1 }"
            )
            completed = subprocess.run(
                ["pwsh", "-NoProfile", "-NonInteractive", "-Command", parser, str(script)],
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                check=False,
            )
            self.assertEqual(0, completed.returncode, completed.stdout)


if __name__ == "__main__":
    unittest.main()
