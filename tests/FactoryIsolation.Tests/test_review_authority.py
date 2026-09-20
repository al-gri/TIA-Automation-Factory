import hashlib
import json
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HELPER = ROOT / "agents" / "runtime" / "review-authority.py"


class ReviewAuthorityTests(unittest.TestCase):
    def run_helper(self, *args, expect=0):
        completed = subprocess.run(
            ["python3", str(HELPER), *map(str, args)],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
        self.assertEqual(expect, completed.returncode, completed.stderr)
        return completed

    def git(self, cwd, *args, expect=0):
        completed = subprocess.run(
            ["git", *map(str, args)],
            cwd=cwd,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
        )
        self.assertEqual(
            expect,
            completed.returncode,
            msg=f"git {' '.join(map(str, args))}\nstdout:\n{completed.stdout}\nstderr:\n{completed.stderr}",
        )
        return completed

    def configure_identity(self, repo):
        self.git(repo, "config", "user.name", "Factory Isolation Test")
        self.git(repo, "config", "user.email", "factory-isolation@example.invalid")

    def test_exact_base_task_path_and_hash_validate(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            task = root / "task.json"
            task.write_text(json.dumps({"id": "T-1", "review": {"riskClass": "HIGH", "reviewType": "CODE_REVIEW"}}), encoding="utf-8")
            base = "a" * 40
            state = {
                "baseSha": base,
                "taskPath": "tasks/T-1.json",
                "taskSha256": hashlib.sha256(task.read_bytes()).hexdigest(),
                "taskId": "T-1",
                "candidateSha": "b" * 40,
            }
            state_path = root / "state.json"
            state_path.write_text(json.dumps(state), encoding="utf-8")
            self.run_helper(
                "validate", "--state", state_path, "--current-base-sha", base,
                "--task-path", "tasks/T-1.json", "--task-file", task,
            )

    def test_same_identity_but_changed_task_policy_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            original = root / "original.json"
            original.write_text(json.dumps({
                "id": "T-1",
                "review": {"riskClass": "HIGH", "reviewType": "CODE_REVIEW"},
                "candidatePolicy": {"allowedPaths": ["src/A.cs"]},
            }), encoding="utf-8")
            base = "a" * 40
            state = {
                "baseSha": base,
                "taskPath": "tasks/T-1.json",
                "taskSha256": hashlib.sha256(original.read_bytes()).hexdigest(),
                "taskId": "T-1",
                "candidateSha": "b" * 40,
            }
            state_path = root / "state.json"
            state_path.write_text(json.dumps(state), encoding="utf-8")

            changed = root / "changed.json"
            changed.write_text(json.dumps({
                "id": "T-1",
                "review": {"riskClass": "HIGH", "reviewType": "CODE_REVIEW"},
                "candidatePolicy": {"allowedPaths": ["src/**"]},
            }), encoding="utf-8")
            completed = self.run_helper(
                "validate", "--state", state_path, "--current-base-sha", base,
                "--task-path", "tasks/T-1.json", "--task-file", changed, expect=2,
            )
            self.assertIn("stale continuation task/policy hash", completed.stderr)

    def test_main_movement_is_rejected_even_when_task_blob_is_same(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            task = root / "task.json"
            task.write_text(json.dumps({"id": "T-1"}), encoding="utf-8")
            state = {
                "baseSha": "a" * 40,
                "taskPath": "tasks/T-1.json",
                "taskSha256": hashlib.sha256(task.read_bytes()).hexdigest(),
                "taskId": "T-1",
                "candidateSha": "b" * 40,
            }
            state_path = root / "state.json"
            state_path.write_text(json.dumps(state), encoding="utf-8")
            completed = self.run_helper(
                "validate", "--state", state_path, "--current-base-sha", "c" * 40,
                "--task-path", "tasks/T-1.json", "--task-file", task, expect=2,
            )
            self.assertIn("stale continuation base", completed.stderr)

    def test_continuation_rejects_main_move_between_dispatch_and_start(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            task = root / "task.json"
            task.write_text(json.dumps({"id": "T-1", "candidatePolicy": {"allowedPaths": ["src/A.cs"]}}), encoding="utf-8")
            task_hash = hashlib.sha256(task.read_bytes()).hexdigest()
            completed = self.run_helper(
                "verify-continuation",
                "--bound-base-sha", "a" * 40,
                "--live-main-sha", "c" * 40,
                "--task-path", "tasks/T-1.json",
                "--task-sha256", task_hash,
                "--task-file", task,
                "--task-id", "T-1",
                expect=2,
            )
            self.assertIn("stale continuation base", completed.stderr)

    def test_continuation_rejects_task_policy_change_after_dispatch(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            original = root / "original.json"
            original.write_text(json.dumps({"id": "T-1", "candidatePolicy": {"allowedPaths": ["src/A.cs"]}}), encoding="utf-8")
            bound_hash = hashlib.sha256(original.read_bytes()).hexdigest()
            changed = root / "changed.json"
            changed.write_text(json.dumps({"id": "T-1", "candidatePolicy": {"allowedPaths": ["src/B.cs"]}}), encoding="utf-8")
            completed = self.run_helper(
                "verify-continuation",
                "--bound-base-sha", "a" * 40,
                "--live-main-sha", "a" * 40,
                "--task-path", "tasks/T-1.json",
                "--task-sha256", bound_hash,
                "--task-file", changed,
                "--task-id", "T-1",
                expect=2,
            )
            self.assertIn("stale continuation task/policy hash", completed.stderr)

    def test_atomic_repair_push_rejects_main_move_after_early_check(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            remote = root / "remote.git"
            seed = root / "seed"
            publisher = root / "publisher"
            racer = root / "racer"

            self.git(root, "init", "--bare", remote)
            self.git(root, "init", seed)
            self.configure_identity(seed)
            (seed / "authority.txt").write_text("base\n", encoding="utf-8")
            self.git(seed, "add", "authority.txt")
            self.git(seed, "commit", "-m", "base")
            self.git(seed, "branch", "-M", "main")
            self.git(seed, "remote", "add", "origin", remote)
            self.git(seed, "push", "-u", "origin", "main")
            base_sha = self.git(seed, "rev-parse", "HEAD").stdout.strip()
            self.git(remote, "symbolic-ref", "HEAD", "refs/heads/main")

            self.git(seed, "checkout", "-b", "agent/task")
            (seed / "candidate.txt").write_text("candidate\n", encoding="utf-8")
            self.git(seed, "add", "candidate.txt")
            self.git(seed, "commit", "-m", "candidate")
            candidate_sha = self.git(seed, "rev-parse", "HEAD").stdout.strip()
            self.git(seed, "push", "-u", "origin", "agent/task")

            self.git(root, "clone", "--branch", "agent/task", remote, publisher)
            self.configure_identity(publisher)
            (publisher / "candidate.txt").write_text("candidate repaired\n", encoding="utf-8")
            self.git(publisher, "add", "candidate.txt")
            self.git(publisher, "commit", "-m", "repair prepared")

            # Simulate main advancing after an earlier local freshness check but
            # before the privileged publication network action.
            self.git(root, "clone", "--branch", "main", remote, racer)
            self.configure_identity(racer)
            (racer / "authority.txt").write_text("moved\n", encoding="utf-8")
            self.git(racer, "add", "authority.txt")
            self.git(racer, "commit", "-m", "move main")
            self.git(racer, "push", "origin", "main")

            failed = self.git(
                publisher,
                "push",
                "--atomic",
                f"--force-with-lease=refs/heads/main:{base_sha}",
                f"--force-with-lease=refs/heads/agent/task:{candidate_sha}",
                "origin",
                f"{base_sha}:refs/heads/main",
                "HEAD:refs/heads/agent/task",
                expect=1,
            )
            self.assertTrue(
                "stale info" in failed.stderr.lower()
                or "rejected" in failed.stderr.lower()
                or "atomic push failed" in failed.stderr.lower(),
                failed.stderr,
            )
            remote_candidate = self.git(remote, "rev-parse", "refs/heads/agent/task").stdout.strip()
            remote_main = self.git(remote, "rev-parse", "refs/heads/main").stdout.strip()
            self.assertEqual(candidate_sha, remote_candidate)
            self.assertNotEqual(base_sha, remote_main)


if __name__ == "__main__":
    unittest.main()
