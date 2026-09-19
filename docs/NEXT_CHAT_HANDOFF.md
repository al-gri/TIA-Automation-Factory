# TIA Automation Factory — Fresh Chat Handoff

Checkpoint date: 2026-09-19
Trusted `main` at checkpoint: `69f554445003975279a391d6c2672d45054ab418`

GitHub is the sole durable source of truth. This file is the latest operational transfer checkpoint, not a historical diary. A fresh chat must still verify all live SHAs, PRs, Actions and issue state before acting.

## Mandatory startup

Read, in order:

1. `AGENTS.md`
2. `docs/PROJECT_STATE.md`
3. this file
4. `docs/AI_COLLABORATION_MODEL.md`
5. `docs/DEVELOPMENT_METHODOLOGY.md`
6. latest relevant entries in `docs/METHODOLOGY_JOURNAL.md`
7. `docs/EXTERNAL_REVIEW_PROTOCOL.md` before review work
8. `docs/CHAT_HANDOFF_PROTOCOL.md` when transferring the primary role between chats
9. `docs/GOVERNANCE_BOOTSTRAP.md` only when a live task genuinely uses the exceptional bootstrap lane

Then independently inspect live GitHub tasks, open PRs/issues, exact candidate SHAs, Actions/TIA evidence, pending external-review requests and methodology issue #25.

`IndustrialMDE` is outside scope and must not be touched.

## Primary role

The fresh chat assumes the connected primary ChatGPT role:

- Senior Architect / orchestrator;
- normal independent reviewer for coding-agent work when authorship permits;
- methodology curator;
- delegated routine technical merge authority.

If primary materially authored/co-authored a candidate, required independent review is `chatgpt-secondary` in a fresh isolated chat.

## Durable completed milestones

- ARCH-001 PR #17 merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`.
- PLC-001 PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after trusted TIA V21 compile with 0 errors / 0 warnings.
- Governance PR #24 merged at `ef7e5a00e74a9d3b994c23637aab6fc2ae2546f5`.
- GOV-CTX-001 PR #30 merged at `0260117391abf5f0a8375699dca12caa06bafb8b`.
- GOV-BOOT-001 PR #32 merged at `60bd20841360998406db59cfb13a61cb33982566`; issue #31 is closed. Permanent governance bootstrap now exists and normal task-backed automation remains fail-closed.
- OLQ-001 implementation PR #22 merged at `5c8c957e6abb7004e4ee9e9382de97347ebfc9c6`.
- OLQ-INFRA-002 PR #27 merged at `24f3a8fad542132a7aa9369be4451dd6ca0ae23f` after exact-SHA CI and fresh independent `chatgpt-secondary` APPROVE.

## Current objective

Finish real Siemens Open Library V19 -> TIA Portal V21 qualification in a way that produces deterministic sanitized evidence, then obtain explicit human acceptance before the qualified profile becomes a normal generator dependency.

## Active work 1 — OLQ-INFRA-003 / issue #36 / PR #37

Purpose: repair the post-merge runtime observability defect exposed by trusted qualification run `35426328232`.

Observed trusted runtime facts:

- trusted Windows runner checked out merged `main` and verified SHA;
- `TiaV21Worker` built successfully with 0 warnings / 0 errors;
- the real first `qualify-library` invocation started;
- the outer 45-minute job timeout cancelled the operation before public evidence was finalized;
- GitHub cleanup terminated remaining Siemens Portal-related processes;
- this proves neither vendor-library failure nor qualification success.

Current PR #37 live checkpoint:

- branch: `chatgpt/olq-infra-003-bounded-timeout`
- exact head: `0c9633fe657b8949beea76f87f3a63411cd103e0`
- base: `main` at `69f554445003975279a391d6c2672d45054ab418`
- changed-file scope: exactly `.github/workflows/tia-v21-library-qualification.yml`
- exact-SHA CI: #264 / run `35429912399` — PASS
- candidate Windows/TIA execution: none
- primary ChatGPT materially authored the candidate -> required reviewer: fresh `chatgpt-secondary`

Important stale evidence:

- prior review-ready SHA `cff83ee7284fa1ea2c4d75621ddfe720914a5688` and its review package are stale because `main` moved during CHAT-HANDOFF task authorization/recovery and PR #37 was explicitly resynchronized.
- do not accept any verdict bound to `cff83ee...` for current PR #37.

Next gate for PR #37:

1. prepare/record a fresh review request bound to `0c9633fe657b8949beea76f87f3a63411cd103e0`;
2. obtain isolated `chatgpt-secondary` CODE_REVIEW;
3. if APPROVE and live head/CI/scope remain unchanged, primary may delegated-merge;
4. rerun trusted `/run-olq-001` from issue #19 only after merge.

## Active work 2 — CHAT-HANDOFF-001 / issue #38

Trusted task `tasks/CHAT-HANDOFF-001.json` is on `main`.

Purpose: make transfer from a context-exhausted primary chat to a fresh primary chat a repository-first freshness transaction rather than a transcript/memory transfer.

Candidate branch: `chatgpt/chat-handoff-protocol`.

Primary ChatGPT authors this documentation/process change, so eventual merge requires exact-SHA CI plus fresh isolated `chatgpt-secondary` review.

Do not merge this handoff candidate before PR #37 review/merge is resolved unless the resulting PR #37 base/head invalidation is explicitly accepted and re-reviewed. The preferred order is to finish PR #37 first, then synchronize/review/merge the handoff candidate.

Issue #38 records the trigger commands and full required transaction. Candidate `docs/CHAT_HANDOFF_PROTOCOL.md` defines the detailed procedure.

## Handoff-protocol incident recorded during this checkpoint

While establishing CHAT-HANDOFF-001, primary ChatGPT twice issued a connector `create_file` call with `branch=main` for a temporary placeholder instead of first creating/verifying a candidate branch. Both placeholder files were immediately reverted and no placeholder remains in the tree, but `main` history moved.

Consequences:

- the previously prepared PR #37 review package became stale even though the accidental tree changes were reverted;
- trusted task `tasks/CHAT-HANDOFF-001.json` was intentionally added to `main` and normalized to the canonical task schema;
- PR #37 was rebuilt against the new `main` while preserving its one-workflow diff, producing current head `0c9633fe...` and fresh CI #264 PASS.

Methodology lesson: every authority-bearing repository write must have an explicit verified target ref/branch precondition, and a handoff audit must detect base/head/review invalidation before transferring control.

## Other queued work

- AUTO-001 / issue #35 remains the accepted hybrid-autonomy initiative, downstream of the current OLQ qualification blocker.
- After accepted Open Library qualification: continue roadmap `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.

## Current human action

The only current manual review action is to run a **fresh** `chatgpt-secondary` review for PR #37 using the package generated for exact head `0c9633fe...`. Do not reuse the previous `cff83ee...` package.

No Windows/TIA manual action should be taken on candidate code.

## Hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch generation for MVP;
- no generic multi-vendor plugin architecture now;
- no broad UI/HMI expansion before the PLC/Open Library vertical slice is stable;
- candidate source/scripts never execute on trusted Windows/TIA before independent approval + merge;
- self-hosted Windows/TIA workflows execute trusted `main` only;
- no `.zal19/.zal21` vendor payloads in Git;
- no secrets or human private signing material in repository/CI/runners;
- do not modify `IndustrialMDE`.

## Fresh-chat next safe action

1. Verify live `main`, PR #37 head/base/diff and CI.
2. Verify any returned secondary review is bound to current exact PR #37 head.
3. If no valid current review exists, prepare the fresh isolated review package; do not reuse stale `cff83ee...` evidence.
4. Continue OLQ-INFRA-003 through review -> delegated merge -> trusted rerun.
5. After PR #37 is resolved, synchronize and independently review CHAT-HANDOFF-001 before merging its protocol candidate.
