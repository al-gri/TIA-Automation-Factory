# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-27
Checkpoint trusted main: `0a1d891bd7c26a4dbe057ca9d9a83e1c2b0e31b1`

GitHub is the only durable source of truth. Re-fetch live refs/tasks/PRs/reviews/Actions before acting; this file is a recovery aid, not a substitute for live evidence.

## Project and operating boundaries

Repository: `al-gri/TIA-Automation-Factory`.

Goal: convert a vendor-neutral automation model into deterministic Siemens PLC artifacts and eventually assemble/verify the selected vertical slice through trusted TIA Portal V21 Openness.

Architecture:

```text
Automation input
 -> Domain
 -> PlcCompiler / PLC IR
 -> SiemensBackend
 -> GeneratorCli
 -> deterministic PLC/source artifacts
 -> trusted TiaV21Worker on merged main
 -> TIA Portal V21
```

Domain/compiler stay vendor-neutral. `Siemens.Engineering` stays inside `src/TiaV21Worker`. Candidate source executes on disposable Linux; Windows/TIA executes merged trusted main only. `IndustrialMDE` is out of scope.

## Completed generator foundation CORE

`GEN-VALVE-FOUNDATION-CORE-001` is merged and green. The trusted generator now has:

- versioned unambiguous canonical parsed-model identity in PlcCompiler;
- SiemensBackend-only bounded emitted-name/symbol policy;
- Motor/Time compatibility and collision regressions.

## Active generator foundation OUTPUT (#62)

Trusted task: `tasks/GEN-VALVE-FOUNDATION-OUTPUT-001.json`.

Active PR #102:

- exact head `cc370d6d19c65c483837e1a8358dc9f931c2dc87`;
- base `0a1d891bd7c26a4dbe057ca9d9a83e1c2b0e31b1`;
- exact five allowed paths only;
- CI #490 / run `36344246671`: SUCCESS;
- net10 tests 96/96 PASS;
- legacy Motor artifact generation PASS;
- no candidate Windows/TIA execution;
- awaiting fresh isolated `chatgpt-secondary` review because primary authored the implementation.

Review request: `ER-102-cc370d6d19c6-CODE_REVIEW-1-chatgpt-secondary-manual`.

Finish this gate before starting new discretionary generator work.

## Open Library qualification (#61)

The evidence-projection and bounded prerequisite-classifier work is merged.

Trusted run #320 / `36273653086` on `main@0a1d891bd7c26a4dbe057ca9d9a83e1c2b0e31b1` failed with sanitized evidence:

`phase:retrieve-with-upgrade type:EngineeringTargetInvocationException hresult:0x80131500 tags:unknown detail-count:3 msgfp:eefb8fcdb933339b dtlfp:f85ef7ebe21aad89`

The classifier did not prove support-package/software-product/unsupported-library-element or another safe prerequisite category. #61 therefore remains BLOCKED on unknown trusted vendor prerequisite. Do not speculate, change migration APIs, claim a qualified Valve profile, or trigger another qualification run without a new reviewed evidence-backed authority.

## Future trusted Valve acceptance (#63)

Planning-only. Do not start implementation until:

1. #61 provides a reviewed qualified Valve contract/profile identity;
2. #62 deterministic generator output/manifest semantics are reviewed, merged and post-merge green.

## Review/merge model

- coding-agent-only candidates -> primary `chatgpt` independent review;
- primary-authored/co-authored candidates -> fresh isolated `chatgpt-secondary`;
- approvals bind exact current SHA;
- repairs are bounded by trusted task;
- routine technical merge is delegated to primary only when all exact gates pass.

## Handoff-process state

Trusted task `CHAT-HANDOFF-001` exists, but the old protocol PR #39 remained stale/unmerged and main snapshot docs were stale. A new handoff synchronization candidate is being prepared from this exact current main without moving main, so it does not stale PR #102.

The handoff docs candidate itself is primary-authored and must receive fresh isolated `chatgpt-secondary` review before merge. It should not be merged ahead of PR #102 if that would unnecessarily stale PR #102 review/base evidence.

## Hard boundaries

- no unqualified Valve parameter/dependency/version/DB/ownership/target facts;
- no candidate source on trusted Windows/TIA;
- no vendor archive/project payload or raw vendor diagnostics in GitHub;
- no F-safety generation/validation;
- no broad UI/HMI/multi-vendor expansion before the vertical slice is stable;
- never modify `IndustrialMDE`.
