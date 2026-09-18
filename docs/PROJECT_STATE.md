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
3. Primary connected ChatGPT is Senior Architect, normal reviewer for coding-agent work, orchestrator and delegated technical merge authority.
4. A fresh isolated second ChatGPT (`chatgpt-secondary`) is the independent reviewer when primary ChatGPT materially authored/co-authored the candidate.
5. Gemini has no standing project role.

Reviewer independence is authorship-based:

- one independent external reviewer is required by default for LOW/MEDIUM/HIGH work;
- coding-agent candidate -> primary `chatgpt` when independent;
- primary-ChatGPT-authored/co-authored candidate -> `chatgpt-secondary`;
- simultaneous second review is escalation only;
- intentionally requested conflicting independent reviews -> `REVIEW_CONFLICT`.

Technical merge decisions are delegated to primary connected ChatGPT after exact-SHA verification of all applicable gates. Human input is reserved for strategic/materially irreversible decisions, risk waivers, destructive external actions, licensing/vendor-distribution choices and unresolved review conflict/ambiguity.

Real TIA Portal V21 compile/Openness evidence remains authoritative Siemens acceptance.

## Accepted architecture — ARCH-001

PR #17 is merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`; issue #18 is closed.

Important accepted constraints:

- `SameScan` vs `PreviousState` temporal semantics;
- controller-global semantic SCC/DAG validation;
- atomic once-per-scan unit/application containers plus executable-container quotient-DAG validation;
- explicit public interfaces/orchestration for cross-unit same-scan data;
- cross-controller runtime links rejected until explicit communication semantics exist;
- V19 -> V21 Open Library upgrade is a separate HIGH-risk qualification event, never normal generation;
- qualified native V21 library profiles are normal generator dependencies;
- explicit DB access mode including Standard/non-optimized legacy Error DB support;
- CPU System/Clock memory requirements are target-profile preflight concerns;
- `TiaV21Worker` remains a narrow trusted Openness boundary.

## PLC-001 — completed

PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919`.

Exact reviewed candidate: `c4999464457eb5715c5b8590cb6b4d077002640f`.

Result: PLC `TIME` support is green through Linux tests, generator regression and trusted TIA Portal V21 compile with **0 errors / 0 warnings**.

## OLQ-001 — active

Tracking issue: #19.

Canonical task: `tasks/OLQ-001.json`.

Risk: **HIGH** — TIA Openness library migration and trusted Windows boundary.

Goal: create a deterministic reusable native V21 qualified Siemens Open Library artifact/profile from an operator-controlled V19 `.zal19`, without putting vendor payload in GitHub.

### Infrastructure completed

PR #21 merged at `7430f83140de4bdf4d4b53c564373b15d14fb378`, enabling task-gated `src/TiaV21Worker/**` candidate work while preserving protected orchestration paths and trusted-main-only Windows/TIA execution.

PR #23 merged at `6ca057eb5dddf3986e1b00ef8e653c044c998938`, enabling bounded HIGH repair while keeping task policy authoritative.

### Candidate PR #22

PR #22: `Agent: task-OLQ-001 Implement trusted V21 Open Library qualification command`.

Current implementation candidate contains only `src/TiaV21Worker/Program.cs` changes and is mergeable.

Coding history:

- original implementation: OpenRouter coding agent;
- repair round 1: bounded autonomous repair;
- repair round 2: bounded autonomous repair;
- trusted `maxRepairAttempts=2` was then exhausted;
- primary ChatGPT made one bounded maintainer fix for the remaining C# `CS0157` cleanup-control-flow defect;
- exact current code candidate after that maintainer fix: `4354bebbf2a3bf745b09589d6abac0938d5b5664`;
- CI #164 / run `35366781733`: PASS.

Gemini review of that exact candidate confirmed the code defects F001-F008 were resolved but identified governance finding F009: the trusted task authorized only reviewer slot `chatgpt`, while primary ChatGPT had become a material co-author and therefore could not independently approve the candidate.

### Reviewer-policy transition

Human operator explicitly decided to replace Gemini with a second isolated ChatGPT reviewer because Gemini access to GitHub was unreliable for this workflow.

Governance PR #24 is active. Its purpose is to:

- make `chatgpt-secondary` the independent reviewer when primary ChatGPT authored/co-authored a candidate;
- remove Gemini as a standing project reviewer;
- preserve one-independent-reviewer-by-default policy;
- update `tasks/OLQ-001.json` reviewer slots from the stale ChatGPT/Gemini transition to `chatgpt` + `chatgpt-secondary`;
- keep all OLQ acceptance criteria, protected paths, repair limits and TIA trust boundaries unchanged.

Because primary ChatGPT authors this governance change, PR #24 itself requires independent review by a fresh `chatgpt-secondary` session before delegated merge.

## Required order of work

1. **DONE:** ARCH-001 merged.
2. **DONE:** PLC-001 merged after exact TIA validation.
3. **DONE:** OLQ infrastructure PR #21 merged.
4. **DONE:** HIGH bounded-repair PR #23 merged.
5. **ACTIVE:** finish reviewer-policy PR #24 with independent `chatgpt-secondary` review and delegated merge.
6. Re-review unchanged OLQ code candidate PR #22 under the new authorized `chatgpt-secondary` slot.
7. If independent review is `APPROVE` and exact-SHA CI remains green, primary ChatGPT performs delegated merge of PR #22.
8. From trusted `main`, build `TiaV21Worker` on the TIA V21 Windows runner.
9. Run qualification against the actual external Siemens Open Library V19 `.zal19`.
10. Run the same qualification identity a second time to prove deterministic reuse/no silent replacement.
11. Persist manifest/diagnostics/hashes only; vendor `.zal19/.zal21` payload stays outside public GitHub.
12. Human acceptance is required before the resulting qualified library profile becomes a normal generator dependency.
13. Continue roadmap: `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.

## Hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch generation for MVP;
- no generic multi-vendor plugin architecture now;
- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- no infrastructure expansion without a demonstrated generator blocker;
- no candidate execution on trusted Windows/TIA before independent approval + merge;
- do not modify `IndustrialMDE`.

## Fresh-chat rule

Connected primary ChatGPT inspects GitHub itself, processes authorized review/orchestration work and applies delegated technical merge authority when gates pass.

When primary ChatGPT is not independent, it must prepare a complete ready-to-paste package for `chatgpt-secondary`; it must not self-approve.
