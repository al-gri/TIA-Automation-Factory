# TIA Automation Factory — Fresh Chat Handoff

Updated: 2026-09-18

GitHub is the sole durable source of truth. Do not use old chat history as project state.

## Mandatory startup

Read, in order:

1. `AGENTS.md`
2. `docs/PROJECT_STATE.md`
3. this file
4. `docs/AI_COLLABORATION_MODEL.md`
5. `docs/EXTERNAL_REVIEW_PROTOCOL.md`

Then inspect current PRs, Actions, versioned tasks, issue #19 and pending external-review requests.

`IndustrialMDE` is outside scope and must not be touched.

## Roles

- coding provider: OpenRouter first, official DeepSeek `deepseek-flash` fallback;
- ChatGPT: Senior Architect + primary connected reviewer;
- Gemini: independent reviewer/red-team when authorship or escalation requires it;
- TIA Portal V21 real compile/Openness execution: authoritative Siemens acceptance;
- no automatic merge.

Reviewer independence is authorship-based. One independent external reviewer is the default even for HIGH risk. Coding-agent-authored candidate -> ChatGPT; ChatGPT-authored/co-authored candidate -> Gemini. Add a second reviewer only by escalation or explicit human request.

## Current operational state

### Architecture

PR #17 is merged. Main architecture merge commit: `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`. Issue #18 is closed.

Accepted direction:

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

Key rules:

- React Flow is not the canonical PLC format;
- Domain/PLC IR contain no Siemens names;
- `SameScan` and `PreviousState` are explicit temporal dependencies;
- controller-global semantic DAG and atomic-container quotient DAG must both be realizable;
- normal generation consumes only a qualified native V21 Open Library profile;
- V19 -> V21 upgrade is a separate HIGH-risk qualification event;
- explicit `DbAccessMode`; legacy alarm Error DB may require Standard/non-optimized access;
- target profile declares System/Clock memory requirements and trusted assembler preflight validates them;
- normal device FBs are multi-instances inside bounded unit/subsystem application FBs;
- Open Library internal instance memory is private;
- F-safety generation remains out of scope.

### PLC-001

PR #16 is merged.

Exact reviewed candidate: `c4999464457eb5715c5b8590cb6b4d077002640f`.

Merge commit: `64daa260416b7a4163f7627b668ae155db694919`.

`TIME` support passed deterministic Linux tests, connected ChatGPT review and exact trusted TIA Portal V21 compile. Final diagnostics: **0 errors / 0 warnings**. Exact `UDT_ValveConfig.scl` SHA256: `903e0f42db6879449d9a221bb2a5d476c9b41fd7685ba19db821caea17e86332`.

### Active next task: OLQ-001

Issue #19 tracks `OLQ-001: qualify Siemens Open Library V19 for TIA Portal V21`.

Goal: qualify the supplied external `.zal19` archive once into a deterministic native V21 library profile. Migration must never occur in normal project generation.

Before the coding agent can start, a concrete workflow blocker must be fixed:

- current `agent.yml` requires GeneratorCli input/output for every task;
- current `agent.yml` and `external-review-request.yml` reject all `src/TiaV21Worker/**` changes;
- current `agent.yml` still forces ChatGPT+Gemini for every HIGH task despite accepted one-independent-reviewer policy.

The alignment must be minimal: a trusted task may explicitly opt in to `src/TiaV21Worker/**`; all other protected paths remain forbidden; GeneratorCli evidence becomes optional for non-generator tasks; trusted task reviewer slots are respected. `external-review-response.yml` already avoids pre-merge Candidate Validation for HIGH work, so candidate TIA-worker code must not execute on Windows before merge.

Because the current ChatGPT authors the workflow/trust-boundary alignment, **Gemini must independently review that HIGH-risk infrastructure candidate** before human merge.

## Required work order

1. **DONE:** ARCH-001 merged.
2. **DONE:** PLC-001 merged after exact TIA validation.
3. Create minimal OLQ workflow-alignment PR.
4. Independent Gemini review of exact alignment SHA; human merge when approved.
5. Add trusted `tasks/OLQ-001.json` with explicit worker-path opt-in.
6. Start coding agent.
7. ChatGPT reviews exact coding-agent candidate.
8. Human merges approved qualification code.
9. Run qualification from trusted `main` on TIA V21 with the external `.zal19`; persist manifest/diagnostics and obtain human acceptance.
10. Continue `OL-001` -> `OL-002` -> compiler foundations -> `GEN-001` WaterSystem valve slice.

Do not add broad infrastructure, a generic plugin system, or UI work ahead of this sequence.

## Fresh-chat behavior

When the user says `проверь репозиторий`, `проверь запросы`, `что ждёт review?`, etc., inspect GitHub directly and process authorized pending ChatGPT review/orchestration actions without asking the user to collect context.

If Gemini is required, provide a complete ready-to-paste request bound to the exact current SHA; never impersonate Gemini.