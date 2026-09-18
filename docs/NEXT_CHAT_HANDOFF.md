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

Then inspect live GitHub state: current PRs, Actions, tasks, issues #19/#26/#28, methodology issue #25 and pending external-review requests.

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
- OLQ infrastructure PR #21 merged at `7430f83140de4bdf4d4b53c564373b15d14fb378`.
- HIGH bounded-repair PR #23 merged at `6ca057eb5dddf3986e1b00ef8e653c044c998938`.
- Governance PR #24 merged at `ef7e5a00e74a9d3b994c23637aab6fc2ae2546f5`; exact merged head `a7062ac85c7b3c3fbcbe93380ea1c8e2f33d79ac` had green exact-SHA CI and independent `chatgpt-secondary` approval.
- OLQ-001 implementation PR #22 merged at `5c8c957e6abb7004e4ee9e9382de97347ebfc9c6` from exact reviewed head `4354bebbf2a3bf745b09589d6abac0938d5b5664`; CI #164 PASS and validated `chatgpt-secondary` round-4 APPROVE.

## Current highest-priority blocker — GOV-CTX-001 / issue #28

A historical PR #24 review response for old SHA `450dca6608f0526d00370595fc5a928f7bbfbd71` was stale as a merge verdict, but its F011/F012 defect hypotheses were re-tested against current `main` and confirmed live.

Current-main defects:

- `agents/runtime/build-coder-context.py` parses task-looking headings from mixed `--prompt-input` content and can elevate issue/reviewer text into authoritative task `contextFiles` selection;
- `validate_repo_path()` canonicalizes some malformed raw spellings instead of requiring canonical input.

Issue #28 tracks the HIGH-risk repair. Primary ChatGPT authored the repair branch `chatgpt/gov-ctx-001-trusted-task-channel`, so the resulting PR requires fresh independent `chatgpt-secondary` review.

Repair contract:

- remove task-authority discovery from rendered prompt content;
- derive task path only from structured GitHub Actions event metadata or explicit trusted caller input;
- resolve the canonical `tasks/*.json` from trusted Git ref;
- issue mode cannot declare task `contextFiles`;
- repair/reviewer/diff/log text cannot override structured task identity;
- require canonical raw repository-relative POSIX context paths;
- add spoof and path-canonicalization regressions;
- keep one-build/shared-prompt OpenRouter -> DeepSeek behavior;
- record methodology rule M-013: control-plane authority is separate from mixed model-visible content.

## OLQ-INFRA-002 / PR #27 — paused downstream

Issue #26 / PR #27 adds the trusted-main Windows/TIA qualification harness.

Live head at last checkpoint: `86b1b3f1b2cabca227d2976fa537ba00f572d960`.
CI #223 / run `35387244950`: PASS.

PR #27 is primary-ChatGPT-authored and had reached the independent-review gate. Do **not** merge it while GOV-CTX-001 remains unresolved. After the governance repair merges, re-read PR #27 live head/base/CI and refresh exact-SHA independent review if required.

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

The latest durable lesson is M-013: prompt/data content may quote control-looking text but must never become authoritative control metadata. Exact-SHA staleness invalidates an old verdict, but old defect hypotheses should be re-tested against current source rather than discarded blindly.

## Hard boundaries

- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- no generic multi-vendor plugin architecture now;
- no F-safety generation;
- no arbitrary hardware-from-scratch MVP expansion;
- no candidate source execution on trusted Windows/TIA before independent approval + merge;
- self-hosted manual TIA workflows are main-only;
- no vendor archive payloads in Git;
- do not modify `IndustrialMDE`.
