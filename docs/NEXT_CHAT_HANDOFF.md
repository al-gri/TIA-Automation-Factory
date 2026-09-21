# TIA Automation Factory — Fresh Chat Handoff

Updated: 2026-09-21

GitHub is the sole durable source of truth. Do not use old chat history as project state.

## Mandatory startup

Read in order: `AGENTS.md`, `docs/PROJECT_STATE.md`, this file, `docs/AI_COLLABORATION_MODEL.md`, `docs/DEVELOPMENT_METHODOLOGY.md`, latest relevant `docs/METHODOLOGY_JOURNAL.md`, and `docs/EXTERNAL_REVIEW_PROTOCOL.md` before review work. Then inspect live tasks, PRs/issues, exact candidate SHAs, Actions and methodology issue #25.

`IndustrialMDE` is outside scope and must not be touched.

## Roles

- coding: OpenRouter first, DeepSeek `deepseek-flash` fallback;
- primary connected ChatGPT: Senior Architect, independent reviewer for coding-agent candidates, orchestrator, methodology curator and delegated merge authority;
- `chatgpt-secondary`: required when primary materially authored/co-authored the candidate;
- trusted Windows/TIA runs merged trusted `main` only.

## Trusted checkpoint

Live `main` before this documentation branch: `f67788b20143e69074b19c7008a9157c052b1899`.

Re-fetch live `main` before every mutation/review because documentation merges may move it.

Trusted task authorities already merged on main:

- `tasks/TIA-AUTH-V21-ALLOWLIST-001.json` -> issue #80;
- `tasks/GEN-VALVE-FOUNDATION-001.json` -> issue #62;
- `tasks/OLQ-VALVE-PROFILE-001.json` remains the original issue #61 authority/history, but its latest candidate exhausted its repair budget.

## Current product sequence

Canonical priority: **#80 / #61 -> #62 -> #63**, with #62 contract-independent foundations allowed to run in parallel with #61.

### #80 — TIA V21 AllowList

Next implementation lane. Replace the legacy versioned `Whitelist` path with V21 `SOFTWARE\Siemens\Automation\Openness\AllowList\TiaV21Worker.exe\Entry`, preserve exact executable identity sync and narrow fail-closed ACLs, and make bootstrap ACL detection SID-normalized/idempotent.

Live source preflight found both `new TiaPortal(...)` paths already guarded by `SynchronizeWhitelist()`, so the trusted task's narrow allowed scope is sufficient.

Initial coding run still requires manual GitHub Actions `Autonomous Agent` workflow-dispatch because the connected GitHub toolset exposes no initial dispatch mutation.

### #61 — truthful V21 Valve profile

PR #81 is **closed unmerged** at exact head `8101d1e197cf33eaf96874acb4a9933fb5256656` after 2/2 repairs. It remains historical evidence only. Do not reopen or merge it.

Any new candidate-changing continuation requires explicit human authorization because `maxRepairAttempts=2` is exhausted.

Recommended continuation after authorization is a fresh decomposed authority, not repair 3/2 and not a blind rerun of the old monolithic task:

1. qualification transaction/reuse + deterministic recovery tests + real native V21 open or sanitized BLOCKED prerequisite;
2. only after successful native V21 open, actual `fbValve_Solenoid` contract/dependency/ownership/reference compile/save/reopen.

Preserve exactly `RetrieveWithUpgrade -> Save -> Archive -> current-version Retrieve`; do not guess Siemens migration or Valve facts.

### #62 — generator foundations

Trusted `GEN-VALVE-FOUNDATION-001` may run in parallel. Extend the current tiny `AutomationDevice -> PlcCompiler -> SiemensBackend -> GeneratorCli` path additively with deterministic canonical input identity, explicit opaque profile identity, manifest/artifact hashes, output containment, Siemens engineering-name/symbol validation and stable diagnostics/tests.

Existing two-argument Motor CLI behavior stays compatible. No unqualified Valve parameter/dependency/version/ownership/DB/target facts.

Initial coding run also requires manual `Autonomous Agent` workflow-dispatch.

### #63 — first real trusted TIA acceptance

Do not start implementation until #61 has a reviewed qualified Valve contract/profile identity and #62 has reviewed deterministic generated artifact/manifest semantics.

The eventual trusted-main truth gate binds exact commit/input/profile/artifact identities, imports only the selected dependency closure/artifacts into CPU `OrderNumber:6ES7 516-3AP03-0AB0/V4.0`, compiles with zero errors, classifies warnings, explicitly saves/closes/reopens and verifies expected blocks plus instance/data ownership in the reopened project.

## Operator-action queue

Issue #82 is the single deferred queue. Do not interrupt the operator unless they explicitly ask what is needed.

Current items after a fresh live check:

- H004: explicit human authorization for any fresh #61 candidate-changing continuation after #81 exhausted 2/2;
- H005: launch `Autonomous Agent` for `tasks/TIA-AUTH-V21-ALLOWLIST-001.json`;
- H006: launch `Autonomous Agent` for `tasks/GEN-VALVE-FOUNDATION-001.json`.

If any item has already been completed by another path, supersede it rather than presenting stale instructions.

## Evidence rules not to reinterpret

- Linux green is not TIA acceptance.
- GitHub `action_required` with zero jobs on a bot-created PR is an Actions approval/policy gate, not a test failure.
- Successful qualification requires real native V21 reopen, not a matching file/flag.
- Candidate source/scripts never execute on trusted Windows/TIA before independent review + merge.
- Vendor `.zal19/.zal21` and project payloads remain private and never enter GitHub.

## Open PR hygiene

PR #81 is closed historical evidence. PR #39 and legacy experiment PRs #4/#5/#10/#13 are old and are not active implementation authority.

## Hard boundaries

No F-safety generation, no broad UI/HMI/graph/catalog expansion, no arbitrary hardware-from-scratch MVP expansion, no generic multi-vendor plugin architecture, no candidate source on trusted Windows/TIA, no vendor payloads in Git, and never modify `IndustrialMDE`.
