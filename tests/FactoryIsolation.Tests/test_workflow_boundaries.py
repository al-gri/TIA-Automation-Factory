import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WF = ROOT / ".github" / "workflows"


class FactoryIsolationWorkflowTests(unittest.TestCase):
    def read(self, name):
        return (WF / name).read_text(encoding="utf-8")

    def step_block(self, text, name, next_name=None):
        start = text.index(f"- name: {name}")
        end = text.index(f"- name: {next_name}", start) if next_name else len(text)
        return text[start:end]

    def assert_final_check_before_action(self, block, action):
        action_index = block.index(action)
        prefix = block[:action_index]
        fetch_index = prefix.rfind('git fetch --no-tags origin main')
        verify_index = prefix.rfind('verify-continuation')
        self.assertGreater(fetch_index, -1, block)
        self.assertGreater(verify_index, fetch_index, block)
        self.assertLess(verify_index, action_index, block)
        between = block[verify_index:action_index]
        self.assertNotIn('git commit', between)
        self.assertNotIn('gh auth setup-git', between)
        self.assertNotIn('jq -n', between)
        self.assertNotIn("cat >", between)

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

    def test_repair_is_bound_to_reviewed_base_task_hash_at_start_publish_and_push(self):
        text = self.read("agent-repair.yml")
        self.assertIn("BOUND_BASE_SHA: ${{ github.event.client_payload.base_sha }}", text)
        self.assertIn("TASK_SHA256: ${{ github.event.client_payload.task_sha256 }}", text)
        self.assertIn('TRUSTED_MAIN_SHA="$BOUND_BASE_SHA"', text)
        self.assertGreaterEqual(text.count("verify-continuation"), 2)
        self.assertGreaterEqual(text.count("git fetch --no-tags origin main"), 3)
        self.assertIn("Trusted main moved after external review", text)
        self.assertIn("Trusted main moved before repair publication", text)
        self.assertIn("Trusted main moved at final repair publication boundary", text)
        self.assertIn("review-authority.py", text)

    def test_repair_push_uses_server_enforced_atomic_leases_at_final_boundary(self):
        text = self.read("agent-repair.yml")
        block = self.step_block(
            text,
            "Commit and push repair from clean publisher",
            "Record repair and dispatch fresh exact-SHA review",
        )
        commit_index = block.index('git commit -m')
        auth_index = block.index('gh auth setup-git')
        fetch_index = block.index('git fetch --no-tags origin main', auth_index)
        branch_check_index = block.index('git ls-remote origin "refs/heads/$BRANCH"', fetch_index)
        push_index = block.index('git push --atomic', branch_check_index)
        self.assertLess(commit_index, auth_index)
        self.assertLess(auth_index, fetch_index)
        self.assertLess(fetch_index, branch_check_index)
        self.assertLess(branch_check_index, push_index)
        self.assertIn('--force-with-lease="refs/heads/main:$TRUSTED_MAIN_SHA"', block)
        self.assertIn('--force-with-lease="refs/heads/$BRANCH:$CANDIDATE_SHA"', block)
        self.assertIn('"$TRUSTED_MAIN_SHA:refs/heads/main"', block)
        self.assertIn('"HEAD:refs/heads/$BRANCH"', block)

    def test_review_request_reconstructs_authority_only_in_clean_publisher(self):
        text = self.read("external-review-request.yml")
        self.assertIn("  build-review-package:", text)
        self.assertIn("  publish-review-package:", text)
        build = text[text.index("  build-review-package:"):text.index("  publish-review-package:")]
        self.assertNotIn("pull-requests: write", build)
        self.assertNotIn("gh pr comment", build)
        self.assertNotIn("review-request.md", build)
        self.assertNotIn("candidate.diff", build)
        self.assertIn("Upload bounded evidence only", build)

        publish = text[text.index("  publish-review-package:"):]
        self.assertIn("pull-requests: write", publish)
        self.assertIn("gh pr comment", publish)
        self.assertNotIn("dotnet test", publish)
        self.assertIn("Reconstruct and revalidate authoritative review state", publish)
        self.assertIn("Build self-contained package in clean publisher", publish)
        self.assertIn("Build source context from immutable Git objects", publish)
        self.assertIn("candidate.diff", publish)
        self.assertIn("review-request.md", publish)
        self.assertIn("Trusted main moved after evidence collection", publish)
        self.assertIn("non-authoritative Linux evidence", publish)
        self.assertIn("escaped external-review-state marker", publish)
        self.assertIn('test "$(jq -r .headRefOid "$RUNNER_TEMP/pr.json")" = "$CANDIDATE_SHA"', publish)

    def test_response_propagates_bound_authority_to_every_continuation(self):
        text = self.read("external-review-response.yml")
        self.assertIn("taskSha256", text)
        self.assertIn("baseSha", text)
        self.assertIn("trusted-review-authority", text)
        self.assertIn("verify-continuation", text)
        self.assertGreaterEqual(text.count("git fetch --no-tags origin main"), 7)
        self.assertIn('--arg base_sha "$BASE_SHA"', text)
        self.assertIn('--arg task_sha256 "$TASK_SHA256"', text)
        self.assertIn('base_sha:$base_sha', text)
        self.assertIn('task_sha256:$task_sha256', text)
        self.assertIn('event_type:"candidate-validation"', text)
        self.assertIn('event_type:"candidate-repair"', text)

    def test_response_rechecks_authority_at_each_privileged_action_boundary(self):
        text = self.read("external-review-response.yml")

        publish = self.step_block(
            text,
            "Publish validated review state",
            "Continue approved LOW/MEDIUM candidate to trusted validation",
        )
        self.assertLess(publish.index("review-result-comment.md"), publish.rindex("verify-continuation"))
        self.assert_final_check_before_action(publish, 'gh pr comment "$PR_NUMBER" --body-file')

        validation = self.step_block(
            text,
            "Continue approved LOW/MEDIUM candidate to trusted validation",
            "Record approved HIGH candidate for delegated merge decision",
        )
        self.assertLess(validation.index("approved-dispatch.json"), validation.index("verify-continuation"))
        self.assert_final_check_before_action(validation, 'gh api --method POST')
        second_comment = validation.rindex('gh pr comment "$PR_NUMBER"')
        verify_before_comment = validation.rfind("verify-continuation", 0, second_comment)
        fetch_before_comment = validation.rfind('git fetch --no-tags origin main', 0, second_comment)
        self.assertGreater(verify_before_comment, fetch_before_comment)

        high = self.step_block(
            text,
            "Record approved HIGH candidate for delegated merge decision",
            "Dispatch bounded repair after changes required",
        )
        self.assertLess(high.index("high-approval-comment.md"), high.index("verify-continuation"))
        self.assert_final_check_before_action(high, 'gh pr comment "$PR_NUMBER" --body-file')

        repair = self.step_block(text, "Dispatch bounded repair after changes required")
        dispatch_index = repair.index('gh api --method POST')
        dispatch_verify = repair.rfind("verify-continuation", 0, dispatch_index)
        dispatch_fetch = repair.rfind('git fetch --no-tags origin main', 0, dispatch_index)
        self.assertGreater(dispatch_verify, dispatch_fetch)
        comment_index = repair.rindex('gh pr comment "$PR_NUMBER" --body-file')
        comment_verify = repair.rfind("verify-continuation", 0, comment_index)
        comment_fetch = repair.rfind('git fetch --no-tags origin main', 0, comment_index)
        self.assertGreater(comment_verify, comment_fetch)
        self.assertGreater(comment_verify, dispatch_index)

    def test_candidate_validation_is_linux_only_read_only_and_bound(self):
        text = self.read("candidate-validation.yml")
        self.assertNotIn("self-hosted", text)
        self.assertNotIn("TiaV21Worker", text)
        self.assertNotIn("contents: write", text)
        self.assertNotIn("pull-requests: write", text)
        self.assertNotIn("gh pr comment", text)
        self.assertIn("Candidate Validation is Linux-only", text)
        self.assertIn("validate-current", text)
        self.assertIn("BOUND_BASE_SHA: ${{ github.event.client_payload.base_sha }}", text)
        self.assertIn("TASK_SHA256: ${{ github.event.client_payload.task_sha256 }}", text)
        self.assertIn("verify-continuation", text)
        self.assertIn("Trusted main moved after external review", text)


if __name__ == "__main__":
    unittest.main()
