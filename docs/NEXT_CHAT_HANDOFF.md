# TIA Automation Factory — Fresh Chat Handoff

Checkpoint date: 2026-09-19
Trusted `main` at checkpoint: `f6357be194266a787e7c3b3384e2cae8a3366b64`

GitHub is the sole durable source of truth. This is an operational checkpoint, not permission to trust stale snapshots. A fresh primary chat must independently re-fetch live GitHub before acting.

## Mandatory startup

Read in order:

1. `AGENTS.md` from trusted `main`.
2. `docs/PROJECT_STATE.md` and `docs/NEXT_CHAT_HANDOFF.md`; note that `main` still has stale pre-handoff snapshots until CHAT-HANDOFF-001 / PR #39 merges, so also inspect the current PR #39 candidate versions.
3. `docs/AI_COLLABORATION_MODEL.md`.
4. `docs/DEVELOPMENT_METHODOLOGY.md` and recent `docs/METHODOLOGY_JOURNAL.md` entries.
5. `docs/EXTERNAL_REVIEW_PROTOCOL.md` before review work.
6. Active trusted tasks and live PR/issues/Actions state.
7. Methodology telemetry issue #25 when recent workflow evidence matters.

`IndustrialMDE` is outside scope and must not be touched.

## Role transferred

The fresh chat assumes the connected **primary ChatGPT** role: Senior Architect, task designer, orchestrator, normal independent reviewer when authorship permits, methodology curator and delegated routine technical merge authority.

Preferred normal operating model from this checkpoint forward:

```text
OpenRouter / DeepSeek write code
 -> primary ChatGPT reviews/orchestrates
 -> coding provider repairs if needed
 -> primary ChatGPT re-reviews
 -> merge
 -> trusted-main TIA when applicable
```

The human is strategic/risk/physical-machine authority, not a routine message bus. Do not ask the human to copy review JSON between chats in normal coding-agent work.

## Current objective

Resume product progress with minimum extra process work:

1. eliminate the interactive TIA Openness access popup from trusted worker execution;
2. rerun OLQ qualification non-interactively;
3. resolve the real `RetrieveWithUpgrade` Siemens/Open Library migration defect if it remains;
4. achieve deterministic OLQ PASS and request explicit human acceptance;
5. move directly into `OL-001 -> OL-002 -> compiler foundations -> GEN-001`.

## What changed since the previous handoff

### OLQ-DIAG-001 completed

PR #41 was manually repaired by primary ChatGPT to close the previously identified sequencing/token findings. Exact repaired candidate:

`f03de3a4212c4ff87c4c9d3442f91ad9ee09b781`

Exact CI #290 passed. The human granted a **one-time waiver only for PR #41** because primary had become an author. PR #41 merged as:

`9275025fc0aa60f76081f0b2a2e7846296f22718`

The waiver did not change standing reviewer policy. Future routine product code should be authored by OpenRouter/DeepSeek so primary remains independent reviewer.

### Trusted OLQ run #30 produced actionable Siemens diagnostics

Run:

`35444033247`

Trusted head:

`9275025fc0aa60f76081f0b2a2e7846296f22718`

Result: final qualification `FAIL`, after sanitized evidence publication.

Public-safe diagnostic:

```text
phase:retrieve-with-upgrade
type:EngineeringTargetInvocationException
hresult:0x80131500
```

During the run the operator also saw Siemens `Openness access (0033:000666)` for the newly built `TiaV21Worker.exe`.

These are separate problems:

- popup = trusted worker authorization/autonomy defect;
- `retrieve-with-upgrade` exception = actual Siemens/Open Library migration defect.

Do not conflate them.

## ACTIVE task — TIA-AUTH-001 / issue #46

Trusted task:

`tasks/TIA-AUTH-001.json`

Issue:

`#46`

Trusted task commit was introduced after PR #41 and explicitly allows bounded `src/TiaV21Worker/**` changes via `candidatePolicy.allowTiaV21WorkerChanges=true`.

Goal: make trusted Windows/TIA execution non-interactive by synchronizing the current worker's own Siemens Openness whitelist entry before TIA connection, with one narrowly scoped elevated bootstrap for registry ACLs if required.

Authorized candidate scope:

- `src/TiaV21Worker/**`;
- `scripts/windows/**` only for the one-time bootstrap;
- no `.github/**`, `agents/**`, `tasks/**`, runner config, secrets, vendor payload handling, generator/domain/compiler semantics or `IndustrialMDE`.

Reviewer slot: primary `chatgpt`, provided candidate remains coding-agent-authored.

### Live state at handoff

There is **no TIA-AUTH implementation PR yet**.

Live open PR query showed only:

- draft PR #39 `CHAT-HANDOFF-001`.

Therefore the TIA-AUTH coding agent has not produced a candidate at this checkpoint.

### Exact first action for the fresh chat

1. Re-fetch `main`, issue #46, `tasks/TIA-AUTH-001.json`, open PRs and recent Actions.
2. Check connected GitHub tools for the ability to create `workflow_dispatch`.
3. If dispatch is available, start `.github/workflows/agent.yml` on `main` with:
   - `source=task`
   - `task_path=tasks/TIA-AUTH-001.json`
   - empty `issue_number`.
4. If dispatch is **not** available, ask the human for exactly one UI action:
   - GitHub -> Actions -> Autonomous Agent -> Run workflow
   - branch `main`
   - source `task`
   - task path `tasks/TIA-AUTH-001.json`
   - issue number empty.

Do **not** ask the human to install `gh`, configure a terminal, reconstruct context, or shuttle JSON between agents.

After a candidate exists, primary handles review/repair/merge autonomously under normal exact-SHA gates.

## After TIA-AUTH merge

Run trusted `/run-olq-001` from issue #19.

Acceptance for TIA-AUTH is only that the interactive `Openness access` dialog no longer appears and the worker fails safely before TIA if bootstrap permission is missing.

OLQ PASS is not required by TIA-AUTH.

If OLQ still fails at:

```text
phase:retrieve-with-upgrade
type:EngineeringTargetInvocationException
hresult:0x80131500
```

create a new bounded Siemens/Open Library migration task. Use official V21 Openness API evidence already linked in issue #19; do not broaden TIA-AUTH or guess from raw vendor data.

## Periodic full repository audit — procedure exists, execution deferred

Created on trusted `main`:

- `tasks/REPO-AUDIT-001.json`;
- `docs/INDEPENDENT_REPOSITORY_AUDIT.md`;
- tracking issue #47;
- `reviews/repository-audits/`.

Audit scope includes whole-repository architecture/code/tests/workflows, AI operating model, all prompts/runtime, TIA/Open Library boundary and simplification advice.

Trial cadence is recorded in issue #47, but the human explicitly chose to run the first deep audit later. It is **not** the current task and must not block product progress.

When the audit is eventually run, its recommendations are advisory until triaged; prompt/methodology cleanup must not automatically become generator blockers.

## CHAT-HANDOFF-001 / issue #38 / PR #39

PR #39 remains draft, primary-authored, documentation/process-only and far behind current `main`.

Live state before this snapshot refresh:

- branch: `chatgpt/chat-handoff-protocol`;
- head: `e9f3146bf0af6244d50c1ae1e2900ba9c6cd631d`;
- old base snapshot: `69f554445003975279a391d6c2672d45054ab418`;
- mergeable: true;
- draft: true;
- changed files: 8 authorized documentation/process files.

This handoff refresh creates new commits on the PR #39 branch, so the prior exact head is historical. Re-fetch live PR #39 before any review action.

PR #39 does **not** block TIA-AUTH/OLQ/generator work. Eventually:

1. synchronize it with current `main`;
2. verify documentation-only scope;
3. exact-head CI;
4. isolated `chatgpt-secondary` review because primary authored it;
5. merge if approved.

## Other non-blocking work

- issue #47 repository audit: deferred until later by human decision;
- issue #35 AUTO-001: useful but not a prerequisite for current product path;
- issue #42 coder-runtime validation may remain historical/open; do not prioritize it over the generator unless it reappears as a concrete failure.

## Hard boundaries

- candidate source/scripts never execute on trusted Windows/TIA before independent review + merge;
- self-hosted TIA workflows execute trusted `main` only;
- no `.zal19/.zal21` vendor payload in Git/GitHub artifacts;
- no raw Siemens exception/path exposure in public evidence;
- no secrets/private signing keys in repository/CI/runners;
- no F-safety scope expansion;
- no broad React Flow/UI work before the PLC/Open Library vertical slice is stable;
- do not modify `IndustrialMDE`.

## Fresh-chat success criterion

The new chat is correctly synchronized if, after its own live GitHub verification, it can answer all of these without asking the human for old-chat context:

- current `main` SHA;
- why OLQ currently fails;
- why the popup is separate from migration failure;
- which task is active;
- whether a TIA-AUTH candidate PR already exists;
- exact next action;
- what the human does and does not need to do.
