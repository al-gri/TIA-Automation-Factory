#!/usr/bin/env python3
import argparse
import json
from pathlib import Path

AGENT_EMAIL = "tia-automation-agent@users.noreply.github.com"
PRIMARY_SLOT = "chatgpt"
SECONDARY_SLOT = "chatgpt-secondary"


def expected_slot(author_emails):
    emails = [email.strip().lower() for email in author_emails if email.strip()]
    if not emails:
        raise ValueError("candidate author evidence is empty")
    if all(email == AGENT_EMAIL for email in emails):
        return PRIMARY_SLOT
    return SECONDARY_SLOT


def allowed_slots(task):
    review = task.get("review")
    if not isinstance(review, dict):
        raise ValueError("trusted task review object is required")
    slots = review.get("reviewerSlots")
    if slots is None or slots == []:
        # Legacy explicit default: absence/emptiness authorizes only the primary
        # reviewer. It can never authorize chatgpt-secondary implicitly.
        return [PRIMARY_SLOT]
    if not isinstance(slots, list) or not slots or not all(isinstance(slot, str) and slot for slot in slots):
        raise ValueError("review.reviewerSlots must be a non-empty array of strings when declared")
    if len(set(slots)) != len(slots):
        raise ValueError("review.reviewerSlots must not contain duplicates")
    return slots


def main():
    parser = argparse.ArgumentParser(description="Resolve and authorize independent reviewer slots.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    expected = subparsers.add_parser("expected-slot")
    expected.add_argument("--emails-file", required=True)

    validate = subparsers.add_parser("validate-slot")
    validate.add_argument("--emails-file", required=True)
    validate.add_argument("--reviewer-slot", required=True)

    authorize = subparsers.add_parser("authorize-slot")
    authorize.add_argument("--task-file", required=True)
    authorize.add_argument("--reviewer-slot", required=True)

    args = parser.parse_args()

    if args.command == "authorize-slot":
        task = json.loads(Path(args.task_file).read_text(encoding="utf-8"))
        slots = allowed_slots(task)
        if args.reviewer_slot not in slots:
            raise SystemExit(
                f"reviewer slot {args.reviewer_slot!r} is not authorized by trusted task; allowed={slots!r}"
            )
        print(args.reviewer_slot)
        return

    emails = Path(args.emails_file).read_text(encoding="utf-8").splitlines()
    slot = expected_slot(emails)

    if args.command == "expected-slot":
        print(slot)
        return

    if args.reviewer_slot != slot:
        raise SystemExit(
            f"reviewer slot {args.reviewer_slot!r} is not independent for current candidate authorship; expected {slot!r}"
        )
    print(slot)


if __name__ == "__main__":
    main()
