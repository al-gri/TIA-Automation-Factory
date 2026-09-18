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
- Rare governance-authority recursion uses only the manual bootstrap lane in `docs/GOVERNANCE_BOOTSTRAP.md`; task-only automation remains fail-closed and bootstrap authority must have provenance separated from the conflicted primary.

## Accepted architecture — ARCH-001

PR #17 merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`; issue #18 is closed.

Important accepted constraints include SameScan/PreviousState semantics, global SCC/DAG validation, atomic once-per-scan containers, explicit cross-unit interfaces, no cross-controller runtime links without communication semantics, separate HIGH-risk V19 -> V21 Open Library qualification, native qualified V21 profiles as normal generator dependencies, explicit DB access mode, target-profile CPU memory requirements, and a narrow trusted `TiaV21Worker` Openness boundary.

## PLC-001 — completed

PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after exact trusted TIA Portal V21 compile with **0 errors / 0 warnings**.

## Governance hardening — PR #24 and GOV-CTX-001 completed

PR #24 merged at `ef7e5a00e74a9d3b994c23637aab6fc2ae2546f5`; exact merged candidate head was `a7062ac85c7b3c3fbcbe93380ea1c8e2f33d79ac` after green exact-SHA CI and independent `chatgpt-secondary` approval.

Its accepted governance includes authorship-based reviewer independence, deterministic Candidate Validation, fresh review after candidate-changing repair, main-only trusted Windows/TIA execution, OpenRouter -> DeepSeek continuity, methodology telemetry and the coding-context bundle.

GOV-CTX-001 / issue #28 repaired a later-confirmed trust-origin defect in that coding-context bundle. PR #30 merged at `0260117391abf5f0a8375699dca12caa06bafb8b` from exact reviewed candidate `e3d2912d74cf83256aafe1bd597f49f360411d34` after CI #224 PASS and fresh independent `chatgpt-secondary` APPROVE. The human operator granted a one-time exact-SHA governance waiver because no trusted `tasks/GOV-CTX-001.json` existed and the current policy had no non-recursive bootstrap path.

Current trusted context behavior now keeps task/control authority separate from mixed model-visible prompt content and rejects non-canonical repository paths fail-closed. Issue #28 is closed.

## GOV-BOOT-001 / issue #31 — active governance blocker

Issue #31 tracks the authorization recursion exposed by PR #30. PR #32 is the primary-authored candidate for the permanent bootstrap lane.

The first independent review of PR #32 at exact candidate `2d69e0bba51bb5de2672aa7f453bbe812575f0e6` returned `CHANGES_REQUIRED` with major findings F001-F004:

- human root authority was only primary/connector asserted;
- the issue body did not contain all mandatory bounded-authorization fields;
- eligibility incorrectly depended on coding-agent protected paths;
- primary-persisted secondary JSON lacked provenance separated from the candidate author.

The findings were accepted. Issue #31 was rewritten as the frozen bounded scope contract. Its current exact UTF-8 body SHA-256 is:

`28abdc7a5837c8c93049f5b6ed668cafdd0c7de8275e02e6e14e55296b8cd47a`

The issue edit itself was performed through the ChatGPT connector, so it is **not** human authorization. Before the repaired candidate can be accepted, the repository owner/human operator must create a direct GitHub comment outside ChatGPT/Codex/GitHub-App execution that explicitly authorizes issue #31 / task `GOV-BOOT-001` at that exact body hash. The verifier must require the direct human author and `performed_via_github_app` absent or `null`.

Current candidate branch: `chatgpt/gov-boot-001-bootstrap-lane`.

Repaired permanent rule:

- normal task-backed automation remains unchanged and fail-closed;
- bootstrap eligibility is semantic and limited to genuine recursion in the repository's normative authority/review-control model, not path-name membership or a merely missing task;
- the frozen issue body contains the bounded task/risk/reviewer/Windows/repository/scope contract;
- human authorization must be a direct non-app-mediated GitHub artifact bound to the exact issue-body SHA-256;
- candidate files cannot authorize the same candidate;
- primary-authored candidate requires HIGH-risk exact-SHA deterministic CI and fresh isolated `chatgpt-secondary` review;
- an authority-bearing secondary `APPROVE` must also enter GitHub through provenance-separated direct human relay/attestation of the exact JSON payload;
- no candidate Windows/TIA execution and no `IndustrialMDE` scope;
- material issue-body/scope change invalidates prior human authorization.

Round 1 is invalid for any repaired head. After the repaired exact head is stable and CI is green, a fresh round-2 secondary review is required. The direct human scope authorization must exist before that review can satisfy the bootstrap gate.

## OLQ-001 — implementation merged; real qualification pending

Tracking issue: #19. Canonical task: `tasks/OLQ-001.json`. Risk: HIGH.

PR #22 merged at `5c8c957e6abb7004e4ee9e9382de97347ebfc9c6` from exact candidate `4354bebbf2a3bf745b09589d6abac0938d5b5664` after CI #164 / run `35366781733` PASS and validated `chatgpt-secondary` round-4 APPROVE.

The merged worker provides the bounded V19 `.zal19` -> native V21 `.zal21` qualification command, deterministic source/build identity, archive hashing, native-current-version reopen verification and machine-readable evidence. Windows/TIA build and real vendor-library qualification are deliberately post-merge acceptance gates.

## OLQ-INFRA-002 / PR #27 — paused behind GOV-BOOT-001

Issue #26 / PR #27 adds the trusted-main Windows/TIA harness required to run OLQ-001 twice against the runner-local external Siemens Open Library archive while publishing sanitized evidence only.

Last known head before governance refresh: `86b1b3f1b2cabca227d2976fa537ba00f572d960`; CI #223 / run `35387244950`: PASS.

PR #27 is primary-ChatGPT-authored. It must be re-read against the current `main` after GOV-BOOT-001 is resolved. Its previous review package was prepared against the pre-PR30 base and must not be used blindly after governance changes.

## Methodology system

- `docs/DEVELOPMENT_METHODOLOGY.md` — curated reusable rules and experiments.
- `docs/METHODOLOGY_JOURNAL.md` — milestone-level lessons.
- issue #25 — append-only raw methodology telemetry.
- `.github/workflows/methodology-telemetry.yml` — selected workflow/PR lifecycle telemetry.
- `docs/INFRASTRUCTURE_LOG.md` — concise infrastructure chronology.

Current new rules:

- **M-013** — control-plane authority must remain separate from mixed model-visible prompt content.
- **M-014** — bootstrap governance explicitly; a candidate/author-controlled connector must never manufacture its own authority.

## Required order of work

1. **ACTIVE:** finish GOV-BOOT-001 / issue #31: repaired policy candidate -> direct human scope authorization for frozen issue hash -> exact-SHA CI -> fresh `chatgpt-secondary` round 2 -> provenance-separated direct human relay/attestation of an APPROVE -> delegated merge if all gates pass.
2. Re-check PR #27 against the resulting trusted `main`; refresh exact-SHA independent review as required, then delegated merge if gates pass.
3. From trusted `main`, run the OLQ-001 Windows/TIA qualification harness.
4. Build `TiaV21Worker` against installed TIA V21 net48 Openness assemblies.
5. Qualify the real operator-controlled Siemens Open Library V19 `.zal19` twice and verify deterministic source/build/qualification/archive identity plus native reopen.
6. Persist sanitized hashes/manifest/status only; never commit/upload `.zal19/.zal21` vendor payload.
7. Human acceptance is required before the resulting qualified profile becomes a normal generator dependency.
8. Continue roadmap: `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.
9. Perform methodology checkpoint at every logical milestone.

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

Connected primary ChatGPT inspects GitHub itself, processes authorized review/orchestration work, maintains methodology checkpoints, and applies delegated technical merge authority when gates pass. When primary ChatGPT is not independent, it prepares a complete fresh `chatgpt-secondary` package and never self-approves. If governance bootstrap is active, it must verify semantic eligibility, the frozen issue-body fingerprint, direct-human authorization provenance and `docs/GOVERNANCE_BOOTSTRAP.md` before accepting authority-bearing review evidence or merge.
