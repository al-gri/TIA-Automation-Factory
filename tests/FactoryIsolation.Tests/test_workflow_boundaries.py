import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WF = ROOT / ".github" / "workflows"


class FactoryIsolationWorkflowTests(unittest.TestCase):
    def read(self, name):
        return (WF / name).read_text(encoding="utf-8")

    def test_agent_splits_candidate_execution_from_write_publisher(self):
        text = self.read("agent.yml")
        self.assertIn("permissions:\n  contents: read", text)
        self.assertIn("  implement:", text)
        self.assertIn("  publish:", text)
        self.assertIn("needs: implement", text)
        self.assertIn("name: candidate-patch-${{ github.run_id }}", text)
        self.assertIn("contents: write", text[text.index("  publish:"):])
        implement = text[text.index("  implement:"):text.index("  publish:")]
        self.assertNotIn("contents: write", implement)
        self.assertNotIn("pull-requests: write", implement)
        self.assertNotIn("gh pr create", implement)
        self.assertNotIn("git push", implement)
        self.assertNotIn("git add -A", text)
        self.assertIn("candidatePolicy.allowedPaths", text)
        self.assertIn("Issue-mode publication is disabled", text)

    def test_repair_splits_execution_from_publication_and_checks_branch_head(self):
        text = self.read("agent-repair.yml")
        self.assertIn("  repair:", text)
        self.assertIn("  publish-repair:", text)
        repair = text[text.index("  repair:"):text.index("  publish-repair:")]
        self.assertNotIn("contents: write", repair)
        self.assertNotIn("git push", repair)
        self.assertNotIn("gh pr comment", repair)
        self.assertNotIn("git add -A", text)
        self.assertIn('git ls-remote origin "refs/heads/$BRANCH"', text)
        self.assertIn("--scope-base \"$TRUSTED_MAIN_SHA\"", text)
        self.assertIn("build-coder-context.py", text)
        self.assertIn("trusted-runtime", text)

    def test_review_request_executes_candidate_only_in_read_only_job(self):
        text = self.read("external-review-request.yml")
        self.assertIn("  build-review-package:", text)
        self.assertIn("  publish-review-package:", text)
        build = text[text.index("  build-review-package:"):text.index("  publish-review-package:")]
        self.assertNotIn("pull-requests: write", build)
        self.assertNotIn("gh pr comment", build)
        publish = text[text.index("  publish-review-package:"):]
        self.assertIn("pull-requests: write", publish)
        self.assertIn("gh pr comment", publish)
        self.assertNotIn("dotnet test", publish)
        self.assertIn("test \"$(jq -r .headRefOid \"$RUNNER_TEMP/pr.json\")\" = \"$CANDIDATE_SHA\"", publish)

    def test_candidate_validation_is_linux_only_and_read_only(self):
        text = self.read("candidate-validation.yml")
        self.assertNotIn("self-hosted", text)
        self.assertNotIn("TiaV21Worker", text)
        self.assertNotIn("contents: write", text)
        self.assertNotIn("pull-requests: write", text)
        self.assertNotIn("gh pr comment", text)
        self.assertIn("Candidate Validation is Linux-only", text)
        self.assertIn("validate-current", text)


if __name__ == "__main__":
    unittest.main()
