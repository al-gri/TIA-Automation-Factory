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
HASH_RE = re.compile(r"^[0-9a-f]{64}$")


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


def parse_task(task_file: Path) -> dict:
    try:
        task = json.loads(task_file.read_text(encoding="utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise AuthorityError(f"invalid trusted task JSON: {exc}") from exc
    if not isinstance(task, dict) or not isinstance(task.get("id"), str) or not task["id"]:
        raise AuthorityError("trusted task requires a non-empty id")
    return task


def validate_sha(raw: str, label: str) -> str:
    value = str(raw).lower()
    if not SHA_RE.fullmatch(value):
        raise AuthorityError(f"{label} is invalid")
    return value


def validate_task_hash(raw: str) -> str:
    value = str(raw).lower()
    if not HASH_RE.fullmatch(value):
        raise AuthorityError("taskSha256 is invalid")
    return value


def validate_continuation_authority(
    bound_base_sha: str,
    live_main_sha: str,
    task_path: str,
    task_sha: str,
    task_file: Path,
    task_id: str | None = None,
) -> None:
    bound = validate_sha(bound_base_sha, "bound base SHA")
    live = validate_sha(live_main_sha, "live main SHA")
    if live != bound:
        raise AuthorityError(f"stale continuation base: expected {bound}, live main is {live}")

    canonical_task_path(task_path)
    expected_hash = validate_task_hash(task_sha)
    actual_hash = task_sha256(task_file)
    if actual_hash != expected_hash:
        raise AuthorityError("stale continuation task/policy hash")

    task = parse_task(task_file)
    if task_id is not None and task.get("id") != task_id:
        raise AuthorityError("stale continuation task identity")


def validate_bound_state(state: dict, current_base_sha: str, task_path: str, task_file: Path) -> None:
    required = ("baseSha", "taskPath", "taskSha256", "taskId", "candidateSha")
    for key in required:
        if key not in state:
            raise AuthorityError(f"missing review authority field: {key}")

    candidate_sha = validate_sha(str(state["candidateSha"]), "candidate SHA")
    del candidate_sha
    canonical = canonical_task_path(task_path)
    if canonical != canonical_task_path(str(state["taskPath"])):
        raise AuthorityError("stale external review task path")

    validate_continuation_authority(
        str(state["baseSha"]),
        current_base_sha,
        canonical,
        str(state["taskSha256"]),
        task_file,
        str(state["taskId"]),
    )


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

    continuation = sub.add_parser("verify-continuation")
    continuation.add_argument("--bound-base-sha", required=True)
    continuation.add_argument("--live-main-sha", required=True)
    continuation.add_argument("--task-path", required=True)
    continuation.add_argument("--task-sha256", required=True)
    continuation.add_argument("--task-file", type=Path, required=True)
    continuation.add_argument("--task-id")

    args = parser.parse_args()
    try:
        if args.command == "task-sha":
            print(task_sha256(args.task))
        elif args.command == "validate":
            state = json.loads(args.state.read_text(encoding="utf-8"))
            if not isinstance(state, dict):
                raise AuthorityError("review state must be a JSON object")
            validate_bound_state(state, args.current_base_sha, args.task_path, args.task_file)
        else:
            validate_continuation_authority(
                args.bound_base_sha,
                args.live_main_sha,
                args.task_path,
                args.task_sha256,
                args.task_file,
                args.task_id,
            )
    except (AuthorityError, OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        print(f"review-authority: {exc}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
