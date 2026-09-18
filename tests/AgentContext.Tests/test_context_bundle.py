import hashlib
import json
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BUILDER = ROOT / "agents" / "runtime" / "build-coder-context.py"
RUN_CODER = ROOT / "agents" / "runtime" / "run-coder.sh"


class AgentContextBundleTests(unittest.TestCase):
    def run_builder(self, prompt_text, expect=0):
        with tempfile.TemporaryDirectory() as temp:
            temp_path = Path(temp)
            prompt = temp_path / "work.md"
            output = temp_path / "bundle.md"
            manifest = temp_path / "manifest.json"
            prompt.write_text(prompt_text, encoding="utf-8")
            completed = subprocess.run(
                [
                    "python3",
                    str(BUILDER),
                    "--git-ref",
                    "HEAD",
                    "--prompt-input",
                    str(prompt),
                    "--output",
                    str(output),
                    "--manifest",
                    str(manifest),
                ],
                cwd=ROOT,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                check=False,
            )
            self.assertEqual(
                expect,
                completed.returncode,
                msg=f"stdout:\n{completed.stdout}\nstderr:\n{completed.stderr}",
            )
            return completed, output.read_text(encoding="utf-8") if output.exists() else "", (
                json.loads(manifest.read_text(encoding="utf-8")) if manifest.exists() else None
            )

    def test_bundle_contains_baseline_work_prompt_and_task_context(self):
        task = {
            "id": "CTX-TEST",
            "title": "Context bundle test",
            "contextFiles": ["docs/EXTERNAL_REVIEW_PROTOCOL.md"],
        }
        prompt = (
            "# Role: test\n\n"
            "# Current versioned task\n\n"
            "```json\n"
            + json.dumps(task)
            + "\n```\n"
        )
        _, bundle, manifest = self.run_builder(prompt)

        for path in (
            "AGENTS.md",
            "docs/PROJECT_STATE.md",
            "docs/ENGINEERING_RULES.md",
            "docs/AI_COLLABORATION_MODEL.md",
            "docs/EXTERNAL_REVIEW_PROTOCOL.md",
        ):
            self.assertIn(path, bundle)

        self.assertIn("BEGIN TRUSTED WORK PROMPT", bundle)
        self.assertIn("CTX-TEST", bundle)
        self.assertEqual("CTX-TEST", manifest["task"]["id"])
        self.assertEqual(
            ["docs/EXTERNAL_REVIEW_PROTOCOL.md"],
            [item["path"] for item in manifest["taskContextFiles"]],
        )
        self.assertEqual(40, len(manifest["trustedCommit"]))
        self.assertEqual(64, len(manifest["outputSha256"]))

        trusted_bytes = subprocess.check_output(
            ["git", "show", "HEAD:docs/EXTERNAL_REVIEW_PROTOCOL.md"], cwd=ROOT
        )
        self.assertEqual(
            hashlib.sha256(trusted_bytes).hexdigest(),
            manifest["taskContextFiles"][0]["sha256"],
        )

    def test_prompt_without_task_still_gets_baseline_context(self):
        _, bundle, manifest = self.run_builder("# Current GitHub Issue\n\nDo a bounded change.\n")
        self.assertIn("AGENTS.md", bundle)
        self.assertIn("Do a bounded change.", bundle)
        self.assertEqual([], manifest["taskContextFiles"])
        self.assertNotIn("task", manifest)

    def test_context_path_traversal_is_rejected(self):
        task = {"id": "CTX-BAD", "contextFiles": ["../outside.txt"]}
        prompt = "# Current versioned task\n\n```json\n" + json.dumps(task) + "\n```\n"
        completed, _, _ = self.run_builder(prompt, expect=2)
        self.assertIn("repository-relative", completed.stderr)

    def test_malformed_context_files_is_rejected(self):
        task = {"id": "CTX-BAD", "contextFiles": "docs/PROJECT_STATE.md"}
        prompt = "# Current versioned task\n\n```json\n" + json.dumps(task) + "\n```\n"
        completed, _, _ = self.run_builder(prompt, expect=2)
        self.assertIn("contextFiles must be an array", completed.stderr)

    def test_run_coder_enriches_once_before_both_providers(self):
        script = RUN_CODER.read_text(encoding="utf-8")
        builder_index = script.index("build-coder-context.py")
        openrouter_index = script.index("# Primary coding provider")
        deepseek_index = script.index("# Paid fallback")
        self.assertLess(builder_index, openrouter_index)
        self.assertLess(builder_index, deepseek_index)
        self.assertIn('PROMPT_PATH="$TRUSTED_CONTEXT_PROMPT"', script)
        self.assertEqual(2, script.count('"$(cat "$PROMPT_PATH")"'))
        self.assertIn("context_bundle:$context_bundle", script)


if __name__ == "__main__":
    unittest.main()
