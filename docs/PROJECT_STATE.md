# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-19
Trusted `main` checkpoint: `f6357be194266a787e7c3b3384e2cae8a3366b64`

GitHub is the only durable source of truth. Fresh sessions must re-fetch live refs, PRs, tasks, Actions and issue state before acting. `IndustrialMDE` is outside scope and must not be modified.

## Project goal

Build a production engineering system that converts a vendor-neutral automation model into modular Siemens PLC code/projects and verifies/assembles them through TIA Portal V21 Openness.

Baseline path:

```text
Engineering UI
 -> canonical AutomationProject
 -> Domain
 -> PLC Compiler / PLC IR
 -> Siemens Backend / qualified Open Library bindings
 -> generated PLC artifact/package
 -> trusted TiaV21Worker / ProjectAssembler
 -> TIA Portal V21
```

A second first-class output is the reusable AI-assisted development methodology, but process work must not unnecessarily delay generator delivery.

## Operating model

- Coding providers: OpenRouter first, official DeepSeek `deepseek-flash` fallback.
- Routine source changes should be authored by the coding agent; primary connected ChatGPT should normally remain the independent reviewer/orchestrator rather than manually writing candidate code.
- Primary connected ChatGPT: Senior Architect, task designer, normal independent reviewer when authorship permits, root-cause analyst, methodology curator and delegated routine technical merge authority.
- `chatgpt-secondary` is escalation/independence support when primary materially authored/co-authored a candidate; the human must not routinely act as a message bus between AI agents.
- One independent reviewer is required by default; exact-SHA review semantics remain authoritative.
- Candidate code executes only on disposable Linux before merge; trusted Windows/TIA checks out trusted `main` only.
- Real TIA Portal V21 evidence is authoritative Siemens acceptance.
- Vendor archives and raw Siemens diagnostics remain outside GitHub.

## Durable completed milestones relevant to current work

- ARCH-001 PR #17 merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`.
- PLC-001 PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after TIA V21 compile 0 errors / 0 warnings.
- GOV-BOOT-001 PR #32 merged; permanent bootstrap lane exists.
- OLQ-001 implementation PR #22, OLQ-INFRA-002 PR #27 and OLQ-INFRA-003 PR #37 are merged.
- CODER-RUNTIME-001 PR #43 and CODER-REPAIR-RUNTIME-001 PR #45 are merged; large prompts use stdin and repair orchestration is pinned to immutable trusted-main state.
- OLQ-DIAG-001 PR #41 merged as `9275025fc0aa60f76081f0b2a2e7846296f22718`. A one-time human waiver applied only to that exact candidate because primary had manually repaired it; this did not change standing reviewer policy.

## Current OLQ evidence

Trusted OLQ run `35444033247` / run #30 executed trusted main `9275025fc0aa60f76081f0b2a2e7846296f22718` and completed with final qualification failure after publishing sanitized evidence.

Current actionable diagnostic:

```text
phase:retrieve-with-upgrade
type:EngineeringTargetInvocationException
hresult:0x80131500
```

During that run the operator also observed Siemens dialog `Openness access (0033:000666)` for the freshly built `TiaV21Worker.exe`.

Important separation:

- the popup is an autonomy/runner authorization defect;
- the `RetrieveWithUpgrade` exception is a separate Siemens/Open Library migration defect;
- fixing the popup does not imply OLQ PASS.

## ACTIVE priority — TIA-AUTH-001 / issue #46

Trusted task: `tasks/TIA-AUTH-001.json`.
Risk: HIGH.
Issue: #46.

Goal: eliminate the interactive TIA Openness authorization popup for trusted-main `TiaV21Worker.exe` without weakening UAC/TIA security or the candidate->Windows trust boundary.

Authorized coding-agent scope:

- `src/TiaV21Worker/**`;
- one narrowly scoped bootstrap script under `scripts/windows/**`;
- no `.github/**`, `agents/**`, `tasks/**`, runner config, secrets, generator/domain/compiler semantics, vendor payloads or `IndustrialMDE` changes.

The task allows bounded TiaV21Worker changes with `candidatePolicy.allowTiaV21WorkerChanges=true` and requires primary `chatgpt` review because the intended author is the coding agent.

At this checkpoint there is **no implementation PR for TIA-AUTH-001**. The only open PR is draft PR #39.

### Next safe action

Start `Autonomous Agent` in **task mode** with:

- branch/ref: `main`;
- `source=task`;
- `task_path=tasks/TIA-AUTH-001.json`;
- empty `issue_number`.

The connected GitHub connector did not expose new `workflow_dispatch` creation at the previous checkpoint. A fresh primary should re-check available GitHub tools first. If dispatch is still unavailable, ask the human only for this single GitHub UI click. Do **not** ask the human to install `gh`, configure a terminal, or shuttle JSON between chats.

After the agent creates a candidate:

1. primary re-fetches exact PR head/scope/provider audit/CI;
2. primary performs independent exact-SHA CODE_REVIEW;
3. bounded coding-agent repair if needed;
4. delegated merge when gates are green;
5. if worker reports bootstrap-required, provide one minimal elevated Windows bootstrap action to the operator;
6. rerun trusted `/run-olq-001` from issue #19 and verify no Openness popup appears.

If `retrieve-with-upgrade` still fails afterward, create a separate bounded Siemens/Open Library migration task based on the exact phase/type/HRESULT evidence. Do not broaden TIA-AUTH-001.

## Periodic independent repository audit — created, execution deferred

Standing procedure now exists:

- `tasks/REPO-AUDIT-001.json`;
- `docs/INDEPENDENT_REPOSITORY_AUDIT.md`;
- tracking issue #47;
- `reviews/repository-audits/` report location.

Trial cadence: before a new roadmap milestone or after every 5 merged implementation PRs, whichever comes first, plus selected out-of-cycle trust/prompt/architecture incidents.

The human explicitly deferred the first full audit until later. It is **not** the current blocker and must not delay TIA-AUTH-001 / OLQ / generator progress.

## CHAT-HANDOFF-001 / issue #38 / PR #39

Trusted task `tasks/CHAT-HANDOFF-001.json` is on `main`.

PR #39 is still draft, primary-authored and documentation/process-only. Live checkpoint before this handoff showed:

- branch `chatgpt/chat-handoff-protocol`;
- head before the current snapshot refresh: `e9f3146bf0af6244d50c1ae1e2900ba9c6cd631d`;
- old base snapshot `69f554445003975279a391d6c2672d45054ab418`;
- mergeable but far behind current `main`.

This handoff refresh updates its state snapshots again. PR #39 must eventually be synchronized with current `main`, receive fresh exact-head CI and isolated `chatgpt-secondary` review before merge. It does **not** block current generator/OLQ work.

## Queued product path

After removing the popup and resolving the real Open Library migration blocker:

1. achieve real deterministic OLQ PASS;
2. stop for explicit human acceptance of OLQ-001;
3. `OL-001` inspect qualified V21 valve contract;
4. `OL-002` target profile/preflight/materialization;
5. PLC compiler foundations;
6. `GEN-001` first real generated valve application in TIA V21.

AUTO-001 / issue #35 and PR #39 should not be put in front of this product path unless a concrete trust defect requires it.

## Hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch MVP expansion;
- no generic multi-vendor plugin architecture now;
- no broad UI/HMI expansion before PLC/Open Library vertical slice stability;
- no candidate execution on trusted Windows/TIA before review + merge;
- self-hosted TIA workflows execute trusted `main` only;
- no vendor `.zal19/.zal21` payload in Git/GitHub artifacts;
- no raw Siemens exception/path publication;
- no secret/private signing material in repository/CI/runners;
- do not modify `IndustrialMDE`.
