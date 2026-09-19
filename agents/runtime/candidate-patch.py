#!/usr/bin/env python3
"""Prepare and validate bounded candidate patches across the trust boundary.

The coding job may execute candidate-controlled code but has no publication token.
It materializes this helper from a trusted commit before candidate execution and uses
`prepare` to create a patch artifact. A fresh publisher runs `apply` from a trusted
checkout, independently validates task-authorized scope/modes, and only then commits.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import stat
import subprocess
from pathlib import Path, PurePosixPath
from typing import Iterable


PROTECTED_PATTERNS = (
    ".github/**",
    "agents/**",
    "tasks/**",
    ".gemini/**",
    ".openhands/**",
    ".gitignore",
    "opencode.json",
)
ALLOWED_GIT_MODES = {"100644", "100755"}


class PatchError(ValueError):
    pass


def run_git(*args: str, input_bytes: bytes | None = None) -> bytes:
    completed = subprocess.run(
        ["git", *args],
        input=input_bytes,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if completed.returncode != 0:
        stderr = completed.stderr.decode("utf-8", errors="replace").strip()
        raise PatchError(f"git {' '.join(args)} failed: {stderr}")
    return completed.stdout


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def canonical_path(raw: str) -> str:
    if not raw or "\\" in raw or raw != raw.strip():
        raise PatchError(f"non-canonical repository path: {raw!r}")
    path = PurePosixPath(raw)
    normalized = str(path)
    if (
        path.is_absolute()
        or normalized in {"", "."}
        or normalized != raw
        or ".." in path.parts
        or "." in path.parts
        or normalized == ".git"
        or normalized.startswith(".git/")
    ):
        raise PatchError(f"unsafe repository path: {raw!r}")
    return normalized


def matches(pattern: str, path: str) -> bool:
    if pattern.endswith("/**"):
        root = canonical_path(pattern[:-3])
        return path == root or path.startswith(root + "/")
    return canonical_path(pattern) == path


def task_policy(task_path: Path) -> tuple[dict, bytes, list[str]]:
    raw = task_path.read_bytes()
    try:
        task = json.loads(raw.decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise PatchError(f"invalid trusted task JSON: {exc}") from exc
    if not isinstance(task, dict) or not isinstance(task.get("id"), str) or not task["id"]:
        raise PatchError("trusted task requires non-empty id")
    policy = task.get("candidatePolicy")
    if not isinstance(policy, dict):
        raise PatchError("trusted task requires candidatePolicy")
    allowed = policy.get("allowedPaths")
    if not isinstance(allowed, list) or not allowed:
        raise PatchError("candidatePolicy.allowedPaths must be a non-empty array")
    normalized_patterns: list[str] = []
    for raw_pattern in allowed:
        if not isinstance(raw_pattern, str) or not raw_pattern:
            raise PatchError("candidatePolicy.allowedPaths entries must be non-empty strings")
        if raw_pattern.endswith("/**"):
            canonical_path(raw_pattern[:-3])
        else:
            canonical_path(raw_pattern)
        normalized_patterns.append(raw_pattern)

    allow_worker = policy.get("allowTiaV21WorkerChanges", False)
    if not isinstance(allow_worker, bool):
        raise PatchError("candidatePolicy.allowTiaV21WorkerChanges must be boolean")

    task_protected = task.get("protectedPaths", [])
    if task_protected is None:
        task_protected = []
    if not isinstance(task_protected, list) or not all(isinstance(x, str) and x for x in task_protected):
        raise PatchError("protectedPaths must be an array of non-empty strings when present")
    effective_protected = list(PROTECTED_PATTERNS)
    for protected in task_protected:
        root = protected[:-3] if protected.endswith("/**") else protected
        canonical_path(root)
        if allow_worker and (root == "src/TiaV21Worker" or root.startswith("src/TiaV21Worker/")):
            continue
        if protected not in effective_protected:
            effective_protected.append(protected)

    for pattern in normalized_patterns:
        root = pattern[:-3] if pattern.endswith("/**") else pattern
        for protected in effective_protected:
            protected_root = protected[:-3] if protected.endswith("/**") else protected
            if (
                root == protected_root
                or root.startswith(protected_root + "/")
                or protected_root.startswith(root + "/")
            ):
                raise PatchError(f"allowed path overlaps protected infrastructure: {pattern}")
        if (root == "src/TiaV21Worker" or root.startswith("src/TiaV21Worker/")) and not allow_worker:
            raise PatchError("TiaV21Worker allowedPaths require allowTiaV21WorkerChanges=true")
    task["_effectiveProtectedPaths"] = effective_protected
    return task, raw, normalized_patterns


def validate_path(path: str, patterns: list[str]) -> str:
    path = canonical_path(path)
    for protected in PROTECTED_PATTERNS:
        if matches(protected, path):
            raise PatchError(f"protected path is never publishable by coding agent: {path}")
    if not any(matches(pattern, path) for pattern in patterns):
        raise PatchError(f"path is outside candidatePolicy.allowedPaths: {path}")
    return path


def nul_paths(data: bytes) -> list[str]:
    if not data:
        return []
    items = data.rstrip(b"\0").split(b"\0")
    result: list[str] = []
    for item in items:
        if not item:
            continue
        result.append(item.decode("utf-8", errors="strict"))
    return result


def workspace_paths() -> list[str]:
    tracked = nul_paths(run_git("diff", "--name-only", "-z", "HEAD"))
    untracked = nul_paths(run_git("ls-files", "--others", "--exclude-standard", "-z"))
    return sorted(set(tracked + untracked))


def index_changed_paths(base: str) -> list[str]:
    return sorted(set(nul_paths(run_git("diff", "--cached", "--name-only", "--no-renames", "-z", base))))


def git_mode_for_index_path(path: str) -> str | None:
    out = run_git("ls-files", "-s", "--", path).decode("utf-8", errors="strict").strip()
    if not out:
        return None
    first = out.splitlines()[0]
    return first.split(" ", 1)[0]


def validate_modes(paths: Iterable[str]) -> None:
    for path in paths:
        mode = git_mode_for_index_path(path)
        if mode is None:
            continue
        if mode not in ALLOWED_GIT_MODES:
            raise PatchError(f"unsupported git mode {mode} for {path}; symlinks/submodules are forbidden")


def git_is_clean() -> bool:
    completed = subprocess.run(
        ["git", "status", "--porcelain=v1", "--untracked-files=all"],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if completed.returncode != 0:
        raise PatchError(completed.stderr.decode("utf-8", errors="replace"))
    return not bool(completed.stdout)


def stage_validated(paths: list[str]) -> None:
    for path in paths:
        completed = subprocess.run(
            ["git", "add", "--all", "--", path],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=False,
        )
        if completed.returncode != 0:
            raise PatchError(
                f"failed to stage {path}: {completed.stderr.decode('utf-8', errors='replace').strip()}"
            )


def prepare(task_path: Path, patch_path: Path, manifest_path: Path) -> None:
    task, task_raw, patterns = task_policy(task_path)
    base_sha = run_git("rev-parse", "HEAD").decode("ascii").strip()
    if len(base_sha) != 40:
        raise PatchError("HEAD did not resolve to a commit SHA")

    paths = workspace_paths()
    if not paths:
        raise PatchError("candidate produced no repository changes")
    validated = [validate_path(path, patterns) for path in paths]

    stage_validated(validated)
    staged = index_changed_paths("HEAD")
    if staged != sorted(validated):
        raise PatchError(f"staged path set differs from validated path set: {staged} != {sorted(validated)}")
    validate_modes(staged)

    patch = run_git("diff", "--cached", "--binary", "--full-index", "--no-renames", "HEAD")
    if not patch:
        raise PatchError("validated candidate patch is empty")
    patch_path.write_bytes(patch)

    manifest = {
        "version": 1,
        "taskId": task["id"],
        "taskSha256": sha256(task_raw),
        "baseSha": base_sha,
        "changedPaths": staged,
        "patchSha256": sha256(patch),
    }
    manifest_path.write_text(json.dumps(manifest, sort_keys=True, indent=2) + "\n", encoding="utf-8")


def apply_patch(task_path: Path, patch_path: Path, manifest_path: Path, expected_head: str, scope_base: str) -> None:
    task, task_raw, patterns = task_policy(task_path)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    if not isinstance(manifest, dict) or manifest.get("version") != 1:
        raise PatchError("unsupported candidate patch manifest")
    head = run_git("rev-parse", "HEAD").decode("ascii").strip()
    if head != expected_head:
        raise PatchError(f"publisher HEAD mismatch: expected {expected_head}, got {head}")
    if manifest.get("taskId") != task["id"]:
        raise PatchError("patch taskId does not match trusted task")
    if manifest.get("taskSha256") != sha256(task_raw):
        raise PatchError("patch task hash does not match trusted task")
    if manifest.get("baseSha") != expected_head:
        raise PatchError("patch baseSha does not match publisher HEAD")
    patch = patch_path.read_bytes()
    if manifest.get("patchSha256") != sha256(patch):
        raise PatchError("patch hash does not match manifest")
    if not git_is_clean():
        raise PatchError("publisher checkout must be clean before applying candidate patch")

    for args in (
        ("apply", "--cached", "--check", "--whitespace=nowarn", str(patch_path)),
        ("apply", "--cached", "--whitespace=nowarn", str(patch_path)),
    ):
        run_git(*args)

    staged = index_changed_paths("HEAD")
    manifest_paths = sorted(manifest.get("changedPaths") or [])
    if staged != manifest_paths:
        raise PatchError(f"applied patch path set differs from manifest: {staged} != {manifest_paths}")
    for path in staged:
        validate_path(path, patterns)
    validate_modes(staged)

    merge_base = run_git("merge-base", scope_base, "HEAD").decode("ascii").strip()
    final_paths = index_changed_paths(merge_base)
    if not final_paths:
        raise PatchError("final candidate scope is empty")
    for path in final_paths:
        validate_path(path, patterns)
    validate_modes(final_paths)


def validate_current(task_path: Path, base: str) -> None:
    _, _, patterns = task_policy(task_path)
    merge_base = run_git("merge-base", base, "HEAD").decode("ascii").strip()
    paths = nul_paths(run_git("diff", "--name-only", "--no-renames", "-z", merge_base, "HEAD"))
    if not paths:
        raise PatchError("existing candidate has no changes from trusted base")
    for path in paths:
        validate_path(path, patterns)
    for path in paths:
        out = run_git("ls-tree", "HEAD", "--", path).decode("utf-8", errors="strict").strip()
        if not out:
            continue
        mode = out.split(" ", 1)[0]
        if mode not in ALLOWED_GIT_MODES:
            raise PatchError(f"unsupported committed git mode {mode} for {path}")


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)

    p = sub.add_parser("prepare")
    p.add_argument("--task", type=Path, required=True)
    p.add_argument("--patch", type=Path, required=True)
    p.add_argument("--manifest", type=Path, required=True)

    a = sub.add_parser("apply")
    a.add_argument("--task", type=Path, required=True)
    a.add_argument("--patch", type=Path, required=True)
    a.add_argument("--manifest", type=Path, required=True)
    a.add_argument("--expected-head", required=True)
    a.add_argument("--scope-base", required=True)

    v = sub.add_parser("validate-current")
    v.add_argument("--task", type=Path, required=True)
    v.add_argument("--base", required=True)

    args = parser.parse_args()
    try:
        if args.command == "prepare":
            prepare(args.task, args.patch, args.manifest)
        elif args.command == "apply":
            apply_patch(args.task, args.patch, args.manifest, args.expected_head, args.scope_base)
        else:
            validate_current(args.task, args.base)
    except (PatchError, OSError, json.JSONDecodeError, UnicodeDecodeError) as exc:
        print(f"candidate-patch: {exc}", file=os.sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
