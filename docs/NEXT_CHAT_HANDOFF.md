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

Then inspect current PRs, Actions, versioned tasks and pending external-review requests.

`IndustrialMDE` is outside scope and must not be touched.

## Roles

- coding provider: OpenRouter first, official DeepSeek `deepseek-flash` fallback;
- ChatGPT: Senior Architect + primary connected reviewer;
- Gemini: independent reviewer/red-team only according to risk policy;
- TIA Portal V21 real compile: authoritative Siemens acceptance;
- no automatic merge.

## Current operational state

Infrastructure/review/TIA plumbing is proven and frozen unless a real generator blocker requires change.

### PLC-001

PR #16, candidate `e5d92e3dba337aa055b2dfc3aadd91e3217fab90`.

`TIME` support passed deterministic tests, connected ChatGPT review and exact real TIA Portal V21 compile with **0 errors / 0 warnings**. PR remains a human merge decision.

### Architecture PR #17

Draft HIGH-risk proposal.

Current exact proposal SHA:

`7367a0bbc22987bc68275feab59e7f198b73b314`

Tracking issue: #18.

Round 1 Gemini review on old SHA `d8a32593ae8ab7a8a027c41afa6c8eb286065886` returned `CHANGES_REQUIRED`:

- isolate V19->V21 upgrade from normal generation;
- explicit Standard/non-optimized Error DB policy;
- System/Clock memory target preflight;
- formal SCC + DAG + stable topological scheduling.

All four findings are incorporated in the current proposal. Old review does not approve the new SHA.

**Next architecture gate:** independent ChatGPT + Gemini round 2 on exact SHA `7367a0bbc22987bc68275feab59e7f198b73b314`. Merge only when both approve the same exact SHA and the human chooses merge.

## Current architecture direction

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
- stateful boundaries + SCC validation + stable topological sort define scan scheduling;
- normal generation consumes only a qualified native V21 Open Library profile;
- V19->V21 upgrade is a separate HIGH-risk qualification/migration event;
- explicit `DbAccessMode`; legacy alarm Error DB can be Standard/non-optimized;
- target profile declares System/Clock memory requirements; assembler preflight validates them;
- normal device FBs are multi-instances inside bounded unit/subsystem application FBs;
- avoid one giant plant-wide DB and wrapper-per-device explosion;
- internal Open Library FB memory is private;
- mode/simulation are subsystem contexts;
- SCL stays primary until a concrete blocker requires another exchange format;
- F-safety generation remains out of scope.

## Required work order after architecture acceptance

1. `OLQ-001`: qualify supplied V19 library into deterministic native V21 library profile/artifact.
2. `OL-001`: inspect exact qualified `fbValve_Solenoid`, HMI/Error UDTs, dependencies/constants.
3. `OL-002`: prove CPU preflight and clean exact library materialization.
4. PLC IR own type system.
5. minimal SCL AST.
6. explicit DB access-mode support and real TIA proof.
7. canonical `TwoPositionValve`.
8. exact data-driven valve binding.
9. `GEN-001`: WaterSystem valve vertical slice -> real TIA V21 compile 0 errors.
10. only then multi-unit scale, graph/scan compiler, I/O/additional devices and broad React Flow UI.

Do not create a new infrastructure task unless one of these real generator tasks exposes a concrete blocker.

## Fresh-chat behavior

When the user says `проверь репозиторий`, `проверь запросы`, `что ждёт review?`, etc., inspect GitHub directly and process authorized pending ChatGPT review/orchestration actions without asking the user to collect context.

If Gemini is required, provide a complete ready-to-paste request bound to the exact current SHA; never impersonate Gemini.