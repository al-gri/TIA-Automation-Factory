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
8. `docs/GOVERNANCE_BOOTSTRAP.md` when protected governance bootstrap work is active

Then inspect live GitHub state: current PRs, Actions, tasks, issues #19/#26/#31, methodology issue #25 and pending external-review requests.

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
- GOV-CTX-001 PR #30 merged at `0260117391abf5f0a8375699dca12caa06bafb8b` from exact candidate `e3d2912d74cf83256aafe1bd597f49f360411d34`, CI #224 PASS and fresh `chatgpt-secondary` APPROVE. Human granted a one-time exact-SHA waiver because the governance bootstrap path was undefined. Issue #28 is closed.

## Current highest-priority blocker — GOV-BOOT-001 / issue #31

PR #30 exposed an authorization recursion: the independent review was technically valid, but no trusted `tasks/GOV-CTX-001.json` on `main` could authorize the secondary slot, and adding such authorization inside the same candidate would be self-authorization.

Human operator directed that this bootstrap gap be eliminated after PR #30 merged. Issue #31 is the bounded authority for this work; its authorized body fingerprint is:

`6ef39216ad77793837a1184323a6b70d0e19edaf47300d0966fc962122b7c9ee`

Current branch: `chatgpt/gov-boot-001-bootstrap-lane`.

Design:

- add normative `docs/GOVERNANCE_BOOTSTRAP.md`;
- normal trusted-task automation remains unchanged and fail-closed;
- bootstrap applies only to genuine protected-governance authorization recursion;
- human authorizes a bounded GitHub issue and primary records exact issue-body SHA-256;
- any material body/scope change invalidates authorization;
- primary-authored bootstrap candidate is HIGH risk and requires exact-SHA CI + fresh isolated `chatgpt-secondary` review;
- candidate cannot authorize itself;
- no Windows/TIA candidate execution and no `IndustrialMDE` scope;
- after exact candidate gates pass, primary may delegated-merge without another routine human confirmation because scope was already human-authorized.

This candidate is itself primary-authored and therefore cannot be self-reviewed. Prepare a fresh secondary package after exact-SHA CI.

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
- M-014: governance bootstrap must be explicit; a candidate must never authorize itself.

## Hard boundaries

- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- no generic multi-vendor plugin architecture now;
- no F-safety generation;
- no arbitrary hardware-from-scratch MVP expansion;
- no candidate source execution on trusted Windows/TIA before independent approval + merge;
- self-hosted manual TIA workflows are main-only;
- no vendor archive payloads in Git;
- do not modify `IndustrialMDE`.
