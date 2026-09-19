import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / ".github" / "workflows" / "agent-repair.yml"


class RepairRuntimeSourceTests(unittest.TestCase):
    def test_repair_establishes_trusted_main_boundary_before_candidate_orchestration(self):
        workflow = WORKFLOW.read_text(encoding="utf-8")

        checkout_marker = "ref: ${{ github.event.client_payload.branch }}"
        fetch_marker = 'git fetch origin main "$BRANCH"'
        pin_marker = "TRUSTED_MAIN_SHA=$(git rev-parse origin/main)"
        protected_diff_marker = (
            'git diff --name-only "$TRUSTED_MAIN_SHA"...HEAD | sort -u > '
            '"$RUNNER_TEMP/current-candidate-files.txt"'
        )
        trusted_policy_marker = (
            'git show "$TRUSTED_MAIN_SHA:agents/runtime/reviewer-policy.py" > '
            '"$RUNNER_TEMP/trusted-reviewer-policy.py"'
        )
        policy_invoke_marker = (
            'python3 "$RUNNER_TEMP/trusted-reviewer-policy.py" authorize-slot'
        )
        runtime_materialize_marker = (
            'git show "$TRUSTED_MAIN_SHA:agents/runtime/run-coder.sh" > '
            '"$RUNNER_TEMP/trusted-run-coder.sh"'
        )
        runtime_invoke_marker = 'bash "$RUNNER_TEMP/trusted-run-coder.sh"'

        self.assertIn(checkout_marker, workflow)
        self.assertIn(fetch_marker, workflow)
        self.assertIn(pin_marker, workflow)
        self.assertIn(protected_diff_marker, workflow)
        self.assertIn(trusted_policy_marker, workflow)
        self.assertIn(policy_invoke_marker, workflow)
        self.assertIn(runtime_materialize_marker, workflow)
        self.assertIn(runtime_invoke_marker, workflow)

        # Candidate protected orchestration must be rejected before any reviewer-policy
        # executable is invoked, and no branch-local reviewer-policy/run-coder invocation
        # may remain anywhere in the repair workflow.
        self.assertLess(workflow.index(fetch_marker), workflow.index(pin_marker))
        self.assertLess(workflow.index(pin_marker), workflow.index(protected_diff_marker))
        self.assertLess(workflow.index(protected_diff_marker), workflow.index(trusted_policy_marker))
        self.assertLess(workflow.index(trusted_policy_marker), workflow.index(policy_invoke_marker))
        self.assertNotIn("python3 agents/runtime/reviewer-policy.py", workflow)
        self.assertNotIn("bash agents/runtime/run-coder.sh", workflow)
        self.assertNotIn("git show origin/main:agents/runtime/run-coder.sh", workflow)

        # All trusted task/prompt/runtime reads are pinned to the captured immutable SHA.
        self.assertIn('git show "$TRUSTED_MAIN_SHA:$TASK_PATH"', workflow)
        self.assertIn('git show "$TRUSTED_MAIN_SHA:agents/prompts/coder.md"', workflow)
        self.assertIn('git show "$TRUSTED_MAIN_SHA:agents/prompts/repair.md"', workflow)
        self.assertIn('git diff --no-ext-diff --unified=80 "$TRUSTED_MAIN_SHA"...HEAD', workflow)
        self.assertIn('CODER_TRUSTED_GIT_REF: ${{ steps.context.outputs.trusted_main_sha }}', workflow)
        self.assertIn('echo "trusted_main_sha=$TRUSTED_MAIN_SHA" >> "$GITHUB_OUTPUT"', workflow)

        # A fresh trusted reviewer-policy copy is also used after model execution before
        # dispatching the next review request.
        self.assertIn(
            'git show "$TRUSTED_MAIN_SHA:agents/runtime/reviewer-policy.py" > '
            '"$RUNNER_TEMP/trusted-reviewer-policy-post.py"',
            workflow,
        )
        self.assertIn(
            'python3 "$RUNNER_TEMP/trusted-reviewer-policy-post.py" authorize-slot',
            workflow,
        )

        # The trusted runtime transport must not change where candidate edits land.
        self.assertIn('git diff --name-only "$BEFORE_SHA"', workflow)
        self.assertIn('git push origin "HEAD:$BRANCH"', workflow)
        self.assertIn('git config user.email "tia-automation-agent@users.noreply.github.com"', workflow)


if __name__ == "__main__":
    unittest.main()
