# External Review Protocol

Status: Phase 2 baseline protocol.

This protocol defines how an autonomous implementation agent requests independent review from ChatGPT and/or Gemini without relying on previous chat context.

## Purpose

External review is an approval gate, not a replacement for deterministic tests or TIA Portal compilation. The implementer or author may prepare evidence and review packages, but must never approve the same candidate as its required independent reviewer.

GitHub is the source of truth for the task, candidate commit, review package, reviewer response, and state transition.

## Roles

- **Implementer:** OpenRouter first, official DeepSeek API / `deepseek-flash` as continuity fallback.
- **Primary external reviewer:** ChatGPT, especially for architecture, API boundaries, maintainability, root-cause analysis, and difficult implementation choices when ChatGPT is independent of the candidate authorship.
- **Independent verification reviewer:** Gemini, especially for red-team review, missed requirements, edge cases, PLC semantics, security, disputed work, and candidates materially authored by ChatGPT.
- **Human operator:** final merge/approval authority; performs copy/paste only when an isolated external reviewer is actually required.
- **Deterministic authorities:** Linux build/tests/generator checks and, where applicable, the trusted TIA Portal V21 acceptance gate.

## Reviewer independence

The required reviewer must be independent of the candidate author/implementer.

- An implementer cannot approve its own work.
- ChatGPT cannot provide the required independent approval for a candidate it materially authored or repaired.
- Gemini cannot provide the required independent approval for a candidate it materially authored.
- A fresh second ChatGPT chat is not a standing reviewer role and is not required merely to create another ChatGPT identity.
- If ChatGPT authored/co-authored a HIGH-risk candidate, Gemini is the normal independent reviewer.
- If the coding agent authored the candidate and ChatGPT did not materially co-author it, ChatGPT is the normal independent reviewer.

Independence is determined by authorship and evidence separation, not by the number of chats.

## Review risk policy

### LOW

Typical examples: isolated tests, simple UDT additions, small mappings, narrow bug fixes.

Required review: one independent external reviewer. ChatGPT is the default for coding-agent candidates.

### MEDIUM

Typical examples: PLC compiler behavior, PLC IR changes, non-trivial Siemens backend generation, interlocks, state-machine logic, cross-layer contracts.

Required review: one independent external reviewer. ChatGPT is the default for coding-agent candidates; use Gemini instead when ChatGPT materially co-authored the candidate. Add a second reviewer only when findings, uncertainty, size, security/safety ambiguity, or a human request justifies escalation.

### HIGH

Typical examples: architecture changes, PLC semantics with safety implications, trust-boundary changes, TIA worker changes, new DSL/project formats, major compiler redesign.

Required review: one independent external reviewer distinct from the author/implementer.

Default reviewer selection:

- ChatGPT-authored/co-authored candidate -> Gemini;
- coding-agent-authored candidate with ChatGPT independent -> ChatGPT;
- Gemini-authored candidate -> ChatGPT.

A second independent reviewer is optional escalation, not a routine gate.

If dual review is intentionally requested, reviewers receive the same immutable original package and must not see each other's conclusions until both reviews exist. If their conclusions disagree, state becomes `REVIEW_CONFLICT`; the candidate is not accepted automatically.

## Review package requirements

Every package must be self-contained and safe to give to a reviewer with no prior conversation context. It must include only bounded, relevant evidence rather than an entire repository or a long agent conversation.

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

The package must not steer the reviewer toward approval. Statements such as "all requirements are satisfied" are implementation claims and must not be treated as evidence.

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

Intentionally requested dual-review disagreement
    -> REVIEW_CONFLICT
```

`REVIEW_CONFLICT` and `BLOCKED` require an explicit external decision before implementation continues.

All repairs remain bounded by the task-level `maxRepairAttempts`. No external review path may create an unbounded retry loop.

## Architecture gate

If a proposed implementation crosses an established architecture boundary, changes a public compiler/domain contract, modifies the trusted Windows/TIA boundary, or introduces a new persistent project/DSL format, an architecture review occurs before implementation.

The architecture package must present the problem, constraints, alternatives, recommendation, consequences, and exact decision requested. Approval authorizes implementation within the approved constraints; it does not waive the later code-review gate.

For a HIGH-risk architecture proposal, one reviewer independent of the proposal author is required. If ChatGPT authored/co-authored the proposal, Gemini fills that independent slot; do not create a mandatory second ChatGPT chat.

## Code-review gate

After deterministic Linux checks pass, the candidate code-review package is generated from the current task and candidate state. `CHANGES_REQUIRED` returns the same candidate branch to bounded repair. The next review package must describe the new candidate SHA and prior finding IDs so the reviewer can verify that repairs actually address them.

The implementation reviewer must be independent of the implementation candidate. Architecture approval does not allow an implementation author to self-approve code.

## PLC/TIA review

Real TIA V21 diagnostics remain authoritative for import/compile status. External PLC review may assess generated semantics, naming, interfaces, and adequacy of PLC-specific tests, but it must not claim successful TIA compilation without trusted `tia-diagnostics.json` evidence.

## Human workflow

The intended human action is deliberately small:

1. Ask the connected ChatGPT operator to inspect pending work.
2. If ChatGPT is eligible and independent, it performs the review directly from GitHub.
3. If ChatGPT is not independent or an explicit red-team is required, ChatGPT prepares a complete Gemini package; the human only pastes it and returns the resulting JSON.
4. Trusted automation validates the JSON schema and resumes the next bounded state.
5. The human makes the separate merge decision.

A fresh second ChatGPT chat is optional only when intentionally requested, not part of the normal workflow.

## Acceptance rules for the future automation

Before an external response may influence execution, trusted automation must:

- parse it as JSON;
- validate it against the versioned schema;
- bind it to the expected task, candidate SHA, review type, reviewer slot, and round;
- verify the reviewer slot is eligible under the authorship/independence rule;
- reject unknown statuses or malformed findings;
- never treat prose outside the validated JSON as an approval signal;
- preserve the response as GitHub evidence;
- prevent a candidate agent from editing trusted review/orchestration definitions without appropriate independent review.

No automatic merge is introduced by this protocol.
