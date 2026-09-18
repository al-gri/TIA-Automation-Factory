# TIA Automation Factory — Fresh Chat Handoff

Updated: 2026-09-18

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
- round 2 exact candidate `6bd2860c49713f49cc7a30ac6ec2eceb1db4a1d4` -> `CHANGES_REQUIRED`, major F005.

F005 found that owner authorship plus `performed_via_github_app == null` is only negative GitHub-App attribution and does not prove direct human origin against non-App credentialed API paths. The prior direct owner comment `5736439687` is therefore historical evidence only and is not sufficient under the repaired mechanism.

Issue #31 has been rewritten for the second/final candidate-changing repair. Current exact UTF-8 body SHA-256:

`c037d2a568644813cbeaa0c626761c845c2ca046c8a6d7ba557573491ae49099`

The repaired bootstrap trust root is now **positive cryptographic provenance**:

- human scope authorization is an SSH-signed Git attestation commit created outside ChatGPT/Codex/project automation;
- GitHub must report `verification.verified=true`, `reason=valid`, and an SSH signature, with repository owner `al-gri` as author and committer;
- the signed payload binds repository, issue #31, task `GOV-BOOT-001`, purpose `scope-authorization` and the exact live issue-body hash;
- GitHub web-flow signatures, ordinary owner comments, app-attribution metadata and unsigned connector/API commits are insufficient by themselves;
- the human signing private key must never be available to project automation, PATs used by automation, CI, runners or coding providers;
- attestation branches are evidence transport only and are never merged into `main`.

For an authority-bearing secondary APPROVE, the human must create a separate SSH-signed review-attestation commit containing request ID, reviewer slot, exact candidate SHA, round, exact review JSON and its SHA-256.

The two default candidate-changing bootstrap repairs are now consumed. After the current repaired candidate is frozen, any further candidate-changing repair requires a fresh explicit human decision; otherwise state becomes `BLOCKED`.

Current candidate branch: `chatgpt/gov-boot-001-bootstrap-lane`.

### Next gates

1. freeze repaired PR #32 exact head and require exact-SHA CI PASS;
2. create/verify SSH-signed scope-attestation commit for issue-body hash `c037d2a568644813cbeaa0c626761c845c2ca046c8a6d7ba557573491ae49099`;
3. prepare fresh isolated `chatgpt-secondary` round-3 package with F001-F005 and signed-attestation evidence;
4. if APPROVE, create/verify separate SSH-signed review-attestation commit with exact JSON;
5. delegated merge only if every exact-candidate gate remains green.

Normal task-backed automation remains task-only and fail-closed throughout.

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

## Hard boundaries

- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- no generic multi-vendor plugin architecture now;
- no F-safety generation;
- no arbitrary hardware-from-scratch MVP expansion;
- no candidate source execution on trusted Windows/TIA before independent approval + merge;
- self-hosted manual TIA workflows are main-only;
- no vendor archive payloads in Git;
- do not modify `IndustrialMDE`.
