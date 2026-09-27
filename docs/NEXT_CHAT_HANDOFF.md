# TIA Automation Factory — Fresh Chat Handoff

Updated: 2026-09-27
Checkpoint trusted main: `0a1d891bd7c26a4dbe057ca9d9a83e1c2b0e31b1`

GitHub is the sole durable source of truth. Re-fetch live state before acting; this checkpoint is navigation, not authority. `IndustrialMDE` is outside scope.

## Mandatory startup

Read `AGENTS.md`, `docs/PROJECT_STATE.md`, this file, `docs/AI_COLLABORATION_MODEL.md`, `docs/DEVELOPMENT_METHODOLOGY.md`, latest relevant `docs/METHODOLOGY_JOURNAL.md`, and `docs/EXTERNAL_REVIEW_PROTOCOL.md` before review work. When this handoff-protocol candidate is merged, also read `docs/CHAT_HANDOFF_PROTOCOL.md`.

## Current objective

Finish the contract-independent generator foundation (#62) without inventing Valve/TIA contract facts. #61 qualification remains blocked by insufficient sanitized vendor evidence.

## #61 — V21 Open Library qualification

Trusted qualification run #320 / `36273653086` executed merged `main@0a1d891bd7c26a4dbe057ca9d9a83e1c2b0e31b1`.

Post-merge CI #487, TIA V21 Verification #18 and TIA V21 End-to-End #15 were green before the run.

Final sanitized result is **FAIL**:

`phase:retrieve-with-upgrade type:EngineeringTargetInvocationException hresult:0x80131500 tags:unknown detail-count:3 msgfp:eefb8fcdb933339b dtlfp:f85ef7ebe21aad89`

The prerequisite classifier did not resolve the root cause. Under trusted task `OLQ-DIAG-PREREQ-CLASSIFIER-001`, stop and retain BLOCKED/unknown rather than speculating about a support package, product, license, library element, migration workaround or Valve contract.

Do not trigger another `/run-olq-001` without a new reviewed evidence-backed authority.

## #62 — active OUTPUT candidate

Trusted task: `tasks/GEN-VALVE-FOUNDATION-OUTPUT-001.json`.

PR #102 `GEN: add deterministic output manifest and containment` is the active candidate.

Checkpoint identity:

- head: `cc370d6d19c65c483837e1a8358dc9f931c2dc87`;
- base: `0a1d891bd7c26a4dbe057ca9d9a83e1c2b0e31b1`;
- changed paths: exactly the five task-authorized GeneratorCli/test paths;
- CI #490 / run `36344246671`: **SUCCESS**;
- net10 SiemensBackend.Tests: 96/96 PASS;
- legacy Motor generation: PASS;
- no Windows/TIA candidate execution;
- current review state: **WAITING FOR FRESH ISOLATED `chatgpt-secondary` REVIEW** because primary authored the implementation.

Review identity already embedded in PR #102 body:

- `reviewRequestId`: `ER-102-cc370d6d19c6-CODE_REVIEW-1-chatgpt-secondary-manual`
- `reviewerSlot`: `chatgpt-secondary`
- task: `GEN-VALVE-FOUNDATION-OUTPUT-001`
- candidate: `cc370d6d19c65c483837e1a8358dc9f931c2dc87`
- round: 1

No review verdict was present at handoff audit time. Re-fetch before requesting/reusing anything.

### Next safe product action

1. obtain one fresh isolated `chatgpt-secondary` exact-SHA review of PR #102 using the self-contained package already in its body;
2. if exact current head/base/CI/scope still match and verdict is APPROVE with no critical/major finding, primary may delegated-merge;
3. verify post-merge CI before declaring #62 foundation complete;
4. do not start #63 until #61 has a reviewed qualified Valve contract/profile and #62 is merged/green.

## Handoff protocol candidate

Main docs were materially stale at this checkpoint and `docs/CHAT_HANDOFF_PROTOCOL.md` was not yet on main. Old PR #39 is stale on an ancient base and is not active product authority.

The current handoff sync is prepared separately from current main so it does **not** move main or stale PR #102. Because primary authors this documentation/process candidate, it requires fresh isolated `chatgpt-secondary` exact-SHA review before merge.

Do not merge the handoff-doc candidate ahead of an in-flight PR #102 review unless prepared to refresh PR #102 base/CI/review afterward. Prefer finishing PR #102 first, then non-destructively refresh the handoff-doc candidate to the new main and review/merge it.

## Historical/open PR hygiene

Legacy experimental/historical PRs are not active implementation authority. In particular, do not treat old PR #39 or PR #90 as the current product candidate. Re-fetch and use trusted tasks/current PR state rather than reviving stale branches.

## Hard boundaries

- GitHub is sole source of truth;
- exact-SHA reviews do not transfer across candidate changes;
- candidate source never runs on trusted Windows/TIA before independent review + merge;
- vendor payload/raw Siemens text stays out of GitHub;
- no unverified Valve facts;
- no F-safety/broad UI/multi-vendor expansion;
- never modify `IndustrialMDE`.
