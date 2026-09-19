import json
import os
from pathlib import Path
import stat
import subprocess
import tempfile
import unittest


REPO_ROOT = Path(__file__).resolve().parents[2]
RUN_CODER = REPO_ROOT / "agents" / "runtime" / "run-coder.sh"


class RunCoderPromptTransportTests(unittest.TestCase):
    def test_large_prompt_is_streamed_to_opencode_stdin(self):
        with tempfile.TemporaryDirectory() as raw_temp:
            temp = Path(raw_temp)
            prompt = temp / "prompt.md"
            audit = temp / "audit.json"
            capture_stdin = temp / "captured-stdin.bin"
            capture_argv = temp / "captured-argv.bin"
            fake_bin = temp / "bin"
            fake_bin.mkdir()

            marker = "CODER-RUNTIME-LARGE-PROMPT-END"
            prompt.write_text(("x" * (220 * 1024)) + "\n" + marker + "\n", encoding="utf-8")

            fake_opencode = fake_bin / "opencode"
            fake_opencode.write_text(
                "#!/usr/bin/env bash\n"
                "set -euo pipefail\n"
                "printf '%s\\0' \"$@\" > \"$FAKE_OPENCODE_ARGV\"\n"
                "cat > \"$FAKE_OPENCODE_STDIN\"\n"
                "printf '%s\\n' '{\"type\":\"step_finish\",\"part\":{\"tokens\":{\"input\":1,\"output\":1,\"reasoning\":0,\"cache\":{\"read\":0,\"write\":0}},\"cost\":0}}'\n",
                encoding="utf-8",
            )
            fake_opencode.chmod(fake_opencode.stat().st_mode | stat.S_IXUSR)

            env = os.environ.copy()
            env.update(
                {
                    "PATH": str(fake_bin) + os.pathsep + env.get("PATH", ""),
                    "RUNNER_TEMP": str(temp),
                    "OPENROUTER_API_KEY": "test-key",
                    "DEEPSEEK_API_KEY": "",
                    "OPENROUTER_TIMEOUT_MINUTES": "1",
                    "CODER_TRUSTED_GIT_REF": "HEAD",
                    "CODER_TRUSTED_TASK_PATH": "",
                    "GITHUB_EVENT_PATH": "",
                    "GITHUB_EVENT_NAME": "",
                    "FAKE_OPENCODE_STDIN": str(capture_stdin),
                    "FAKE_OPENCODE_ARGV": str(capture_argv),
                }
            )

            completed = subprocess.run(
                ["bash", str(RUN_CODER), str(prompt), str(audit)],
                cwd=REPO_ROOT,
                env=env,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                timeout=30,
                check=False,
            )

            self.assertEqual(
                0,
                completed.returncode,
                msg=f"stdout:\n{completed.stdout}\nstderr:\n{completed.stderr}",
            )

            streamed = capture_stdin.read_bytes()
            self.assertGreater(len(streamed), 220 * 1024)
            self.assertIn(marker.encode("utf-8"), streamed)

            argv = [part.decode("utf-8") for part in capture_argv.read_bytes().split(b"\0") if part]
            self.assertIn("run", argv)
            self.assertIn("--model", argv)
            self.assertNotIn(marker, "\n".join(argv))
            self.assertLess(max(map(len, argv)), 4096)

            audit_data = json.loads(audit.read_text(encoding="utf-8"))
            self.assertEqual("openrouter", audit_data["selected_provider"])
            self.assertEqual("success", audit_data["final_outcome"])


if __name__ == "__main__":
    unittest.main()
