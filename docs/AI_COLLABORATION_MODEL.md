# AI Collaboration Model

Status: accepted and active Phase 2 design. I6 baseline is frozen; external-review protocol implementation is tracked under issue #7.

## Goal

Run the software factory autonomously for most implementation work while keeping architecture and critical verification under independent review. The system should minimize paid API usage without weakening the deterministic Linux/TIA acceptance path.

## Roles

### DeepSeek API — primary implementer

DeepSeek is the default autonomous coding worker. The primary model is `deepseek-flash`. It is responsible for:

- reading a versioned Git task;
- implementing the smallest coherent source/test change;
- running Linux build/tests/generator checks;
- performing bounded repairs from structured feedback;
- preparing candidate PRs;
- generating self-contained external review packages;
- resuming work after structured review feedback is written back into GitHub.

DeepSeek must not approve its own work and must not make unreviewed architecture changes across established boundaries.

Target share of AI work: approximately 75-85%.

### ChatGPT — Senior Architect and primary external reviewer

ChatGPT may operate in either of two equivalent reviewer modes:

- connected mode: the user asks ChatGPT to check pending DeepSeek requests; ChatGPT reads the GitHub source of truth, performs the authorized review, and writes the structured response back to GitHub;
- copy/paste fallback: a self-contained review package is pasted into a fresh chat and the returned JSON is pasted back into GitHub.

Primary responsibilities:

- architecture and API-boundary review;
- code-quality and maintainability review;
- difficult debugging and root-cause analysis;
- review of compiler/domain design decisions;
- arbitration of complex implementation choices.

Target share of AI work: approximately 10-15%.

### Gemini — independent verification / red-team reviewer

Gemini is used as an independent second opinion, especially when a change is risky, architecture-sensitive, PLC-semantic, security-related, large, or disputed.

Its review prompt should intentionally search for missed requirements, edge cases, unsafe assumptions, insufficient tests, and hidden regressions rather than merely confirming the implementer's approach.

Target share of AI work: approximately 5-10%.

### Human operator

The human remains the approval authority but should not have to manually assemble technical context. In connected ChatGPT mode the normal interaction is simply to ask for pending DeepSeek reviews and read the resulting decision/status. The GitHub copy/paste flow remains available when a connector is unavailable or an intentionally isolated fresh reviewer chat is desired.

GitHub remains the source of truth for tasks, candidate diffs, review packages, reviewer feedback, and state transitions.

## Connected ChatGPT operator console

When the GitHub connection is available, phrases such as `проверь запросы DeepSeek`, `проверь DeepSeek`, `что ждёт review?`, or `проверь PR #N` are operator commands, not requests for the user to inspect GitHub manually.

For a general pending-review request ChatGPT should:

1. inspect the repository for current `WAITING_FOR_EXTERNAL_REVIEW` requests;
2. ignore stale requests whose candidate SHA no longer matches the current PR head or which already have a terminal response;
3. read the trusted task, bounded candidate diff/source context, deterministic Linux evidence, generated artifact evidence, prior findings, and TIA evidence when present;
4. independently decide `APPROVE`, `CHANGES_REQUIRED`, or `BLOCKED` for the authorized ChatGPT/reviewer slot;
5. write a schema-valid `/external-review` response back to the same PR, preserving `reviewRequestId`, `reviewerSlot`, task ID, candidate SHA, review type, and review round;
6. report to the user what was reviewed, the decision, the important findings, and which automated gate was triggered next.

ChatGPT must not answer the independent `gemini` slot. For HIGH-risk work it may complete only the ChatGPT slot; the Gemini slot must remain independently reviewed before aggregate acceptance.

Connected review never authorizes automatic merge. A final merge remains a distinct human decision.

## Risk-based review policy

### LOW risk

Examples: small mapper changes, simple UDT additions, isolated tests, straightforward bug fixes.

Required path:

`DeepSeek -> deterministic Linux checks -> one external reviewer -> TIA when applicable`

The default connected reviewer slot is ChatGPT unless a trusted task specifies another allowed slot.

### MEDIUM risk

Examples: PlcCompiler changes, new PLC IR behavior, non-trivial SiemensBackend changes, interlocks, state-machine logic, cross-layer interfaces.

Required path:

`DeepSeek -> ChatGPT review -> Gemini if uncertainty/findings justify escalation -> TIA`

### HIGH risk

Examples: architecture changes, PLC semantics with safety implications, trust-boundary changes, TIA worker changes, new DSL/project formats, major compiler redesign.

Required path:

`Architecture proposal -> independent ChatGPT review + independent Gemini review -> approved decision -> DeepSeek implementation -> independent implementation review -> deterministic gates -> TIA`

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

The package must avoid dumping the entire repository or a long agent conversation. Source of truth is the current workspace/task/evidence, not chat history.

## Reviewer response contract

External reviewer responses are validated against `reviews/schemas/external-review-response.schema.json` and bound to the expected request ID, reviewer slot, task, candidate SHA, review type, and round by trusted automation.

The recognized outcomes are:

- `APPROVE` -> next deterministic gate.
- `CHANGES_REQUIRED` -> bounded repair on the same candidate.
- `BLOCKED` -> human/architecture escalation.
- conflicting independent reviews -> `REVIEW_CONFLICT`.

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

The first Phase 2 implementation remains intentionally simple:

- DeepSeek API / `deepseek-flash` as the primary paid autonomous coder with hard turn/time limits;
- ChatGPT as the connected primary reviewer or copy/paste reviewer fallback;
- Gemini as independent verification/red-team reviewer where the risk policy requires it;
- record calls, tokens, cache hits, model/provider, and reported/estimated paid cost per task;
- do not introduce a complex LiteLLM multi-provider gateway until measured usage shows that it is needed.

The optimization target is not absolute zero cost. It is high 24/7 availability with most routine work on inexpensive inference and expensive human-grade review used only where it adds value.

## Security and acceptance boundaries

This operating model does not change the trusted TIA boundary:

- AI-authored candidate code executes only on disposable Linux runners;
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
4. DeepSeek API / `deepseek-flash` added as the primary coding provider with bounded execution and provider audit.
5. Route DeepSeek task candidates through external review before Candidate Validation/TIA.
6. Resume bounded DeepSeek repairs from validated external review results.
7. Measure real cost/throughput for several tasks.
8. Add LiteLLM/provider pooling only if actual measurements justify the complexity.

Tracking issue: #7.
