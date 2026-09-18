# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-18

GitHub is the only durable source of truth. Fresh sessions recover state from this file, `AGENTS.md`, `docs/NEXT_CHAT_HANDOFF.md`, `docs/AI_COLLABORATION_MODEL.md`, `docs/DEVELOPMENT_METHODOLOGY.md`, current tasks/PRs/Actions and methodology telemetry issue #25.

## Project

Repository: `al-gri/TIA-Automation-Factory`

Purpose: build a production engineering system that converts a vendor-neutral automation model into modular Siemens PLC code/projects and verifies/assembles them through TIA Portal V21 Openness.

A second explicit project output is the reusable AI-assisted software-development methodology described in `docs/DEVELOPMENT_METHODOLOGY.md` and `docs/METHODOLOGY_JOURNAL.md`.

Baseline product flow:

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

Coding provider order:

1. OpenRouter first.
2. Official DeepSeek `deepseek-flash` fallback.
3. Primary connected ChatGPT is Senior Architect, normal reviewer for coding-agent work, orchestrator, methodology curator and delegated technical merge authority.
4. A fresh isolated second ChatGPT (`chatgpt-secondary`) is the independent reviewer when primary ChatGPT materially authored/co-authored the candidate.
5. Gemini has no standing project role.

One independent external reviewer is required by default for LOW/MEDIUM/HIGH work. Reviewer independence is authorship-based.

Technical merge decisions are delegated to primary connected ChatGPT after exact-SHA verification of all applicable gates. Human input is reserved for strategic/materially irreversible decisions, risk waivers, destructive external actions, licensing/vendor-distribution choices and unresolved review conflict/ambiguity.

Real TIA Portal V21 compile/Openness evidence remains authoritative Siemens acceptance.

## Trusted coding context

Governance PR #24 adds a bounded trusted coding-context mechanism:

- baseline context is read from trusted Git state;
- current rules/state are included before provider selection;
- trusted tasks may declare bounded `contextFiles` for design contracts and qualified Siemens/Open Library profiles;
- candidate workspace files cannot redefine trusted context;
- OpenRouter and DeepSeek receive the same enriched prompt;
- context manifest records trusted commit, included paths, byte sizes and SHA-256 identities.

This is intended to avoid dependence on model memory or accidental file discovery without introducing RAG/vector-database complexity.

## Methodology system

The project now treats methodology as a first-class parallel deliverable.

- `docs/DEVELOPMENT_METHODOLOGY.md` — curated reusable rules and active experiments.
- `docs/METHODOLOGY_JOURNAL.md` — milestone-level lessons.
- issue #25 — append-only raw methodology telemetry.
- `.github/workflows/methodology-telemetry.yml` — automatic logging of selected workflow completions and PR lifecycle events after PR #24 merges.
- `docs/INFRASTRUCTURE_LOG.md` — concise chronological infrastructure record.

Primary ChatGPT must perform methodology checkpoints at logical milestones without waiting for a user reminder.

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

### Infrastructure already merged

- PR #21 merged at `7430f83140de4bdf4d4b53c564373b15d14fb378` — task-gated `src/TiaV21Worker/**` candidate work with protected orchestration.
- PR #23 merged at `6ca057eb5dddf3986e1b00ef8e653c044c998938` — bounded HIGH repair support.

### Candidate PR #22

PR #22: `Agent: task-OLQ-001 Implement trusted V21 Open Library qualification command`.

Current implementation code head after two autonomous repair attempts plus one bounded primary-ChatGPT maintainer repair:

`4354bebbf2a3bf745b09589d6abac0938d5b5664`

CI #164 / run `35366781733`: PASS.

Code findings F001-F008 were resolved. The candidate became primary-ChatGPT-co-authored, so primary ChatGPT is no longer an independent reviewer for PR #22. Final review must use `chatgpt-secondary` after the governance task authorizes that slot on trusted `main`.

## Governance / infrastructure PR #24 — active blocker before PR #22

PR #24 is the active governance/infrastructure candidate. Its current scope now includes:

- replace Gemini standing review with authorship-based `chatgpt` / `chatgpt-secondary` review;
- treat task `reviewerSlots` as authorization, not mandatory multi-review;
- exact-SHA authorship independence and fail-closed task authorization;
- fresh external review after every candidate-changing repair;
- deterministic Candidate Validation with no hidden LLM reviewer;
- single trusted Candidate Validation dispatch source after external APPROVE;
- retire legacy I5 validation bypass and migrate `INFRA-001` into the reviewed task model;
- main-only self-hosted Windows/TIA manual workflows;
- trusted coding-context bundle for OpenRouter/DeepSeek;
- automatic methodology telemetry and living methodology documentation.

Independent review rounds F001-F010 found real workflow/governance defects and drove the hardening above. F010 identified that manual TIA workflows could check out a non-main ref; the current candidate now guards self-hosted manual TIA jobs to `refs/heads/main` and explicitly checks out `main`, with repository-wide regression coverage.

Because primary ChatGPT materially authored PR #24, **every previous secondary review is stale after the latest source/documentation changes**. Before merge, inspect the live PR #24 head and exact-SHA CI and obtain a fresh `chatgpt-secondary` review for that exact current SHA.

Do not reuse an APPROVE/CHANGES_REQUIRED payload for an older PR #24 SHA as the final gate.

## Required order of work

1. **DONE:** ARCH-001 merged.
2. **DONE:** PLC-001 merged after exact TIA validation.
3. **DONE:** OLQ infrastructure PR #21 merged.
4. **DONE:** HIGH bounded-repair PR #23 merged.
5. **ACTIVE:** finish PR #24: exact current SHA -> green CI -> fresh independent `chatgpt-secondary` review -> delegated merge if gates pass.
6. Re-review unchanged OLQ implementation PR #22 under trusted `chatgpt-secondary` authorization.
7. If independent review is APPROVE and exact-SHA CI remains green, primary ChatGPT performs delegated merge of PR #22.
8. From trusted `main`, build `TiaV21Worker` on the TIA V21 Windows runner.
9. Run qualification against the actual external Siemens Open Library V19 `.zal19`.
10. Run the same qualification identity a second time to prove deterministic reuse/no silent replacement.
11. Persist manifest/diagnostics/hashes only; vendor `.zal19/.zal21` payload stays outside public GitHub.
12. Human acceptance is required before the resulting qualified library profile becomes a normal generator dependency.
13. Continue roadmap: `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.
14. At each milestone, update methodology journal/rules from GitHub evidence and rely on issue #25 for raw automated telemetry.

## Hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch generation for MVP;
- no generic multi-vendor plugin architecture now;
- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- no candidate execution on trusted Windows/TIA before independent approval + merge;
- self-hosted manual TIA workflows must be main-only;
- no vendor archive payloads in Git;
- do not modify `IndustrialMDE`.

## Fresh-chat rule

Connected primary ChatGPT inspects GitHub itself, processes authorized review/orchestration work, maintains the methodology checkpoint, and applies delegated technical merge authority when gates pass.

When primary ChatGPT is not independent, it must prepare a complete ready-to-paste package for `chatgpt-secondary`; it must not self-approve.
