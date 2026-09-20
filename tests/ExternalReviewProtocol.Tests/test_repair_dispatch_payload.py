import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


class RepairDispatchPayloadTests(unittest.TestCase):
    def test_repair_dispatch_uses_bounded_nested_review_metadata(self):
        response = (ROOT / ".github" / "workflows" / "external-review-response.yml").read_text(encoding="utf-8")
        repair = (ROOT / ".github" / "workflows" / "agent-repair.yml").read_text(encoding="utf-8")

        expected_payload = "client_payload:{review_source:\"external-review\",pr_number:$pr,candidate_sha:$candidate_sha,branch:$branch,base_sha:$base_sha,task_path:$task_path,task_sha256:$task_sha256,attempt:$attempt,review:{type:$review_type,risk_class:$risk_class,reviewer_slot:$reviewer_slot}}"
        self.assertIn(expected_payload, response)
        self.assertNotIn("validation_run_id:0", response)

        self.assertIn("github.event.client_payload.review.type", repair)
        self.assertIn("github.event.client_payload.review.risk_class", repair)
        self.assertIn("github.event.client_payload.review.reviewer_slot", repair)
        self.assertNotIn("github.event.client_payload.review_type", repair)
        self.assertNotIn("github.event.client_payload.risk_class", repair)
        self.assertNotIn("github.event.client_payload.reviewer_slot", repair)

    def test_repair_dispatch_has_nine_top_level_properties(self):
        response = (ROOT / ".github" / "workflows" / "external-review-response.yml").read_text(encoding="utf-8")
        marker = "client_payload:{review_source:\"external-review\""
        start = response.index(marker) + len("client_payload:{")
        end = response.index("}}}'", start)
        body = response[start:end + 1]
        depth = 0
        count = 1
        for char in body:
            if char == "{":
                depth += 1
            elif char == "}":
                depth -= 1
            elif char == "," and depth == 0:
                count += 1
        self.assertEqual(9, count)
        self.assertLessEqual(count, 10)


if __name__ == "__main__":
    unittest.main()
