# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-18

GitHub is the only durable source of truth. Fresh sessions recover state from this file, `AGENTS.md`, `docs/NEXT_CHAT_HANDOFF.md`, `docs/AI_COLLABORATION_MODEL.md`, current tasks/PRs/Actions and review requests.

## Project

Repository: `al-gri/TIA-Automation-Factory`

Purpose: build a production engineering system that converts a vendor-neutral automation model edited through React/React Flow/table/property views into modular Siemens PLC code/projects and verifies/assembles them through TIA Portal V21 Openness.

Baseline flow:

```text
Engineering UI
 -> canonical AutomationProject
 -> Domain
 -> PLC Compiler / PLC IR
 -> Siemens Backend / qualified Open Library bindings
 -> generated PLC artifact/package
 -> trusted TIA V21 Worker / ProjectAssembler
 -> TIA Portal V21
 -> compile/save/diagnostics
```

`IndustrialMDE` is outside scope and must not be modified.

Domain/compiler/backend must not reference `Siemens.Engineering`; Openness remains isolated in `src/TiaV21Worker` targeting .NET Framework 4.8.

## Operating model

Infrastructure is proven and frozen unless a real generator blocker requires change.

Coding provider order:

1. OpenRouter first.
2. Official DeepSeek `deepseek-flash` fallback.
3. Gemini is independent review/red-team only.
4. ChatGPT is Senior Architect and primary connected reviewer.

No automatic merge. Real TIA Portal V21 compile is authoritative Siemens acceptance.

Risk policy:

- LOW: ChatGPT review + deterministic/TIA gates as applicable.
- MEDIUM: ChatGPT; Gemini only when escalation/uncertainty requires it.
- HIGH architecture/PLC semantics/TIA trust/library migration/security: independent ChatGPT + Gemini on the same exact SHA; disagreement => `REVIEW_CONFLICT`.

## Proven infrastructure / Phase 2

Connected repository-first review and trusted TIA validation are proven end to end. Historical proof is in `docs/PHASE2_PROOF_2026-09-18.md`.

Do not add more infrastructure-only smoke work without a concrete generator need.

## PLC-001 — first real generator task

Task: `tasks/PLC-001.json`

Candidate PR: #16

Candidate SHA: `e5d92e3dba337aa055b2dfc3aadd91e3217fab90`

Goal: add PLC `TIME` end-to-end through JSON -> Domain -> PLC IR -> Siemens SCL.

Current evidence:

- coding agent completed bounded change;
- deterministic Linux tests PASS;
- Motor regression PASS;
- connected ChatGPT review APPROVE;
- exact `UDT_ValveConfig.scl` passed trusted Windows transfer;
- TIA Portal V21 import/generation/compile PASS;
- TIA result: **0 errors / 0 warnings**.

PR #16 remains a human merge decision. Automation must not auto-merge it.

## Siemens Open Library baseline

Accepted current research baseline: `docs/OPEN_LIBRARY_BASELINE.md`.

Important confirmed principles:

- normal field-device FB integration favors multi-instance memory;
- internal library FB memory is private;
- HMI/Error UDTs and documented outputs form the external contract;
- `iStatus`/scrolling `iErrorCode` are not PLC-control state;
- mode and simulation are normally subsystem/system contexts;
- Open Library constants/tag-table and CPU System/Clock memory are prerequisites;
- legacy Open Library alarm generation requires Error DB standard/non-optimized access;
- interlock/permissive preserve named condition/HMI semantics;
- sequencer blocks require special shared-instance semantic design.

## Architecture proposal PR #17

PR #17: `Architecture: target PLC generator design and implementation roadmap`

Status: **DRAFT / NOT ACCEPTED**

Current exact candidate SHA: `7367a0bbc22987bc68275feab59e7f198b73b314`

Tracking issue: #18

Proposal documents:

- `docs/TARGET_ARCHITECTURE.md`
- `docs/OPEN_LIBRARY_INTEGRATION_RULES.md`
- `docs/ENGINEERING_RULES.md`
- `docs/GENERATOR_ROADMAP.md`

### Architecture review Round 1

Independent Gemini review on old SHA `d8a32593ae8ab7a8a027c41afa6c8eb286065886` returned **CHANGES_REQUIRED** with four blocking findings:

1. dynamic V19->V21 library upgrade must not be part of normal generation;
2. Error DB access mode must explicitly support Standard/non-optimized for legacy Open Library alarm tooling;
3. target CPU System/Clock memory prerequisites need deterministic preflight validation;
4. combinational scheduling needs formal SCC/DAG/stable topological ordering.

All four findings were incorporated into current proposal SHA `7367a0bbc22987bc68275feab59e7f198b73b314`.

Because SHA changed, Round 1 does not approve the current candidate. **Round 2 independent ChatGPT + Gemini review is required before PR #17 can merge.**

## Current target architecture direction

The proposal currently defines:

- React Flow as UI adapter only; canonical versioned `AutomationProject` is the source language;
- vendor-neutral Domain and PLC IR;
- explicit PLC scan semantics with stateful boundaries, SCC validation and stable topological scheduling;
- qualified/versioned Open Library catalog/bindings;
- V19->V21 upgrade as a separate HIGH-risk library qualification event;
- normal generation consumes only a qualified native V21 library profile;
- SCL as primary generated language with a minimal structural AST before real FB calls;
- explicit per-DB `DbAccessMode` with Standard/non-optimized Error DB support;
- bounded unit/subsystem instance DBs with Open Library device FBs as multi-instances;
- no giant plant-wide instance DB and no wrapper FB per physical device by default;
- mode/simulation at subsystem level;
- target-profile preflight for CPU System/Clock memory prerequisites;
- TiaV21Worker evolves only as needed into a narrow declarative ProjectAssembler;
- broad React Flow UI deferred until first real valve vertical slice is proven.

## Required order of work

Do not deviate without a concrete blocker/review decision.

1. Human decides whether to merge already-passing PLC-001 PR #16.
2. Complete independent architecture review Round 2 for current PR #17 SHA `7367a0bbc22987bc68275feab59e7f198b73b314`.
3. Resolve any Round 2 findings; merge PR #17 only when both independent reviewer slots approve the exact same SHA and human chooses merge.
4. After architecture acceptance, first implementation priority is **OLQ-001**: qualify supplied Open Library V19 into a deterministic native V21 library profile/artifact. This is a real generator dependency, not infrastructure exploration.
5. Then **OL-001** inspect exact qualified `fbValve_Solenoid`, HMI/Error UDTs, dependencies and constants.
6. Then **OL-002** prove target-profile CPU prerequisite preflight and clean library materialization in V21.
7. Then implement compiler foundations actually required by the real FB slice: PLC IR type system, minimal SCL AST and explicit DB access mode.
8. Define canonical `TwoPositionValve` and exact data-driven Open Library binding.
9. First major milestone **GEN-001**: `TwoPositionValve -> fbValve_Solenoid -> FB_WaterSystem + bounded DB_WaterSystem + HMI/Error DBs -> real TIA V21 compile 0 errors`.
10. Only after GEN-001: scale to multiple units, source maps, typed graph/scheduling, I/O and additional device families.
11. Broad React Flow/product UI follows the proven generator, not the other way around.

Detailed task/milestone roadmap exists only in PR #17 until that architecture is approved and merged.

## Non-goals / hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch generation for MVP;
- no generic multi-vendor plugin architecture now;
- no SimaticML/YAML backend without a concrete SCL blocker;
- no broad HMI/SiVArc/alarm automation before PLC/Open Library vertical slice is stable;
- no infrastructure expansion without a demonstrated generator blocker.

## Fresh-chat rule

When asked to check repository/reviews/agent state, connected ChatGPT must inspect GitHub itself, process authorized pending ChatGPT review/orchestration work, write durable results back to GitHub and report only what matters operationally.

If Gemini review is required, ChatGPT must not impersonate Gemini; it must provide a complete ready-to-paste review request bound to the exact current SHA.