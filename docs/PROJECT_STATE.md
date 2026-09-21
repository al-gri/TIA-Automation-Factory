# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-21

GitHub is the only durable source of truth. Read live GitHub state before acting; this snapshot is a recovery aid and coding-context baseline, not a substitute for current tasks, PRs, reviews and Actions evidence.

## Project

Repository: `al-gri/TIA-Automation-Factory`.

Purpose: build a production engineering system that converts a vendor-neutral automation model into modular Siemens PLC code/projects and verifies/assembles them through TIA Portal V21 Openness. A second explicit output is the reusable AI-assisted development methodology.

Baseline flow:

```text
Engineering UI
 -> canonical AutomationProject
 -> Domain
 -> PLC Compiler / PLC IR
 -> Siemens Backend / qualified Open Library bindings
 -> generated PLC artifact/package
 -> trusted TIA V21 Worker / ProjectAssembler
 -> TIA Portal V21
 -> compile/save/reopen/diagnostics
```

`IndustrialMDE` is outside scope and must not be modified. Domain/compiler remain vendor-neutral; `Siemens.Engineering` is isolated in `src/TiaV21Worker`.

## Operating model

- Coding provider order: OpenRouter first, DeepSeek `deepseek-flash` fallback.
- Primary connected ChatGPT: Senior Architect, normal independent reviewer for coding-agent work, orchestrator, methodology curator and delegated technical merge authority.
- `chatgpt-secondary`: fresh isolated reviewer only when primary materially authored/co-authored the candidate or another independent opinion is explicitly required.
- Candidate source executes on disposable Linux only; trusted Windows/TIA executes merged trusted `main` only.
- Reviews bind the exact candidate SHA. Candidate-changing repair invalidates prior approval.
- Normal implementation authority is a versioned trusted task on `main`; repair budgets are authoritative and cannot be extended without explicit human decision.

## Current trusted main checkpoint

Live checkpoint before this documentation branch: `main@f67788b20143e69074b19c7008a9157c052b1899`.

That main contains the independently reviewed/merged task authorities:

- `tasks/TIA-AUTH-V21-ALLOWLIST-001.json` for issue #80;
- `tasks/GEN-VALVE-FOUNDATION-001.json` for issue #62;
- `tasks/OLQ-VALVE-PROFILE-001.json` remains the original #61 authority/history, but its latest candidate exhausted the trusted repair budget.

Always re-fetch live `main` before mutation because this snapshot PR will advance the branch after merge.

## Product lane A — TIA V21 Openness AllowList (#80)

Trusted task: `tasks/TIA-AUTH-V21-ALLOWLIST-001.json`, HIGH risk.

Goal: replace the legacy versioned `Openness\Whitelist\<version>\Entries\...` path with the V21 version-independent Registry64 `SOFTWARE\Siemens\Automation\Openness\AllowList\TiaV21Worker.exe\Entry`, preserve exact Path/DateModified/FileHash synchronization and narrow fail-closed ACL behavior, and make bootstrap ACL detection genuinely SID-normalized/idempotent.

Preflight against current worker source found two `new TiaPortal(...)` paths and two corresponding `WhitelistManager.SynchronizeWhitelist()` guards, with no separate attach path. The task's allowed implementation scope is therefore sufficient without `Program.cs` changes.

Next implementation action requires an `Autonomous Agent` workflow-dispatch run for the trusted task. The connected GitHub toolset currently has no initial workflow-dispatch mutation.

## Product lane B — truthful V21 Valve qualification (#61)

Issue #61 remains active and HIGH risk.

Historical trusted baseline source archive SHA256:
`ed8fe3f52e90399475b321e40f7ce842d86e414324f06f1b078f44fcb666eed3`.

TIA/Openness identity:
`Siemens.Engineering v21.0.0.0 (file: 2100.0.121.1)`.

Selected CPU:
`OrderNumber:6ES7 516-3AP03-0AB0/V4.0`.

Selected library object:
`fbValve_Solenoid`.

Latest candidate PR #81 was closed unmerged at exact head `8101d1e197cf33eaf96874acb4a9933fb5256656` after exhausting trusted `maxRepairAttempts=2`. Exact-SHA review remained `CHANGES_REQUIRED` with unresolved MAJOR defects, including worker compile drift, missing mandatory tests and incorrect qualification/reference lifecycle. PR #81 is historical evidence only and must not be reopened or merged.

Any new candidate-changing continuation of #61 requires explicit human authorization because the trusted repair budget is exhausted. The recommended route is not a blind rerun of the monolithic task. Planning evidence in issue #61 splits replacement work into:

1. a small qualification transaction/native-V21-open gate with deterministic recovery/reuse tests and exact `RetrieveWithUpgrade -> Save -> Archive -> current-version Retrieve` preservation;
2. only after real native V21 open, a separate Valve contract/dependency/ownership/reference compile/save/reopen gate.

Do not invent migration APIs, Valve contract facts, dependency names, versions, memory addresses, DB/ownership requirements or success provenance.

## Product lane C — generator foundation (#62)

Trusted task: `tasks/GEN-VALVE-FOUNDATION-001.json`, MEDIUM risk.

This lane is intentionally independent of unknown Valve interface facts and may proceed in parallel with #61. Current product code is deliberately small:

`AutomationDevice -> AutomationCompiler -> PlcIrDataType -> SclDataTypeGenerator -> GeneratorCli`.

The foundation task extends that path additively with deterministic canonical input identity, explicit opaque profile identity, multi-artifact manifest/hash plumbing, output-root containment, centralized Siemens engineering-name/symbol validation and stable diagnostics/tests. Existing two-argument Motor CLI behavior must remain compatible. Domain/IR must not absorb filesystem/hash/manifest/Siemens-profile mechanics.

No `fbValve_Solenoid` parameter, dependency, version, DB/ownership or target prerequisite fact may be guessed before #61 proves it.

Next implementation action requires an `Autonomous Agent` workflow-dispatch run for this trusted task.

## Product lane D — trusted TIA acceptance (#63)

Issue #63 `TIA-VALVE-ACCEPT-001` is planning-only until #61 provides a reviewed qualified Valve profile/contract identity and #62 provides reviewed deterministic generated artifacts/manifest semantics.

The eventual trusted-main truth gate will bind exact generator commit/input/profile/artifact hashes, import only the selected dependency closure/artifacts into the exact CPU, compile with zero errors, classify warnings, explicitly save, close and natively reopen, then verify expected blocks and instance/data ownership in the reopened project. It proves only this selected compile/save/reopen slice, not runtime correctness, commissioning readiness or safety certification.

## Operator-action ledger

Issue #82 is the single deferred human-action queue. Do not interrupt the operator for routine work; surface current items only when explicitly asked.

Current live items:

- H004: explicit human decision authorizing any fresh #61 candidate-changing continuation after PR #81 exhausted 2/2 repairs; recommended authorization is for primary to prepare the decomposed replacement task authority, not repair 3/2.
- H005: launch `Autonomous Agent` with `source=task`, `task_path=tasks/TIA-AUTH-V21-ALLOWLIST-001.json`, empty `issue_number`.
- H006: launch `Autonomous Agent` with `source=task`, `task_path=tasks/GEN-VALVE-FOUNDATION-001.json`, empty `issue_number`.

Revalidate live GitHub before presenting or acting on any ledger item.

## Review/CI caveat

A PR CI run created by `github-actions[bot]` may show GitHub-level `action_required` with zero jobs before workflow execution. That is an Actions approval/policy gate, not deterministic test failure. Keep it distinct from Autonomous Agent Linux acceptance and from real failed CI jobs.

## Current order of work

1. Execute #80 AllowList implementation and independently review/merge it; prove repeated non-interactive trusted-main Openness admission on Windows/TIA.
2. Continue #61 only after explicit human authorization following the exhausted repair budget; prefer the two-stage replacement decomposition described above.
3. Execute #62 generator foundations in parallel; independently review exact-SHA candidate and keep all unqualified Valve facts out.
4. Complete the real Valve generator binding only after #61 provides the qualified contract.
5. Execute #63 trusted TIA import/compile/save/reopen acceptance for the exact selected profile/artifact set.
6. Perform the next full audit/methodology checkpoint at the #63 milestone.

## Hard boundaries

- no Siemens F-safety generation/validation;
- no arbitrary hardware-from-scratch generation for MVP;
- no generic multi-vendor plugin architecture now;
- no broad UI/HMI/graph/catalog expansion before the Valve vertical slice is stable;
- no candidate execution on trusted Windows/TIA before independent approval + merge;
- self-hosted TIA workflows are trusted-main only;
- no vendor archive/project payloads in Git;
- never modify `IndustrialMDE`.
