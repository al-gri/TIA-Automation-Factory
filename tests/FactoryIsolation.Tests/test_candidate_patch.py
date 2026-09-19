import json
import os
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HELPER = ROOT / "agents" / "runtime" / "candidate-patch.py"


def git(repo: Path, *args: str, check=True):
    return subprocess.run(
        ["git", *args], cwd=repo, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=check
    )


def write_task(root: Path, allowed, allow_worker=False):
    task = {
        "id": "TEST-001",
        "candidatePolicy": {
            "allowedPaths": allowed,
            "allowTiaV21WorkerChanges": allow_worker,
        },
    }
    root.mkdir(parents=True, exist_ok=True)
    path = root / "task.json"
    path.write_text(json.dumps(task), encoding="utf-8")
    return path


class CandidatePatchTests(unittest.TestCase):
    def setUp(self):
        self.tmp = Path(tempfile.mkdtemp(prefix="candidate-patch-"))
        self.repo = self.tmp / "repo"
        self.repo.mkdir()
        git(self.repo, "init", "-q")
        git(self.repo, "config", "user.name", "test")
        git(self.repo, "config", "user.email", "test@example.invalid")
        (self.repo / "src").mkdir()
        (self.repo / "src" / "allowed.txt").write_text("base\n", encoding="utf-8")
        (self.repo / "other.txt").write_text("base\n", encoding="utf-8")
        git(self.repo, "add", "src/allowed.txt", "other.txt")
        git(self.repo, "commit", "-qm", "base")
        self.base = git(self.repo, "rev-parse", "HEAD").stdout.strip()

    def tearDown(self):
        shutil.rmtree(self.tmp)

    def run_helper(self, *args, repo=None, check=False):
        target = repo or self.repo
        return subprocess.run(
            ["python3", str(HELPER), *map(str, args)],
            cwd=target,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=check,
        )

    def test_prepare_and_fresh_publisher_apply_allowed_patch(self):
        task = write_task(self.tmp / "task-a", ["src/allowed.txt"])
        (self.repo / "src" / "allowed.txt").write_text("candidate\n", encoding="utf-8")
        patch = self.tmp / "candidate.patch"
        manifest = self.tmp / "manifest.json"
        result = self.run_helper("prepare", "--task", task, "--patch", patch, "--manifest", manifest)
        self.assertEqual(0, result.returncode, result.stderr)

        publisher = self.tmp / "publisher"
        git(self.repo, "clone", "-q", str(self.repo), str(publisher))
        git(publisher, "checkout", "-q", self.base)
        pub_task = write_task(self.tmp / "task-b", ["src/allowed.txt"])
        result = self.run_helper(
            "apply", "--task", pub_task, "--patch", patch, "--manifest", manifest,
            "--expected-head", self.base, "--scope-base", self.base, repo=publisher
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("candidate\n", git(publisher, "show", ":src/allowed.txt").stdout)
        staged = git(publisher, "diff", "--cached", "--name-only").stdout.splitlines()
        self.assertEqual(["src/allowed.txt"], staged)
        git(publisher, "switch", "-c", "publish-test")
        git(publisher, "config", "user.name", "publisher")
        git(publisher, "config", "user.email", "publisher@example.invalid")
        git(publisher, "commit", "-qm", "publish")
        self.assertEqual("candidate\n", git(publisher, "show", "HEAD:src/allowed.txt").stdout)

    def test_out_of_scope_byproduct_is_rejected_before_patch(self):
        task = write_task(self.tmp / "task-a", ["src/allowed.txt"])
        (self.repo / "src" / "allowed.txt").write_text("candidate\n", encoding="utf-8")
        cache = self.repo / "tests" / "x" / "__pycache__"
        cache.mkdir(parents=True)
        (cache / "leak.pyc").write_bytes(b"bytecode")
        result = self.run_helper(
            "prepare", "--task", task, "--patch", self.tmp / "p", "--manifest", self.tmp / "m"
        )
        self.assertNotEqual(0, result.returncode)
        self.assertIn("outside candidatePolicy.allowedPaths", result.stderr)

    def test_symlink_is_rejected(self):
        task = write_task(self.tmp / "task-a", ["src/**"])
        os.symlink("allowed.txt", self.repo / "src" / "link.txt")
        result = self.run_helper(
            "prepare", "--task", task, "--patch", self.tmp / "p", "--manifest", self.tmp / "m"
        )
        self.assertNotEqual(0, result.returncode)
        self.assertIn("unsupported git mode 120000", result.stderr)

    def test_candidate_git_hook_never_enters_patch(self):
        task = write_task(self.tmp / "task-a", ["src/allowed.txt"])
        hook = self.repo / ".git" / "hooks" / "pre-commit"
        hook.write_text("#!/bin/sh\nexit 99\n", encoding="utf-8")
        hook.chmod(0o755)
        (self.repo / "src" / "allowed.txt").write_text("candidate\n", encoding="utf-8")
        patch = self.tmp / "candidate.patch"
        manifest = self.tmp / "manifest.json"
        result = self.run_helper("prepare", "--task", task, "--patch", patch, "--manifest", manifest)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertNotIn(".git/hooks", patch.read_text(encoding="utf-8", errors="replace"))

    def test_existing_candidate_scope_is_positive_not_denylist(self):
        task = write_task(self.tmp / "task-a", ["src/allowed.txt"])
        (self.repo / "other.txt").write_text("bad candidate\n", encoding="utf-8")
        git(self.repo, "add", "other.txt")
        git(self.repo, "commit", "-qm", "out of scope")
        result = self.run_helper("validate-current", "--task", task, "--base", self.base)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("outside candidatePolicy.allowedPaths", result.stderr)

    def test_worker_scope_requires_explicit_worker_opt_in(self):
        task = write_task(self.tmp / "task-a", ["src/TiaV21Worker/Program.cs"], allow_worker=False)
        result = self.run_helper("validate-current", "--task", task, "--base", self.base)
        self.assertNotEqual(0, result.returncode)
        self.assertIn("allowTiaV21WorkerChanges=true", result.stderr)

    def test_tampered_patch_path_is_rejected_by_fresh_publisher(self):
        task = write_task(self.tmp / "task-a", ["src/allowed.txt"])
        (self.repo / "src" / "allowed.txt").write_text("candidate\n", encoding="utf-8")
        patch = self.tmp / "candidate.patch"
        manifest = self.tmp / "manifest.json"
        self.assertEqual(0, self.run_helper("prepare", "--task", task, "--patch", patch, "--manifest", manifest).returncode)
        raw = patch.read_text(encoding="utf-8")
        patch.write_text(raw.replace("src/allowed.txt", "other.txt"), encoding="utf-8")
        data = json.loads(manifest.read_text(encoding="utf-8"))
        import hashlib
        data["patchSha256"] = hashlib.sha256(patch.read_bytes()).hexdigest()
        data["changedPaths"] = ["other.txt"]
        manifest.write_text(json.dumps(data), encoding="utf-8")

        publisher = self.tmp / "publisher"
        git(self.repo, "clone", "-q", str(self.repo), str(publisher))
        git(publisher, "checkout", "-q", self.base)
        pub_task = write_task(self.tmp / "task-b", ["src/allowed.txt"])
        result = self.run_helper(
            "apply", "--task", pub_task, "--patch", patch, "--manifest", manifest,
            "--expected-head", self.base, "--scope-base", self.base, repo=publisher
        )
        self.assertNotEqual(0, result.returncode)

    def test_allowed_deletion_is_preserved(self):
        task = write_task(self.tmp / "task-del", ["src/allowed.txt"])
        (self.repo / "src" / "allowed.txt").unlink()
        patch = self.tmp / "delete.patch"
        manifest = self.tmp / "delete-manifest.json"
        result = self.run_helper("prepare", "--task", task, "--patch", patch, "--manifest", manifest)
        self.assertEqual(0, result.returncode, result.stderr)
        data = json.loads(manifest.read_text(encoding="utf-8"))
        self.assertEqual(["src/allowed.txt"], data["changedPaths"])

    def test_path_traversal_patch_is_rejected(self):
        task = write_task(self.tmp / "task-traversal", ["src/allowed.txt"])
        patch = self.tmp / "traversal.patch"
        patch.write_text(
            "diff --git a/src/allowed.txt b/../../escape.txt\n"
            "index df967b9..8baef1b 100644\n"
            "--- a/src/allowed.txt\n"
            "+++ b/../../escape.txt\n"
            "@@ -1 +1 @@\n"
            "-base\n"
            "+escape\n",
            encoding="utf-8",
        )
        import hashlib
        manifest = self.tmp / "traversal-manifest.json"
        task_raw = task.read_bytes()
        manifest.write_text(json.dumps({
            "version":1, "taskId":"TEST-001", "taskSha256":hashlib.sha256(task_raw).hexdigest(),
            "baseSha":self.base, "changedPaths":["src/allowed.txt"],
            "patchSha256":hashlib.sha256(patch.read_bytes()).hexdigest(),
        }), encoding="utf-8")
        publisher = self.tmp / "publisher-traversal"
        git(self.repo, "clone", "-q", str(self.repo), str(publisher))
        git(publisher, "checkout", "-q", self.base)
        result = self.run_helper(
            "apply", "--task", task, "--patch", patch, "--manifest", manifest,
            "--expected-head", self.base, "--scope-base", self.base, repo=publisher
        )
        self.assertNotEqual(0, result.returncode)

    def test_scope_validation_uses_merge_base_when_main_advances(self):
        task = write_task(self.tmp / "task-merge-base", ["src/allowed.txt"])
        candidate = self.tmp / "candidate-branch"
        git(self.repo, "clone", "-q", str(self.repo), str(candidate))
        git(candidate, "config", "user.name", "test")
        git(candidate, "config", "user.email", "test@example.invalid")
        git(candidate, "checkout", "-qb", "candidate")
        (candidate / "src" / "allowed.txt").write_text("candidate\n", encoding="utf-8")
        git(candidate, "add", "src/allowed.txt")
        git(candidate, "commit", "-qm", "candidate")

        (self.repo / "other.txt").write_text("main advanced\n", encoding="utf-8")
        git(self.repo, "add", "other.txt")
        git(self.repo, "commit", "-qm", "main advance")
        advanced = git(self.repo, "rev-parse", "HEAD").stdout.strip()
        git(candidate, "fetch", "-q", str(self.repo), advanced)
        result = self.run_helper("validate-current", "--task", task, "--base", advanced, repo=candidate)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
