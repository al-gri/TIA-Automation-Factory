import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github" / "workflows" / "agent-repair.yml"


class RepairRuntimeSourceTests(unittest.TestCase):
    def test_repair_executes_trusted_main_run_coder_in_candidate_worktree(self):
        workflow = WORKFLOW.read_text(encoding="utf-8")

        checkout_marker = "ref: ${{ github.event.client_payload.branch }}"
        fetch_marker = 'git fetch origin main "$BRANCH"'
        materialize_marker = (
            'git show origin/main:agents/runtime/run-coder.sh > '
            '"$RUNNER_TEMP/trusted-run-coder.sh"'
        )
        invoke_marker = 'bash "$RUNNER_TEMP/trusted-run-coder.sh"'

        self.assertIn(checkout_marker, workflow)
        self.assertIn(fetch_marker, workflow)
        self.assertIn(materialize_marker, workflow)
        self.assertIn('chmod +x "$RUNNER_TEMP/trusted-run-coder.sh"', workflow)
        self.assertIn(invoke_marker, workflow)
        self.assertNotIn("bash agents/runtime/run-coder.sh", workflow)

        self.assertLess(workflow.index(fetch_marker), workflow.index(materialize_marker))
        self.assertLess(workflow.index(materialize_marker), workflow.index(invoke_marker))

        # The trusted runtime transport must not change where candidate edits land.
        self.assertIn('git diff --name-only "$BEFORE_SHA"', workflow)
        self.assertIn('git push origin "HEAD:$BRANCH"', workflow)
        self.assertIn('git config user.email "tia-automation-agent@users.noreply.github.com"', workflow)


if __name__ == "__main__":
    unittest.main()
