#!/usr/bin/env python3
"""Bind external-review authority to one immutable base and trusted task blob."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path, PurePosixPath

SHA_RE = re.compile(r"^[0-9a-f]{40}$")


class AuthorityError(ValueError):
    pass


def canonical_task_path(raw: str) -> str:
    if not isinstance(raw, str) or not raw or raw != raw.strip() or "\\" in raw:
        raise AuthorityError("taskPath must be canonical repository-relative POSIX text")
    path = PurePosixPath(raw)
    normalized = str(path)
    if (
        path.is_absolute()
        or normalized != raw
        or ".." in path.parts
        or "." in path.parts
        or not normalized.startswith("tasks/")
        or not normalized.endswith(".json")
    ):
        raise AuthorityError(f"invalid taskPath: {raw!r}")
    return normalized


def task_sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def validate_bound_state(state: dict, current_base_sha: str, task_path: str, task_file: Path) -> None:
    required = ("baseSha", "taskPath", "taskSha256", "taskId", "candidateSha")
    for key in required:
        if key not in state:
            raise AuthorityError(f"missing review authority field: {key}")

    base_sha = str(state["baseSha"]).lower()
    candidate_sha = str(state["candidateSha"]).lower()
    current_base_sha = current_base_sha.lower()
    if not SHA_RE.fullmatch(base_sha) or not SHA_RE.fullmatch(candidate_sha):
        raise AuthorityError("review authority contains an invalid SHA")
    if not SHA_RE.fullmatch(current_base_sha):
        raise AuthorityError("current base SHA is invalid")
    if current_base_sha != base_sha:
        raise AuthorityError(
            f"stale external review base: expected {base_sha}, current main is {current_base_sha}"
        )

    canonical = canonical_task_path(task_path)
    if canonical != canonical_task_path(str(state["taskPath"])):
        raise AuthorityError("stale external review task path")

    actual_hash = task_sha256(task_file)
    expected_hash = str(state["taskSha256"]).lower()
    if not re.fullmatch(r"[0-9a-f]{64}", expected_hash):
        raise AuthorityError("review authority taskSha256 is invalid")
    if actual_hash != expected_hash:
        raise AuthorityError("stale external review task/policy hash")

    try:
        task = json.loads(task_file.read_text(encoding="utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise AuthorityError(f"invalid trusted task JSON: {exc}") from exc
    if not isinstance(task, dict) or task.get("id") != state["taskId"]:
        raise AuthorityError("stale external review task identity")


def main() -> int:
    parser = argparse.ArgumentParser()
    sub = parser.add_subparsers(dest="command", required=True)

    fingerprint = sub.add_parser("task-sha")
    fingerprint.add_argument("--task", type=Path, required=True)

    validate = sub.add_parser("validate")
    validate.add_argument("--state", type=Path, required=True)
    validate.add_argument("--current-base-sha", required=True)
    validate.add_argument("--task-path", required=True)
    validate.add_argument("--task-file", type=Path, required=True)

    args = parser.parse_args()
    try:
        if args.command == "task-sha":
            print(task_sha256(args.task))
        else:
            state = json.loads(args.state.read_text(encoding="utf-8"))
            if not isinstance(state, dict):
                raise AuthorityError("review state must be a JSON object")
            validate_bound_state(state, args.current_base_sha, args.task_path, args.task_file)
    except (AuthorityError, OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        print(f"review-authority: {exc}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
