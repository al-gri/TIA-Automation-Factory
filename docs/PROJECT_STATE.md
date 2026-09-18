# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-18

This is the first operational state document to read after root `AGENTS.md`.

GitHub is the only durable source of truth. A fresh ChatGPT / Gemini / coding-agent session must recover the project from this repository, issues, pull requests, Actions evidence, and versioned tasks without relying on any previous chat.

For a clean continuation, also read `docs/NEXT_CHAT_HANDOFF.md`.

## Project

Repository: `al-gri/TIA-Automation-Factory`

Purpose: build an automation engineering software factory that converts vendor-neutral automation models into Siemens PLC artifacts and verifies them through a trusted TIA Portal V21 / TIA Openness boundary.

Architecture baseline:

```text
Automation input
  -> Domain Model
  -> PLC Compiler / PLC IR
  -> Siemens Backend
  -> generated PLC artifact
  -> trusted TIA V21 Worker
  -> TIA Portal V21
```

Modern Domain / compiler / backend code must not depend on `Siemens.Engineering`. TIA Openness stays isolated in `src/TiaV21Worker` targeting .NET Framework 4.8.

`IndustrialMDE` is outside this repository's scope and must not be modified.

## Current operating model

Infrastructure through I6 is complete and frozen. Phase 2 repository-first AI orchestration is active and proven end to end.

Coding provider order is authoritative and implemented in `agents/runtime/run-coder.sh` plus the agent/repair workflows:

1. **OpenRouter first** using the configured free coding model while quota is available.
2. **DeepSeek second** using official API model `deepseek-flash` when OpenRouter is unavailable, rate-limited, timed out, or its daily allowance is exhausted.
3. If OpenRouter hits quota after producing real workspace changes, those changes are preserved and DeepSeek continues the same bounded task instead of restarting from zero.
4. Gemini is not a routine coding fallback; it is reserved for independent review / red-team escalation.

ChatGPT is the Senior Architect and primary connected external reviewer. TIA Portal V21 remains the deterministic Siemens acceptance authority. No automatic merge is allowed.

## Fresh-chat operator contract

Root `AGENTS.md` is mandatory. `docs/NEXT_CHAT_HANDOFF.md` contains the full durable handoff.

When the user says `проверь репозиторий`, `проверь DeepSeek`, `проверь запросы DeepSeek`, `что ждёт review?`, or equivalent, connected ChatGPT must inspect GitHub itself, perform any authorized ChatGPT review/orchestration action, write durable results back to GitHub, and report only the operationally important result to the user.

The user must not be asked to manually collect context already present in GitHub.

If Gemini is required by risk policy, ChatGPT must not impersonate Gemini. It must provide a complete ready-to-paste Gemini message generated from GitHub source of truth.

## Provider implementation proof

OpenRouter is a proven working coding provider from the earlier autonomous/I6 path. DeepSeek official API is also proven independently.

DeepSeek proof run `35318239703`:

- provider: `deepseek`
- model: `deepseek/deepseek-flash`
- step finishes: 14
- input tokens: 16,324
- output tokens: 2,271
- reasoning tokens: 1,490
- cache read tokens: 187,264
- reported cost: `$0.005266992`

Repository-first / OpenRouter-first policy implementation was merged in PR #15. Main implementation commit for that infrastructure slice: `77d383a8073fb04286f54d47a3fa87b2653dbf83`.

That change also fixed Candidate Validation so trusted task JSON is always resolved from `main` rather than from the candidate checkout.

The exact OpenRouter -> DeepSeek continuation path is configured and ready. Normal generator-development tasks should collect live provider-order/fallback metrics rather than adding infrastructure-only smoke tests without a concrete need.

## Connected ChatGPT -> TIA proof

Task: `tasks/PHASE2-001.json`

Candidate PR: #13

Candidate SHA: `178f1dc376bc347a4dbb533a5efd43e4e6996dc3`

Connected ChatGPT recovered the pending external review entirely from GitHub, checked the trusted task, candidate SHA, bounded diff, source context, Linux test evidence, and generated artifact, then submitted a schema-valid `APPROVE` back to GitHub.

External Review Response run `35319434427` validated and bound that response to the exact request/task/SHA/reviewer slot and dispatched Candidate Validation.

The first Candidate Validation attempt exposed the old task-source trust bug. PR #15 fixed it by resolving task state from trusted `main`.

Fresh Candidate Validation run `35322785146` then completed successfully end to end:

- trusted task resolution from `main`: PASS;
- protected-path check: PASS;
- deterministic Linux evidence: PASS;
- Requirements Reviewer: PASS;
- candidate PLC artifact generation/package: PASS;
- trusted Windows/TIA V21 acceptance: PASS;
- PLC/TIA Reviewer: PASS;
- `repair-or-finish`: PASS with no repair required.

TIA V21 diagnostics for the exact `UDT_Motor.scl` candidate artifact:

```json
{
  "success": true,
  "state": "Success",
  "warnings": 0,
  "errors": 0
}
```

`UDT_Motor (UDT)` and `Main (OB1)` both compiled successfully.

Detailed proof record: `docs/PHASE2_PROOF_2026-09-18.md`.

## First real generator task — PLC-001

Task: `tasks/PLC-001.json`

Candidate PR: #16

Candidate SHA: `e5d92e3dba337aa055b2dfc3aadd91e3217fab90`

Goal: add the first Open-Library-driven scalar prerequisite, `TIME`, through the full JSON -> Domain -> PLC IR -> Siemens SCL -> TIA V21 path.

Observed state:

- coding provider: OpenRouter free model; DeepSeek fallback was not needed;
- deterministic Linux tests: PASS;
- Motor regression generation: PASS;
- connected ChatGPT external review: APPROVE;
- exact `UDT_ValveConfig.scl` candidate transferred through the trusted Windows boundary: PASS;
- TIA Portal V21 import/generation/compile: PASS;
- TIA result: 0 errors / 0 warnings.

PR #16 remains a human merge decision; passing automation does not auto-merge.

## Open Library architecture analysis

`docs/OPEN_LIBRARY_BASELINE.md` records the first accepted research baseline from the supplied Siemens Open Library V19 archive.

Important confirmed principles include:

- normal Open Library FB integration favors multi-instance memory;
- internal FB instance memory is private and must not become an application API;
- HMI/Error UDTs and explicit outputs are the external contract;
- `iStatus` and scrolling `iErrorCode` are HMI display values, not PLC-control state;
- mode is normally organized per subsystem for medium/large systems;
- simulation is propagated as PLC context through `bInSimulate`;
- Open Library constants/tag table and CPU System/Clock memory are project prerequisites;
- `fbInterlock`/`fbPermissive` preserve named condition semantics;
- sequencer blocks have special shared-instance semantics and require separate design.

## Target architecture proposal under review

Draft PR: #17

Tracking issue: #18

Current proposal head: `f546185080b5505ed89fcc907ed7af30e076ebb2`

Risk: **HIGH**.

The proposal is documentation-only and is **not yet accepted architecture**. It contains:

- `docs/TARGET_ARCHITECTURE.md`;
- `docs/OPEN_LIBRARY_INTEGRATION_RULES.md`;
- `docs/ENGINEERING_RULES.md`;
- `docs/GENERATOR_ROADMAP.md`.

It proposes the route from React/React Flow through a canonical UI-independent automation model, deterministic compiler/PLC IR, Siemens/Open-Library catalog/bindings and modular multi-instance application generation to a future narrow trusted TIA V21 ProjectAssembler.

Before PR #17 can be merged, repository HIGH-risk policy requires independent ChatGPT + independent Gemini architecture review. Issue #18 contains the ready-to-paste Gemini review package bound to the exact proposal SHA. A reviewer disagreement must be treated as `REVIEW_CONFLICT`, not silently resolved.

## Risk / Gemini policy

LOW risk: OpenRouter/DeepSeek coder -> ChatGPT external review -> deterministic/TIA gates as applicable.

MEDIUM risk: OpenRouter/DeepSeek coder -> ChatGPT review -> Gemini only if findings/uncertainty justify escalation -> deterministic/TIA gates.

HIGH risk / architecture / PLC semantics / security: independent ChatGPT and Gemini reviews are required. Reviewer disagreement produces `REVIEW_CONFLICT` and blocks automatic acceptance.

## Current phase and next work

Core Phase 2 plumbing is operational and frozen. Work is now in generator architecture / compiler / Siemens Open Library development.

Current order of work:

1. Human decides whether to merge passing PLC-001 PR #16.
2. Complete HIGH-risk independent architecture review for PR #17, including the Gemini review package in issue #18.
3. Resolve any architecture findings before merging PR #17.
4. After architecture is accepted, create the next small versioned implementation task from the accepted roadmap rather than adding ad-hoc features.
5. Proposed next compiler tasks are: give PLC IR its own type system, then introduce a minimal structural SCL AST while preserving existing generated output.
6. Then prove trusted V21 upgrade/inspection/materialization of the supplied Open Library and exact `fbValve_Solenoid` dependencies before broad catalog expansion.
7. First major generator milestone is the real vertical slice: vendor-neutral `TwoPositionValve` -> pinned `fbValve_Solenoid` mapping -> multi-instance subsystem FB + HMI/Error DB -> real TIA V21 compile with zero errors.
8. React Flow/product UI work follows that proof; React Flow must remain an editor adapter rather than the canonical PLC semantics model.
9. Continue collecting provider/model/token/cache/cost/duration/repair metrics on real tasks; do not add provider/infrastructure complexity without measured need.

For normal implementation work:

1. create / use a versioned task under `tasks/`;
2. coding agent attempts OpenRouter first and DeepSeek second;
3. candidate passes deterministic Linux checks;
4. connected ChatGPT external review is required according to risk policy;
5. `CHANGES_REQUIRED` resumes bounded repair on the same PR;
6. `APPROVE` continues the unchanged candidate through trusted Candidate Validation / TIA;
7. Gemini is invoked only when risk policy requires an independent second reviewer;
8. no automatic merge.

## Required documents

- `AGENTS.md` — mandatory clean-chat / agent entry point and user-command semantics.
- `README.md` — repository overview and mandatory start links.
- `docs/PROJECT_STATE.md` — current authoritative operational snapshot.
- `docs/NEXT_CHAT_HANDOFF.md` — full fresh-chat project handoff and work order.
- `docs/AI_COLLABORATION_MODEL.md` — roles, provider cascade, risk policy.
- `docs/EXTERNAL_REVIEW_PROTOCOL.md` — normative external-review protocol.
- `docs/OPEN_LIBRARY_BASELINE.md` — current accepted Open Library research baseline.
- `docs/PHASE2_PROOF_2026-09-18.md` — completed connected-review/TIA proof.
- `docs/INFRASTRUCTURE_LOG.md` — chronological infrastructure history.
- `docs/GENERATOR_CHAT_HANDOFF.md` — generator/domain/compiler focused handoff.
- `tasks/*.json` — trusted executable task specifications.
- `reviews/` — review templates and machine-readable response schema.
- `.github/workflows/` — trusted orchestration implementation.

Proposal documents in PR #17 are not normative until that PR passes HIGH-risk review and is merged.

## Human-facing response policy

When the user asks ChatGPT to check the repository or AI requests, the chat response should contain only:

- what is currently waiting / broken / completed;
- what ChatGPT checked or changed;
- the review decision when applicable;
- the next gate;
- whether the user must do anything.

Do not dump routine logs or large diffs into chat. If Gemini is needed, include the complete ready-to-paste Gemini message directly in the response.
