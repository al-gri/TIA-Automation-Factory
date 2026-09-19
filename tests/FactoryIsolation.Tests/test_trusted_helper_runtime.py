import json
import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TRUSTED_RUNNER = ROOT / "agents" / "runtime" / "run-coder.sh"
TRUSTED_CONTEXT = ROOT / "agents" / "runtime" / "build-coder-context.py"


class TrustedHelperRuntimeTests(unittest.TestCase):
    def test_stale_candidate_context_helper_cannot_influence_trusted_runtime(self):
        with tempfile.TemporaryDirectory(prefix="trusted-helper-") as temp:
            root = Path(temp)
            repo = root / "repo"
            repo.mkdir()
            subprocess.run(["git", "init", "-q"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.name", "test"], cwd=repo, check=True)
            subprocess.run(["git", "config", "user.email", "test@example.invalid"], cwd=repo, check=True)

            (repo / "docs").mkdir()
            baseline = {
                "AGENTS.md": "trusted agents contract\n",
                "docs/PROJECT_STATE.md": "trusted project state\n",
                "docs/ENGINEERING_RULES.md": "trusted engineering rules\n",
                "docs/AI_COLLABORATION_MODEL.md": "trusted collaboration model\n",
            }
            for rel, text in baseline.items():
                path = repo / rel
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(text, encoding="utf-8")

            runtime = repo / "agents" / "runtime"
            runtime.mkdir(parents=True)
            stale_marker = repo / "stale-helper-ran.txt"
            (runtime / "build-coder-context.py").write_text(
                "from pathlib import Path\n"
                f"Path({str(stale_marker)!r}).write_text('STALE', encoding='utf-8')\n"
                "raise SystemExit(91)\n",
                encoding="utf-8",
            )
            subprocess.run(["git", "add", "."], cwd=repo, check=True)
            subprocess.run(["git", "commit", "-qm", "candidate baseline"], cwd=repo, check=True)
            head = subprocess.run(
                ["git", "rev-parse", "HEAD"], cwd=repo, text=True,
                stdout=subprocess.PIPE, check=True,
            ).stdout.strip()

            trusted_runtime = root / "trusted-runtime"
            trusted_runtime.mkdir()
            shutil.copy2(TRUSTED_RUNNER, trusted_runtime / "run-coder.sh")
            shutil.copy2(TRUSTED_CONTEXT, trusted_runtime / "build-coder-context.py")

            # Reproduce the workflow's trusted-helper substitution before run-coder executes.
            shutil.copy2(trusted_runtime / "build-coder-context.py", runtime / "build-coder-context.py")

            fake_bin = root / "bin"
            fake_bin.mkdir()
            fake_opencode = fake_bin / "opencode"
            fake_opencode.write_text(
                "#!/bin/sh\n"
                "printf '%s\\n' '{\"type\":\"step_finish\",\"part\":{\"tokens\":{\"input\":1,\"output\":1,\"reasoning\":0,\"cache\":{\"read\":0,\"write\":0}},\"cost\":0}}'\n"
                "exit 0\n",
                encoding="utf-8",
            )
            fake_opencode.chmod(0o755)

            prompt = root / "prompt.md"
            prompt.write_text("bounded test prompt\n", encoding="utf-8")
            audit = root / "audit.json"
            runner_temp = root / "runner-temp"
            runner_temp.mkdir()

            env = os.environ.copy()
            env.update({
                "PATH": str(fake_bin) + os.pathsep + env.get("PATH", ""),
                "RUNNER_TEMP": str(runner_temp),
                "OPENROUTER_API_KEY": "test-key",
                "DEEPSEEK_API_KEY": "",
                "CODER_TRUSTED_GIT_REF": head,
                "CODER_TRUSTED_TASK_PATH": "",
                "OPENROUTER_TIMEOUT_MINUTES": "1",
            })
            completed = subprocess.run(
                ["bash", str(trusted_runtime / "run-coder.sh"), str(prompt), str(audit)],
                cwd=repo,
                env=env,
                text=True,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
            )
            self.assertEqual(0, completed.returncode, completed.stderr)
            self.assertFalse(stale_marker.exists(), "candidate stale helper executed")

            data = json.loads(audit.read_text(encoding="utf-8"))
            self.assertEqual("openrouter", data["selected_provider"])
            self.assertEqual(head, data["context_bundle"]["trustedCommit"])
            context = (runner_temp / "coder-trusted-context.md").read_text(encoding="utf-8")
            self.assertIn("trusted agents contract", context)
            self.assertNotIn("STALE", context)


if __name__ == "__main__":
    unittest.main()
