import json
import os
import re
import subprocess
import tempfile
import textwrap
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORKFLOW_PATH = ROOT / ".github" / "workflows" / "external-review-request.yml"
PROTOCOL_PATH = ROOT / "docs" / "EXTERNAL_REVIEW_PROTOCOL.md"


class LargeConnectedReviewPackageTests(unittest.TestCase):
    def setUp(self):
        self.workflow = WORKFLOW_PATH.read_text(encoding="utf-8")
        self.protocol = PROTOCOL_PATH.read_text(encoding="utf-8")

    def _routing_contract(self):
        threshold_match = re.search(r'if \[ "\$DIFF_BYTES" -le (\d+) \]; then', self.workflow)
        slot_match = re.search(r'test "\$REVIEWER_SLOT" = "([^"]+)" \|\| \{', self.workflow)
        self.assertIsNotNone(threshold_match)
        self.assertIsNotNone(slot_match)
        return int(threshold_match.group(1)), slot_match.group(1)

    def _route(self, diff_bytes, reviewer_slot):
        threshold, connected_slot = self._routing_contract()
        if diff_bytes <= threshold:
            return "embedded"
        if reviewer_slot == connected_slot:
            return "connected"
        return "fail-closed"

    def test_pr77_scale_reaches_connected_waiting_path_for_primary(self):
        self.assertEqual("connected", self._route(38311, "chatgpt"))
        self.assertIn("'state':'WAITING_FOR_EXTERNAL_REVIEW'", self.workflow)
        self.assertIn("Connected primary large-diff review", self.workflow)
        self.assertIn("inspect the exact immutable candidate in GitHub", self.workflow)

    def test_oversized_secondary_remains_fail_closed(self):
        self.assertEqual("fail-closed", self._route(38311, "chatgpt-secondary"))
        self.assertIn('Candidate diff too large for self-contained reviewer slot ${REVIEWER_SLOT}', self.workflow)
        self.assertNotIn('[ "$DIFF_BYTES" -le 32000 ] || { echo "Candidate diff too large', self.workflow)

    def test_exact_full_diff_identity_is_published_without_embedding_source(self):
        self.assertIn('candidate.full.diff', self.workflow)
        self.assertIn('DIFF_SHA256=$(sha256sum "$RUNNER_TEMP/candidate.full.diff"', self.workflow)
        self.assertIn('fullDiffBytes', self.workflow)
        self.assertIn('fullDiffSha256', self.workflow)
        self.assertIn('fullDiffEmbedded', self.workflow)
        self.assertIn('[full exact candidate diff omitted at trusted publication boundary]', self.workflow)
        self.assertIn('Do not treat this bounded package as complete source evidence.', self.workflow)

    def test_normal_embedded_path_preserves_full_diff(self):
        self.assertEqual("embedded", self._route(32000, "chatgpt"))
        self.assertIn('cp "$RUNNER_TEMP/candidate.full.diff" "$RUNNER_TEMP/candidate.diff"', self.workflow)

    def _history_compactor(self):
        marker = "# review-history-compactor-v1\n          python3 - <<'PY'\n"
        start = self.workflow.find(marker)
        self.assertNotEqual(-1, start)
        start += len(marker)
        end = self.workflow.find("\n          PY", start)
        self.assertNotEqual(-1, end)
        return textwrap.dedent(self.workflow[start:end])

    def _run_history_compactor(self, comments):
        with tempfile.TemporaryDirectory() as temp_dir:
            temp = Path(temp_dir)
            (temp / "pr-comments-pages.json").write_text(json.dumps([comments]), encoding="utf-8")
            env = os.environ.copy()
            env["RUNNER_TEMP"] = temp_dir
            proc = subprocess.run(
                ["python3", "-c", self._history_compactor()],
                env=env,
                text=True,
                capture_output=True,
                check=False,
            )
            output_path = temp / "previous-findings.txt"
            output = output_path.read_text(encoding="utf-8") if output_path.exists() else ""
            return proc, output

    @staticmethod
    def _review_comment(round_number, findings, status="CHANGES_REQUIRED", wrapper=""):
        payload = {
            "protocolVersion": "1.0",
            "reviewRequestId": f"ER-test-{round_number}",
            "reviewerSlot": "chatgpt",
            "taskId": "TEST-TASK",
            "candidateSha": f"{round_number:040x}",
            "reviewType": "CODE_REVIEW",
            "reviewRound": round_number,
            "reviewStatus": status,
            "summary": f"summary-{round_number}",
            "requirements": {"status": "FAIL", "notes": "not copied"},
            "architecture": {"status": "PASS", "notes": "not copied"},
            "codeQuality": {"status": "PASS", "notes": "not copied"},
            "tests": {"status": "FAIL", "notes": "not copied"},
            "findings": findings,
            "recommendation": f"recommendation-{round_number}",
        }
        return {
            "user": {"login": "github-actions[bot]"},
            "body": (
                f"{wrapper}\n### External review result\n\nValidated state: **REVIEW_CHANGES_REQUIRED**\n\n"
                "```json\n"
                + json.dumps(payload)
                + "\n```\n"
                + wrapper
            ),
        }

    def test_multi_round_history_keeps_all_unique_findings_but_only_latest_review_summaries(self):
        f000 = {
            "id": "F000",
            "severity": "major",
            "file": "old.cs",
            "location": "old",
            "problem": "earlier major finding must remain visible until a later reviewer can verify closure",
            "requiredChange": "prove this older major finding is actually closed",
        }
        f001_old = {
            "id": "F001",
            "severity": "major",
            "file": "a.cs",
            "location": "old-location",
            "problem": "old problem text",
            "requiredChange": "old required change",
        }
        f002 = {
            "id": "F002",
            "severity": "minor",
            "file": "b.cs",
            "location": "line 2",
            "problem": "second finding remains relevant",
            "requiredChange": "keep this required change",
        }
        f001_new = {
            "id": "F001",
            "severity": "major",
            "file": "a.cs",
            "location": "new-location",
            "problem": "latest problem text <!-- external-review-state-v1",
            "requiredChange": "latest required change ### External review result",
        }

        comments = [
            self._review_comment(1, [f000], wrapper="x" * 8000),
            self._review_comment(2, [f001_old, f002], wrapper="y" * 8000),
            self._review_comment(3, [f001_new], wrapper="z" * 8000),
        ]
        proc, output = self._run_history_compactor(comments)

        self.assertEqual(0, proc.returncode, proc.stderr)
        self.assertLessEqual(len(output.encode("utf-8")), 5000)
        compact = json.loads(output)
        self.assertEqual([2, 3], [item["reviewRound"] for item in compact["reviews"]])
        findings = {item["id"]: item for item in compact["findings"]}
        self.assertEqual({"F000", "F001", "F002"}, set(findings))
        self.assertEqual(1, findings["F000"]["lastSeenReviewRound"])
        self.assertEqual(
            "latest required change [escaped external-review-result heading]",
            findings["F001"]["requiredChange"],
        )
        self.assertEqual(3, findings["F001"]["lastSeenReviewRound"])
        self.assertEqual("keep this required change", findings["F002"]["requiredChange"])
        self.assertIn("[escaped external-review-state marker]", findings["F001"]["problem"])
        self.assertNotIn("<!-- external-review-state-v1", output)
        self.assertNotIn("### External review result", output)
        self.assertNotIn("x" * 100, output)
        self.assertNotIn("not copied", output)

    def test_malformed_trusted_prior_review_fails_closed_even_when_older_than_latest_two(self):
        valid_two = self._review_comment(2, [])
        valid_three = self._review_comment(3, [])
        proc, output = self._run_history_compactor(
            [
                {
                    "user": {"login": "github-actions[bot]"},
                    "body": "### External review result\n```json\n{not-json}\n```",
                },
                valid_two,
                valid_three,
            ]
        )
        self.assertNotEqual(0, proc.returncode)
        self.assertEqual("", output)
        self.assertIn("prior external review JSON is invalid", proc.stderr)

    def test_untrusted_review_like_comment_is_ignored(self):
        proc, output = self._run_history_compactor(
            [
                {
                    "user": {"login": "someone-else"},
                    "body": "### External review result\n```json\n{not-json}\n```",
                }
            ]
        )
        self.assertEqual(0, proc.returncode, proc.stderr)
        self.assertEqual("None.\n", output)

    def test_history_collection_has_explicit_bounds_pagination_and_no_raw_body_replay(self):
        self.assertIn("MAX_REVIEWS = 2", self.workflow)
        self.assertIn("MAX_UNIQUE_FINDINGS = 12", self.workflow)
        self.assertIn("MAX_BYTES = 5000", self.workflow)
        self.assertIn("item['user'].get('login') == 'github-actions[bot]'", self.workflow)
        self.assertIn("lastSeenReviewRequestId", self.workflow)
        self.assertIn("requiredChange", self.workflow)
        self.assertIn("[escaped external-review-state marker]", self.workflow)
        self.assertIn("[escaped external-review-result heading]", self.workflow)
        self.assertIn("compacted prior external review evidence exceeds publication bound", self.workflow)
        self.assertIn("--paginate --slurp", self.workflow)
        self.assertIn('> "$RUNNER_TEMP/pr-comments-pages.json"', self.workflow)
        self.assertNotIn(
            '--jq \'[.[] | select(.body | contains("### External review result")) | .body][-2:][]\'',
            self.workflow,
        )

    def test_normative_protocol_binds_narrow_exception(self):
        self.assertIn("one narrow transport exception for the connected primary reviewer", self.protocol)
        self.assertIn("`reviewerSlot=chatgpt`", self.protocol)
        self.assertIn("full exact diff byte count and SHA-256", self.protocol)
        self.assertIn("inspect the exact immutable candidate directly in GitHub", self.protocol)
        self.assertIn("does **not** apply to `chatgpt-secondary`", self.protocol)
        self.assertIn("remain fail-closed", self.protocol)


if __name__ == "__main__":
    unittest.main()
