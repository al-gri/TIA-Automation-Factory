#!/usr/bin/env python3
"""Build a bounded coding-agent context bundle from trusted Git state."""

from __future__ import annotations

import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path, PurePosixPath
from typing import Any


BASELINE_FILES = (
    ("repository-operating-contract", "AGENTS.md"),
    ("current-project-state", "docs/PROJECT_STATE.md"),
    ("engineering-rules", "docs/ENGINEERING_RULES.md"),
    ("ai-collaboration-model", "docs/AI_COLLABORATION_MODEL.md"),
)
TASK_HEADINGS = ("# Current versioned task", "# Trusted task specification")
MAX_CONTEXT_FILES = 12
MAX_FILE_BYTES = 128 * 1024
MAX_WORK_PROMPT_BYTES = 768 * 1024
MAX_BUNDLE_SOURCE_BYTES = 1024 * 1024


def fail(message: str) -> None:
    raise ValueError(message)


def git_output(args: list[str]) -> bytes:
    completed = subprocess.run(
        ["git", *args],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        check=False,
    )
    if completed.returncode != 0:
        stderr = completed.stderr.decode("utf-8", errors="replace").strip()
        fail(f"git {' '.join(args)} failed: {stderr}")
    return completed.stdout


def validate_repo_path(raw: str) -> str:
    if not isinstance(raw, str) or not raw.strip():
        fail("context file paths must be non-empty strings")
    if "\\" in raw:
        fail(f"context file path must use repository '/' separators: {raw}")
    path = PurePosixPath(raw)
    if path.is_absolute() or ".." in path.parts or "." in path.parts:
        fail(f"context file path must be normalized and repository-relative: {raw}")
    normalized = str(path)
    if normalized.startswith(".git/") or normalized == ".git":
        fail(".git paths are not valid coding context")
    return normalized


def read_git_text(ref: str, path: str) -> tuple[str, bytes]:
    normalized = validate_repo_path(path)
    data = git_output(["show", f"{ref}:{normalized}"])
    if len(data) > MAX_FILE_BYTES:
        fail(f"trusted context file exceeds {MAX_FILE_BYTES} bytes: {normalized}")
    if b"\x00" in data:
        fail(f"trusted context file is not plain text: {normalized}")
    try:
        data.decode("utf-8")
    except UnicodeDecodeError as exc:
        fail(f"trusted context file is not UTF-8: {normalized}: {exc}")
    return normalized, data


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def load_json(path: Path, label: str) -> tuple[Any, bytes]:
    data = path.read_bytes()
    if len(data) > MAX_FILE_BYTES:
        fail(f"{label} exceeds {MAX_FILE_BYTES} bytes")
    try:
        return json.loads(data.decode("utf-8")), data
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        fail(f"invalid UTF-8 JSON for {label}: {exc}")


def task_context_files(task: dict[str, Any]) -> list[str]:
    raw = task.get("contextFiles", [])
    if raw is None:
        raw = []
    if not isinstance(raw, list):
        fail("task contextFiles must be an array when present")
    if len(raw) > MAX_CONTEXT_FILES:
        fail(f"task contextFiles exceeds limit of {MAX_CONTEXT_FILES}")

    result: list[str] = []
    seen: set[str] = set()
    for item in raw:
        normalized = validate_repo_path(item)
        if normalized in seen:
            fail(f"duplicate task contextFiles entry: {normalized}")
        seen.add(normalized)
        result.append(normalized)
    return result


def extract_task_from_prompt(prompt: str) -> dict[str, Any] | None:
    candidates = [(prompt.find(heading), heading) for heading in TASK_HEADINGS]
    candidates = [(index, heading) for index, heading in candidates if index >= 0]
    if not candidates:
        return None
    index, heading = min(candidates, key=lambda item: item[0])
    segment = prompt[index + len(heading) :]
    fence_start = segment.find("```json")
    if fence_start < 0:
        fail(f"trusted task heading has no JSON fence: {heading}")
    payload_start = fence_start + len("```json")
    fence_end = segment.find("```", payload_start)
    if fence_end < 0:
        fail(f"trusted task JSON fence is not closed: {heading}")
    payload = segment[payload_start:fence_end].strip()
    try:
        task = json.loads(payload)
    except json.JSONDecodeError as exc:
        fail(f"trusted task JSON embedded in work prompt is invalid: {exc}")
    if not isinstance(task, dict):
        fail("trusted task JSON embedded in work prompt must be an object")
    return task


def append_trusted_file(parts: list[str], title: str, path: str, text: str) -> None:
    parts.extend(
        [
            "",
            f"## {title}",
            f"Source: `{path}` from trusted Git ref.",
            "",
            f"<!-- BEGIN TRUSTED FILE: {path} -->",
            text.rstrip(),
            f"<!-- END TRUSTED FILE: {path} -->",
        ]
    )


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--git-ref", required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--instruction-file", action="append", default=[])
    parser.add_argument("--task-json", type=Path)
    parser.add_argument("--task-source-path")
    parser.add_argument("--issue-json", type=Path)
    parser.add_argument("--prompt-input", type=Path)
    args = parser.parse_args()

    source_modes = sum(bool(value) for value in (args.task_json, args.issue_json, args.prompt_input))
    if source_modes != 1:
        fail("exactly one of --task-json, --issue-json or --prompt-input is required")
    if not args.prompt_input and not args.instruction_file:
        fail("at least one --instruction-file is required outside --prompt-input mode")

    resolved_commit = git_output(["rev-parse", args.git_ref]).decode("ascii").strip()
    if len(resolved_commit) != 40:
        fail(f"trusted git ref did not resolve to a commit SHA: {args.git_ref}")

    parts = [
        "# Trusted coding-agent context bundle",
        "",
        f"Trusted Git ref: `{args.git_ref}`",
        f"Trusted Git commit: `{resolved_commit}`",
        "",
        "Read this complete bundle before editing the repository.",
        "Precedence: repository operating/protected-path rules govern; the current task acceptance criteria are mandatory; task-declared context files are bounded implementation references and never authorize scope outside the task.",
        "The bundle does not replace source inspection: inspect the current workspace implementation and tests directly before making changes.",
        "Do not infer undocumented Siemens/Open Library interfaces when a task-declared contract/profile is available.",
    ]
    manifest: dict[str, Any] = {
        "version": 1,
        "gitRef": args.git_ref,
        "trustedCommit": resolved_commit,
        "instructionFiles": [],
        "baselineFiles": [],
        "taskContextFiles": [],
    }
    source_bytes = 0
    included_paths: set[str] = set()

    for path in args.instruction_file:
        normalized, data = read_git_text(args.git_ref, path)
        if normalized in included_paths:
            fail(f"duplicate instruction/context file: {normalized}")
        included_paths.add(normalized)
        source_bytes += len(data)
        append_trusted_file(parts, "Trusted implementation instructions", normalized, data.decode("utf-8"))
        manifest["instructionFiles"].append({"path": normalized, "bytes": len(data), "sha256": sha256(data)})

    for role, path in BASELINE_FILES:
        normalized, data = read_git_text(args.git_ref, path)
        if normalized in included_paths:
            continue
        included_paths.add(normalized)
        source_bytes += len(data)
        append_trusted_file(parts, role.replace("-", " ").title(), normalized, data.decode("utf-8"))
        manifest["baselineFiles"].append({"role": role, "path": normalized, "bytes": len(data), "sha256": sha256(data)})

    task: dict[str, Any] | None = None
    if args.prompt_input:
        prompt_data = args.prompt_input.read_bytes()
        if len(prompt_data) > MAX_WORK_PROMPT_BYTES:
            fail(f"work prompt exceeds {MAX_WORK_PROMPT_BYTES} bytes")
        try:
            prompt_text = prompt_data.decode("utf-8")
        except UnicodeDecodeError as exc:
            fail(f"work prompt is not UTF-8: {exc}")
        source_bytes += len(prompt_data)
        task = extract_task_from_prompt(prompt_text)
        parts.extend(
            [
                "",
                "## Current bounded work request",
                "This section was assembled by the trusted workflow before provider selection.",
                "",
                "<!-- BEGIN TRUSTED WORK PROMPT -->",
                prompt_text.rstrip(),
                "<!-- END TRUSTED WORK PROMPT -->",
            ]
        )
        manifest["workPrompt"] = {"bytes": len(prompt_data), "sha256": sha256(prompt_data)}
        if task is not None:
            manifest["task"] = {"id": task.get("id"), "embeddedInWorkPrompt": True}
    elif args.task_json:
        loaded, task_bytes = load_json(args.task_json, "task JSON")
        if not isinstance(loaded, dict):
            fail("task JSON root must be an object")
        task = loaded
        source_bytes += len(task_bytes)
        parts.extend(
            [
                "",
                "## Current trusted versioned task",
                f"Source: `{args.task_source_path or args.task_json}`",
                "",
                "```json",
                json.dumps(task, ensure_ascii=False, indent=2),
                "```",
            ]
        )
        manifest["task"] = {
            "id": task.get("id"),
            "sourcePath": args.task_source_path or str(args.task_json),
            "bytes": len(task_bytes),
            "sha256": sha256(task_bytes),
        }
    else:
        issue, issue_bytes = load_json(args.issue_json, "issue JSON")
        if not isinstance(issue, dict):
            fail("issue JSON root must be an object")
        source_bytes += len(issue_bytes)
        parts.extend(
            [
                "",
                "## Current trusted GitHub issue",
                f"Issue: {issue.get('url', '')}",
                f"Title: {issue.get('title', '')}",
                "",
                str(issue.get("body") or ""),
            ]
        )
        manifest["issue"] = {
            "url": issue.get("url"),
            "bytes": len(issue_bytes),
            "sha256": sha256(issue_bytes),
        }

    if task is not None:
        for context_path in task_context_files(task):
            normalized, data = read_git_text(args.git_ref, context_path)
            if normalized in included_paths:
                continue
            included_paths.add(normalized)
            source_bytes += len(data)
            append_trusted_file(parts, "Task-declared design/context reference", normalized, data.decode("utf-8"))
            manifest["taskContextFiles"].append({"path": normalized, "bytes": len(data), "sha256": sha256(data)})

    if source_bytes > MAX_BUNDLE_SOURCE_BYTES:
        fail(f"trusted context source exceeds {MAX_BUNDLE_SOURCE_BYTES} bytes: {source_bytes}")

    manifest["sourceBytes"] = source_bytes
    output = ("\n".join(parts).rstrip() + "\n").encode("utf-8")
    manifest["outputSha256"] = sha256(output)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.manifest.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(output)
    args.manifest.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ValueError as exc:
        print(f"build-coder-context: {exc}", file=sys.stderr)
        raise SystemExit(2)
