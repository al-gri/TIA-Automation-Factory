#!/usr/bin/env python3
"""Fail-closed selector for the autonomous coding task queue.

The selector reads trusted task JSON files from the checked-out main branch and a
snapshot of pull requests produced by `gh pr list --state all --json ...`.
Historical tasks are ignored unless they explicitly opt in with
`execution.enabled: true`.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any


class SupervisorError(RuntimeError):
    pass


def load_tasks(tasks_dir: Path) -> dict[str, dict[str, Any]]:
    tasks: dict[str, dict[str, Any]] = {}
    for path in sorted(tasks_dir.glob("*.json")):
        data = json.loads(path.read_text(encoding="utf-8"))
        task_id = data.get("id")
        if not isinstance(task_id, str) or not task_id:
            raise SupervisorError(f"{path}: missing non-empty id")
        if task_id in tasks:
            raise SupervisorError(f"duplicate task id: {task_id}")
        data["__path"] = path.as_posix()
        tasks[task_id] = data
    return tasks


def task_prs(task: dict[str, Any], prs: list[dict[str, Any]]) -> list[dict[str, Any]]:
    task_id = task["id"]
    task_path = task["__path"]
    body_marker = f"Task: `{task_path}`"
    title_marker = f"task-{task_id}"
    matches: list[dict[str, Any]] = []
    for pr in prs:
        body = pr.get("body") or ""
        title = pr.get("title") or ""
        if body_marker in body or title_marker in title:
            matches.append(pr)
    return matches


def task_state(task: dict[str, Any], prs: list[dict[str, Any]]) -> str:
    matches = task_prs(task, prs)
    merged = [pr for pr in matches if pr.get("mergedAt")]
    open_prs = [pr for pr in matches if str(pr.get("state", "")).upper() == "OPEN"]
    closed_unmerged = [
        pr
        for pr in matches
        if str(pr.get("state", "")).upper() == "CLOSED" and not pr.get("mergedAt")
    ]

    if merged:
        return "DONE"
    if len(open_prs) > 1:
        return "BLOCKED_AMBIGUOUS_CANDIDATES"
    if len(open_prs) == 1:
        return "WAITING_FOR_REVIEW"
    if closed_unmerged:
        return "BLOCKED_CLOSED_UNMERGED"
    return "UNSTARTED"


def execution_metadata(task: dict[str, Any]) -> tuple[bool, int, list[str]]:
    execution = task.get("execution")
    if execution is None:
        return False, 0, []
    if not isinstance(execution, dict):
        raise SupervisorError(f"{task['id']}: execution must be an object")

    enabled = execution.get("enabled", False)
    if not isinstance(enabled, bool):
        raise SupervisorError(f"{task['id']}: execution.enabled must be boolean")
    if not enabled:
        return False, 0, []

    priority = execution.get("priority", 0)
    if not isinstance(priority, int):
        raise SupervisorError(f"{task['id']}: execution.priority must be integer")

    depends_on = execution.get("dependsOn", [])
    if not isinstance(depends_on, list) or any(
        not isinstance(dep, str) or not dep for dep in depends_on
    ):
        raise SupervisorError(f"{task['id']}: execution.dependsOn must be string array")
    if task["id"] in depends_on:
        raise SupervisorError(f"{task['id']}: task cannot depend on itself")

    return True, priority, depends_on


def select(tasks: dict[str, dict[str, Any]], prs: list[dict[str, Any]]) -> dict[str, Any]:
    states = {task_id: task_state(task, prs) for task_id, task in tasks.items()}
    candidates: list[tuple[int, str, str]] = []
    blocked: dict[str, str] = {}

    for task_id, task in tasks.items():
        enabled, priority, depends_on = execution_metadata(task)
        if not enabled:
            continue

        state = states[task_id]
        if state != "UNSTARTED":
            blocked[task_id] = state
            continue

        missing = [dep for dep in depends_on if dep not in tasks]
        if missing:
            blocked[task_id] = "BLOCKED_UNKNOWN_DEPENDENCY:" + ",".join(sorted(missing))
            continue

        incomplete = [dep for dep in depends_on if states[dep] != "DONE"]
        if incomplete:
            blocked[task_id] = "BLOCKED_DEPENDENCY:" + ",".join(sorted(incomplete))
            continue

        candidates.append((-priority, task_id, task["__path"]))

    candidates.sort()
    selected = None
    if candidates:
        _, task_id, path = candidates[0]
        selected = {"taskId": task_id, "taskPath": path}

    return {
        "selected": selected,
        "states": states,
        "blocked": blocked,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tasks-dir", default="tasks")
    parser.add_argument("--prs-json", required=True)
    args = parser.parse_args()

    tasks = load_tasks(Path(args.tasks_dir))
    prs = json.loads(Path(args.prs_json).read_text(encoding="utf-8"))
    if not isinstance(prs, list):
        raise SupervisorError("PR snapshot must be a JSON array")

    print(json.dumps(select(tasks, prs), indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
