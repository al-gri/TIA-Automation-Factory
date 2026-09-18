# External Review Protocol

Status: Phase 2 baseline protocol.

This protocol defines how an autonomous implementation agent requests independent review from ChatGPT and/or Gemini without relying on any previous chat context.

## Purpose

External review is an approval gate, not a replacement for deterministic tests or TIA Portal compilation. The implementer may prepare evidence and review packages, but it must never approve its own work.

GitHub is the source of truth for the task, candidate commit, review package, reviewer response, and state transition.

## Roles

- **Implementer:** DeepSeek API after Phase 2 provider integration. Until then, the current coder runtime may exercise the same protocol.
- **Primary external reviewer:** ChatGPT, especially for architecture, API boundaries, maintainability, root-cause analysis, and difficult implementation choices.
- **Independent verification reviewer:** Gemini, especially for red-team review, missed requirements, edge cases, PLC semantics, security, and second opinions.
- **Human operator:** copies a generated package into a fresh reviewer chat and pastes the structured response back into GitHub.
- **Deterministic authorities:** Linux build/tests/generator checks and, where applicable, the trusted TIA Portal V21 acceptance gate.

## Review risk policy

### LOW

Typical examples: isolated tests, simple UDT additions, small mappings, narrow bug fixes.

Required review: one independent external reviewer.

### MEDIUM

Typical examples: PLC compiler behavior, PLC IR changes, non-trivial Siemens backend generation, interlocks, state-machine logic, cross-layer contracts.

Required review: ChatGPT. Add Gemini when findings, uncertainty, size, or semantics justify a second opinion.

### HIGH

Typical examples: architecture changes, PLC semantics with safety implications, trust-boundary changes, TIA worker changes, new DSL/project formats, major compiler redesign.

Required review: independent ChatGPT and Gemini reviews of the same original package. They must not see each other's conclusions until both reviews exist.

If independent conclusions disagree, state becomes `REVIEW_CONFLICT`. The candidate is not accepted automatically.

## Review package requirements

Every package must be self-contained and safe to paste into a brand-new chat. It must include only bounded, relevant evidence rather than an entire repository or a long agent conversation.

Required sections:

1. Reviewer role and explicit instruction that no prior context exists.
2. Project purpose and relevant architecture/trust boundaries.
3. Exact task, requirements, and acceptance criteria.
4. Risk class and requested review type.
5. Candidate commit/PR identifiers.
6. Implementation summary supplied only as orientation, never as proof.
7. Complete candidate diff when reasonably bounded; otherwise the full changed symbols plus enough unchanged adjacent context to review them correctly.
8. Deterministic Linux evidence: build/tests/generator output relevant to the task.
9. Generated PLC artifact and TIA diagnostics when the review occurs after TIA acceptance or concerns PLC semantics.
10. Previous review findings and repair attempts only when they materially affect the current round.
11. Explicit review objectives.
12. The exact machine-readable response contract.

Do not include secrets, API keys, unrelated logs, full repository dumps, or hidden chain-of-thought. The reviewer evaluates requirements, code, evidence, and architecture constraints only.

## Reviewer independence

The implementer must not phrase the package to steer the reviewer toward approval. Statements such as "all requirements are satisfied" are implementation claims and must not be treated as evidence.

For dual review, ChatGPT and Gemini receive the same immutable review package. Reviewer A's result is not included in Reviewer B's initial prompt.

## Response contract

The reviewer must return exactly one JSON object conforming to:

`reviews/schemas/external-review-response.schema.json`

Top-level status values:

- `APPROVE` — no critical or major issue blocks the requested gate.
- `CHANGES_REQUIRED` — actionable candidate changes are required.
- `BLOCKED` — the reviewer cannot safely approve or specify a bounded repair because a human/architecture decision or missing evidence is required.

Findings use severity `critical`, `major`, or `minor`. A response with `APPROVE` must not contain any `critical` or `major` finding.

## State transitions

```text
AGENT_RUNNING
    -> TESTING
    -> WAITING_FOR_EXTERNAL_REVIEW

WAITING_FOR_EXTERNAL_REVIEW
    -> APPROVED_EXTERNAL_REVIEW      (APPROVE)
    -> REVIEW_CHANGES_REQUIRED       (CHANGES_REQUIRED)
    -> BLOCKED                       (BLOCKED)

REVIEW_CHANGES_REQUIRED
    -> REPAIRING
    -> TESTING
    -> WAITING_FOR_EXTERNAL_REVIEW

Dual review disagreement
    -> REVIEW_CONFLICT
```

`REVIEW_CONFLICT` and `BLOCKED` require an explicit external decision before implementation continues.

All repairs remain bounded by the task-level `maxRepairAttempts`. No external review path may create an unbounded retry loop.

## Architecture gate

If a proposed implementation crosses an established architecture boundary, changes a public compiler/domain contract, modifies the trusted Windows/TIA boundary, or introduces a new persistent project/DSL format, an architecture review occurs before implementation.

The architecture package must present the problem, constraints, alternatives, recommendation, consequences, and exact decision requested. Approval authorizes implementation within the approved constraints; it does not waive the later code-review gate.

## Code-review gate

After deterministic Linux checks pass, the candidate code-review package is generated from the current task and candidate state. `CHANGES_REQUIRED` returns the same candidate branch to bounded repair. The next review package must describe the new candidate SHA and prior finding IDs so the reviewer can verify that repairs actually address them.

## PLC/TIA review

Real TIA V21 diagnostics remain authoritative for import/compile status. External PLC review may assess generated semantics, naming, interfaces, and adequacy of PLC-specific tests, but it must not claim successful TIA compilation without the trusted `tia-diagnostics.json` evidence.

## Human copy/paste workflow

The intended human action is deliberately small:

1. Open the generated `review-request.md` in GitHub.
2. Paste the entire file into a fresh ChatGPT or Gemini chat.
3. Paste the reviewer's JSON response back into the designated GitHub issue/PR comment.
4. Trusted automation validates the JSON schema and resumes the next bounded state.

The reviewer chat requires no project history beyond the generated package.

## Acceptance rules for the future automation

Before a pasted external response may influence execution, trusted automation must:

- parse it as JSON;
- validate it against the versioned schema;
- bind it to the expected task, candidate SHA, review type, reviewer slot, and round;
- reject unknown statuses or malformed findings;
- never treat prose outside the validated JSON as an approval signal;
- preserve the response as GitHub evidence;
- prevent a candidate agent from editing trusted review/orchestration definitions.

No automatic merge is introduced by this protocol.
