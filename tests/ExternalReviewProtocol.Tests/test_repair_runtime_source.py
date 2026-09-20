import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / '.github' / 'workflows' / 'agent-repair.yml'


class RepairRuntimeSourceTests(unittest.TestCase):
    def test_repair_materializes_complete_trusted_helper_chain(self):
        workflow = WORKFLOW.read_text(encoding='utf-8')
        self.assertIn('BOUND_BASE_SHA: ${{ github.event.client_payload.base_sha }}', workflow)
        self.assertIn('TASK_SHA256: ${{ github.event.client_payload.task_sha256 }}', workflow)
        self.assertIn('TRUSTED_MAIN_SHA="$BOUND_BASE_SHA"', workflow)
        self.assertIn('agents/runtime/run-coder.sh', workflow)
        self.assertIn('agents/runtime/build-coder-context.py', workflow)
        self.assertIn('agents/runtime/candidate-patch.py', workflow)
        self.assertIn('agents/runtime/reviewer-policy.py', workflow)
        self.assertIn('agents/runtime/review-authority.py', workflow)
        self.assertIn('"$RUNNER_TEMP/trusted-runtime/run-coder.sh"', workflow)
        self.assertIn('"$RUNNER_TEMP/trusted-runtime/candidate-patch.py" validate-current', workflow)
        self.assertIn('"$RUNNER_TEMP/trusted-runtime/review-authority.py" verify-continuation', workflow)
        self.assertIn('cp "$RUNNER_TEMP/trusted-runtime/build-coder-context.py" agents/runtime/build-coder-context.py', workflow)

    def test_repair_publication_is_fresh_and_separate(self):
        workflow = WORKFLOW.read_text(encoding='utf-8')
        repair_start = workflow.index('  repair:')
        publish_start = workflow.index('  publish-repair:')
        repair = workflow[repair_start:publish_start]
        publish = workflow[publish_start:]
        self.assertNotIn('contents: write', repair)
        self.assertNotIn('git push', repair)
        self.assertNotIn('gh pr comment', repair)
        self.assertIn('contents: write', publish)
        self.assertIn('Fresh exact candidate checkout', publish)
        self.assertIn('git ls-remote origin "refs/heads/$BRANCH"', publish)
        self.assertIn('--scope-base "$TRUSTED_MAIN_SHA"', publish)
        self.assertIn('Trusted main moved before repair publication', publish)
        self.assertIn('Trusted main moved immediately before repair push', publish)
        self.assertNotIn('git add -A', workflow)


if __name__ == '__main__':
    unittest.main()
