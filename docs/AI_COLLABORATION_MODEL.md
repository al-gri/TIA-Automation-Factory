# AI Collaboration Model

Status: accepted and active Phase 2 design. I6 baseline is frozen; external-review protocol implementation is tracked under issue #7.

## Goal

Run the software factory autonomously for most implementation work while keeping architecture and critical verification under independent review. Minimize paid inference without weakening deterministic Linux/TIA acceptance.

## Repository-first context discipline

GitHub is the sole durable source of truth for this project. Chat history, saved memory, and local notes are not authoritative project state.

A brand-new ChatGPT / Gemini / coding-agent session must be able to recover everything needed for its role from the repository plus GitHub issues, PRs, workflow state, and artifacts.

The required clean-session entry points are:

1. root `AGENTS.md`;
2. `docs/PROJECT_STATE.md`;
3. this document;
4. `docs/EXTERNAL_REVIEW_PROTOCOL.md` when review is involved;
5. the active task under `tasks/` and the relevant PR / workflow evidence.

All architecture decisions, role rules, provider policy, task requirements, current blockers, review states, and meaningful outcomes must be persisted to GitHub. Chat is only an operator console.

Secret values are never project context and must not be committed; only secret names, purpose, and expected GitHub Actions location may be documented.

## Roles

### Coding agent — OpenRouter first, DeepSeek continuity fallback

Routine implementation uses a bounded two-provider cascade:

1. OpenRouter / OpenCode uses the configured free coding model while the daily allowance is available.
2. Official DeepSeek API / `deepseek-flash` takes over when OpenRouter is unavailable, rate-limited, or its daily allowance is exhausted.

The coding agent is responsible for:

- reading a versioned Git task;
- implementing the smallest coherent source/test change;
- running Linux build/tests/generator checks;
- performing bounded repairs from structured feedback;
- preparing candidate PRs;
- generating self-contained external review packages;
- resuming work after structured review feedback is written back into GitHub.

If OpenRouter reaches quota after it already produced useful workspace changes, those changes are preserved and DeepSeek continues the same bounded task rather than restarting from zero. Provider process success is never the acceptance criterion; deterministic gates and independent review remain authoritative.

The coding agent must not approve its own work and must not make unreviewed architecture changes across established boundaries.

### ChatGPT — Senior Architect and primary external reviewer

ChatGPT may operate in either of two equivalent reviewer modes:

- connected mode: the user asks ChatGPT to check pending coding-agent / DeepSeek requests; ChatGPT reads the GitHub source of truth, performs the authorized review, and writes the structured response back to GitHub;
- copy/paste fallback: a self-contained review package is pasted into a fresh chat and the returned JSON is pasted back into GitHub.

Primary responsibilities:

- architecture and API-boundary review;
- code-quality and maintainability review;
- difficult debugging and root-cause analysis;
- review of compiler/domain design decisions;
- arbitration of complex implementation choices.

### Gemini — independent verification / red-team reviewer

Gemini is reserved for independent review and escalation, especially when a change is risky, architecture-sensitive, PLC-semantic, security-related, large, or disputed.

Gemini is **not** a routine coding fallback. This preserves independence between implementer and reviewer roles.

When Gemini is required, ChatGPT must prepare a complete ready-to-paste Gemini message from GitHub source of truth. The human must not have to manually collect task context, diffs, evidence, architecture rules, or the response schema.

### Human operator

The human remains the approval authority but should not have to manually assemble technical context. In connected ChatGPT mode the normal interaction is simply to ask for pending reviews and read the resulting decision/status. The GitHub copy/paste flow remains available when a connector is unavailable or an intentionally isolated fresh reviewer chat is desired.

GitHub remains the source of truth for tasks, candidate diffs, review packages, reviewer feedback, and state transitions.

## Connected ChatGPT operator console

When the GitHub connection is available, phrases such as `проверь запросы DeepSeek`, `проверь DeepSeek`, `что ждёт review?`, `проверь репозиторий`, or `проверь PR #N` are operator commands, not requests for the user to inspect GitHub manually.

For a general repository/review request ChatGPT should:

1. read `AGENTS.md` and `docs/PROJECT_STATE.md` if this is a fresh session;
2. inspect current tasks, open candidate PRs, pending `WAITING_FOR_EXTERNAL_REVIEW` requests, and failing/running trusted workflows;
3. ignore stale requests whose candidate SHA no longer matches the current PR head or which already have a terminal response;
4. read the trusted task, bounded candidate diff/source context, deterministic Linux evidence, generated artifact evidence, prior findings, and TIA evidence when present;
5. independently decide `APPROVE`, `CHANGES_REQUIRED`, or `BLOCKED` for the authorized ChatGPT/reviewer slot;
6. write a schema-valid `/external-review` response back to the same PR, preserving request ID, reviewer slot, task ID, candidate SHA, review type, and review round;
7. persist any durable new blocker/decision in GitHub;
8. report to the user only the operationally useful result: what was checked, the decision/failure, the next gate, and whether the user must do anything.

ChatGPT must not answer the independent `gemini` slot. For HIGH-risk work it may complete only the ChatGPT slot; the Gemini slot must remain independently reviewed before aggregate acceptance.

If Gemini is required, the ChatGPT response to the human must include the full ready-to-paste Gemini message. Do not merely tell the user to ask Gemini to review the PR.

Connected review never authorizes automatic merge. A final merge remains a distinct human decision.

## Risk-based review policy

### LOW risk

Examples: small mapper changes, simple UDT additions, isolated tests, straightforward bug fixes.

Required path:

`OpenRouter/DeepSeek coder -> deterministic Linux checks -> ChatGPT external review -> TIA when applicable`

### MEDIUM risk

Examples: PlcCompiler changes, new PLC IR behavior, non-trivial SiemensBackend changes, interlocks, state-machine logic, cross-layer interfaces.

Required path:

`OpenRouter/DeepSeek coder -> ChatGPT review -> Gemini if uncertainty/findings justify escalation -> TIA`

### HIGH risk

Examples: architecture changes, PLC semantics with safety implications, trust-boundary changes, TIA worker changes, new DSL/project formats, major compiler redesign.

Required path:

`Architecture proposal -> independent ChatGPT review + independent Gemini review -> approved decision -> coding agent implementation -> independent implementation review -> deterministic gates -> TIA`

ChatGPT and Gemini must review the same original package independently before seeing each other's conclusions.

If their conclusions conflict, the task enters `REVIEW_CONFLICT`; it must not be automatically accepted.

## External Review Protocol

The normative protocol is versioned in `docs/EXTERNAL_REVIEW_PROTOCOL.md`.

Every external review request must be self-contained so it can be reviewed from a brand-new chat with zero previous conversation context or through the connected ChatGPT console without relying on chat history.

A review package must contain:

- project purpose and relevant architecture boundaries;
- exact Git task and acceptance criteria;
- risk class and requested review type;
- implementation summary;
- complete candidate diff or bounded relevant code excerpts;
- unchanged interfaces/classes needed to understand the diff;
- Linux build/test/generator evidence;
- TIA diagnostics when available;
- previous repair/reviewer findings when relevant;
- explicit review objectives;
- a strict machine-readable response format.

The package must avoid dumping the entire repository or a long agent conversation. Source of truth is current Git/task/evidence, not chat history.

## Reviewer response contract

External reviewer responses are validated against `reviews/schemas/external-review-response.schema.json` and bound to the expected request ID, reviewer slot, task, candidate SHA, review type, and round by trusted automation.

The recognized outcomes are:

- `APPROVE` -> next deterministic gate.
- `CHANGES_REQUIRED` -> bounded repair on the same candidate.
- `BLOCKED` -> human/architecture escalation.
- conflicting independent reviews -> `REVIEW_CONFLICT`.

## Optional Cline / editor-agent role

Cline or a similar interactive coding agent is allowed as a human-supervised development interface, not as the production orchestrator.

Good use cases:

- interactive prototyping;
- reproducing and fixing a local bug with immediate compiler/test feedback;
- editing/refactoring while a human is actively watching;
- quickly exploring code paths before creating a normal Git task/PR.

It must use the same GitHub context and constraints as every other agent: read `AGENTS.md`, `docs/PROJECT_STATE.md`, the active task, and architecture docs; work on a branch; obey protected paths; run deterministic tests; and submit normal PR/review/TIA gates.

It must not receive unrestricted control of the trusted Windows/TIA machine and must not create a parallel source of project truth in local Cline history.

Because the current autonomous cloud pipeline already provides code execution, tests, bounded repair, external review, and TIA acceptance, Cline is optional convenience rather than a missing architectural component. Add deeper Cline automation only if measured workflow friction justifies it.

## Target states

The Phase 2 supervisor should support at least:

```text
READY
AGENT_RUNNING
TESTING
WAITING_FOR_EXTERNAL_REVIEW
WAITING_FOR_CODE_REVIEW
WAITING_FOR_ARCH_REVIEW
APPROVED_EXTERNAL_REVIEW
REVIEW_CHANGES_REQUIRED
REVIEW_CONFLICT
REPAIRING
WAITING_FOR_TIA
TIA_FAILED
DONE
BLOCKED
```

No unbounded retry loop is allowed. Existing task-level `maxRepairAttempts` remains authoritative.

## Cost and provider strategy

The current provider strategy is intentionally simple:

- OpenRouter free coding model first;
- DeepSeek official API / `deepseek-flash` as the paid continuity fallback;
- ChatGPT as connected primary external reviewer;
- Gemini only as independent verification/red-team reviewer when risk policy requires it;
- record calls, tokens, cache hits, model/provider, fallback reason, and reported/estimated paid cost per task;
- do not introduce a complex LiteLLM multi-provider gateway until measured usage shows that it is needed.

The optimization target is reliable 24/7 progress with free quota consumed first and inexpensive paid inference used only when necessary.

## Security and acceptance boundaries

This operating model does not change the trusted TIA boundary:

- AI-authored candidate code executes only on disposable Linux runners in the autonomous path;
- the Windows runner checks out trusted `main`;
- Windows receives only bounded PLC artifacts;
- trusted `TiaV21Worker` remains the only TIA Openness execution path;
- deterministic tests and real TIA compile remain authoritative;
- external LLM review never replaces TIA compile;
- no automatic merge;
- protected infrastructure remains protected from candidate agents.

## Implementation order

1. I6 baseline completed and frozen.
2. Versioned external-review protocol, templates, response schema, trusted renderer/validator, and CI tests added.
3. Explicit GitHub external-review state transitions and automatic self-contained package generation from task + diff + evidence added.
4. Connected ChatGPT review and bounded repair loop added.
5. Coding provider order changed to OpenRouter first, DeepSeek `deepseek-flash` second; Gemini reserved for independent review.
6. Ensure trusted task state is always resolved from `main` during Candidate Validation.
7. Measure real cost/throughput for several tasks.
8. Add LiteLLM/provider pooling or deeper Cline automation only if measured usage shows clear benefit.

Tracking issue: #7.
