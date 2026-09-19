# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-19

GitHub is the only durable source of truth. Read live GitHub state before acting; this snapshot is a recovery aid, not a substitute for current PR/Actions/task evidence.

## Project

Repository: `al-gri/TIA-Automation-Factory`.

Purpose: build a production engineering system that converts a vendor-neutral automation model into modular Siemens PLC code/projects and verifies/assembles them through TIA Portal V21 Openness. A second explicit output is the reusable AI-assisted development methodology.

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

`IndustrialMDE` is outside scope and must not be modified. Domain/compiler/backend remain vendor-neutral; `Siemens.Engineering` is isolated in `src/TiaV21Worker`.

## Operating model

- Coding provider order: OpenRouter first, official DeepSeek `deepseek-flash` fallback.
- Primary connected ChatGPT: Senior Architect, normal independent reviewer for coding-agent work, orchestrator, methodology curator and delegated technical merge authority.
- `chatgpt-secondary`: fresh isolated reviewer when primary materially authored/co-authored a candidate.
- Gemini has no standing project role.
- Candidate source executes on disposable Linux only; trusted Windows/TIA executes trusted `main` only.
- One independent reviewer is required by default; approval binds to the exact candidate SHA.
- Normal implementation authority comes from versioned trusted tasks on `main`.

## Durable completed milestones

- ARCH-001 PR #17 merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`.
- PLC-001 PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after trusted TIA V21 compile: 0 errors / 0 warnings.
- Governance bootstrap PR #32 is merged at `60bd20841360998406db59cfb13a61cb33982566`; it is no longer an active blocker.
- OLQ trusted harness PR #27 is merged at `24f3a8fad542132a7aa9369be4451dd6ca0ae23f`; `/run-olq-001` is the trusted-main qualification trigger on issue #19.
- OLQ-DIAG-001 diagnostic instrumentation is merged and exposes public-safe phase/type/HRESULT while retaining full raw exception details runner-local only.
- TIA-AUTH implementation and repairs through PR #55 are functionally accepted. PR #55 merged at `755a3d2014105a15697e8d945c7ca8feb0891792`.
- Trusted OLQ run `35458909671` on that merge successfully passed whitelist synchronization/Openness admission and reached TIA Portal V21 `RetrieveWithUpgrade`; `phase:bootstrap-required` did not recur. Issues #46 and #54 are closed completed.

## Current product blocker — OLQ V19 -> V21 migration

Tracking issue: #19.

Latest trusted evidence: run `35458909671`:

- source: `Open Library V19.zal19` (vendor payload remains outside Git);
- source SHA256: `ed8fe3f52e90399475b321e40f7ce842d86e414324f06f1b078f44fcb666eed3`;
- TIA/Openness identity: `Siemens.Engineering v21.0.0.0 (file: 2100.0.121.1)`;
- qualification identity: `olq-c6c75a368b16bbf34a367cb843ba5e55`;
- failure: `phase:retrieve-with-upgrade type:EngineeringTargetInvocationException hresult:0x80131500`;
- no native `.zal21` archive was produced, so native reopen and deterministic second run were not reached.

Do not guess a migration workaround from the wrapper exception. The current public token lacks the Siemens reason/detail needed to distinguish likely causes.

## Active trusted task — OLQ-DIAG-002

Issue: #56. Task: `tasks/OLQ-DIAG-002.json`. Risk: HIGH.

Goal: keep `RetrieveWithUpgrade -> Save -> Archive -> current-version Retrieve` unchanged while deriving bounded privacy-safe diagnostics from `EngineeringException.MessageData` / `DetailMessageData`.

Candidate scope is `src/TiaV21Worker/Program.cs` only (plus at most one tiny source-level test if strictly justified). Public evidence may contain only fixed allowlisted semantic tags, detail count and deterministic SHA-256 fingerprints; arbitrary Siemens exception text, paths, product/library object names and vendor contents must remain private.

No candidate Windows/TIA execution. After independent exact-SHA review and merge, rerun `/run-olq-001` from issue #19 and use the resulting safe tags/fingerprints to design the next migration task.

## Separate cleanup — TIA-AUTH-IDEMP-001

Issue: #53. Task: `tasks/TIA-AUTH-IDEMP-001.json`. Risk: HIGH.

The elevated bootstrap now applies the correct narrow ACL, but two consecutive runs both print `Granting...`; existing-rule detection is not idempotent. The queued repair is script-only and must normalize existing ACL identities to SID while preserving exact `SetValue | QueryValues`, Allow, no inheritance/propagation and the exact application Entry key.

This cleanup must not be mixed into OLQ-DIAG-002. It is queued behind the migration diagnostic because TIA-AUTH functional admission is already accepted.

## Current trusted main checkpoint

Operational checkpoint before this documentation update: `4b27064e19395a350bb0e2879eb959640cc0775d`.

CI #317 / run `35459383614` on that checkpoint: PASS.

That commit contains both trusted tasks `tasks/OLQ-DIAG-002.json` and `tasks/TIA-AUTH-IDEMP-001.json`. Always read live `main` before dispatch/review because this state-documentation commit moves the branch afterward.

## Open PR hygiene

PR #39 is an older draft handoff-protocol candidate and is not part of the active OLQ implementation path. Legacy experimental PRs #4/#5/#10/#13 also remain open historically. Do not treat any of them as active implementation authority without a fresh live task/scope/evidence audit.

## Required order of work

1. Launch Autonomous Agent in task mode for `tasks/OLQ-DIAG-002.json`.
2. Primary connected ChatGPT reviews the coding-agent exact candidate, enforces Linux CI/scope, and delegated-merges only if all gates pass.
3. Run trusted `/run-olq-001` and inspect only the new privacy-safe tags/counts/fingerprints.
4. Design the smallest evidence-driven migration repair or prerequisite task; do not add fallback APIs/workarounds speculatively.
5. After the migration diagnostic path is settled, execute `TIA-AUTH-IDEMP-001` and verify two consecutive elevated bootstrap runs, with the second a no-op.
6. Once a deterministic native V21 qualified profile exists and receives explicit human acceptance, continue `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.
7. Perform a methodology checkpoint at every logical milestone.

## Hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch generation for MVP;
- no generic multi-vendor plugin architecture now;
- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- no candidate execution on trusted Windows/TIA before independent approval + merge;
- self-hosted TIA workflows are trusted-main only;
- no vendor archive payloads in Git;
- do not modify `IndustrialMDE`.
