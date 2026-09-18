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

## Current highest-priority blocker — GOV-BOOT-001 / issue #31 / PR #32

Round 1 secondary review of exact candidate `2d69e0bba51bb5de2672aa7f453bbe812575f0e6` returned `CHANGES_REQUIRED` with four major findings F001-F004. The findings were accepted; that SHA must never be merged.

The repair changes the bootstrap design in four material ways:

- human root authorization must be a direct GitHub action whose API metadata is not app-mediated;
- the frozen issue body itself contains task/risk/reviewer/Windows/repository/scope requirements;
- eligibility is semantic governance-authority recursion, not physical membership in coding-agent protected paths;
- an authority-bearing secondary APPROVE must enter GitHub through provenance separated from primary.

Issue #31 is now the frozen bounded scope contract. Current exact UTF-8 body SHA-256:

`28abdc7a5837c8c93049f5b6ed668cafdd0c7de8275e02e6e14e55296b8cd47a`

Important: the issue-body edit was performed through `chatgpt-codex-connector`; it does **not** constitute human authorization.

### Required next human artifact

Before a repaired candidate can satisfy bootstrap review/merge gates, the human operator/repository owner must create a direct GitHub comment on issue #31 outside ChatGPT/Codex/GitHub-App execution that explicitly authorizes:

- issue `#31`;
- task `GOV-BOOT-001`;
- exact issue-body hash `28abdc7a5837c8c93049f5b6ed668cafdd0c7de8275e02e6e14e55296b8cd47a`;
- the bounded governance scope in the issue body.

Primary must verify from GitHub API that the author is the human operator and `performed_via_github_app` is absent or `null`.

Current branch: `chatgpt/gov-boot-001-bootstrap-lane`.

After the repaired exact head is stable:

1. run exact-SHA CI;
2. prepare a fresh `chatgpt-secondary` round-2 package including F001-F004 and the direct human authorization comment;
3. if the secondary returns APPROVE, the human directly relays/attests the exact JSON into GitHub with request ID, reviewer slot, candidate SHA, round and payload SHA-256;
4. primary verifies provenance/content/schema/exact SHA and delegated-merges only if every gate remains green.

Normal task-backed automation remains task-only and fail-closed throughout.

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
- M-014: governance bootstrap must be explicit; the candidate/author-controlled connector must never manufacture its own authority.

## Hard boundaries

- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- no generic multi-vendor plugin architecture now;
- no F-safety generation;
- no arbitrary hardware-from-scratch MVP expansion;
- no candidate source execution on trusted Windows/TIA before independent approval + merge;
- self-hosted manual TIA workflows are main-only;
- no vendor archive payloads in Git;
- do not modify `IndustrialMDE`.
