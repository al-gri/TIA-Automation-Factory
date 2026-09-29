# TIA Automation Factory — Fresh Chat Handoff

Updated: 2026-09-29
Checkpoint trusted main: `920c4e1a276c77af95288f42da94ddfa3ce414d3`

GitHub is the sole durable source of truth. Re-fetch live state before acting; this checkpoint is navigation, not authority. `IndustrialMDE` is outside scope.

## Mandatory startup

Read:

1. `AGENTS.md`;
2. `docs/PROJECT_STATE.md`;
3. this file;
4. `docs/CHAT_HANDOFF_PROTOCOL.md` once PR #103 is merged;
5. `docs/AI_COLLABORATION_MODEL.md`;
6. `docs/DEVELOPMENT_METHODOLOGY.md`;
7. latest relevant `docs/METHODOLOGY_JOURNAL.md`;
8. `docs/EXTERNAL_REVIEW_PROTOCOL.md` before review work;
9. `docs/GOVERNANCE_BOOTSTRAP.md` only for genuine authority recursion.

Then independently re-fetch live tasks/PRs/Actions/review state.

## Operator-local Git context

Do not ask the operator to rediscover the normal local clone unless they say it changed.

Canonical local clone:
`C:\Users\user\Documents\GitHub\TIA-Automation-Factory`

Detailed local Git/PowerShell/signing instructions are versioned in `docs/CHAT_HANDOFF_PROTOCOL.md`. Never request a private-key body or passphrase.

## Current product state

### #62 — generator OUTPUT foundation

PR #102 is **MERGED**.

- accepted exact product candidate: `e4b0165fc86af24a147c10c8be7ee8d25422acdb`;
- merge/main commit: `920c4e1a276c77af95288f42da94ddfa3ce414d3`;
- review round 2: APPROVE under `ER-102-e4b0165fc86a-CODE_REVIEW-2-chatgpt-secondary-manual`;
- repair usage: 1/2;
- post-merge CI #500 / `36547750103`: SUCCESS;
- net10 tests: 106/106 PASS;
- net48 test assembly: PASS;
- legacy Motor generation: PASS;
- trusted-main TIA V21 End-to-End #16 / `36547750239`: **SUCCESS**;
- TIA diagnostics: `success=true`, `state=Success`, **0 errors / 0 warnings**; `UDT_Motor (UDT)` updated and compile finished successfully.

Do not reuse any pre-repair/stale review package for #102.

### #61 — V21 Open Library qualification

Still **BLOCKED / unknown**.

Trusted sanitized evidence from run #320 / `36273653086`:

`phase:retrieve-with-upgrade type:EngineeringTargetInvocationException hresult:0x80131500 tags:unknown detail-count:3 msgfp:eefb8fcdb933339b dtlfp:f85ef7ebe21aad89`

Do not infer a support package/product/license/library-element prerequisite, change migration behavior or trigger another qualification run without new reviewed evidence-backed authority.

### #63 — real Valve vertical slice

Planning-only. The deterministic #62 foundation is now merged; the remaining semantic prerequisite is a reviewed #61 Valve contract/profile. Do not invent `fbValve_Solenoid` parameters/dependencies/ownership meanwhile.

## Current handoff/docs candidate

PR #103 implements `CHAT-HANDOFF-001` and is the active docs/process candidate.

It contains:

- transactional repository-first chat transfer;
- stale evidence handling;
- refreshed snapshot structure;
- operator-local Git/Windows context so future exceptional human-only Git/SSH steps do not repeat environment discovery.

Primary authored this docs candidate, so final review must be fresh isolated `chatgpt-secondary`.

Before review/merge, verify PR #103 is refreshed onto current main and that any in-progress TIA result above is correctly represented. Any candidate change requires fresh exact-SHA CI and review.

## Next safe actions

1. obtain terminal result for trusted-main TIA V21 End-to-End #16 / `36547750239`;
2. update PR #103 snapshots if that result changes state;
3. verify exact PR #103 CI;
4. obtain one fresh isolated `chatgpt-secondary` review of exact PR #103 head;
5. if APPROVE and gates remain exact, delegated-merge PR #103 and verify post-merge CI;
6. then return to #61 evidence-driven prerequisite diagnosis / product planning. Do not start a real Valve call before #61 contract qualification.

## Historical/bootstrap evidence

Governance bootstrap issue #106 is closed completed. Its evidence branches are historical transport only and must never be merged.

Old handoff PR #39 and historical experimental PRs are not current authority.

## Hard boundaries

- GitHub is sole source of truth;
- exact-SHA reviews do not transfer across candidate changes;
- candidate source never runs on trusted Windows/TIA before independent review + merge;
- vendor payload/raw Siemens text stays out of GitHub;
- no unverified Valve facts;
- no F-safety/broad UI/multi-vendor expansion;
- never modify `IndustrialMDE`.
