# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-19

GitHub is the only durable source of truth. Fresh sessions recover state from this file, `AGENTS.md`, `docs/NEXT_CHAT_HANDOFF.md`, `docs/AI_COLLABORATION_MODEL.md`, `docs/DEVELOPMENT_METHODOLOGY.md`, current trusted tasks, PRs, issues, Actions/TIA evidence and methodology issue #25.

## Project

Repository: `al-gri/TIA-Automation-Factory`

Purpose: build a production engineering system that converts a vendor-neutral automation model into modular Siemens PLC code/projects and verifies/assembles them through TIA Portal V21 Openness.

A second first-class output is the reusable AI-assisted software-development methodology captured in `docs/DEVELOPMENT_METHODOLOGY.md` and `docs/METHODOLOGY_JOURNAL.md`.

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

## Operating model

- Coding provider order: OpenRouter first, official DeepSeek `deepseek-flash` fallback.
- Primary connected ChatGPT: Senior Architect, orchestrator, normal independent reviewer when authorship permits, methodology curator and delegated routine technical merge authority.
- `chatgpt-secondary`: fresh isolated reviewer when primary ChatGPT materially authored/co-authored a candidate.
- Gemini has no standing role.
- One independent reviewer is required by default for LOW/MEDIUM/HIGH work.
- Exact-SHA review is mandatory; candidate-changing edits invalidate prior review.
- Real TIA Portal V21/Openness evidence is authoritative Siemens acceptance.
- Normal work is authorized by trusted versioned tasks on `main`.
- Exceptional governance recursion uses only `docs/GOVERNANCE_BOOTSTRAP.md` with positive SSH-signed human provenance.
- Candidate source/scripts never execute on trusted Windows/TIA before independent approval and merge.

## Durable completed milestones

### ARCH-001

PR #17 merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`; issue #18 closed.

Accepted architecture includes SameScan/PreviousState semantics, global SCC/DAG validation, atomic once-per-scan containers, explicit cross-unit interfaces, no cross-controller runtime links without communication semantics, one-time HIGH-risk V19 -> V21 Open Library qualification, native qualified V21 profiles as normal generator dependencies, explicit DB access mode and a narrow trusted `TiaV21Worker` Openness boundary.

### PLC-001

PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after trusted TIA Portal V21 compile with **0 errors / 0 warnings**.

### Governance hardening

- PR #24 merged at `ef7e5a00e74a9d3b994c23637aab6fc2ae2546f5` after exact-SHA CI and independent `chatgpt-secondary` approval.
- GOV-CTX-001 PR #30 merged at `0260117391abf5f0a8375699dca12caa06bafb8b` after CI #224 PASS and fresh independent review.
- GOV-BOOT-001 PR #32 merged at `60bd20841360998406db59cfb13a61cb33982566`; issue #31 closed. The permanent exceptional bootstrap lane is now defined. Normal task-backed automation remains task-only and fail-closed.

### OLQ-001 implementation

PR #22 merged at `5c8c957e6abb7004e4ee9e9382de97347ebfc9c6` from exact reviewed candidate `4354bebbf2a3bf745b09589d6abac0938d5b5664` after CI #164 PASS and independent round-4 APPROVE.

The trusted worker contains the bounded V19 `.zal19` -> native V21 `.zal21` qualification command, source/build identity, deterministic archive identity/hashing, native-current-version reopen verification and machine-readable evidence.

### OLQ-INFRA-002 trusted execution harness

PR #27 merged at `24f3a8fad542132a7aa9369be4451dd6ca0ae23f` after exact-SHA CI and fresh isolated `chatgpt-secondary` APPROVE.

The harness runs only trusted `main` on `[self-hosted, Windows, X64, tia-v21]`, keeps vendor payloads and raw diagnostics runner-local, and publishes sanitized evidence only.

## Current trusted `main`

Checkpoint SHA: `69f554445003975279a391d6c2672d45054ab418`.

During creation of CHAT-HANDOFF-001 two temporary placeholder writes accidentally targeted `main` and were immediately reverted. No placeholder remains in the tree. The history movement nevertheless invalidated the previously prepared PR #37 review package. This is treated as an observability/process incident, not hidden history.

Trusted task `tasks/CHAT-HANDOFF-001.json` is present on `main` and defines the bounded documentation/process candidate for seamless primary-chat transfer.

## Active priority — complete real Open Library qualification

Tracking issue: #19 `OLQ-001`.

The goal is one deterministic reusable native TIA Portal V21 qualified Siemens Open Library profile from the operator-controlled V19 `.zal19`, followed by explicit human acceptance before that profile becomes a normal generator dependency.

Trusted runtime progression:

1. initial run was blocked before worker execution by Windows PowerShell execution policy;
2. operator changed CurrentUser execution policy to `RemoteSigned` without weakening MachinePolicy/UserPolicy;
3. later run produced sanitized `BLOCKED` because runner-local archive env configuration was absent;
4. operator configured runner-local source/output paths and relaunched the user-mode runner;
5. trusted run `35426328232` reached the first real `qualify-library` invocation but the outer 45-minute job timeout cancelled the operation before sanitized evidence was finalized;
6. post-job cleanup terminated remaining Siemens Portal-related processes.

The timeout run proves neither vendor-library failure nor successful qualification.

## OLQ-INFRA-003 — issue #36 / PR #37

Purpose: make the real qualification invocation timeout bounded and observable so a long/stalled run yields sanitized `BLOCKED` evidence instead of an evidence-less generic FAIL.

Current live checkpoint:

- branch: `chatgpt/olq-infra-003-bounded-timeout`
- base: `main` at `69f554445003975279a391d6c2672d45054ab418`
- exact head: `0c9633fe657b8949beea76f87f3a63411cd103e0`
- changed files: exactly `.github/workflows/tia-v21-library-qualification.yml`
- exact-SHA CI #264 / run `35429912399`: **PASS**
- candidate Windows/TIA execution: none
- required reviewer: `chatgpt-secondary` because primary ChatGPT materially authored the candidate.

The previous candidate `cff83ee7284fa1ea2c4d75621ddfe720914a5688` and its review package are stale after the `main` history change/resynchronization and must not be reused.

Next gate: fresh isolated exact-SHA secondary CODE_REVIEW for `0c9633fe...`; on APPROVE with unchanged head/scope/CI, primary may delegated-merge and rerun `/run-olq-001` from issue #19.

## CHAT-HANDOFF-001 — issue #38

Trusted task: `tasks/CHAT-HANDOFF-001.json` on `main`.

Candidate branch: `chatgpt/chat-handoff-protocol`.

Goal: make replacement of a context-exhausted primary chat a repository-first freshness transaction: audit live GitHub, reconcile stale/chat-only state, persist factual checkpoint, then emit a compact bootstrap prompt for the fresh primary chat.

Candidate scope is documentation/process only. It must not change product code, workflows, TIA worker, runner configuration, review authority, delegated merge semantics or `IndustrialMDE`.

Primary ChatGPT authors the candidate, so exact-SHA CI plus fresh isolated `chatgpt-secondary` approval are required before merge.

Preferred sequencing: finish PR #37 first; then synchronize/review/merge CHAT-HANDOFF-001 so this documentation change does not unnecessarily stale the protected workflow review again.

## Methodology system

Durable methodology surfaces:

- `docs/DEVELOPMENT_METHODOLOGY.md` — curated reusable rules and experiments;
- `docs/METHODOLOGY_JOURNAL.md` — milestone lessons;
- issue #25 — append-only raw methodology telemetry;
- `docs/INFRASTRUCTURE_LOG.md` — concise infrastructure/process chronology.

Current accepted rules include:

- M-013 — control-plane authority stays separate from mixed model-visible prompt content;
- M-014 — governance bootstrap requires semantic eligibility and positive cryptographic human provenance;
- CHAT-HANDOFF-001 candidate proposes the next rule: role transfer is a transactional repository freshness checkpoint, not memory transfer.

## Accepted next operating-model initiative

AUTO-001 / issue #35 remains accepted: routine OpenRouter/DeepSeek implementation should run autonomously to explicit checkpoints; `WAITING_FOR_REVIEW` means primary performs full semantic/code/architecture review when independent; primary-authored candidates route to `chatgpt-secondary`.

AUTO-001 remains downstream of the immediate OLQ qualification blocker.

## Required order of work

1. Obtain fresh exact-SHA `chatgpt-secondary` review for PR #37 head `0c9633fe...`.
2. If gates remain green and verdict is APPROVE, delegated-merge PR #37.
3. Rerun trusted `/run-olq-001` from issue #19.
4. If PASS, verify sanitized source/build/qualification/archive identities, native reopen and deterministic second run, then stop for explicit human qualification acceptance.
5. If bounded timeout/BLOCKED persists, use the resulting sanitized phase evidence to decide whether a separate worker-level stall task is required; do not broaden OLQ-INFRA-003 semantics retroactively.
6. Synchronize and independently review CHAT-HANDOFF-001, then merge if gates pass.
7. Continue AUTO-001 and roadmap `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.
8. Perform methodology checkpoints at every logical milestone.

## Hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch generation for MVP;
- no generic multi-vendor plugin architecture now;
- no broad UI/HMI expansion before PLC/Open Library vertical slice stability;
- no candidate execution on trusted Windows/TIA before independent approval + merge;
- self-hosted manual TIA workflows are main-only;
- no vendor archive payloads in Git;
- no secrets/private human signing key material in repository/CI/runners;
- do not modify `IndustrialMDE`.

## Fresh-chat rule

A fresh primary ChatGPT must recover state from GitHub, not old chat memory. When the human requests chat transfer, follow `docs/CHAT_HANDOFF_PROTOCOL.md`: freeze discretionary work, perform a live freshness audit, persist factual state, mark stale evidence explicitly and generate a compact bootstrap prompt for the next chat.
