import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class PromptAlignmentTests(unittest.TestCase):
    def read(self, path):
        return (ROOT / path).read_text(encoding="utf-8")

    def test_coder_prompt_has_bounded_finish_and_blocker_contract(self):
        text = self.read("agents/prompts/coder.md")
        for marker in (
            "CANDIDATE_DEFECT",
            "INFRASTRUCTURE_DEFECT",
            "MISSING_EVIDENCE",
            "EXTERNAL_TIA_BLOCKER",
            "READY_FOR_REVIEW",
            "BLOCKED",
            "candidatePolicy.allowedPaths",
            "allowTiaV21WorkerChanges",
            "opencode.json",
        ):
            self.assertIn(marker, text)
        self.assertIn("Do not commit, push", text)
        self.assertIn("Windows/TIA execution occurs only from trusted merged `main`", text)

    def test_repair_prompt_obeys_positive_worker_opt_in_and_blocker_classes(self):
        text = self.read("agents/prompts/repair.md")
        self.assertIn("`src/TiaV21Worker/**` is **not** categorically forbidden", text)
        self.assertIn("candidatePolicy.allowTiaV21WorkerChanges", text)
        self.assertIn("candidatePolicy.allowedPaths", text)
        self.assertIn("opencode.json", text)
        for marker in (
            "CANDIDATE_DEFECT",
            "INFRASTRUCTURE_DEFECT",
            "MISSING_EVIDENCE",
            "EXTERNAL_TIA_BLOCKER",
            "READY_FOR_REVIEW",
            "BLOCKED",
        ):
            self.assertIn(marker, text)
        self.assertIn("Do not spend a repair attempt on transport overflow", text)

    def test_legacy_reviewer_prompts_are_retired_redirects(self):
        for path, canonical in (
            ("agents/prompts/reviewer-requirements.md", "reviews/templates/code-review.md"),
            ("agents/prompts/reviewer-tia.md", "reviews/templates/plc-review.md"),
        ):
            text = self.read(path)
            self.assertTrue(text.startswith("# RETIRED"), path)
            self.assertIn("non-authoritative", text)
            self.assertIn(canonical, text)
            self.assertIn("reviews/schemas/external-review-response.schema.json", text)
            self.assertNotIn('"status": "PASS | CHANGES_REQUIRED | BLOCKED"', text)

    def test_canonical_review_templates_have_one_schema_and_required_lenses(self):
        code = self.read("reviews/templates/code-review.md")
        plc = self.read("reviews/templates/plc-review.md")
        shared_schema = "reviews/schemas/external-review-response.schema.json"
        self.assertIn(shared_schema, code)
        self.assertIn(shared_schema, plc)
        self.assertIn("every applicable task acceptance criterion separately", code)
        self.assertIn("A required check that was not executed is **not PASS**", code)
        self.assertIn("Provenance-first review", plc)
        self.assertIn("qualification", plc)
        self.assertIn("reuse", plc)
        self.assertIn("No unexecuted check may be reported as `PASS`", plc)

    def test_active_runtime_and_workflows_do_not_call_retired_prompt_protocols(self):
        retired = ("reviewer-requirements.md", "reviewer-tia.md")
        paths = list((ROOT / ".github" / "workflows").glob("*.yml"))
        paths += list((ROOT / "agents" / "runtime").glob("*.py"))
        paths += list((ROOT / "agents" / "runtime").glob("*.sh"))
        for path in paths:
            text = path.read_text(encoding="utf-8")
            for marker in retired:
                self.assertNotIn(marker, text, f"{path.relative_to(ROOT)} still calls {marker}")

    def test_normative_primary_orchestration_priorities_are_live(self):
        text = self.read("AGENTS.md")
        self.assertIn("## Primary orchestration priorities", text)
        self.assertIn("time to the first usable real device", text)
        self.assertIn("Delegate routine product implementation to the coding-agent lane by default", text)
        self.assertIn("Use one independent reviewer by default", text)
        self.assertIn("Windows/TIA execution on trusted merged `main`", text)
        self.assertIn("concrete blocker", text)
        self.assertIn("positive allowed paths", text)

    def test_autonomy_overview_matches_current_provider_and_review_model(self):
        text = self.read("docs/AGENT_AUTONOMY.md")
        self.assertIn("OpenRouter", text)
        self.assertIn("DeepSeek", text)
        self.assertIn("deepseek-flash", text)
        self.assertIn("reviews/templates/code-review.md", text)
        self.assertIn("reviews/templates/plc-review.md", text)
        self.assertIn("One independent reviewer is the default", text)
        self.assertIn("separate fresh publisher", text)
        self.assertNotIn("gemini-3.8-flash", text)
        self.assertNotIn("Two independent read-only review sessions are planned", text)

    def test_task_is_exactly_bounded_and_secondary_reviewed(self):
        task = json.loads(self.read("tasks/PROMPT-ALIGN-001.json"))
        self.assertEqual("PROMPT-ALIGN-001", task["id"])
        self.assertEqual("HIGH", task["review"]["riskClass"])
        self.assertEqual("ARCHITECTURE_REVIEW", task["review"]["reviewType"])
        self.assertEqual(["chatgpt-secondary"], task["review"]["reviewerSlots"])
        self.assertFalse(task["candidatePolicy"]["allowTiaV21WorkerChanges"])
        self.assertEqual(
            {
                "AGENTS.md",
                "agents/prompts/coder.md",
                "agents/prompts/repair.md",
                "agents/prompts/reviewer-requirements.md",
                "agents/prompts/reviewer-tia.md",
                "reviews/templates/code-review.md",
                "reviews/templates/plc-review.md",
                "docs/AGENT_AUTONOMY.md",
                "tests/ExternalReviewProtocol.Tests/test_prompt_alignment.py",
                "tasks/PROMPT-ALIGN-001.json",
            },
            set(task["candidatePolicy"]["allowedPaths"]),
        )


if __name__ == "__main__":
    unittest.main()
