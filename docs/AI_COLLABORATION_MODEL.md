# AI Collaboration Model

Status: accepted and active Phase 2 design. I6 baseline is frozen; external-review protocol implementation is tracked under issue #7.

## Goal

Run the software factory autonomously for most implementation work while keeping architecture and critical verification under independent review. Minimize paid inference and human coordination without weakening deterministic Linux/TIA acceptance.

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

### ChatGPT — Senior Architect, primary external reviewer, delegated technical merge authority

ChatGPT may operate in either of two modes:

- connected mode: the user asks ChatGPT to inspect pending coding-agent / DeepSeek requests; ChatGPT reads the GitHub source of truth, performs the authorized review, writes the structured response back to GitHub, and may execute the delegated technical merge when all applicable gates are satisfied;
- isolated fallback: a self-contained review package is pasted into a clean reviewer chat only when deliberate isolation is useful or a connector is unavailable.

Primary responsibilities:

- architecture and API-boundary design/review;
- code-quality and maintainability review;
- difficult debugging and root-cause analysis;
- review of compiler/domain design decisions;
- arbitration of complex implementation choices;
- routine technical merge/no-merge decisions after verifying current exact-SHA gates.

ChatGPT may author architecture or bounded repairs. When it materially authored or co-authored the candidate under review, it must not approve that same candidate as the required independent reviewer.

### Gemini — independent verification / red-team reviewer

Gemini is reserved for independent review and escalation, especially when a change is risky, architecture-sensitive, PLC-semantic, security-related, large, disputed, or authored by the primary ChatGPT architect.

Gemini is **not** a routine coding fallback. This preserves independence between implementer/author and reviewer roles.

When Gemini is required, ChatGPT must prepare a complete ready-to-paste Gemini message from GitHub source of truth. The human must not have to manually collect task context, diffs, evidence, architecture rules, or the response schema.

### Human operator

The human remains the strategic/risk authority but has delegated routine technical merge decisions to connected ChatGPT within the accepted gates. Human input is required for strategic or materially irreversible decisions, risk waivers, destructive external actions, licensing/vendor-distribution decisions, or unresolved reviewer conflict/ambiguity.

GitHub remains the source of truth for tasks, candidate diffs, review packages, reviewer feedback, merge evidence, and state transitions.

## Independence rule

The required reviewer must be independent of the candidate author/implementer.

- The coding agent cannot approve its own candidate.
- ChatGPT cannot fill the required independent-review slot for a candidate it materially authored or repaired.
- Gemini cannot fill the required independent-review slot for a candidate it materially authored.
- A second ChatGPT chat is **not** a standing project role and is never required merely to create another ChatGPT identity.
- When the primary ChatGPT authored a HIGH-risk candidate, Gemini is the normal independent reviewer.
- When the coding agent authored the candidate and ChatGPT did not materially co-author it, ChatGPT is the normal independent reviewer.
- A second independent reviewer is added only for explicit escalation: unresolved uncertainty, disputed findings, security/safety-sensitive ambiguity, or a human request for another opinion.

Independence is about authorship and evidence separation, not about multiplying chats.

## Connected ChatGPT operator console

When the GitHub connection is available, phrases such as `проверь запросы DeepSeek`, `проверь DeepSeek`, `что ждёт review?`, `проверь репозиторий`, or `проверь PR #N` are operator commands, not requests for the user to inspect GitHub manually.

For a general repository/review request ChatGPT should:

1. read `AGENTS.md` and `docs/PROJECT_STATE.md` if this is a fresh session;
2. inspect current tasks, open candidate PRs, pending `WAITING_FOR_EXTERNAL_REVIEW` requests, and failing/running trusted workflows;
3. ignore stale requests whose candidate SHA no longer matches the current PR head or which already have a terminal response;
4. read the trusted task, bounded candidate diff/source context, deterministic Linux evidence, generated artifact evidence, prior findings, and TIA evidence when present;
5. independently decide `APPROVE`, `CHANGES_REQUIRED`, or `BLOCKED` only when ChatGPT is eligible for the required reviewer slot under the independence rule;
6. write a schema-valid `/external-review` response back to the same PR, preserving request ID, reviewer slot, task ID, candidate SHA, review type, and review round;
7. when all delegated technical merge gates are satisfied, make the merge/no-merge decision and execute the merge without a separate human confirmation;
8. persist any durable new blocker/decision in GitHub;
9. report to the user only the operationally useful result: what was checked, the decision/failure, the next gate, and whether the user must do anything.

If ChatGPT is not independent because it materially authored the candidate, it must prepare the Gemini package instead of creating another mandatory ChatGPT chat.

Delegated technical merge never permits self-approval or workflow/bot self-merge. It is a separate operator-authorized decision made by connected ChatGPT after exact-SHA verification of all applicable gates.

## Risk-based review policy

### LOW risk

Examples: small mapper changes, simple UDT additions, isolated tests, straightforward bug fixes.

Required path:

`OpenRouter/DeepSeek coder -> deterministic Linux checks -> one independent external review (ChatGPT by default) -> TIA when applicable -> delegated technical merge`

### MEDIUM risk

Examples: PlcCompiler changes, new PLC IR behavior, non-trivial SiemensBackend changes, interlocks, state-machine logic, cross-layer interfaces.

Required path:

`author/implementer -> deterministic checks -> one independent external review -> Gemini escalation only if uncertainty/findings justify it -> TIA when applicable -> delegated technical merge`

ChatGPT is the default reviewer for coding-agent work. If ChatGPT materially co-authored the candidate, use Gemini instead.

### HIGH risk

Examples: architecture changes, PLC semantics with safety implications, trust-boundary changes, TIA worker changes, new DSL/project formats, major compiler redesign.

Required path:

`proposal/candidate -> one independent reviewer distinct from the author -> approved decision -> coding-agent implementation when applicable -> independent implementation review -> deterministic gates -> TIA when applicable or trusted post-merge TIA by explicit task design -> delegated technical merge`

Reviewer selection:

- ChatGPT-authored/co-authored candidate -> Gemini independent review;
- coding-agent-authored candidate with ChatGPT independent -> ChatGPT review;
- Gemini-authored candidate -> ChatGPT review;
- second reviewer -> only explicit escalation, not a routine gate.

When dual review is intentionally requested, both reviewers must see the same immutable original package independently before seeing each other's conclusions. If their conclusions conflict, the task enters `REVIEW_CONFLICT`; it must not be automatically accepted.

## Delegated technical merge gate

Connected ChatGPT may merge without a separate human confirmation only when all applicable gates are satisfied for the current exact PR head SHA:

- required independent review is valid and `APPROVE`;
- deterministic CI/tests are green;
- required TIA/Openness evidence is green, unless the trusted task explicitly defines trusted Windows/TIA execution as post-merge;
- no unresolved `critical`/`major` finding, `BLOCKED`, or `REVIEW_CONFLICT` exists;
- candidate scope still matches the trusted task and accepted architecture;
- review/evidence is not stale relative to the current head.

ChatGPT must stop for the human on strategic/irreversible decisions, risk waivers, destructive external actions, licensing/vendor-distribution choices, or unresolved reviewer conflict/ambiguity.

## External Review Protocol

The normative protocol is versioned in `docs/EXTERNAL_REVIEW_PROTOCOL.md`.

Every external review request must be self-contained so it can be reviewed from a brand-new reviewer context with zero previous conversation context or through the connected ChatGPT console without relying on chat history.

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

- `APPROVE` -> next deterministic gate or delegated technical merge when all remaining gates are satisfied.
- `CHANGES_REQUIRED` -> bounded repair on the same candidate.
- `BLOCKED` -> human/architecture escalation.
- conflicting intentionally requested independent reviews -> `REVIEW_CONFLICT`.

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
- ChatGPT as Senior Architect and default independent reviewer for coding-agent candidates;
- Gemini as the independent reviewer when ChatGPT authored the candidate and as an escalation/red-team reviewer when risk policy justifies it;
- no standing second-ChatGPT reviewer role;
- record calls, tokens, cache hits, model/provider, fallback reason, and reported/estimated paid cost per task;
- do not introduce a complex LiteLLM multi-provider gateway until measured usage shows that it is needed.

The optimization target is reliable 24/7 progress with free quota consumed first and inexpensive paid inference/review used only when necessary.

## Security and acceptance boundaries

This operating model does not change the trusted TIA boundary:

- AI-authored candidate code executes only on disposable Linux runners in the autonomous path;
- the Windows runner checks out trusted `main`;
- Windows receives only bounded task-approved artifacts/inputs and executes trusted code only;
- trusted `TiaV21Worker` remains the only TIA Openness execution path;
- deterministic tests and real TIA compile/Openness evidence remain authoritative;
- external LLM review never replaces deterministic/TIA acceptance;
- no coding agent, reviewer, or workflow may self-merge; connected ChatGPT may execute a delegated technical merge only after the explicit gate above;
- protected infrastructure remains protected from candidate agents except for explicit trusted-task bounded exceptions already defined by policy.

## Implementation order

1. I6 baseline completed and frozen.
2. Versioned external-review protocol, templates, response schema, trusted renderer/validator, and CI tests added.
3. Explicit GitHub external-review state transitions and automatic self-contained package generation from task + diff + evidence added.
4. Connected ChatGPT review and bounded repair loop added.
5. Coding provider order changed to OpenRouter first, DeepSeek `deepseek-flash` second; Gemini reserved for independent review/escalation.
6. Review policy simplified to one independent reviewer distinct from the author; no standing second-ChatGPT gate.
7. Trusted task state is resolved from `main` during validation/review.
8. Human delegated routine technical merge decisions to connected ChatGPT within explicit gates.
9. Measure real cost/throughput for several tasks.
10. Add LiteLLM/provider pooling or deeper Cline automation only if measured usage shows clear benefit.

Tracking issue: #7.
