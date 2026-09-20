# RETIRED — legacy Requirements Reviewer prompt

This file is intentionally non-authoritative and exists only so historical links fail safely.

Do **not** use it to build a review request, define a response shape, or decide acceptance.

Canonical CODE_REVIEW authority:
- review instructions: `reviews/templates/code-review.md`;
- response contract: `reviews/schemas/external-review-response.schema.json`;
- operating/reviewer independence rules: `AGENTS.md` and `docs/EXTERNAL_REVIEW_PROTOCOL.md`.

The useful rule from the former prompt is preserved in the canonical CODE_REVIEW template: every applicable task acceptance criterion must be evaluated individually against concrete diff/test/evidence, and missing evidence must not be treated as PASS.
