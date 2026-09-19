# TIA Automation Factory — Fresh Chat Handoff

Updated: 2026-09-19

GitHub is the sole durable source of truth. Do not use old chat history as project state.

## Mandatory startup

Read in order: `AGENTS.md`, `docs/PROJECT_STATE.md`, this file, `docs/AI_COLLABORATION_MODEL.md`, `docs/DEVELOPMENT_METHODOLOGY.md`, latest relevant `docs/METHODOLOGY_JOURNAL.md`, and `docs/EXTERNAL_REVIEW_PROTOCOL.md` before review work. Then inspect live tasks, open PRs/issues, exact candidate SHAs, Actions and methodology issue #25.

`IndustrialMDE` is outside scope and must not be touched.

## Roles

- coding: OpenRouter first, DeepSeek `deepseek-flash` fallback;
- primary connected ChatGPT: Senior Architect, independent reviewer for coding-agent candidates, orchestrator, methodology curator and delegated merge authority;
- `chatgpt-secondary`: required when primary materially authored/co-authored the candidate;
- trusted Windows/TIA runs trusted `main` only.

## Checkpoint

State was audited from trusted `main@4b27064e19395a350bb0e2879eb959640cc0775d` before this documentation update. CI #317 / run `35459383614` is PASS. Re-read live `main` because the checkpoint documentation itself advances the branch.

Important completed milestones:

- governance bootstrap PR #32 merged; it is not an active blocker;
- OLQ trusted harness PR #27 merged;
- TIA-AUTH PR #55 merged at `755a3d2014105a15697e8d945c7ca8feb0891792`;
- trusted OLQ run `35458909671` passed whitelist sync/Openness admission and reached TIA V21 migration; issues #46 and #54 are closed.

## Active objective

Qualify the external Siemens Open Library V19 `.zal19` into a deterministic native V21 profile before normal generator work depends on it.

Current blocker from trusted run `35458909671`:

`phase:retrieve-with-upgrade type:EngineeringTargetInvocationException hresult:0x80131500`

This is now a migration diagnosis problem, not a TIA whitelist/auth problem.

## Active task

`tasks/OLQ-DIAG-002.json`, issue #56, HIGH risk.

Candidate scope: `src/TiaV21Worker/Program.cs` only, with at most one tiny source-level test if strictly justified. Preserve the migration sequence and behavior exactly. The task may inspect `EngineeringException.MessageData.Text` / `DetailMessageData` in memory only and expose solely fixed allowlisted semantic tags, detail count and deterministic SHA-256 fingerprints. Never publish arbitrary Siemens exception text, paths, library/product names or vendor contents.

Next safe action: run **Autonomous Agent** in task mode for `tasks/OLQ-DIAG-002.json`. Primary ChatGPT then independently reviews the exact coding-agent candidate, verifies exact-SHA CI, merges under delegated authority if all gates pass, and triggers `/run-olq-001` on issue #19 after merge.

## Queued separate cleanup

`tasks/TIA-AUTH-IDEMP-001.json`, issue #53, HIGH risk. The bootstrap script applies the correct narrow ACL but does not recognize the equivalent rule on the second run. Keep this task separate and run it only after the active OLQ diagnostic. It must not broaden registry rights or scope.

## Evidence that must not be reinterpreted

- `phase:bootstrap-required` from older runs is historical; the latest trusted run passed auth.
- Existing wrapper `EngineeringTargetInvocationException / 0x80131500` is insufficient evidence for a migration fix; do not guess a fallback API or vendor workaround.
- Candidate Windows/TIA execution remains prohibited; diagnostic validation is post-merge trusted-main only.
- Vendor `.zal19/.zal21` payloads remain runner-local/operator-controlled and must never enter GitHub.

## Open PR hygiene

PR #39 is an old draft documentation candidate and not part of the active OLQ path. Legacy experiment PRs #4/#5/#10/#13 are also not active authority. Inspect live state before touching them.

## Hard boundaries

No F-safety generation, no broad UI/HMI expansion, no arbitrary hardware-from-scratch MVP expansion, no generic multi-vendor plugin architecture, no candidate source on trusted Windows/TIA, no vendor payloads in Git, and never modify `IndustrialMDE`.
