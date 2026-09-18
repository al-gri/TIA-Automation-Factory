# External Review Protocol

Status: Phase 2 baseline protocol.

This protocol defines independent review without relying on previous chat context. GitHub is the source of truth for task, candidate SHA, review package, reviewer response and state transition.

## Purpose

External review is an approval gate, not a replacement for deterministic tests or TIA Portal compilation. The implementer/author may prepare evidence and review packages but must never approve the same candidate as its required independent reviewer.

## Roles

- **Implementer:** OpenRouter first, official DeepSeek API / `deepseek-flash` as continuity fallback.
- **Primary external reviewer (`chatgpt`):** connected primary ChatGPT when independent of candidate authorship.
- **Secondary independent reviewer (`chatgpt-secondary`):** a fresh isolated ChatGPT session used when primary ChatGPT materially authored/co-authored the candidate or when an explicit extra independent opinion is requested.
- **Human operator:** strategic/risk authority; routine technical merge decisions are delegated to primary connected ChatGPT when all gates are satisfied.
- **Deterministic authorities:** Linux build/tests/generator checks and, where applicable, trusted TIA Portal V21 acceptance.

Gemini has no standing role in this protocol. `chatgpt-secondary` replaces the previous Gemini reviewer role.

## Reviewer independence

The required reviewer must be independent of the candidate author/implementer.

- An implementer cannot approve its own work.
- Primary ChatGPT cannot provide the required independent approval for a candidate it materially authored or repaired.
- Secondary ChatGPT cannot provide the required independent approval for a candidate it materially authored or repaired.
- Coding-agent-authored candidate with primary ChatGPT independent -> `chatgpt`.
- Primary-ChatGPT-authored/co-authored candidate -> `chatgpt-secondary`.
- Secondary-ChatGPT-authored/co-authored candidate -> primary `chatgpt` if independent.

The secondary reviewer must operate from a fresh chat and a self-contained GitHub-grounded package. It must not rely on the primary chat's context and must not be shown the primary reviewer's conclusion before issuing its own verdict.

One independent reviewer is the default. A simultaneous second reviewer is optional escalation only.

`review.reviewerSlots` is an authorization allow-list, not a set of mandatory simultaneous reviewers. When the field is absent or an empty array on a legacy task, the only authorized default is `chatgpt`; `chatgpt-secondary` is never authorized implicitly and must be explicitly listed by the current trusted task. The only exception to trusted-task slot authorization is the manual protected-governance bootstrap lane in `docs/GOVERNANCE_BOOTSTRAP.md`; that exception is not accepted by task-only review automation.

## Review risk policy

### LOW

One independent external reviewer. Primary ChatGPT normally reviews coding-agent candidates.

### MEDIUM

One independent reviewer distinct from the author. Use `chatgpt-secondary` when primary ChatGPT materially co-authored the candidate. Add another reviewer only for explicit escalation.

### HIGH

Examples: architecture changes, PLC semantics with safety implications, trust-boundary changes, TIA worker changes, new DSL/project formats and major compiler redesign.

Required review: one independent external reviewer distinct from the author/implementer.

Default reviewer selection:

- primary-ChatGPT-authored/co-authored -> `chatgpt-secondary`;
- coding-agent-authored with primary ChatGPT independent -> `chatgpt`;
- secondary-ChatGPT-authored/co-authored -> primary `chatgpt` if independent.

No standing dual-review gate exists. If multiple independent reviews are intentionally requested, reviewers receive the same immutable package and must not see each other's conclusions until both exist. Disagreement becomes `REVIEW_CONFLICT` and blocks automatic acceptance.

## Review package requirements

Every package must be self-contained and safe for a reviewer with no prior conversation context. Include only bounded relevant evidence.

Required sections:

1. reviewer role and instruction that no prior context exists;
2. project purpose and relevant architecture/trust boundaries;
3. exact trusted task and acceptance criteria, or for the exceptional governance-bootstrap lane the authorized issue, its recorded SHA-256 body fingerprint, the durable human-authorization record, and `docs/GOVERNANCE_BOOTSTRAP.md`;
4. risk class and review type;
5. candidate PR and exact SHA;
6. implementation summary as orientation only;
7. complete bounded diff or changed symbols plus enough unchanged context;
8. deterministic Linux evidence;
9. generated artifact/TIA evidence when available and applicable;
10. previous findings/repair attempts when relevant;
11. explicit review objectives;
12. exact machine-readable response contract.

Do not include secrets, unrelated logs, full repository dumps or hidden chain-of-thought. Do not steer the reviewer toward approval.

## Response contract

The reviewer returns exactly one JSON object conforming to:

`reviews/schemas/external-review-response.schema.json`

Top-level outcomes:

- `APPROVE` — no critical or major finding blocks the requested gate;
- `CHANGES_REQUIRED` — bounded candidate changes are required;
- `BLOCKED` — safe approval/repair cannot proceed because evidence or an external decision is missing.

An `APPROVE` response must not contain any `critical` or `major` finding.

Trusted automation binds the response to exact request ID, reviewer slot, task ID, candidate SHA, review type and round. Reviewer slot strings are explicit identities such as `chatgpt` and `chatgpt-secondary`, not interchangeable aliases.

For a governance-bootstrap review, primary ChatGPT performs the same identity/exact-SHA validation manually and records the returned JSON as PR evidence; the task-only automation must continue to reject the missing trusted task rather than infer bootstrap authority.

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

Intentionally requested multi-review disagreement
    -> REVIEW_CONFLICT
```

`REVIEW_CONFLICT` and `BLOCKED` require an explicit external decision before implementation continues. Repairs remain bounded by trusted task `maxRepairAttempts`; bootstrap authorization must likewise define a bounded repair allowance or return to the human when further repair would widen the authorized scope.

## Architecture gate

If a proposal crosses an established architecture boundary, changes a public compiler/domain contract, modifies the trusted Windows/TIA boundary or introduces a persistent project/DSL format, architecture review occurs before implementation.

For HIGH-risk architecture work, one reviewer independent of the proposal author is required. If primary ChatGPT authored/co-authored the proposal, `chatgpt-secondary` fills that slot.

Architecture approval authorizes implementation within approved constraints; it does not waive later code review.

## Code-review gate

After deterministic Linux checks pass, the code-review package is generated from current trusted task/candidate state. `CHANGES_REQUIRED` returns the same candidate branch to bounded repair. The next review package must carry the new candidate SHA and relevant prior finding IDs.

The implementation reviewer must be independent of the implementation candidate. If primary ChatGPT makes a material maintainer repair after reviewing a coding-agent candidate, reviewer ownership transitions to `chatgpt-secondary` for the resulting exact SHA.

## PLC/TIA review

Real TIA V21 diagnostics remain authoritative for import/compile/Openness status. External review may assess semantics, naming, interfaces and test adequacy, but must not claim successful TIA execution without trusted evidence.

After external review, trusted Candidate Validation is deterministic: Linux tests/package generation plus machine validation of TIA V21 diagnostics. It must not invoke Gemini, OpenRouter, or any other hidden LLM reviewer provider. The external `chatgpt`/`chatgpt-secondary` gate is the independent LLM review; trusted TIA diagnostics are the target authority.

Candidate `TiaV21Worker` code is not executed on Windows before independent approval and trusted merge when the task explicitly defines post-merge TIA validation.

## Protected-governance bootstrap review

`docs/GOVERNANCE_BOOTSTRAP.md` defines a narrow manual lane for maintainer/primary-authored protected governance repairs when establishing a trusted task first would create the same authorization recursion being repaired.

The lane is HIGH risk and requires explicit human authorization of a bounded GitHub issue before acceptance. Primary ChatGPT records a SHA-256 fingerprint of the exact authorized issue body. Any material issue-body change invalidates that authorization.

A bootstrap candidate cannot authorize itself. Candidate-branch tasks or policy text do not become trusted authorization for that same candidate. The required independent reviewer is fresh `chatgpt-secondary`, exact-SHA deterministic CI must be green, and no candidate source may execute on trusted Windows/TIA.

Bootstrap evidence is validated and persisted manually by primary ChatGPT. The existing external-review request/response automation remains task-only and fail-closed. This exception must not be implemented by weakening the automation's trusted-task checks.

## Delegated technical merge gate

Primary connected ChatGPT may execute routine merge without separate human confirmation only when all applicable gates pass for the exact current PR head SHA:

1. required independent review is valid and `APPROVE`;
2. deterministic CI/tests are green;
3. required TIA/Openness evidence is green unless trusted task explicitly defines it as post-merge;
4. no unresolved `critical`/`major`, `BLOCKED` or `REVIEW_CONFLICT` exists;
5. candidate still matches trusted task and architecture, or for the exceptional bootstrap lane the live authorized issue still matches its recorded fingerprint and the diff remains inside that human-authorized scope;
6. review/evidence is not stale.

Primary ChatGPT stops for human input on strategic/materially irreversible decisions, risk waivers, destructive external actions, licensing/vendor-distribution decisions, material bootstrap scope widening or unresolved review conflict/ambiguity.

No implementer, coding agent, reviewer or workflow may self-merge automatically.

## Human workflow

The intended human involvement is small:

1. Ask primary connected ChatGPT to inspect pending work.
2. If primary ChatGPT is independent, it reviews directly from GitHub.
3. If primary ChatGPT is not independent, it prepares a complete ready-to-paste `chatgpt-secondary` package.
4. The human pastes that package into a fresh ChatGPT chat and returns only the JSON verdict.
5. Trusted automation validates normal task-backed responses and continues the bounded state machine. For the exceptional governance-bootstrap lane, primary validates and records the response manually because task-only automation must fail closed.
6. Primary ChatGPT performs delegated technical merge when gates pass.

Human scope authorization is additionally required before the exceptional governance-bootstrap lane may be used.

## Acceptance rules for automation

Before any normal task-backed external response affects trusted state or execution, trusted automation must:

- parse JSON;
- validate against the versioned schema;
- bind request ID, reviewer slot, task, candidate SHA, review type and round;
- reject stale candidate SHA;
- re-resolve the current trusted task and verify risk class, review type and requested reviewer-slot authorization;
- derive reviewer eligibility from exact-candidate authorship and verify the reviewer is independent for that SHA;
- require both trusted-task slot authorization **and** exact-candidate authorship-based independence before publishing any `APPROVED_EXTERNAL_REVIEW`, `REVIEW_CHANGES_REQUIRED` or `BLOCKED` review-state marker;
- apply the legacy missing/empty-slot default only to `chatgpt`; never infer authorization for `chatgpt-secondary`;
- reject malformed statuses/findings;
- preserve only fully authorized/independent validated responses as GitHub review-state evidence;
- never treat prose outside validated JSON as approval;
- prevent candidate agents from editing trusted review/orchestration definitions.

Automation must not infer the governance-bootstrap lane from a missing task, issue text, reviewer response, branch naming, or candidate files. Missing trusted-task authorization remains a hard automation failure.

This protocol authorizes primary connected ChatGPT to execute delegated technical merges after gates; it never authorizes workflow/bot self-merge.
