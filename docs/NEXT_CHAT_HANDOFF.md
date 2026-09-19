# TIA Automation Factory — Fresh Chat Handoff

Checkpoint date: 2026-09-19
Trusted `main` at checkpoint: `454ead3ed72cc00cda5d60dcffa23d347e00932e`

GitHub is the sole durable source of truth. This is an operational checkpoint, not permission to trust stale snapshots. A fresh primary chat must independently re-fetch live GitHub before acting.

## Mandatory startup

Read in order:

1. `AGENTS.md` from trusted `main`.
2. `docs/PROJECT_STATE.md` and `docs/NEXT_CHAT_HANDOFF.md`; note that the versions on `main` are stale until CHAT-HANDOFF-001 / PR #39 merges, so also inspect the current PR #39 candidate versions.
3. `docs/AI_COLLABORATION_MODEL.md`.
4. `docs/DEVELOPMENT_METHODOLOGY.md` and recent `docs/METHODOLOGY_JOURNAL.md` entries.
5. `docs/EXTERNAL_REVIEW_PROTOCOL.md` before review work.
6. Active trusted tasks and live PR/issue/Actions state.
7. Methodology telemetry issue #25 when recent workflow evidence matters.

`IndustrialMDE` is outside scope and must not be touched.

## Role transferred

The fresh chat assumes the connected **primary ChatGPT** role: Senior Architect, orchestrator, normal independent reviewer when authorship permits, methodology curator and delegated routine technical merge authority.

Primary must not independently approve code it materially authored. Such candidates require fresh isolated `chatgpt-secondary` review.

## Current objective

Finish real Siemens Open Library V19 -> native TIA Portal V21 qualification with deterministic sanitized evidence, then stop for explicit human acceptance before the qualified profile becomes a normal generator dependency.

## Completed in the outgoing chat

- OLQ-INFRA-003 / PR #37 merged at `6f34fdf1898bd36b772d117af90208a3e270602e` from exact reviewed candidate `ffd75e0951f27567044470991422ac86f51e256e`.
- Trusted OLQ run `35430943663` proved the bounded-timeout harness works: checkout/build/evidence publication completed and the run did **not** time out. It failed qualification with public-safe `Library upgrade/archive operation failed.`, no qualified archive, `nativeReopen=false`, and no second run. Raw Siemens details remained runner-local.
- Issue #40 / trusted task `tasks/OLQ-DIAG-001.json` created to add privacy-safe phase + exception type + HRESULT diagnostics without changing migration semantics.
- Coding-agent PR #41 created at head `1470b2e6d85ca6e54ec3fae7c929a6dd441f97c1`, changing only `src/TiaV21Worker/Program.cs`.
- Primary review of PR #41 returned `CHANGES_REQUIRED`: F001 semantic sequencing bug (Archive could run after failed RetrieveWithUpgrade because guard checked only `saveException == null`) plus stable-token naming issue. Repair attempt 1 never changed candidate code because coding runtime failed before provider startup with `/usr/bin/timeout: Argument list too long`.
- CODER-RUNTIME-001 / PR #43 fixed large prompt transport by streaming the complete trusted prompt through stdin. PR #43 merged at `ccb2c32ae63184317cb61eeb949cc24341111175`; issue #42 remains intentionally open until PR #41 repair proves the runtime fix in the real repair loop.
- A second repair blocker was found: old candidate branches executed stale branch-local `run-coder.sh`. CODER-REPAIR-RUNTIME-001 / PR #45 repaired the trust boundary.
- PR #45 round 1 found major F001: candidate `reviewer-policy.py` executed before protected-path rejection and later trusted reads used mutable `origin/main`.
- Round-1 F001 was repaired by capturing immutable `TRUSTED_MAIN_SHA`, rejecting protected candidate infrastructure before repository-provided orchestration executes, and sourcing task/reviewer-policy/prompts/run-coder from that SHA.
- Fresh isolated `chatgpt-secondary` round 2 APPROVED exact PR #45 head `536033b35a189ea777c189f13d7c75a980fee279`; CI #286/run `35440209501` and #282/run `35440102032` PASS, findings empty.
- PR #45 merged with expected-head guard into trusted `main` commit `454ead3ed72cc00cda5d60dcffa23d347e00932e`; issue #44 is closed.

## Active priority 1 — resume OLQ-DIAG-001 PR #41

Live checkpoint at transfer:

- PR: #41
- branch: `agent/task-olq-diag-001-35431664029`
- candidate SHA: `1470b2e6d85ca6e54ec3fae7c929a6dd441f97c1`
- base at creation: `6ccee0eb531a72a7f1f492960fb2261e427b7d9e`
- changed files: exactly `src/TiaV21Worker/Program.cs`
- trusted task: `tasks/OLQ-DIAG-001.json`
- reviewer slot for agent-authored candidate: primary `chatgpt`
- prior review: `CHANGES_REQUIRED` with F001/F002 as described above
- prior repair run `35432115775`: infrastructure failure only, **no candidate code change**; do not treat it as a semantic repair attempt against the OLQ task budget.

The repair infrastructure is now fixed on trusted `main` by PR #43 + PR #45.

### Fresh-chat next safe action

1. Re-fetch PR #41 live head/diff/comments and `tasks/OLQ-DIAG-001.json`.
2. Confirm head is still `1470b2e6d85ca6e54ec3fae7c929a6dd441f97c1` and no candidate repair occurred after this checkpoint.
3. Re-dispatch the bounded `candidate-repair` flow for the existing `CHANGES_REQUIRED` state using the current trusted-main repair workflow. Do **not** rerun the historical failed Actions run, because a rerun would preserve its old workflow SHA/runtime.
4. Verify the new repair run uses trusted-main runtime and proceeds past prompt transport.
5. If repaired candidate SHA changes, perform a fresh exact-SHA primary CODE_REVIEW. Primary is independent because candidate code remains coding-agent-authored unless primary manually edits candidate source.
6. If APPROVE + exact Linux CI green, merge under delegated gate; candidate Windows/TIA execution remains forbidden pre-merge.
7. After merge, post `/run-olq-001` on issue #19 and inspect the sanitized phase/type/HRESULT evidence.
8. If OLQ passes, stop for explicit human acceptance. If it exposes a real Siemens migration/API/vendor issue, create a new separately scoped task; do not broaden OLQ-DIAG-001 retroactively.

If connected GitHub tooling cannot emit `repository_dispatch`, ask the human only for the minimal mechanical dispatch needed; do not substitute a stale Actions rerun.

## Active priority 2 — finish CHAT-HANDOFF-001 / issue #38 / PR #39

PR #39 is still draft and stale/diverged from current `main` because it was created at base `69f554445003975279a391d6c2672d45054ab418`. Its candidate docs now contain this checkpoint, but the branch must be synchronized with current `main` before review.

Primary authored PR #39. Required gate before merge:

1. synchronize/rebase/merge current trusted `main` into `chatgpt/chat-handoff-protocol` without losing the authorized documentation-only diff;
2. verify scope remains only the CHAT-HANDOFF-001 authorized documentation/process files;
3. obtain fresh exact-head deterministic CI PASS;
4. obtain fresh isolated `chatgpt-secondary` CODE_REVIEW APPROVE;
5. delegated-merge if all gates remain current.

Until PR #39 merges, `AGENTS.md`/state snapshots on `main` do not yet contain the formal handoff command semantics. This checkpoint and issue #38 are the durable transfer record for this invocation.

## Other queued work

- AUTO-001 / issue #35 remains downstream of the OLQ qualification blocker.
- After accepted Open Library qualification: `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.
- Legacy open smoke PRs #4, #5, #10 and #13 exist but are not current active work; do not revive them without an explicit current task.

## Hard boundaries

- no candidate source/scripts on trusted Windows/TIA before independent approval + merge;
- self-hosted TIA workflows execute trusted `main` only;
- no `.zal19/.zal21` vendor payload in Git/GitHub artifacts;
- no raw Siemens exception/path exposure in public evidence;
- no secrets/private signing keys in repository/CI/runners;
- no F-safety scope expansion;
- do not modify `IndustrialMDE`.
