# TIA Automation Factory — Fresh Chat Handoff

Updated: 2026-09-19

GitHub is the sole durable source of truth. Do not use old chat history as project state.

## Mandatory startup

Read, in order:

1. `AGENTS.md`
2. `docs/PROJECT_STATE.md`
3. this file
4. `docs/AI_COLLABORATION_MODEL.md`
5. `docs/DEVELOPMENT_METHODOLOGY.md`
6. latest relevant entries in `docs/METHODOLOGY_JOURNAL.md`
7. `docs/EXTERNAL_REVIEW_PROTOCOL.md` before review work
8. `docs/GOVERNANCE_BOOTSTRAP.md` when governance bootstrap work is active

Then inspect live GitHub state: current PRs, Actions, tasks, issues #19/#26/#31/#35, methodology issue #25 and pending external-review requests.

`IndustrialMDE` is outside scope and must not be touched.

## Roles and authority

- coding provider: OpenRouter first, official DeepSeek `deepseek-flash` fallback;
- primary connected ChatGPT: Senior Architect + normal independent reviewer for coding-agent work + orchestrator + methodology curator + delegated technical merge authority;
- `chatgpt-secondary`: fresh isolated ChatGPT when primary ChatGPT materially authored/co-authored the candidate;
- Gemini: no standing project role;
- real TIA Portal V21 compile/Openness execution: authoritative Siemens acceptance.

## Durable completed milestones

- ARCH-001 PR #17 merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`.
- PLC-001 PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after trusted TIA compile with 0 errors / 0 warnings.
- Governance PR #24 merged at `ef7e5a00e74a9d3b994c23637aab6fc2ae2546f5`; exact merged head `a7062ac85c7b3c3fbcbe93380ea1c8e2f33d79ac` had green exact-SHA CI and independent `chatgpt-secondary` approval.
- OLQ-001 implementation PR #22 merged at `5c8c957e6abb7004e4ee9e9382de97347ebfc9c6`; exact reviewed head `4354bebbf2a3bf745b09589d6abac0938d5b5664`, CI #164 PASS, secondary round-4 APPROVE.
- GOV-CTX-001 PR #30 merged at `0260117391abf5f0a8375699dca12caa06bafb8b` from exact candidate `e3d2912d74cf83256aafe1bd597f49f360411d34`, CI #224 PASS and fresh `chatgpt-secondary` APPROVE. Human granted a one-time exact-SHA waiver because the permanent governance bootstrap path was undefined. Issue #28 is closed.

## Current highest-priority blocker — GOV-BOOT-001 / issue #31 / PR #32

Review history:

- round 1 exact candidate `2d69e0bba51bb5de2672aa7f453bbe812575f0e6` -> `CHANGES_REQUIRED`, F001-F004;
- round 2 exact candidate `6bd2860c49713f49cc7a30ac6ec2eceb1db4a1d4` -> `CHANGES_REQUIRED`, major F005;
- round 3 exact candidate `f078c4b3aba11ebe3dacdf21881dd5b00f5fcedd` -> `CHANGES_REQUIRED`, major F006-F007.

Round 3 independently confirmed F001-F005 repaired, then found:

- F006: the permanent attestation schemas incorrectly hard-coded issue `31` / task `GOV-BOOT-001`;
- F007: the unconditional fail-closed rule required a review attestation before the APPROVE JSON needed to create it existed.

The default two candidate-changing repairs were already consumed. On 2026-09-19 the human explicitly authorized **exactly one additional candidate-changing repair round**, strictly for F006/F007 and without scope widening.

Issue #31 was updated only to durably bind that bounded repair decision. Current exact UTF-8 issue-body SHA-256:

`f9b5405276824eb1df3e367b644df490879ab886e64170e619f5f9416d74283b`

Because the issue-body bytes changed, prior signed scope-attestation commit `4ac2b16c2b1ea81d225779ece58df3131fb18fd6` is historical/stale for the next review. Before a new independent review, the human must create a **fresh SSH-signed scope attestation** for the hash above on the dedicated non-merged attestation branch.

The bounded repair changes only the already-authorized governance surfaces and must remain strictly F006/F007:

- scope/review attestation schemas become invocation-generic and verify repository/issue/task equality against the current frozen authority instead of hard-coding GOV-BOOT-001 constants;
- GOV-BOOT-001 values remain only a current-instance example;
- fail-closed behavior becomes stage-specific: review-attestation evidence is not applicable before an APPROVE exists; CHANGES_REQUIRED grants no authority; after APPROVE, missing/invalid/stale signed review attestation blocks consumption of APPROVE/merge, but mere pre-signature pending state is not prematurely BLOCKED.

Normal task-backed automation remains task-only and fail-closed. Candidate source must not execute on trusted Windows/TIA. `IndustrialMDE` remains out of scope.

Current candidate branch: `chatgpt/gov-boot-001-bootstrap-lane`. Read its exact head live from GitHub.

### Next gates

1. finish/freeze the single F006/F007 repair and require exact-SHA CI PASS;
2. human creates a fresh SSH-signed scope-attestation commit for issue-body hash `f9b5405276824eb1df3e367b644df490879ab886e64170e619f5f9416d74283b`;
3. primary verifies exact signed commit SHA, raw GitHub SSH-signature metadata and payload;
4. prepare fresh isolated `chatgpt-secondary` review for the new exact candidate, carrying F001-F007 history;
5. if APPROVE, human creates/primary verifies separate SSH-signed review-attestation commit with exact JSON;
6. delegated merge only if every exact-candidate gate remains green.

Any further candidate-changing repair after this one needs another fresh explicit human decision; otherwise `BLOCKED`.

## AUTO-001 / issue #35 — accepted next operating-model work

The human accepted a hybrid autonomy model. Routine OpenRouter/DeepSeek implementation should run autonomously to explicit checkpoints. `WAITING_FOR_REVIEW` means primary connected ChatGPT performs a full semantic/code/architecture review when independent; primary-authored candidates route to `chatgpt-secondary`. AUTO-001 must not merge before GOV-BOOT-001 resolves the governance authority ambiguity.

## OLQ-INFRA-002 / PR #27 — paused downstream

Issue #26 / PR #27 adds the trusted-main Windows/TIA qualification harness.

Last known pre-governance head: `86b1b3f1b2cabca227d2976fa537ba00f572d960`; CI #223 PASS.

Do not reuse the old review package blindly: PR #30 changed `main`, and GOV-BOOT-001 must finish first. Then re-read PR #27 live head/base/diff/CI and obtain a fresh exact-SHA independent review if required.

## OLQ-001 next target after governance/harness gates

From trusted `main`:

1. build `TiaV21Worker` on the TIA V21 Windows runner;
2. run qualification against the real operator-controlled Siemens Open Library V19 `.zal19`;
3. run the same source/build identity a second time;
4. require native V21 reopen success and deterministic identity/archive hashes;
5. publish sanitized manifest/hashes/status only — never `.zal19/.zal21` or operator absolute paths;
6. human explicitly accepts the qualified profile before it becomes a normal generator dependency.

Then continue roadmap `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.

## Methodology

Issue #25 is raw append-only telemetry. `docs/DEVELOPMENT_METHODOLOGY.md` and `docs/METHODOLOGY_JOURNAL.md` are curated policy/history. Run a methodology checkpoint at every substantial milestone without waiting for a user reminder.

Latest rules:

- M-013: mixed prompt/data content must never become control-plane authority.
- M-014: governance bootstrap must use semantic eligibility and positive SSH-signed human provenance; the candidate/author-controlled connector must never manufacture its own authority.
- Permanent authority schemas must be invocation-generic, while fail-closed gates must be evaluated at the stage where their evidence can actually exist.

## Hard boundaries

- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- no generic multi-vendor plugin architecture now;
- no F-safety generation;
- no arbitrary hardware-from-scratch MVP expansion;
- no candidate source execution on trusted Windows/TIA before independent approval + merge;
- self-hosted manual TIA workflows are main-only;
- no vendor archive payloads in Git;
- do not modify `IndustrialMDE`.
