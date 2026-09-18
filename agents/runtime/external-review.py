#!/usr/bin/env python3
"""Trusted tooling for external review package generation and response validation."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

PLACEHOLDER_RE = re.compile(r"\{\{([A-Z0-9_]+)\}\}")
SHA_RE = re.compile(r"^[0-9a-fA-F]{40}$")
FINDING_ID_RE = re.compile(r"^F[0-9]{3,}$")

REVIEW_TYPES = {"CODE_REVIEW", "ARCHITECTURE_REVIEW", "PLC_REVIEW"}
REVIEW_STATUSES = {"APPROVE", "CHANGES_REQUIRED", "BLOCKED"}
ASSESSMENT_STATUSES = {"PASS", "FAIL", "NOT_APPLICABLE", "INSUFFICIENT_EVIDENCE"}
SEVERITIES = {"critical", "major", "minor"}
REQUIRED_TOP_LEVEL = {
    "protocolVersion",
    "taskId",
    "candidateSha",
    "reviewType",
    "reviewRound",
    "reviewStatus",
    "summary",
    "requirements",
    "architecture",
    "codeQuality",
    "tests",
    "findings",
    "recommendation",
}
ASSESSMENT_KEYS = {"status", "notes"}
FINDING_REQUIRED = {"id", "severity", "problem", "requiredChange"}
FINDING_ALLOWED = FINDING_REQUIRED | {"file", "location"}


def fail(message: str) -> None:
    raise ValueError(message)


def load_json(path: Path) -> Any:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        fail(f"File not found: {path}")
    except json.JSONDecodeError as exc:
        fail(f"Invalid JSON in {path}: {exc}")


def require_string(value: Any, name: str, *, allow_empty: bool = False) -> str:
    if not isinstance(value, str):
        fail(f"{name} must be a string")
    if not allow_empty and not value.strip():
        fail(f"{name} must not be empty")
    return value


def build_package(template_path: Path, context_path: Path, output_path: Path) -> None:
    template = template_path.read_text(encoding="utf-8")
    context = load_json(context_path)
    if not isinstance(context, dict):
        fail("Review package context must be a JSON object")

    placeholders = sorted(set(PLACEHOLDER_RE.findall(template)))
    missing = [name for name in placeholders if name not in context]
    if missing:
        fail("Missing review package context keys: " + ", ".join(missing))

    rendered = template
    for name in placeholders:
        value = context[name]
        if isinstance(value, (dict, list)):
            replacement = json.dumps(value, indent=2, ensure_ascii=False)
        elif value is None:
            replacement = ""
        else:
            replacement = str(value)
        rendered = rendered.replace("{{" + name + "}}", replacement)

    unresolved = sorted(set(PLACEHOLDER_RE.findall(rendered)))
    if unresolved:
        fail("Unresolved review package placeholders: " + ", ".join(unresolved))

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(rendered, encoding="utf-8")


def validate_assessment(name: str, value: Any) -> None:
    if not isinstance(value, dict):
        fail(f"{name} must be an object")
    if set(value) != ASSESSMENT_KEYS:
        fail(f"{name} must contain exactly: status, notes")
    if value["status"] not in ASSESSMENT_STATUSES:
        fail(f"{name}.status is invalid: {value['status']!r}")
    require_string(value["notes"], f"{name}.notes", allow_empty=True)


def validate_finding(index: int, value: Any) -> None:
    prefix = f"findings[{index}]"
    if not isinstance(value, dict):
        fail(f"{prefix} must be an object")
    keys = set(value)
    if not FINDING_REQUIRED.issubset(keys):
        missing = sorted(FINDING_REQUIRED - keys)
        fail(f"{prefix} missing required keys: {', '.join(missing)}")
    if not keys.issubset(FINDING_ALLOWED):
        extra = sorted(keys - FINDING_ALLOWED)
        fail(f"{prefix} has unknown keys: {', '.join(extra)}")

    finding_id = require_string(value["id"], f"{prefix}.id")
    if not FINDING_ID_RE.fullmatch(finding_id):
        fail(f"{prefix}.id must match F### or higher")
    if value["severity"] not in SEVERITIES:
        fail(f"{prefix}.severity is invalid: {value['severity']!r}")
    require_string(value["problem"], f"{prefix}.problem")
    require_string(value["requiredChange"], f"{prefix}.requiredChange")

    for optional in ("file", "location"):
        if optional in value and value[optional] is not None:
            require_string(value[optional], f"{prefix}.{optional}", allow_empty=True)


def validate_response(
    response_path: Path,
    *,
    expected_task_id: str | None,
    expected_candidate_sha: str | None,
    expected_review_type: str | None,
    expected_review_round: int | None,
) -> dict[str, Any]:
    data = load_json(response_path)
    if not isinstance(data, dict):
        fail("External review response must be a JSON object")

    keys = set(data)
    if keys != REQUIRED_TOP_LEVEL:
        missing = sorted(REQUIRED_TOP_LEVEL - keys)
        extra = sorted(keys - REQUIRED_TOP_LEVEL)
        details = []
        if missing:
            details.append("missing=" + ",".join(missing))
        if extra:
            details.append("extra=" + ",".join(extra))
        fail("Invalid top-level response keys: " + " ".join(details))

    if data["protocolVersion"] != "1.0":
        fail("protocolVersion must be '1.0'")

    task_id = require_string(data["taskId"], "taskId")
    candidate_sha = require_string(data["candidateSha"], "candidateSha")
    if not SHA_RE.fullmatch(candidate_sha):
        fail("candidateSha must be a 40-character Git SHA")

    review_type = data["reviewType"]
    if review_type not in REVIEW_TYPES:
        fail(f"Invalid reviewType: {review_type!r}")

    review_round = data["reviewRound"]
    if isinstance(review_round, bool) or not isinstance(review_round, int) or review_round < 1:
        fail("reviewRound must be an integer >= 1")

    review_status = data["reviewStatus"]
    if review_status not in REVIEW_STATUSES:
        fail(f"Invalid reviewStatus: {review_status!r}")

    require_string(data["summary"], "summary")
    require_string(data["recommendation"], "recommendation")

    for assessment in ("requirements", "architecture", "codeQuality", "tests"):
        validate_assessment(assessment, data[assessment])

    findings = data["findings"]
    if not isinstance(findings, list):
        fail("findings must be an array")
    seen_ids: set[str] = set()
    for index, finding in enumerate(findings):
        validate_finding(index, finding)
        finding_id = finding["id"]
        if finding_id in seen_ids:
            fail(f"Duplicate finding id: {finding_id}")
        seen_ids.add(finding_id)

    if review_status == "APPROVE":
        blocking = [f["id"] for f in findings if f["severity"] in {"critical", "major"}]
        if blocking:
            fail("APPROVE cannot contain critical/major findings: " + ", ".join(blocking))

    if expected_task_id is not None and task_id != expected_task_id:
        fail(f"taskId binding mismatch: expected {expected_task_id!r}, got {task_id!r}")
    if expected_candidate_sha is not None and candidate_sha.lower() != expected_candidate_sha.lower():
        fail("candidateSha binding mismatch")
    if expected_review_type is not None and review_type != expected_review_type:
        fail(f"reviewType binding mismatch: expected {expected_review_type!r}, got {review_type!r}")
    if expected_review_round is not None and review_round != expected_review_round:
        fail(f"reviewRound binding mismatch: expected {expected_review_round}, got {review_round}")

    return data


def make_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)

    build = sub.add_parser("build-package", help="Render a standalone review package from a versioned template")
    build.add_argument("--template", type=Path, required=True)
    build.add_argument("--context", type=Path, required=True)
    build.add_argument("--output", type=Path, required=True)

    validate = sub.add_parser("validate-response", help="Validate and bind an external reviewer JSON response")
    validate.add_argument("--input", type=Path, required=True)
    validate.add_argument("--task-id")
    validate.add_argument("--candidate-sha")
    validate.add_argument("--review-type", choices=sorted(REVIEW_TYPES))
    validate.add_argument("--review-round", type=int)
    validate.add_argument("--normalized-output", type=Path)

    return parser


def main() -> int:
    args = make_parser().parse_args()
    try:
        if args.command == "build-package":
            build_package(args.template, args.context, args.output)
            print(args.output)
            return 0

        data = validate_response(
            args.input,
            expected_task_id=args.task_id,
            expected_candidate_sha=args.candidate_sha,
            expected_review_type=args.review_type,
            expected_review_round=args.review_round,
        )
        normalized = json.dumps(data, indent=2, ensure_ascii=False) + "\n"
        if args.normalized_output:
            args.normalized_output.parent.mkdir(parents=True, exist_ok=True)
            args.normalized_output.write_text(normalized, encoding="utf-8")
        else:
            sys.stdout.write(normalized)
        return 0
    except (OSError, ValueError) as exc:
        print(f"external-review: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
