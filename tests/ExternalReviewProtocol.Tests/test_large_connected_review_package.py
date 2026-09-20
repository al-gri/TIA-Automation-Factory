import re
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

    def test_normative_protocol_binds_narrow_exception(self):
        self.assertIn("one narrow transport exception for the connected primary reviewer", self.protocol)
        self.assertIn("`reviewerSlot=chatgpt`", self.protocol)
        self.assertIn("full exact diff byte count and SHA-256", self.protocol)
        self.assertIn("inspect the exact immutable candidate directly in GitHub", self.protocol)
        self.assertIn("does **not** apply to `chatgpt-secondary`", self.protocol)
        self.assertIn("remain fail-closed", self.protocol)


if __name__ == "__main__":
    unittest.main()
