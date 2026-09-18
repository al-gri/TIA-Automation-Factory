import json
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOOL = ROOT / "agents" / "runtime" / "external-review.py"
POLICY_TOOL = ROOT / "agents" / "runtime" / "reviewer-policy.py"
CODE_TEMPLATE = ROOT / "reviews" / "templates" / "code-review.md"
SHA = "a" * 40
REQUEST_ID = "ER-TEST-001-CODE-1-PRIMARY"
AGENT_EMAIL = "tia-automation-agent@users.noreply.github.com"


class ExternalReviewToolTests(unittest.TestCase):
    def run_tool(self, *args, expect=0):
        completed = subprocess.run(
            ["python3", str(TOOL), *map(str, args)],
            cwd=ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=False,
        )
        self.assertEqual(
            expect,
            completed.returncode,
            msg=f"stdout:\n{completed.stdout}\nstderr:\n{completed.stderr}",
        )
        return completed

    def run_policy(self, *args, expect=0):
        completed = subprocess.run(
            ["python3", str(POLICY_TOOL), *map(str, args)],
            cwd=ROOT,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=False,
        )
        self.assertEqual(
            expect,
            completed.returncode,
            msg=f"stdout:\n{completed.stdout}\nstderr:\n{completed.stderr}",
        )
        return completed

    def valid_response(self):
        return {
            "protocolVersion": "1.0",
            "reviewRequestId": REQUEST_ID,
            "reviewerSlot": "primary",
            "taskId": "TEST-001",
            "candidateSha": SHA,
            "reviewType": "CODE_REVIEW",
            "reviewRound": 1,
            "reviewStatus": "APPROVE",
            "summary": "Candidate satisfies the bounded test task.",
            "requirements": {"status": "PASS", "notes": "Requirements are covered."},
            "architecture": {"status": "PASS", "notes": "Boundaries are preserved."},
            "codeQuality": {"status": "PASS", "notes": "Change is minimal."},
            "tests": {"status": "PASS", "notes": "Deterministic evidence is present."},
            "findings": [],
            "recommendation": "Proceed to the next deterministic gate.",
        }

    def test_build_package_resolves_all_placeholders(self):
        context = {
            "REVIEW_REQUEST_ID": REQUEST_ID,
            "TASK_ID": "TEST-001",
            "CANDIDATE_SHA": SHA,
            "PR_REFERENCE": "#123",
            "REVIEW_ROUND": 1,
            "RISK_CLASS": "LOW",
            "REVIEWER_SLOT": "primary",
            "TASK_SPECIFICATION": "Implement a deterministic example.",
            "ACCEPTANCE_CRITERIA": "- Example exists\n- Tests pass",
            "IMPLEMENTATION_SUMMARY": "Added the bounded example.",
            "CANDIDATE_DIFF": "+example",
            "RELEVANT_SOURCE_CONTEXT": "No additional source context required.",
            "LINUX_EVIDENCE": "2 tests passed",
            "GENERATED_ARTIFACT_EVIDENCE": "artifact generated",
            "TIA_EVIDENCE_OR_NOT_AVAILABLE": "Not applicable for this test.",
            "PREVIOUS_FINDINGS_OR_NONE": "None",
        }
        with tempfile.TemporaryDirectory() as temp:
            temp_path = Path(temp)
            context_path = temp_path / "context.json"
            output_path = temp_path / "review-request.md"
            context_path.write_text(json.dumps(context), encoding="utf-8")

            self.run_tool(
                "build-package",
                "--template",
                CODE_TEMPLATE,
                "--context",
                context_path,
                "--output",
                output_path,
            )

            rendered = output_path.read_text(encoding="utf-8")
            self.assertNotIn("{{", rendered)
            self.assertIn("TEST-001", rendered)
            self.assertIn(REQUEST_ID, rendered)
            self.assertIn(SHA, rendered)
            self.assertIn("2 tests passed", rendered)

    def test_validate_accepts_fully_bound_approve(self):
        with tempfile.TemporaryDirectory() as temp:
            response_path = Path(temp) / "response.json"
            response_path.write_text(json.dumps(self.valid_response()), encoding="utf-8")
            self.run_tool(
                "validate-response",
                "--input",
                response_path,
                "--request-id",
                REQUEST_ID,
                "--reviewer-slot",
                "primary",
                "--task-id",
                "TEST-001",
                "--candidate-sha",
                SHA,
                "--review-type",
                "CODE_REVIEW",
                "--review-round",
                "1",
            )

    def test_validate_rejects_approve_with_major_finding(self):
        response = self.valid_response()
        response["findings"] = [
            {
                "id": "F001",
                "severity": "major",
                "file": "src/Test.cs",
                "location": "TestMethod",
                "problem": "Required behavior is missing.",
                "requiredChange": "Implement the required behavior.",
            }
        ]
        with tempfile.TemporaryDirectory() as temp:
            response_path = Path(temp) / "response.json"
            response_path.write_text(json.dumps(response), encoding="utf-8")
            completed = self.run_tool("validate-response", "--input", response_path, expect=2)
            self.assertIn("APPROVE cannot contain critical/major findings", completed.stderr)

    def test_validate_rejects_candidate_binding_mismatch(self):
        with tempfile.TemporaryDirectory() as temp:
            response_path = Path(temp) / "response.json"
            response_path.write_text(json.dumps(self.valid_response()), encoding="utf-8")
            completed = self.run_tool(
                "validate-response",
                "--input",
                response_path,
                "--candidate-sha",
                "b" * 40,
                expect=2,
            )
            self.assertIn("candidateSha binding mismatch", completed.stderr)

    def test_validate_rejects_reviewer_slot_binding_mismatch(self):
        with tempfile.TemporaryDirectory() as temp:
            response_path = Path(temp) / "response.json"
            response_path.write_text(json.dumps(self.valid_response()), encoding="utf-8")
            completed = self.run_tool(
                "validate-response",
                "--input",
                response_path,
                "--request-id",
                REQUEST_ID,
                "--reviewer-slot",
                "chatgpt-secondary",
                expect=2,
            )
            self.assertIn("reviewerSlot binding mismatch", completed.stderr)

    def test_changes_required_may_contain_major_finding(self):
        response = self.valid_response()
        response["reviewStatus"] = "CHANGES_REQUIRED"
        response["findings"] = [
            {
                "id": "F001",
                "severity": "major",
                "file": None,
                "location": None,
                "problem": "A required test is missing.",
                "requiredChange": "Add the missing deterministic test.",
            }
        ]
        with tempfile.TemporaryDirectory() as temp:
            response_path = Path(temp) / "response.json"
            response_path.write_text(json.dumps(response), encoding="utf-8")
            self.run_tool("validate-response", "--input", response_path)

    def test_pure_agent_authorship_requires_primary_chatgpt(self):
        with tempfile.TemporaryDirectory() as temp:
            emails = Path(temp) / "emails.txt"
            emails.write_text(f"{AGENT_EMAIL}\n{AGENT_EMAIL}\n", encoding="utf-8")
            completed = self.run_policy("expected-slot", "--emails-file", emails)
            self.assertEqual("chatgpt", completed.stdout.strip())
            self.run_policy(
                "validate-slot",
                "--emails-file",
                emails,
                "--reviewer-slot",
                "chatgpt",
            )

    def test_any_maintainer_authorship_requires_secondary_chatgpt(self):
        with tempfile.TemporaryDirectory() as temp:
            emails = Path(temp) / "emails.txt"
            emails.write_text(f"{AGENT_EMAIL}\nmaintainer@example.com\n", encoding="utf-8")
            completed = self.run_policy("expected-slot", "--emails-file", emails)
            self.assertEqual("chatgpt-secondary", completed.stdout.strip())
            rejected = self.run_policy(
                "validate-slot",
                "--emails-file",
                emails,
                "--reviewer-slot",
                "chatgpt",
                expect=1,
            )
            self.assertIn("not independent for current candidate authorship", rejected.stderr)
            self.run_policy(
                "validate-slot",
                "--emails-file",
                emails,
                "--reviewer-slot",
                "chatgpt-secondary",
            )

    def test_external_review_request_supports_secondary_and_enforces_authorship(self):
        workflow = (ROOT / ".github" / "workflows" / "external-review-request.yml").read_text(encoding="utf-8")
        self.assertIn("Reviewer slot (chatgpt or chatgpt-secondary)", workflow)
        self.assertIn("reviewer-policy.py validate-slot", workflow)
        self.assertIn("candidate-author-emails.txt", workflow)
        self.assertIn(".review.reviewerSlots | index($slot) != null", workflow)
        self.assertNotIn("reviewer_slot=chatgpt or gemini", workflow)

    def test_agent_dispatches_exactly_one_primary_review_for_pure_agent_candidate(self):
        workflow = (ROOT / ".github" / "workflows" / "agent.yml").read_text(encoding="utf-8")
        self.assertIn("SLOT=chatgpt", workflow)
        self.assertIn("Trusted task does not authorize the primary reviewer slot required for a pure coding-agent candidate", workflow)
        self.assertNotIn(".review.reviewerSlots[]", workflow)
        self.assertNotIn("while IFS= read -r SLOT", workflow)
        self.assertEqual(1, workflow.count('event_type:"external-review-request"'))

    def test_external_review_response_rechecks_authorship_independence(self):
        workflow = (ROOT / ".github" / "workflows" / "external-review-response.yml").read_text(encoding="utf-8")
        self.assertIn("fetch-depth: 0", workflow)
        self.assertIn("reviewer-policy.py validate-slot", workflow)
        self.assertIn("candidate-author-emails.txt", workflow)
        self.assertIn("reviewer eligibility for the current candidate authorship", workflow)

    def test_olq_task_authorizes_both_authorship_based_slots_without_dual_requirement(self):
        task = json.loads((ROOT / "tasks" / "OLQ-001.json").read_text(encoding="utf-8"))
        self.assertEqual(["chatgpt", "chatgpt-secondary"], task["review"]["reviewerSlots"])
        self.assertEqual(2, task["maxRepairAttempts"])

    def test_high_changes_required_uses_bounded_repair_without_dual_aggregate(self):
        workflow = (ROOT / ".github" / "workflows" / "external-review-response.yml").read_text(encoding="utf-8")
        self.assertIn("if: steps.publish.outputs.state == 'REVIEW_CHANGES_REQUIRED'", workflow)
        self.assertIn('event_type:"candidate-repair"', workflow)
        self.assertNotIn("Resolve HIGH-risk dual-review aggregate", workflow)
        self.assertNotIn("risk_class != 'HIGH' && steps.publish.outputs.state == 'REVIEW_CHANGES_REQUIRED'", workflow)

    def test_repair_honors_task_gated_tia_worker_and_non_generator_tasks(self):
        workflow = (ROOT / ".github" / "workflows" / "agent-repair.yml").read_text(encoding="utf-8")
        self.assertIn("candidatePolicy.allowTiaV21WorkerChanges // false", workflow)
        self.assertIn("src/TiaV21Worker without trusted task opt-in", workflow)
        self.assertIn(".generator.input // empty", workflow)
        self.assertIn("case \"$RISK_CLASS\" in LOW|MEDIUM|HIGH)", workflow)
        self.assertIn(".review.reviewerSlots | index($slot) != null", workflow)


if __name__ == "__main__":
    unittest.main()
