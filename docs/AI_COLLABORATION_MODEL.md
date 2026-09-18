# AI Collaboration Model

Status: accepted and active Phase 2 design.

## Goal

Run the software factory autonomously for most implementation work while keeping architecture and critical verification independently reviewed. GitHub is the sole durable source of truth; chat is only an operator console.

## Repository-first context

A fresh session must recover project state from:

1. `AGENTS.md`;
2. `docs/PROJECT_STATE.md`;
3. `docs/NEXT_CHAT_HANDOFF.md`;
4. this document;
5. `docs/EXTERNAL_REVIEW_PROTOCOL.md` when review is involved;
6. active `tasks/*.json`, PRs, issues, Actions and artifacts;
7. `docs/GOVERNANCE_BOOTSTRAP.md` plus the authorized issue only when protected governance authorization recursion is explicitly active.

All durable role rules, provider policy, blockers, review states and meaningful outcomes must be persisted to GitHub. Secret values are never project context.

## Roles

### Coding agent — OpenRouter first, DeepSeek fallback

Routine implementation uses:

1. OpenRouter / OpenCode with the configured free coding model while available.
2. Official DeepSeek API / `deepseek-flash` as continuity fallback.

The coding agent reads a versioned task, implements the smallest coherent change, runs Linux checks, performs bounded repairs, prepares candidate PRs and records provider audit. It may not approve its own work, widen protected paths or execute candidate source on the trusted Windows/TIA host.

If OpenRouter produces useful partial changes before quota/rate exhaustion, DeepSeek continues the same bounded workspace rather than restarting.

### Primary connected ChatGPT — Senior Architect and orchestrator

Primary ChatGPT is the Senior Architect, default connected reviewer for coding-agent work and delegated technical merge authority.

Responsibilities:

- architecture/API-boundary design and review;
- task decomposition and orchestration;
- code-quality/maintainability review;
- root-cause analysis and bounded maintainer fixes;
- exact-SHA gate verification;
- routine technical merge/no-merge decisions.

Primary ChatGPT may author architecture or repairs. When it materially authors/co-authors a candidate, it cannot fill that candidate's required independent-review slot.

### Secondary independent ChatGPT

A fresh isolated ChatGPT session fills reviewer slot `chatgpt-secondary` when primary ChatGPT is not independent due to authorship, or when an explicit second independent opinion is requested.

The secondary chat receives a self-contained package generated from GitHub source of truth and must not rely on the primary chat's context. It must not be shown the primary reviewer's conclusion before producing its own verdict.

`chatgpt-secondary` replaces the previous Gemini reviewer role. Gemini has no standing role in this project and is not required by governance.

### Human operator

The human is strategic/risk authority and has delegated routine technical merge decisions to primary connected ChatGPT. Human input is reserved for strategic/materially irreversible decisions, project-goal changes, risk waivers, destructive external actions, licensing/vendor-distribution choices, protected-governance bootstrap scope authorization and unresolved reviewer conflict/ambiguity.

## Independence rule

The required reviewer must be independent from the candidate author/implementer.

Default mapping:

- coding-agent-authored candidate and primary ChatGPT did not materially co-author -> `chatgpt`;
- primary-ChatGPT-authored/co-authored candidate -> `chatgpt-secondary`;
- secondary-ChatGPT-authored/co-authored candidate -> primary `chatgpt` if independent.

One independent reviewer is sufficient by default for LOW, MEDIUM and HIGH work. A simultaneous second reviewer is escalation only for unresolved uncertainty, dispute, security/safety ambiguity or explicit human request.

If intentionally requested independent reviews disagree, state is `REVIEW_CONFLICT` and automatic acceptance is forbidden.

Independence is about authorship and evidence separation, not model branding or chat count.

## Connected operator workflow

For a normal repository/review request primary ChatGPT should:

1. recover authoritative state from GitHub;
2. inspect active tasks, candidate PRs, pending review states and relevant workflows;
3. reject stale requests whose candidate SHA no longer matches the PR head;
4. inspect the trusted task, bounded diff/source context and deterministic/TIA evidence;
5. if independent, issue `APPROVE`, `CHANGES_REQUIRED` or `BLOCKED` and write the structured response back to GitHub;
6. if not independent, prepare a complete `chatgpt-secondary` package instead;
7. when merge gates are satisfied, execute the delegated technical merge without separate human confirmation;
8. persist durable new blockers/decisions to GitHub;
9. report only operationally useful results to the user.

For the exceptional protected-governance bootstrap lane, replace step 4's trusted-task scope check with validation of the human-authorized issue, its recorded SHA-256 body fingerprint and `docs/GOVERNANCE_BOOTSTRAP.md`. Normal task-only automation remains unchanged and must fail closed when the task is absent.

## Risk-based review policy

### LOW

`OpenRouter/DeepSeek coder -> deterministic Linux checks -> one independent review -> TIA when applicable -> delegated technical merge`

Primary ChatGPT normally reviews coding-agent work. If primary ChatGPT materially co-authored the candidate, use `chatgpt-secondary`.

### MEDIUM

One independent reviewer distinct from the author is required. Add another reviewer only for explicit escalation.

### HIGH

Examples include architecture changes, PLC semantics with safety implications, trust-boundary changes, TIA worker changes, new DSL/project formats and major compiler redesign.

Required path:

`candidate/proposal -> one independent reviewer distinct from author -> deterministic gates -> TIA when applicable or trusted post-merge TIA by explicit task design -> delegated technical merge`

Reviewer selection:

- primary-ChatGPT-authored/co-authored -> `chatgpt-secondary`;
- coding-agent-authored with primary ChatGPT independent -> `chatgpt`;
- secondary-ChatGPT-authored/co-authored -> primary `chatgpt` if independent.

No standing dual-review requirement exists.

Protected-governance bootstrap is also HIGH risk. It additionally requires prior human scope authorization and an issue-body fingerprint, but it does not waive exact-SHA CI or reviewer independence.

## External Review Protocol

Normative mechanics are in `docs/EXTERNAL_REVIEW_PROTOCOL.md`.

Every external review package must be self-contained and contain the exact task/acceptance criteria, risk class, review type, candidate SHA/PR identity, bounded diff/source context, Linux evidence, TIA evidence when available, prior findings relevant to the round, explicit objectives and exact response contract. For bootstrap governance, the authorized issue + fingerprint + human authorization record replace the absent trusted task for manual review only.

Responses are validated against `reviews/schemas/external-review-response.schema.json` and bound to request ID, reviewer slot, task, candidate SHA, review type and round.

Outcomes:

- `APPROVE` -> next deterministic gate or delegated merge;
- `CHANGES_REQUIRED` -> bounded repair;
- `BLOCKED` -> external/human/architecture decision;
- conflicting intentionally requested reviews -> `REVIEW_CONFLICT`.

No unbounded repair loop is allowed; task `maxRepairAttempts` is authoritative. Bootstrap repairs must remain bounded by the human-authorized scope and return to the human if further repair would materially widen it.

## Delegated technical merge gate

Primary connected ChatGPT may merge without separate human confirmation only when, for the exact current head SHA:

- required independent review is valid and `APPROVE`;
- deterministic CI/tests are green;
- required TIA/Openness evidence is green unless the trusted task explicitly makes it post-merge;
- no unresolved `critical`/`major` finding, `BLOCKED` or `REVIEW_CONFLICT` exists;
- candidate scope matches trusted task and architecture, or for protected-governance bootstrap matches the still-valid human-authorized issue fingerprint and bootstrap policy;
- evidence is not stale.

No workflow, coding agent, reviewer or PR author self-merges automatically.

## Governance bootstrap boundary

The bootstrap lane exists only to repair authorization recursion in protected governance. It is not a fallback for missing tasks.

Normal review automation remains task-only and fail-closed. Candidate-branch tasks/policy files cannot authorize that same candidate. Human authorization is bound to an existing GitHub issue body through SHA-256; material scope changes require fresh human authorization. A primary-authored bootstrap candidate requires fresh isolated `chatgpt-secondary` review and exact-SHA green CI before delegated merge. Candidate source never executes on trusted Windows/TIA through this lane.

## Provider/cost strategy

- OpenRouter free coding model first;
- DeepSeek official API / `deepseek-flash` second;
- primary ChatGPT for architecture/orchestration/default independent review;
- secondary isolated ChatGPT for independent review when primary ChatGPT is an author;
- no standing Gemini role;
- record provider/model, calls, tokens, cache hits, fallback reason and cost evidence;
- do not add a complex provider gateway until measured use justifies it.

## TIA security boundary

This collaboration model does not weaken the trusted TIA boundary:

- AI candidate code executes only on disposable Linux;
- Windows runner checks out trusted `main`;
- Windows executes trusted code only and receives bounded task-approved inputs/artifacts;
- `src/TiaV21Worker` remains the only TIA Openness execution boundary;
- deterministic tests and real TIA Portal V21 evidence remain authoritative;
- external LLM review never replaces deterministic/TIA acceptance.

## Optional Cline role

Cline or another interactive coding agent may be used only as a human-supervised development interface. It must follow the same repository context, branch discipline, protected paths, tests, review and TIA gates. It is not the production orchestrator and never receives unrestricted trusted Windows/TIA access.

## Supervisor states

The workflow should support at least:

```text
READY
AGENT_RUNNING
TESTING
WAITING_FOR_EXTERNAL_REVIEW
APPROVED_EXTERNAL_REVIEW
REVIEW_CHANGES_REQUIRED
REVIEW_CONFLICT
REPAIRING
WAITING_FOR_TIA
TIA_FAILED
DONE
BLOCKED
```

## Implementation order

1. Infrastructure baseline completed/frozen.
2. Versioned external-review protocol and trusted validation added.
3. Connected primary ChatGPT review + bounded repair loop added.
4. OpenRouter first / DeepSeek fallback adopted.
5. One-independent-reviewer authorship policy adopted.
6. Routine technical merge authority delegated to primary ChatGPT.
7. Secondary isolated ChatGPT replaces Gemini as the independent reviewer when primary ChatGPT authored/co-authored a candidate.
8. Explicit manual protected-governance bootstrap lane added without weakening normal task-only automation.
9. Continue measuring real cost/throughput and improve automation only when generator work exposes a concrete blocker.
