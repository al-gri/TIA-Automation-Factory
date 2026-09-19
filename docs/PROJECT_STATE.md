# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-19
Trusted `main` checkpoint: `454ead3ed72cc00cda5d60dcffa23d347e00932e`

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

A second first-class output is the reusable AI-assisted development methodology.

## Operating model

- Coding providers: OpenRouter first, official DeepSeek `deepseek-flash` fallback.
- Primary connected ChatGPT: Senior Architect, orchestrator, normal reviewer when independent, methodology curator, delegated routine technical merge authority.
- `chatgpt-secondary`: fresh isolated reviewer when primary materially authored/co-authored the candidate.
- One independent reviewer is required by default for LOW/MEDIUM/HIGH work.
- Exact-SHA review is mandatory; candidate-changing edits invalidate prior approval.
- Normal work requires a trusted versioned task on `main`.
- Candidate code executes only on disposable Linux before merge; trusted Windows/TIA checks out `main` only.
- Real TIA Portal V21 evidence is authoritative Siemens acceptance.
- Vendor archives and raw Siemens diagnostics remain outside GitHub.

## Durable completed milestones

- ARCH-001 PR #17 merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`.
- PLC-001 PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after TIA V21 compile 0 errors / 0 warnings.
- Governance PR #24 merged at `ef7e5a00e74a9d3b994c23637aab6fc2ae2546f5`.
- GOV-CTX-001 PR #30 merged at `0260117391abf5f0a8375699dca12caa06bafb8b`.
- GOV-BOOT-001 PR #32 merged at `60bd20841360998406db59cfb13a61cb33982566`; issue #31 closed.
- OLQ-001 implementation PR #22 merged at `5c8c957e6abb7004e4ee9e9382de97347ebfc9c6`.
- OLQ-INFRA-002 PR #27 merged at `24f3a8fad542132a7aa9369be4451dd6ca0ae23f`.
- OLQ-INFRA-003 PR #37 merged at `6f34fdf1898bd36b772d117af90208a3e270602e`.
- CODER-RUNTIME-001 PR #43 merged at `ccb2c32ae63184317cb61eeb949cc24341111175`.
- CODER-REPAIR-RUNTIME-001 PR #45 merged at `454ead3ed72cc00cda5d60dcffa23d347e00932e` after exact candidate `536033b35a189ea777c189f13d7c75a980fee279`, fresh `chatgpt-secondary` round-2 APPROVE and green exact-SHA CI #286/#282.

## Current product blocker — real Open Library qualification

Tracking issue: #19.

Trusted OLQ run `35430943663` on merged PR #37 did **not** time out. It proved the new bounded harness publishes sanitized evidence before fail-closed enforcement. Result:

- qualification status: FAIL;
- first worker manifest: `success=false`;
- public-safe failure: `Library upgrade/archive operation failed.`;
- no qualified archive hash;
- `nativeReopen=false`;
- second run not attempted;
- raw `FailureDetails` stayed runner-local.

The current evidence cannot safely distinguish `RetrieveWithUpgrade` vs `Save` vs `Archive`, so no vendor/library root cause may be guessed.

## Active task — OLQ-DIAG-001 / issue #40 / PR #41

Trusted task: `tasks/OLQ-DIAG-001.json`.
Risk: HIGH.

Goal: preserve migration semantics while exposing only stable public diagnostics: operation phase + exception type + hexadecimal HRESULT. Raw exception message/stack/path remains runner-local.

PR #41 live checkpoint at handoff:

- branch: `agent/task-olq-diag-001-35431664029`;
- head: `1470b2e6d85ca6e54ec3fae7c929a6dd441f97c1`;
- changed files: exactly `src/TiaV21Worker/Program.cs`;
- candidate authored by coding agent; primary `chatgpt` is the independent reviewer unless primary later edits source.

Primary review returned `CHANGES_REQUIRED`:

- F001: semantic sequencing regression — Archive can run after failed `RetrieveWithUpgrade` because the candidate guards Archive only on `saveException == null`;
- F002: public phase tokens should be stable semantic strings (`native-reopen`, `native-reopen-close`, etc.), not incidental enum `ToString()` names.

The first bounded repair run `35432115775` made **no candidate change**. It failed before provider startup because the complete repair prompt was passed through argv and hit `/usr/bin/timeout: Argument list too long`.

That repair infrastructure is now fixed on trusted `main`:

1. PR #43 streams full prompts via stdin for OpenRouter and DeepSeek.
2. PR #45 makes Autonomous Repair use orchestration sourced from one immutable trusted-main SHA and rejects protected candidate infrastructure before candidate repository code can control the trust boundary.

Issue #42 remains open only until the PR #41 repair loop demonstrates successful provider execution past prompt transport. Issue #44 is closed.

### Next required action

Re-dispatch a **new** bounded `candidate-repair` event for PR #41 from current trusted `main`. Do not rerun the historical failed Actions run, because a GitHub rerun would reuse its old workflow/runtime snapshot.

After a candidate-changing repair:

- require fresh exact-SHA primary CODE_REVIEW;
- exact Linux CI must be green;
- no candidate Windows/TIA execution;
- on APPROVE, delegated merge;
- then trusted `/run-olq-001` from issue #19.

If the trusted diagnostic run identifies a real Siemens migration/API/vendor issue, create a new separately scoped task. Do not broaden OLQ-DIAG-001.

## CHAT-HANDOFF-001 / issue #38 / PR #39

Trusted task `tasks/CHAT-HANDOFF-001.json` is on `main`.

PR #39 defines the repository-first primary-chat transfer procedure. It is primary-authored, documentation/process only, and requires exact-SHA CI + fresh isolated `chatgpt-secondary` APPROVE before merge.

At this checkpoint PR #39 is still draft and diverged from current `main` because it was created from old base `69f554445003975279a391d6c2672d45054ab418`. Its candidate snapshots have been refreshed to record this handoff, but the branch must be synchronized with current `main` before review/merge.

Until PR #39 merges, the formal handoff command semantics are not yet on `main`; issue #38 plus the current PR #39 candidate are the durable transfer record.

## Methodology lessons confirmed in this chat

- Large model/review context must use stream/file transport, not one large argv element.
- Repair orchestration for an old candidate must come from trusted current infrastructure, not the stale candidate branch.
- A trusted ref must be captured as an immutable commit SHA before candidate-controlled repository executables run; protected-path rejection must precede candidate orchestration execution.
- Exact-SHA review remains authoritative even for infrastructure fixes; no-op/history movement still invalidates a frozen review identity.
- Primary-chat transfer is a repository freshness transaction, not transcript copying.

## Queued work

- AUTO-001 / issue #35 remains downstream of accepted OLQ qualification.
- After explicit human acceptance of the qualified V21 profile: `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.
- Legacy open smoke PRs #4/#5/#10/#13 are historical and are not active work.

## Hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch MVP expansion;
- no generic multi-vendor plugin architecture now;
- no broad UI/HMI expansion before PLC/Open Library vertical slice stability;
- no candidate execution on trusted Windows/TIA before review + merge;
- no vendor `.zal19/.zal21` payload in Git/GitHub artifacts;
- no raw Siemens exception/path publication;
- no secret/private signing material in repository/CI/runners;
- do not modify `IndustrialMDE`.
