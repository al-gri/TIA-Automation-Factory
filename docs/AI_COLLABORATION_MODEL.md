# AI Collaboration Model

Status: accepted and active Phase 2 design. I6 baseline is frozen; external-review protocol implementation is in progress under issue #7.

## Goal

Run the software factory autonomously for most implementation work while keeping architecture and critical verification under independent review. The system should minimize paid API usage without weakening the deterministic Linux/TIA acceptance path.

## Roles

### DeepSeek API — primary implementer

DeepSeek is the default autonomous coding worker. It is responsible for:

- reading a versioned Git task;
- implementing the smallest coherent source/test change;
- running Linux build/tests/generator checks;
- performing bounded repairs from structured feedback;
- preparing candidate PRs;
- generating self-contained external review packages;
- resuming work after structured review feedback is pasted back into GitHub.

DeepSeek must not approve its own work and must not make unreviewed architecture changes across established boundaries.

Target share of AI work: approximately 75-85%.

### ChatGPT — Senior Architect and primary external reviewer

ChatGPT is used manually through a fresh chat with no prior conversation context. The review package must therefore contain all context required for an independent decision.

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

The human remains the approval authority but should not have to manually assemble technical context. The target interaction is:

1. Open the generated GitHub review package.
2. Copy it into a new ChatGPT or Gemini chat.
3. Copy the structured reviewer response back into GitHub.
4. Let the autonomous worker resume from that feedback.

GitHub remains the source of truth for tasks, candidate diffs, review packages, reviewer feedback, and state transitions.

## Risk-based review policy

### LOW risk

Examples: small mapper changes, simple UDT additions, isolated tests, straightforward bug fixes.

Required path:

`DeepSeek -> deterministic Linux checks -> one external reviewer -> TIA when applicable`

The external reviewer may alternate between ChatGPT and Gemini.

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

Every external review request must be self-contained so it can be pasted into a brand-new chat with zero previous context.

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

External reviewer responses are validated against `reviews/schemas/external-review-response.schema.json` and bound to the expected task, candidate SHA, review type, and round by trusted automation.

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

- DeepSeek API as the primary paid autonomous coder with hard budget/turn limits;
- ChatGPT and Gemini used manually as external reviewers through their chat products;
- record calls, tokens, cache hits, model/provider, and estimated paid cost per task;
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
3. Add explicit GitHub external-review state transitions and automatic self-contained package generation from task + diff + evidence.
4. Add DeepSeek API as the primary coding provider with hard budgets and provider audit.
5. Resume bounded repairs from pasted external review results.
6. Measure real cost/throughput for several days.
7. Add LiteLLM/provider pooling only if actual measurements justify the complexity.

Tracking issue: #7.
