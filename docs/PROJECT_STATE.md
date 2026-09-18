# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-18

GitHub is the only durable source of truth. Fresh sessions recover state from this file, `AGENTS.md`, `docs/NEXT_CHAT_HANDOFF.md`, `docs/AI_COLLABORATION_MODEL.md`, current tasks/PRs/Actions and review requests.

## Project

Repository: `al-gri/TIA-Automation-Factory`

Purpose: build a production engineering system that converts a vendor-neutral automation model into modular Siemens PLC code/projects and verifies/assembles them through TIA Portal V21 Openness.

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

Infrastructure is frozen unless a concrete generator task exposes a blocker.

Coding provider order:

1. OpenRouter first.
2. Official DeepSeek `deepseek-flash` fallback.
3. Gemini is independent review/red-team only.
4. ChatGPT is Senior Architect, primary connected reviewer, and delegated technical merge authority.

Reviewer independence is authorship-based:

- one independent external reviewer is required by default for LOW/MEDIUM/HIGH work;
- coding-agent candidate -> ChatGPT is the normal independent reviewer;
- ChatGPT-authored/co-authored candidate -> Gemini is the normal independent reviewer;
- a second reviewer is escalation only for uncertainty, dispute, security/safety ambiguity, or explicit human request;
- intentionally requested dual-review disagreement -> `REVIEW_CONFLICT`.

Technical merge decisions are delegated to connected ChatGPT after exact-SHA verification of all applicable gates. Human input is reserved for strategic/materially irreversible decisions, risk waivers, destructive external actions, licensing/vendor-distribution choices, or unresolved review conflict/ambiguity.

Real TIA Portal V21 compile/Openness evidence remains authoritative Siemens acceptance.

## Accepted architecture — ARCH-001

PR #17 is **MERGED**.

Main architecture merge commit: `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`.

Tracking issue #18 is closed.

Accepted architecture documents:

- `docs/TARGET_ARCHITECTURE.md`
- `docs/OPEN_LIBRARY_INTEGRATION_RULES.md`
- `docs/ENGINEERING_RULES.md`
- `docs/GENERATOR_ROADMAP.md`
- `docs/AI_COLLABORATION_MODEL.md`
- `docs/EXTERNAL_REVIEW_PROTOCOL.md`

Important accepted constraints include:

- `SameScan` vs `PreviousState` temporal dependency semantics;
- controller-global semantic SCC/DAG validation;
- atomic once-per-scan unit/application containers plus executable-container quotient-DAG validation;
- explicit public interfaces/orchestration for cross-unit same-scan data;
- ordinary cross-controller runtime connections rejected until explicit communication semantics exist;
- V19 -> V21 Open Library upgrade is a separate HIGH-risk qualification event, never normal generation;
- qualified native V21 library profiles are normal generator dependencies;
- explicit DB access mode, including Standard/non-optimized legacy Error DB support;
- CPU System/Clock memory requirements are target-profile preflight concerns;
- `TiaV21Worker` remains a narrow trusted Openness boundary, not a semantic compiler.

## PLC-001 — completed

Task: `tasks/PLC-001.json`.

PR #16 is **MERGED**.

Exact reviewed candidate: `c4999464457eb5715c5b8590cb6b4d077002640f`.

Merge commit: `64daa260416b7a4163f7627b668ae155db694919`.

Result:

- PLC `TIME` supported JSON -> Domain -> PLC IR -> Siemens SCL;
- deterministic Linux tests PASS;
- Motor regression PASS;
- independent connected ChatGPT review APPROVE;
- exact generated `UDT_ValveConfig.scl` SHA256: `903e0f42db6879449d9a221bb2a5d476c9b41fd7685ba19db821caea17e86332`;
- trusted TIA Portal V21 import/generation/compile PASS;
- final TIA result: **0 errors / 0 warnings**.

## OLQ-001 — active

Tracking issue: #19 — `OLQ-001: qualify Siemens Open Library V19 for TIA Portal V21`.

Goal: create one deterministic reusable native V21 qualified Siemens Open Library profile from an operator-controlled external V19 `.zal19` archive.

Risk: **HIGH** — TIA Openness library migration and trusted Windows boundary.

### Workflow unblock completed

PR #21 is **MERGED**.

Exact independently reviewed candidate: `3a66224e361c68ca065d3b3946a6278760b59138`.

Merge commit: `7430f83140de4bdf4d4b53c564373b15d14fb378`.

Gemini independently approved the exact candidate. The merged alignment:

- supports non-GeneratorCli versioned tasks;
- permits bounded `src/TiaV21Worker/**` candidate changes only when a trusted task from `main` explicitly sets `candidatePolicy.allowTiaV21WorkerChanges=true`;
- keeps `.github/**`, `agents/**`, `tasks/**` and other orchestration paths candidate-protected;
- respects explicit one-independent-reviewer task policy for HIGH work;
- preserves trusted-main-only Windows/TIA execution.

### Versioned task ready

Canonical task: `tasks/OLQ-001.json`.

Task commit: `48b7ae7e80505ef75fc05ceef3e48bef2cd836e8`.

Key task requirements:

- preserve the existing SCL smoke path;
- add explicit `qualify-library` behavior in `TiaV21Worker`;
- hash source `.zal19` before TIA processing;
- use V21 `GlobalLibraries.RetrieveWithUpgrade(...)` for the old compressed archive;
- save the upgraded library and archive it as explicit `.zal21` with `LibraryArchivationMode.Compressed`;
- derive deterministic qualification identity from source SHA256 + TIA/Openness build;
- fail on identity/hash mismatch instead of silent overwrite;
- verify the result using current-version `GlobalLibraries.Retrieve(...)`, not another upgrade;
- record machine-readable manifest/diagnostics and hashes only; vendor payload stays outside GitHub;
- no normal generator path performs migration.

### Current operational blocker

The repository workflow itself is ready, but the currently connected GitHub API/connector does not expose GitHub Actions `workflow_dispatch` creation. Therefore connected ChatGPT cannot directly start `Autonomous Agent` in task mode through the available connector.

Do **not** work around this by using issue-mode `agent-ready`: issue mode intentionally does not receive the trusted `TiaV21Worker` task opt-in and would correctly reject the OLQ candidate.

The next mechanical action is to run `.github/workflows/agent.yml` / **Autonomous Agent** with:

- `source = task`
- `task_path = tasks/OLQ-001.json`
- `issue_number` left empty

Once that run exists, connected ChatGPT resumes autonomous orchestration: inspect agent result/PR, independently review the exact coding-agent candidate, repair if required, and perform the delegated technical merge when gates pass.

## Required order of work

1. **DONE:** ARCH-001 merged.
2. **DONE:** PLC-001 merged after exact TIA validation.
3. **DONE:** bounded OLQ workflow alignment independently reviewed and merged as PR #21.
4. **DONE:** `tasks/OLQ-001.json` added to trusted `main`.
5. **NEXT:** start `Autonomous Agent` in task mode for `tasks/OLQ-001.json`.
6. ChatGPT independently reviews the exact coding-agent candidate.
7. ChatGPT merges approved qualification code under delegated technical merge authority.
8. Run qualification from trusted `main` on the TIA V21 runner with the actual external `.zal19` input.
9. Persist qualification manifest/diagnostics; human acceptance is required before the resulting library profile becomes a normal generator dependency.
10. Continue roadmap: `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001` WaterSystem valve vertical slice.

## Non-goals / hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch generation for MVP;
- no generic multi-vendor plugin architecture now;
- no SimaticML/YAML backend without a concrete SCL blocker;
- no broad HMI/SiVArc/alarm automation before PLC/Open Library vertical slice is stable;
- no infrastructure expansion without a demonstrated generator blocker.

## Fresh-chat rule

When asked to inspect the repository/reviews/agent state, connected ChatGPT must inspect GitHub itself, process authorized pending ChatGPT review/orchestration work, apply delegated technical merge authority when gates pass, persist durable results, and report only what matters operationally.

If Gemini is required, ChatGPT must not impersonate Gemini; it prepares a complete ready-to-paste request bound to the exact candidate SHA.
