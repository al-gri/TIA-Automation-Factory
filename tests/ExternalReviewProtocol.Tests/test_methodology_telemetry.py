import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github" / "workflows" / "methodology-telemetry.yml"
AGENTS = ROOT / "AGENTS.md"
METHODOLOGY = ROOT / "docs" / "DEVELOPMENT_METHODOLOGY.md"
JOURNAL = ROOT / "docs" / "METHODOLOGY_JOURNAL.md"


class MethodologyTelemetryTests(unittest.TestCase):
    def test_methodology_documents_exist_and_are_in_operating_contract(self):
        self.assertTrue(METHODOLOGY.exists())
        self.assertTrue(JOURNAL.exists())
        agents = AGENTS.read_text(encoding="utf-8")
        self.assertIn("docs/DEVELOPMENT_METHODOLOGY.md", agents)
        self.assertIn("docs/METHODOLOGY_JOURNAL.md", agents)
        self.assertIn("issue #25", agents)
        self.assertIn("methodology checkpoint", agents.lower())

    def test_telemetry_workflow_is_append_only_issue_logging(self):
        text = WORKFLOW.read_text(encoding="utf-8")
        self.assertIn("name: Methodology Telemetry", text)
        self.assertIn("workflow_run:", text)
        self.assertIn("pull_request:", text)
        self.assertIn("types: [closed]", text)
        self.assertIn("issues: write", text)
        self.assertIn("contents: read", text)
        self.assertNotIn("contents: write", text)
        self.assertNotIn("self-hosted", text)
        self.assertNotIn("actions/checkout", text)
        self.assertIn("METHODOLOGY_ISSUE: '25'", text)
        self.assertIn("methodology-event-v1", text)
        self.assertIn("gh issue comment", text)

    def test_telemetry_covers_material_workflow_classes(self):
        text = WORKFLOW.read_text(encoding="utf-8")
        expected = [
            "CI",
            "Autonomous Agent",
            "Autonomous Repair",
            "External Review Request",
            "External Review Response",
            "Candidate Validation",
            "TIA V21 End-to-End",
            "TIA V21 Verification",
        ]
        for name in expected:
            self.assertIn(f"- {name}", text)

    def test_methodology_keeps_raw_evidence_separate_from_policy(self):
        text = METHODOLOGY.read_text(encoding="utf-8")
        self.assertIn("Raw telemetry is **evidence, not policy**", text)
        self.assertIn("Observation", text)
        self.assertIn("Hypothesis", text)
        self.assertIn("Accepted rule", text)
        self.assertIn("At every logical milestone", text)


if __name__ == "__main__":
    unittest.main()
