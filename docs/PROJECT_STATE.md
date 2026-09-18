# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-18

GitHub is the only durable source of truth. Fresh sessions recover state from this file, `AGENTS.md`, `docs/NEXT_CHAT_HANDOFF.md`, `docs/AI_COLLABORATION_MODEL.md`, `docs/DEVELOPMENT_METHODOLOGY.md`, current tasks/PRs/Actions and methodology telemetry issue #25.

## Project

Repository: `al-gri/TIA-Automation-Factory`

Purpose: build a production engineering system that converts a vendor-neutral automation model into modular Siemens PLC code/projects and verifies/assembles them through TIA Portal V21 Openness.

A second explicit output is the reusable AI-assisted software-development methodology described in `docs/DEVELOPMENT_METHODOLOGY.md` and `docs/METHODOLOGY_JOURNAL.md`.

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

- Coding provider order: OpenRouter first, official DeepSeek `deepseek-flash` fallback.
- Primary connected ChatGPT: Senior Architect, normal coding-agent reviewer when independent, orchestrator, methodology curator and delegated technical merge authority.
- `chatgpt-secondary`: fresh isolated reviewer when primary ChatGPT materially authored/co-authored a candidate.
- Gemini has no standing project role.
- One independent external reviewer is required by default for LOW/MEDIUM/HIGH work.
- Real TIA Portal V21 compile/Openness evidence is authoritative Siemens acceptance.
- Normal implementation/review authorization comes from trusted versioned tasks on `main`.
- Rare governance-authority recursion uses only the manual bootstrap lane in `docs/GOVERNANCE_BOOTSTRAP.md`; task-only automation remains fail-closed and bootstrap authority uses positive SSH-signed human provenance.

## Accepted architecture — ARCH-001

PR #17 merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`; issue #18 is closed.

Important accepted constraints include SameScan/PreviousState semantics, global SCC/DAG validation, atomic once-per-scan containers, explicit cross-unit interfaces, no cross-controller runtime links without communication semantics, separate HIGH-risk V19 -> V21 Open Library qualification, native qualified V21 profiles as normal generator dependencies, explicit DB access mode, target-profile CPU memory requirements, and a narrow trusted `TiaV21Worker` Openness boundary.

## PLC-001 — completed

PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after exact trusted TIA Portal V21 compile with **0 errors / 0 warnings**.

## Governance hardening — PR #24 and GOV-CTX-001 completed

PR #24 merged at `ef7e5a00e74a9d3b994c23637aab6fc2ae2546f5`; exact merged candidate head was `a7062ac85c7b3c3fbcbe93380ea1c8e2f33d79ac` after green exact-SHA CI and independent `chatgpt-secondary` approval.

GOV-CTX-001 / issue #28 repaired a later-confirmed trust-origin defect in the coding-context bundle. PR #30 merged at `0260117391abf5f0a8375699dca12caa06bafb8b` from exact reviewed candidate `e3d2912d74cf83256aafe1bd597f49f360411d34` after CI #224 PASS and fresh independent `chatgpt-secondary` APPROVE. Human granted a one-time exact-SHA governance waiver because the permanent bootstrap path did not yet exist. Issue #28 is closed.

## GOV-BOOT-001 / issue #31 / PR #32 — active governance blocker

PR #32 is primary-authored and defines the permanent exceptional bootstrap lane.

Review history:

- round 1 candidate `2d69e0bba51bb5de2672aa7f453bbe812575f0e6` -> `CHANGES_REQUIRED`, major F001-F004;
- round 2 candidate `6bd2860c49713f49cc7a30ac6ec2eceb1db4a1d4` -> `CHANGES_REQUIRED`, major F005.

F001-F004 established that bootstrap must be semantic, issue scope must be complete inside the fingerprinted body, candidate self-authorization must remain impossible, and both human root authority and secondary APPROVE provenance must be separated from primary.

F005 established that owner authorship plus `performed_via_github_app == null` is still only negative attribution, not positive human authentication. The direct owner comment `5736439687` is therefore historical evidence only and no longer satisfies the repaired authority model.

Issue #31 has been rewritten for the second/final candidate-changing repair. Current exact UTF-8 body SHA-256:

`c037d2a568644813cbeaa0c626761c845c2ca046c8a6d7ba557573491ae49099`

Current repaired rule:

- normal task-backed automation remains unchanged and fail-closed;
- bootstrap eligibility is semantic and limited to genuine recursion in the repository's normative authority/review-control model;
- the frozen issue body carries the bounded task/risk/reviewer/Windows/repository/scope contract;
- positive human scope authorization is an **SSH-signed Git attestation commit** created outside ChatGPT/Codex/project automation with a human-controlled signing key unavailable to project automation;
- verifier requires GitHub `verification.verified=true`, `reason=valid`, an SSH signature (not web-flow), owner `al-gri` author/committer identity, exact attestation payload and matching live issue-body hash;
- candidate files cannot authorize the same candidate;
- primary-authored candidate requires HIGH-risk exact-SHA CI and fresh isolated `chatgpt-secondary` review;
- an authority-bearing secondary `APPROVE` must be contained in a separate SSH-signed review-attestation commit bound to exact request/candidate/round/JSON hash;
- no candidate Windows/TIA execution and no `IndustrialMDE` scope;
- attestation branches are evidence transport only and are never merged into `main`.

The default two candidate-changing bootstrap repairs are now consumed. After this repaired candidate is frozen, any further candidate-changing repair requires a new explicit human decision; otherwise state is `BLOCKED`.

Current branch: `chatgpt/gov-boot-001-bootstrap-lane`.

Next gates:

1. freeze the repaired exact head and obtain exact-SHA CI PASS;
2. human creates a valid SSH-signed scope-attestation commit for issue-body hash `c037d2a568644813cbeaa0c626761c845c2ca046c8a6d7ba557573491ae49099` on the dedicated non-merged attestation branch;
3. primary verifies raw GitHub commit signature metadata and exact payload;
4. fresh isolated `chatgpt-secondary` round-3 review;
5. if APPROVE, human creates a separate SSH-signed review-attestation commit containing the exact JSON and its hash;
6. primary verifies all exact-candidate gates and delegated-merges without another routine decision.

## OLQ-001 — implementation merged; real qualification pending

Tracking issue: #19. Canonical task: `tasks/OLQ-001.json`. Risk: HIGH.

PR #22 merged at `5c8c957e6abb7004e4ee9e9382de97347ebfc9c6` from exact candidate `4354bebbf2a3bf745b09589d6abac0938d5b5664` after CI #164 / run `35366781733` PASS and validated `chatgpt-secondary` round-4 APPROVE.

The merged worker provides the bounded V19 `.zal19` -> native V21 `.zal21` qualification command, deterministic source/build identity, archive hashing, native-current-version reopen verification and machine-readable evidence. Windows/TIA build and real vendor-library qualification are deliberately post-merge acceptance gates.

## OLQ-INFRA-002 / PR #27 — paused behind GOV-BOOT-001

Issue #26 / PR #27 adds the trusted-main Windows/TIA harness required to run OLQ-001 twice against the runner-local external Siemens Open Library archive while publishing sanitized evidence only.

Last known head before governance refresh: `86b1b3f1b2cabca227d2976fa537ba00f572d960`; CI #223 / run `35387244950`: PASS.

PR #27 is primary-ChatGPT-authored. It must be re-read against the current `main` after GOV-BOOT-001 is resolved. Its previous review package was prepared against the pre-PR30 base and must not be reused blindly.

## Methodology system

- `docs/DEVELOPMENT_METHODOLOGY.md` — curated reusable rules and experiments.
- `docs/METHODOLOGY_JOURNAL.md` — milestone-level lessons.
- issue #25 — append-only raw methodology telemetry.
- `.github/workflows/methodology-telemetry.yml` — selected workflow/PR lifecycle telemetry.
- `docs/INFRASTRUCTURE_LOG.md` — concise infrastructure chronology.

Current new rules:

- **M-013** — control-plane authority must remain separate from mixed model-visible prompt content.
- **M-014** — governance bootstrap uses semantic eligibility plus positive cryptographic human provenance; a candidate/author-controlled connector must never manufacture its own authority.

## Accepted next operating-model initiative

Issue #35 `AUTO-001` records the accepted hybrid autonomy model: routine OpenRouter/DeepSeek implementation runs to explicit checkpoints; `WAITING_FOR_REVIEW` means primary connected ChatGPT performs a full semantic/code/architecture review when independent; primary-authored candidates route to `chatgpt-secondary`. AUTO-001 must not merge before GOV-BOOT-001 is resolved.

## Required order of work

1. **ACTIVE:** finish GOV-BOOT-001 / issue #31 / PR #32 under the signed-attestation gates above.
2. Re-check PR #27 against the resulting trusted `main`; refresh exact-SHA independent review as required, then delegated merge if gates pass.
3. From trusted `main`, run the OLQ-001 Windows/TIA qualification harness.
4. Build `TiaV21Worker` against installed TIA V21 net48 Openness assemblies.
5. Qualify the real operator-controlled Siemens Open Library V19 `.zal19` twice and verify deterministic source/build/qualification/archive identity plus native reopen.
6. Persist sanitized hashes/manifest/status only; never commit/upload `.zal19/.zal21` vendor payload.
7. Human acceptance is required before the resulting qualified profile becomes a normal generator dependency.
8. Implement AUTO-001 using the post-bootstrap trusted governance state.
9. Continue roadmap: `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.
10. Perform methodology checkpoint at every logical milestone.

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

Connected primary ChatGPT inspects GitHub itself, processes authorized review/orchestration work, maintains methodology checkpoints, and applies delegated technical merge authority when gates pass. When primary ChatGPT is not independent, it prepares a complete fresh `chatgpt-secondary` package and never self-approves. If governance bootstrap is active, it must verify semantic eligibility, the frozen issue-body fingerprint, exact SSH-signed scope/review attestation commits, raw GitHub signature metadata and `docs/GOVERNANCE_BOOTSTRAP.md` before accepting authority-bearing review evidence or merge.
