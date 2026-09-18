import json
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WORKFLOWS = ROOT / ".github" / "workflows"


class TrustedStateMachineTests(unittest.TestCase):
    def test_candidate_validation_has_only_post_approve_dispatch_source(self):
        marker = 'event_type:"candidate-validation"'
        sources = []
        occurrences = 0
        for path in sorted(WORKFLOWS.glob("*.yml")):
            text = path.read_text(encoding="utf-8")
            count = text.count(marker)
            if count:
                sources.append(path.name)
                occurrences += count

        self.assertEqual(
            ["external-review-response.yml"],
            sources,
            msg=f"candidate-validation dispatch must exist only in the post-APPROVE external-review response transition; found {sources}",
        )
        self.assertEqual(1, occurrences)

    def test_legacy_i5_review_bypass_workflow_is_retired(self):
        self.assertFalse((WORKFLOWS / "i5-repair-smoke.yml").exists())

    def test_infra_001_is_compatible_with_reviewed_task_state_machine(self):
        task = json.loads((ROOT / "tasks" / "INFRA-001.json").read_text(encoding="utf-8"))
        review = task.get("review")
        self.assertIsInstance(review, dict)
        self.assertEqual("MEDIUM", review.get("riskClass"))
        self.assertEqual("CODE_REVIEW", review.get("reviewType"))
        self.assertEqual(["chatgpt", "chatgpt-secondary"], review.get("reviewerSlots"))
        self.assertEqual(3, task.get("maxRepairAttempts"))
        self.assertIn(".github/**", task.get("protectedPaths", []))
        self.assertIn("tasks/**", task.get("protectedPaths", []))
        self.assertIn("src/TiaV21Worker/**", task.get("protectedPaths", []))


if __name__ == "__main__":
    unittest.main()
