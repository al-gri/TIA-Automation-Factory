# TIA Automation Factory — Fresh Chat Handoff

Updated: 2026-09-18

GitHub is the sole durable source of truth. Do not use old chat history as project state.

## Mandatory startup

Read, in order:

1. `AGENTS.md`
2. `docs/PROJECT_STATE.md`
3. this file
4. `docs/AI_COLLABORATION_MODEL.md`
5. `docs/DEVELOPMENT_METHODOLOGY.md`
6. latest relevant entries in `docs/METHODOLOGY_JOURNAL.md`
7. `docs/EXTERNAL_REVIEW_PROTOCOL.md` before review work

Then inspect live GitHub state: current PRs, Actions, tasks, issue #19, methodology issue #25 and pending external-review requests.

`IndustrialMDE` is outside scope and must not be touched.

## Roles and authority

- coding provider: OpenRouter first, official DeepSeek `deepseek-flash` fallback;
- primary connected ChatGPT: Senior Architect + normal coding-agent reviewer + orchestrator + methodology curator + delegated technical merge authority;
- `chatgpt-secondary`: fresh isolated ChatGPT used when primary ChatGPT materially authored/co-authored the candidate or when explicit independent escalation is requested;
- Gemini: no standing project role;
- real TIA Portal V21 compile/Openness execution: authoritative Siemens acceptance.

One independent external reviewer is the default even for HIGH risk. Coding-agent-authored candidate -> primary `chatgpt` when independent. Primary-ChatGPT-authored/co-authored candidate -> `chatgpt-secondary`.

Human input is reserved for strategic/materially irreversible decisions, risk waivers, destructive external actions, licensing/vendor-distribution choices and unresolved reviewer conflict/ambiguity.

## Current architecture

Accepted product direction:

```text
React/React Flow/table views
 -> canonical vendor-neutral AutomationProject
 -> Domain
 -> deterministic PLC Compiler / PLC IR
 -> SiemensBackend
 -> qualified Open Library catalog/bindings
 -> deterministic SCL AST/emitter
 -> bounded declarative package
 -> trusted TiaV21Worker / ProjectAssembler
 -> TIA Portal V21
```

PR #17 is merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`.

PLC-001 PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after exact trusted TIA Portal V21 compile: **0 errors / 0 warnings**.

## OLQ infrastructure already merged

- PR #21 merged at `7430f83140de4bdf4d4b53c564373b15d14fb378` — task-gated `TiaV21Worker` candidate support and protected orchestration.
- PR #23 merged at `6ca057eb5dddf3986e1b00ef8e653c044c998938` — bounded HIGH repair support.

## PR #22 — OLQ-001 implementation candidate

Canonical task: `tasks/OLQ-001.json`.

PR #22 exact code head remains:

`4354bebbf2a3bf745b09589d6abac0938d5b5664`

CI #164 / run `35366781733`: PASS.

Two autonomous repairs exhausted trusted `maxRepairAttempts=2`. Primary ChatGPT then made one bounded maintainer repair for the final C# cleanup-control-flow compile defect. Therefore primary ChatGPT is a material co-author and cannot independently approve PR #22.

Code findings F001-F008 are considered resolved. PR #22 waits for governance PR #24 to make `chatgpt-secondary` an authorized reviewer on trusted `main`.

## PR #24 — current active blocker

PR #24 is now a broad but coherent governance/infrastructure hardening candidate authored by primary ChatGPT.

It includes:

- authorship-based `chatgpt` / `chatgpt-secondary` review;
- Gemini removal from standing review runtime;
- fail-closed exact-SHA reviewer authorization;
- every candidate-changing repair returns to fresh external review;
- deterministic Candidate Validation only after exact-SHA APPROVE;
- single repository-wide Candidate Validation dispatch source;
- legacy I5 validation bypass retirement;
- `INFRA-001` migration into reviewed task semantics;
- main-only trusted Windows/TIA manual workflows;
- trusted coding-agent context bundle shared by OpenRouter and DeepSeek;
- methodology-as-a-product docs and automatic telemetry.

Independent secondary review rounds F001-F010 found real defects and shaped these controls. F010 found that manual self-hosted TIA workflows could operate on a selected non-main ref; current PR #24 code now adds both a `refs/heads/main` job guard and explicit `ref: main` checkout, with repository-wide test coverage.

Important: **do not use any old PR #24 review payload as the final gate.** The PR has changed after every prior review, including after the trusted-context and methodology additions. Inspect the live PR #24 head SHA and live exact-SHA CI first.

Because primary ChatGPT authored/co-authored PR #24, final approval must come from a fresh isolated `chatgpt-secondary` review for the exact current head.

## Trusted coding context

PR #24 introduces `agents/runtime/build-coder-context.py` and `docs/CODING_AGENT_CONTEXT.md`.

Before provider selection, coding work receives a bounded context from trusted Git state containing core rules/state plus the trusted work prompt. Tasks may declare focused `contextFiles` such as qualified Siemens/Open Library contracts.

OpenRouter and DeepSeek receive the same enriched context. Candidate workspace versions cannot redefine trusted context. The provider audit records context identities/hashes.

No RAG/vector database is currently required.

## Development methodology — second project output

The user explicitly wants the software-development method itself developed, measured and documented alongside the generator.

Durable surfaces:

- `docs/DEVELOPMENT_METHODOLOGY.md` — living reusable rules/experiments;
- `docs/METHODOLOGY_JOURNAL.md` — curated chronological lessons;
- issue #25 — append-only raw automated methodology telemetry;
- `.github/workflows/methodology-telemetry.yml` — records selected workflow completions and PR lifecycle events automatically after merge;
- `docs/INFRASTRUCTURE_LOG.md` — concise infrastructure chronology.

Primary ChatGPT must perform methodology checkpoints automatically at logical milestones; the user should not need to remind it.

## Immediate next action for the new chat

1. Read the mandatory files above.
2. Inspect live PR #24 head, changed files, comments and exact-SHA CI. Do not assume the SHA from this document is current.
3. If CI is not green, diagnose/fix before review.
4. Because primary ChatGPT authored PR #24, prepare a complete fresh `chatgpt-secondary` review package for the exact current head.
5. If that independent review returns APPROVE and the head/CI remain unchanged, primary ChatGPT performs delegated merge of #24 without asking the user for routine merge permission.
6. Re-open PR #22 under trusted main governance and obtain fresh `chatgpt-secondary` review for exact `4354bebbf2a3bf745b09589d6abac0938d5b5664`.
7. If APPROVE + CI green, primary ChatGPT merges PR #22.
8. From trusted `main`, build `TiaV21Worker` on Windows/TIA V21.
9. Qualify the operator-controlled Siemens Open Library V19 `.zal19`, then repeat the same qualification identity once to prove deterministic reuse/no silent replacement.
10. Persist manifest/diagnostics/hashes only; never commit `.zal19/.zal21` vendor payload.
11. Human acceptance is required before the qualified library profile becomes a normal generator dependency.
12. Continue roadmap: `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.
13. At each completed milestone, update methodology journal/rules and project state from GitHub evidence; raw events should already be appearing in issue #25 once telemetry is merged.

## Hard boundaries

- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- no generic multi-vendor plugin architecture now;
- no F-safety generation;
- no arbitrary hardware-from-scratch MVP expansion;
- no candidate source execution on trusted Windows/TIA before independent approval + merge;
- self-hosted manual TIA workflows are main-only;
- no vendor archive payloads in Git;
- do not modify `IndustrialMDE`.

## Fresh-chat behavior

When asked to continue the project, inspect GitHub directly and act on the live state. Do not repeat questions already answered by repository evidence.

When primary ChatGPT is independent, it reviews/merges within delegated gates. When it is not independent, it prepares the complete `chatgpt-secondary` package and never self-approves.

Treat methodology capture as part of normal completion, not optional documentation cleanup.
