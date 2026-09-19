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
5. `docs/DEVELOPMENT_METHODOLOGY.md` and relevant journal entries;
6. `docs/EXTERNAL_REVIEW_PROTOCOL.md` when review is involved;
7. `docs/CHAT_HANDOFF_PROTOCOL.md` when assuming/handing off the primary role;
8. active `tasks/*.json`, PRs, issues, Actions and artifacts;
9. `docs/GOVERNANCE_BOOTSTRAP.md` plus the authorized issue only when governance-authority recursion is explicitly active.

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
- routine technical merge/no-merge decisions;
- repository-first state/methodology persistence;
- clean transfer of the primary role to a fresh chat when context is exhausted or the human requests handoff.

Primary ChatGPT may author architecture or repairs. When it materially authors/co-authors a candidate, it cannot fill that candidate's required independent-review slot.

### Secondary independent ChatGPT

A fresh isolated ChatGPT session fills reviewer slot `chatgpt-secondary` when primary ChatGPT is not independent due to authorship, or when an explicit second independent opinion is requested.

The secondary chat receives a self-contained package generated from GitHub source of truth and must not rely on the primary chat's context. It must not be shown the primary reviewer's conclusion before producing its own verdict.

`chatgpt-secondary` replaces the previous Gemini reviewer role. Gemini has no standing role in this project and is not required by governance.

### Human operator

The human is strategic/risk authority and has delegated routine technical merge decisions to primary connected ChatGPT. Human input is reserved for strategic/materially irreversible decisions, project-goal changes, risk waivers, destructive external actions, licensing/vendor-distribution choices, governance-bootstrap scope authorization/reauthorization, repair-budget extension and unresolved reviewer conflict/ambiguity.

For the exceptional bootstrap lane, positive human authority is represented by an SSH-signed Git attestation commit created outside ChatGPT/Codex/project automation with a human-controlled signing key. Owner attribution, comments, app-attribution metadata and unsigned API actions are not substitutes.

## Primary-chat continuity

The primary role belongs to the repository operating contract, not to one conversation instance.

When the human writes `переходим в другой чат`, `переходим в новый чат`, `готовь handoff`, or an equivalent unambiguous transfer request, the current primary must execute `docs/CHAT_HANDOFF_PROTOCOL.md`.

The transfer model is:

```text
old primary chat
  -> live GitHub audit
  -> factual state persistence
  -> explicit stale-evidence detection
  -> compact bootstrap prompt
  -> fresh primary chat
  -> independent live GitHub verification
  -> continue current authorized gate
```

The old transcript and hidden memory are not authority and are not required for continuity. The fresh primary receives the same role/merge authority only after it re-runs the repository startup sequence and verifies live state itself.

A handoff must not silently invalidate or carry forward exact-SHA review evidence. If a base/head/review becomes stale during checkpoint persistence, that fact and the required fresh gate are durable handoff state.

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

For the exceptional governance-bootstrap lane, replace step 4's trusted-task scope check with validation of the frozen issue-body fingerprint, the exact SSH-signed human scope-attestation commit and `docs/GOVERNANCE_BOOTSTRAP.md`. A primary-authored bootstrap candidate uses fresh `chatgpt-secondary`; an authority-bearing APPROVE must also be contained in a separate SSH-signed human review-attestation commit. Normal task-only automation remains unchanged and must fail closed when the task is absent.

For a primary-chat transfer, the connected operator workflow is suspended at a handoff checkpoint and `docs/CHAT_HANDOFF_PROTOCOL.md` controls the transition. A handoff does not create new implementation/merge authority.

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

Governance bootstrap is also HIGH risk. It additionally requires semantic authorization-recursion eligibility, a frozen issue-body fingerprint, positive SSH-signed human scope authorization, exact-SHA CI, reviewer independence and a separate SSH-signed authority-bearing secondary review attestation.

## External Review Protocol

Normative mechanics are in `docs/EXTERNAL_REVIEW_PROTOCOL.md`.

Every external review package must be self-contained and contain the exact task/acceptance criteria, risk class, review type, candidate SHA/PR identity, bounded diff/source context, Linux evidence, TIA evidence when available, prior findings relevant to the round, explicit objectives and exact response contract. For bootstrap governance, the frozen issue + fingerprint + signed scope-attestation evidence replace the absent trusted task for manual review only.

Responses are validated against `reviews/schemas/external-review-response.schema.json` and bound to request ID, reviewer slot, task, candidate SHA, review type and round.

Outcomes:

- `APPROVE` -> next deterministic gate or delegated merge;
- `CHANGES_REQUIRED` -> bounded repair;
- `BLOCKED` -> external/human/architecture decision;
- conflicting intentionally requested reviews -> `REVIEW_CONFLICT`.

No unbounded repair loop is allowed; task `maxRepairAttempts` is authoritative. Bootstrap repairs use the bounded budget in `docs/GOVERNANCE_BOOTSTRAP.md`; a material issue-body/scope change invalidates prior signed scope authorization and requires fresh signed reauthorization.

## Delegated technical merge gate

Primary connected ChatGPT may merge without separate human confirmation only when, for the exact current head SHA:

- required independent review is valid and `APPROVE`;
- deterministic CI/tests are green;
- required TIA/Openness evidence is green unless the trusted task explicitly makes it post-merge;
- no unresolved `critical`/`major` finding, `BLOCKED` or `REVIEW_CONFLICT` exists;
- candidate scope matches trusted task and architecture, or for governance bootstrap matches the still-valid **SSH-signed human-authorized** issue fingerprint and bootstrap policy;
- bootstrap APPROVE evidence is contained in the required separate SSH-signed review-attestation commit;
- evidence is not stale.

No workflow, coding agent, reviewer or PR author self-merges automatically.

## Governance bootstrap boundary

The bootstrap lane exists only to repair genuine recursion in the repository's normative authority/review-control model. Eligibility is semantic, not determined by whether changed files happen to appear in the coding-agent protected-path list. It is not a fallback for missing, inconvenient or stale tasks.

Normal review automation remains task-only and fail-closed. Candidate-branch tasks/policy files cannot authorize that same candidate. Human authorization is bound to a frozen GitHub issue body through SHA-256 plus a valid SSH-signed scope-attestation commit under a human-controlled key unavailable to project automation. A primary-authored bootstrap candidate requires fresh isolated `chatgpt-secondary` review and exact-SHA green CI. An authority-bearing secondary APPROVE must be bound into a separate SSH-signed review-attestation commit. Candidate source never executes on trusted Windows/TIA through this lane.

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
8. Explicit manual governance-authority bootstrap lane added without weakening normal task-only automation.
9. Repository-first primary-chat handoff protocol added so conversation replacement does not depend on memory/transcript continuity.
10. Continue measuring real cost/throughput and improve automation only when generator work exposes a concrete blocker.
