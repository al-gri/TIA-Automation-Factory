#!/usr/bin/env python3
import argparse
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


def main():
    parser = argparse.ArgumentParser(description="Resolve the independent reviewer slot from candidate Git authorship.")
    subparsers = parser.add_subparsers(dest="command", required=True)

    expected = subparsers.add_parser("expected-slot")
    expected.add_argument("--emails-file", required=True)

    validate = subparsers.add_parser("validate-slot")
    validate.add_argument("--emails-file", required=True)
    validate.add_argument("--reviewer-slot", required=True)

    args = parser.parse_args()
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
