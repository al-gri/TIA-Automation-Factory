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

Infrastructure is frozen unless a concrete generator task exposes a blocker.

Coding provider order:

1. OpenRouter first.
2. Official DeepSeek `deepseek-flash` fallback.
3. Gemini is independent review/red-team only.
4. ChatGPT is Senior Architect and primary connected reviewer.

No automatic merge. Real TIA Portal V21 compile is authoritative Siemens acceptance.

Reviewer independence is authorship-based:

- one independent external reviewer is required by default for LOW/MEDIUM/HIGH work;
- coding-agent candidate -> ChatGPT is the normal independent reviewer;
- ChatGPT-authored/co-authored candidate -> Gemini is the normal independent reviewer;
- a second reviewer is escalation only for uncertainty, dispute, security/safety ambiguity, or explicit human request;
- intentionally requested dual-review disagreement -> `REVIEW_CONFLICT`.

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
- only `PreviousState` cuts a same-scan dependency cycle;
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

One trusted TIA attempt timed out before diagnostics; a rerun of the unchanged exact candidate succeeded. No PLC-001 code repair was required.

## Active work — OLQ-001

Tracking issue: #19 — `OLQ-001: qualify Siemens Open Library V19 for TIA Portal V21`.

Goal: create one deterministic reusable native V21 qualified Siemens Open Library profile from the externally supplied V19 `.zal19` archive.

Risk: **HIGH** — TIA Openness library migration and trusted Windows boundary.

Hard rules:

- V19 -> V21 migration occurs only in explicit qualification, never in normal generation;
- the vendor library archive is not committed to GitHub;
- source archive SHA256 and exact TIA/Openness build identity are recorded;
- AI-authored candidate executables/scripts never run on the trusted Windows/TIA host;
- qualification code executes on Windows only after independent review + human merge, from trusted `main`;
- human acceptance is required before the resulting profile becomes a normal generator dependency.

## Concrete OLQ automation blocker

Current workflows predate the accepted architecture and block OLQ-001 in three concrete ways:

1. `agent.yml` requires every versioned task to declare a GeneratorCli input/output artifact;
2. `agent.yml` and `external-review-request.yml` unconditionally reject `src/TiaV21Worker/**`, although OLQ-001 legitimately needs a bounded trusted-task opt-in for that path;
3. `agent.yml` still forces ChatGPT+Gemini reviewer slots for every HIGH task, contradicting the accepted authorship-based one-independent-reviewer policy.

`external-review-response.yml` already does **not** dispatch pre-merge Candidate Validation for HIGH work, so the trusted-main Windows boundary is preserved. Do not add speculative pre-merge Windows execution for OLQ-001.

## Required order of work

Do not deviate without a concrete blocker/review decision.

1. **DONE:** merge accepted architecture PR #17.
2. **DONE:** finish, exact-TIA-validate, and merge PLC-001 PR #16.
3. Make the smallest owner-controlled HIGH-risk workflow alignment required by OLQ-001; because ChatGPT authors this trust-boundary change, Gemini is the independent reviewer.
4. After that review and human merge, add `tasks/OLQ-001.json` to trusted `main` with explicit bounded opt-in for `src/TiaV21Worker/**`.
5. Start the coding agent for OLQ-001.
6. ChatGPT independently reviews the exact coding-agent candidate.
7. Human merges approved qualification code.
8. Run qualification from trusted `main` on the TIA V21 runner with the actual external `.zal19` input.
9. Persist qualification manifest/diagnostics; human accepts the qualified profile.
10. Continue roadmap: `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001` WaterSystem valve vertical slice.

## Non-goals / hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch generation for MVP;
- no generic multi-vendor plugin architecture now;
- no SimaticML/YAML backend without a concrete SCL blocker;
- no broad HMI/SiVArc/alarm automation before PLC/Open Library vertical slice is stable;
- no infrastructure expansion without a demonstrated generator blocker.

## Fresh-chat rule

When asked to inspect the repository/reviews/agent state, connected ChatGPT must inspect GitHub itself, process authorized pending ChatGPT review/orchestration work, persist durable results, and report only what matters operationally.

If Gemini is required, ChatGPT must not impersonate Gemini; it prepares a complete ready-to-paste request bound to the exact candidate SHA.