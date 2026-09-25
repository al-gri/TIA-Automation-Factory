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
TEMPLATE_PATH = ROOT / "reviews" / "templates" / "code-review.md"
REVIEW_TOOL_PATH = ROOT / "agents" / "runtime" / "external-review.py"
ALLOWLIST_TASK_PATH = ROOT / "tasks" / "TIA-AUTH-V21-ALLOWLIST-001.json"


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
        marker = "# review-history-compactor-v2\n          python3 - <<'PY'\n"
        start = self.workflow.find(marker)
        self.assertNotEqual(-1, start)
        start += len(marker)
        end = self.workflow.find("\n          PY", start)
        self.assertNotEqual(-1, end)
        return textwrap.dedent(self.workflow[start:end])

    def _run_history_compactor(
        self,
        comments,
        *,
        task_id="TEST-TASK",
        review_type="CODE_REVIEW",
        review_round=4,
    ):
        with tempfile.TemporaryDirectory() as temp_dir:
            temp = Path(temp_dir)
            (temp / "pr-comments-pages.json").write_text(json.dumps([comments]), encoding="utf-8")
            env = os.environ.copy()
            env["RUNNER_TEMP"] = temp_dir
            env["TASK_ID"] = task_id
            env["REVIEW_TYPE"] = review_type
            env["REVIEW_ROUND"] = str(review_round)
            proc = subprocess.run(
                ["python3", "-c", self._history_compactor()],
                env=env,
                text=True,
                capture_output=True,
                check=False,
                cwd=ROOT,
            )
            output_path = temp / "previous-findings.txt"
            output = output_path.read_text(encoding="utf-8") if output_path.exists() else ""
            return proc, output

    def _render_connected_package(self, previous_findings):
        task = json.loads(ALLOWLIST_TASK_PATH.read_text(encoding="utf-8"))
        connected_notice = (
            "CONNECTED PRIMARY LARGE-DIFF FALLBACK: this package is intentionally not self-contained source evidence. "
            "The exact full diff is 38311 bytes with SHA-256 " + "a" * 64 + ". "
            "Reviewer slot chatgpt MUST inspect PR #87 at exact candidate SHA " + "d" * 40 + " directly in GitHub before verdict, "
            "and must not claim omitted source text was reviewed from this package alone.\n\n"
        )
        context = {
            "REVIEW_REQUEST_ID": "ER-87-budget-regression",
            "REVIEWER_SLOT": "chatgpt",
            "TASK_ID": task["id"],
            "CANDIDATE_SHA": "d" * 40,
            "PR_REFERENCE": "#87",
            "REVIEW_ROUND": 3,
            "RISK_CLASS": "HIGH",
            "TASK_SPECIFICATION": json.dumps(task, indent=2, ensure_ascii=False),
            "ACCEPTANCE_CRITERIA": json.dumps(task["acceptance"], indent=2, ensure_ascii=False),
            "IMPLEMENTATION_SUMMARY": connected_notice + (
                "Changed files:\n"
                "scripts/windows/Configure-TiaV21WorkerWhitelist.ps1\n"
                "src/TiaV21Worker/WhitelistManager.cs\n"
                "tests/SiemensBackend.Tests/TiaV21AllowListContractTests.cs\n"
            ),
            "CANDIDATE_DIFF": (
                "[full exact candidate diff omitted at trusted publication boundary]\n"
                "Connected reviewer slot: chatgpt\n"
                "Candidate SHA: " + "d" * 40 + "\n"
                "Full diff bytes: 38311\n"
                "Full diff SHA256: " + "a" * 64 + "\n"
                "Review requirement: inspect the immutable exact candidate in GitHub before verdict. "
                "Do not treat this bounded package as complete source evidence.\n"
            ),
            "RELEVANT_SOURCE_CONTEXT": connected_notice + ("S" * 16000),
            "LINUX_EVIDENCE": "L" * 12000,
            "GENERATED_ARTIFACT_EVIDENCE": "No GeneratorCli task artifact is declared for this task.\n",
            "TIA_EVIDENCE_OR_NOT_AVAILABLE": "Trusted TIA evidence is not included in this pre-merge package.",
            "PREVIOUS_FINDINGS_OR_NONE": previous_findings,
        }
        with tempfile.TemporaryDirectory() as temp_dir:
            temp = Path(temp_dir)
            context_path = temp / "context.json"
            output_path = temp / "review.md"
            context_path.write_text(json.dumps(context), encoding="utf-8")
            proc = subprocess.run(
                [
                    "python3",
                    str(REVIEW_TOOL_PATH),
                    "build-package",
                    "--template",
                    str(TEMPLATE_PATH),
                    "--context",
                    str(context_path),
                    "--output",
                    str(output_path),
                ],
                text=True,
                capture_output=True,
                check=False,
            )
            rendered = output_path.read_bytes() if output_path.exists() else b""
            return proc, rendered

    @staticmethod
    def _review_payload(
        round_number,
        findings,
        *,
        status="CHANGES_REQUIRED",
        task_id="TEST-TASK",
        review_type="CODE_REVIEW",
        reviewer_slot="chatgpt",
        candidate_sha=None,
    ):
        return {
            "protocolVersion": "1.0",
            "reviewRequestId": f"ER-test-{task_id}-{review_type}-{round_number}-{reviewer_slot}",
            "reviewerSlot": reviewer_slot,
            "taskId": task_id,
            "candidateSha": candidate_sha or f"{round_number:040x}",
            "reviewType": review_type,
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

    @staticmethod
    def _review_comment_from_payload(payload, wrapper=""):
        return {
            "user": {"login": "github-actions[bot]"},
            "body": (
                f"{wrapper}\n### External review result\n\nValidated state: **{payload.get('reviewStatus', 'UNKNOWN')}**\n\n"
                "```json\n"
                + json.dumps(payload)
                + "\n```\n"
                + wrapper
            ),
        }

    @classmethod
    def _review_comment(
        cls,
        round_number,
        findings,
        status="CHANGES_REQUIRED",
        wrapper="",
        *,
        task_id="TEST-TASK",
        review_type="CODE_REVIEW",
        reviewer_slot="chatgpt",
        candidate_sha=None,
    ):
        return cls._review_comment_from_payload(
            cls._review_payload(
                round_number,
                findings,
                status=status,
                task_id=task_id,
                review_type=review_type,
                reviewer_slot=reviewer_slot,
                candidate_sha=candidate_sha,
            ),
            wrapper=wrapper,
        )

    @staticmethod
    def _finding(finding_id, severity="major", suffix=""):
        return {
            "id": finding_id,
            "severity": severity,
            "file": "tests/SiemensBackend.Tests/TiaV21AllowListContractTests.cs",
            "location": "source/bootstrap regression",
            "problem": "production contract regression evidence is incomplete " + suffix,
            "requiredChange": "make deterministic evidence inspect the actual production/bootstrap source " + suffix,
        }

    def test_multi_round_history_keeps_all_unique_findings_but_only_latest_review_summaries(self):
        comments = [
            self._review_comment(1, [self._finding("F000", suffix="old")], wrapper="x" * 8000),
            self._review_comment(
                2,
                [self._finding("F001", suffix="old"), self._finding("F002", severity="minor", suffix="keep")],
                wrapper="y" * 8000,
            ),
            self._review_comment(
                3,
                [{
                    "id": "F001",
                    "severity": "major",
                    "file": "a.cs",
                    "location": "new-location",
                    "problem": "latest problem text <!-- external-review-state-v1",
                    "requiredChange": "latest required change ### External review result",
                }],
                wrapper="z" * 8000,
            ),
        ]
        proc, output = self._run_history_compactor(comments)
        self.assertEqual(0, proc.returncode, proc.stderr)
        self.assertLessEqual(len(output.encode("utf-8")), 5000)
        compact = json.loads(output)
        self.assertEqual([2, 3], [item["reviewRound"] for item in compact["reviews"]])
        findings = {item["lineageFindingId"]: item for item in compact["findings"]}
        self.assertEqual({"chatgpt:F000", "chatgpt:F001", "chatgpt:F002"}, set(findings))
        self.assertEqual(1, findings["chatgpt:F000"]["lastSeenReviewRound"])
        self.assertEqual(
            "latest required change [escaped external-review-result heading]",
            findings["chatgpt:F001"]["requiredChange"],
        )
        self.assertEqual(3, findings["chatgpt:F001"]["lastSeenReviewRound"])
        self.assertIn("[escaped external-review-state marker]", findings["chatgpt:F001"]["problem"])
        self.assertNotIn("<!-- external-review-state-v1", output)
        self.assertNotIn("### External review result", output)
        self.assertNotIn("x" * 100, output)
        self.assertNotIn("not copied", output)

    def test_unrelated_trusted_lineage_cannot_overwrite_or_pollute_current_findings(self):
        comments = [
            self._review_comment(1, [self._finding("F001", suffix="current-lineage")]),
            self._review_comment(
                2,
                [self._finding("F001", suffix="unrelated-overwrite"), self._finding("F999", suffix="unrelated")],
                task_id="OTHER-TASK",
                review_type="PLC_REVIEW",
            ),
            self._review_comment(3, [self._finding("F002", severity="minor", suffix="current-later")]),
        ]
        proc, output = self._run_history_compactor(comments)
        self.assertEqual(0, proc.returncode, proc.stderr)
        compact = json.loads(output)
        findings = {item["lineageFindingId"]: item for item in compact["findings"]}
        self.assertEqual({"chatgpt:F001", "chatgpt:F002"}, set(findings))
        self.assertIn("current-lineage", findings["chatgpt:F001"]["problem"])
        self.assertNotIn("unrelated-overwrite", output)
        self.assertNotIn("F999", output)

    def test_reviewer_transition_preserves_colliding_response_local_ids(self):
        comments = [
            self._review_comment(1, [self._finding("F001", suffix="primary")], reviewer_slot="chatgpt"),
            self._review_comment(
                2,
                [self._finding("F001", suffix="secondary")],
                reviewer_slot="chatgpt-secondary",
            ),
        ]
        proc, output = self._run_history_compactor(comments, review_round=3)
        self.assertEqual(0, proc.returncode, proc.stderr)
        compact = json.loads(output)
        findings = {item["lineageFindingId"]: item for item in compact["findings"]}
        self.assertEqual({"chatgpt:F001", "chatgpt-secondary:F001"}, set(findings))
        self.assertIn("primary", findings["chatgpt:F001"]["problem"])
        self.assertIn("secondary", findings["chatgpt-secondary:F001"]["problem"])

    def test_compacted_history_fits_final_connected_package_bound_at_publication_limits(self):
        comments = [
            self._review_comment(1, [self._finding("F001", suffix="round-one")], wrapper="x" * 8000),
            self._review_comment(2, [self._finding("F001", suffix="round-two")], wrapper="y" * 8000),
        ]
        compact_proc, previous_findings = self._run_history_compactor(comments)
        self.assertEqual(0, compact_proc.returncode, compact_proc.stderr)
        render_proc, rendered = self._render_connected_package(previous_findings)
        self.assertEqual(0, render_proc.returncode, render_proc.stderr)
        self.assertLessEqual(len(rendered), 54000)

    def test_max_findings_with_long_text_are_bounded_without_dropping_identity(self):
        findings = [
            self._finding(f"F{index:03d}", suffix=(" long-review-detail" * 300))
            for index in range(1, 13)
        ]
        comments = [self._review_comment(1, findings)]

        first_proc, first_output = self._run_history_compactor(comments, review_round=2)
        second_proc, second_output = self._run_history_compactor(comments, review_round=2)

        self.assertEqual(0, first_proc.returncode, first_proc.stderr)
        self.assertEqual(0, second_proc.returncode, second_proc.stderr)
        self.assertEqual(first_output, second_output)
        self.assertLessEqual(len(first_output.encode("utf-8")), 5000)

        compact = json.loads(first_output)
        self.assertEqual(12, len(compact["findings"]))
        self.assertEqual(
            {f"chatgpt:F{index:03d}" for index in range(1, 13)},
            {item["lineageFindingId"] for item in compact["findings"]},
        )
        for item in compact["findings"]:
            self.assertEqual("major", item["severity"])
            self.assertEqual(1, item["lastSeenReviewRound"])
            self.assertIn("[truncated]", item["problem"])
            self.assertIn("[truncated]", item["requiredChange"])

        render_proc, rendered = self._render_connected_package(first_output)
        self.assertEqual(0, render_proc.returncode, render_proc.stderr)
        self.assertLessEqual(len(rendered), 54000)

    def test_malformed_json_trusted_review_fails_closed_even_when_older_than_latest_two(self):
        proc, output = self._run_history_compactor([
            {
                "user": {"login": "github-actions[bot]"},
                "body": "### External review result\n```json\n{not-json}\n```",
            },
            self._review_comment(2, []),
            self._review_comment(3, []),
        ])
        self.assertNotEqual(0, proc.returncode)
        self.assertEqual("", output)
        self.assertIn("prior external review response contract is invalid", proc.stderr)

    def test_json_valid_but_schema_invalid_trusted_history_fails_closed(self):
        mutations = {
            "negative round": lambda p: p.__setitem__("reviewRound", -1),
            "string round": lambda p: p.__setitem__("reviewRound", "2"),
            "invalid status": lambda p: p.__setitem__("reviewStatus", "MAYBE"),
            "invalid review type": lambda p: p.__setitem__("reviewType", "SECURITY_REVIEW"),
            "malformed sha": lambda p: p.__setitem__("candidateSha", "not-a-sha"),
            "invalid finding id": lambda p: p["findings"][0].__setitem__("id", "BAD"),
            "invalid severity": lambda p: p["findings"][0].__setitem__("severity", "blocker"),
            "invalid assessment": lambda p: p.__setitem__("requirements", {"status": "PASS"}),
        }
        for name, mutate in mutations.items():
            with self.subTest(name=name):
                invalid = self._review_payload(1, [self._finding("F001")])
                mutate(invalid)
                proc, output = self._run_history_compactor([
                    self._review_comment_from_payload(invalid),
                    self._review_comment(2, []),
                    self._review_comment(3, []),
                ])
                self.assertNotEqual(0, proc.returncode)
                self.assertEqual("", output)
                self.assertIn("prior external review response contract is invalid", proc.stderr)

    def test_non_prior_round_for_current_lineage_fails_closed(self):
        proc, output = self._run_history_compactor(
            [self._review_comment(4, [])],
            review_round=4,
        )
        self.assertNotEqual(0, proc.returncode)
        self.assertEqual("", output)
        self.assertIn("non-prior reviewRound", proc.stderr)

    def test_untrusted_review_like_comment_is_ignored(self):
        proc, output = self._run_history_compactor([
            {"user": {"login": "someone-else"}, "body": "### External review result\n```json\n{not-json}\n```"}
        ])
        self.assertEqual(0, proc.returncode, proc.stderr)
        self.assertEqual("None.\n", output)

    def test_history_collection_has_explicit_bounds_pagination_validation_and_lineage(self):
        self.assertIn("MAX_REVIEWS = 2", self.workflow)
        self.assertIn("MAX_UNIQUE_FINDINGS = 12", self.workflow)
        self.assertIn("MAX_BYTES = 5000", self.workflow)
        self.assertIn("item['user'].get('login') == 'github-actions[bot]'", self.workflow)
        self.assertIn("agents/runtime/external-review.py", self.workflow)
        self.assertIn("validate-response", self.workflow)
        self.assertIn("current_task_id", self.workflow)
        self.assertIn("current_review_type", self.workflow)
        self.assertIn("lineageFindingId", self.workflow)
        self.assertIn("payload['reviewerSlot'] + ':' + finding['id']", self.workflow)
        self.assertIn("lastSeenReviewRequestId", self.workflow)
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
