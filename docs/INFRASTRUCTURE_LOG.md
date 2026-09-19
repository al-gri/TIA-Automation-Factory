# Infrastructure Progress Log

Short chronological record of infrastructure work. Keep entries factual and concise. Current policy is defined by `AGENTS.md`, `docs/PROJECT_STATE.md`, and the living methodology documents. Raw high-frequency evidence belongs in methodology issue #25.

## 2026-09-17 — Initial autonomous/TIA infrastructure

### DONE
- Created isolated repository `al-gri/TIA-Automation-Factory`; `IndustrialMDE` remained untouched.
- Added minimal Domain -> PLC IR -> Siemens SCL generator path and Linux CI.
- Registered Windows x64 self-hosted runner with label `tia-v21` and verified TIA Portal V21 / Openness / net48 prerequisites.
- Built `TiaV21Worker` against real V21 assemblies and completed real TIA E2E smoke with 0 errors / 0 warnings.
- Added disposable-Linux autonomous coding, protected-path checks, versioned prompts/tasks and trusted Windows/TIA candidate bridge.
- I5 proved bounded same-PR repair; late OpenRouter quota exposed need for provider continuity/audit.

## 2026-09-18 — Repository-first orchestration and provider continuity

### DONE
- Added self-contained exact-SHA external-review packages and schema-bound responses.
- Added explicit waiting/approve/changes-required/blocked/conflict states and bounded repair.
- Proved connected ChatGPT can recover state from GitHub without prior chat context.
- Added official DeepSeek `deepseek-flash` fallback and provider telemetry.
- Adopted GitHub as sole durable source of truth through `AGENTS.md`, project-state/handoff/collaboration documents.
- Adopted routine coding cascade OpenRouter -> DeepSeek while keeping deterministic/review/TIA acceptance provider-independent.
- PR #17 merged ARCH-001; PR #16 merged PLC-001 after exact TIA V21 PASS.

### SUPERSEDED HISTORICAL POLICY
- Early experiments used Gemini in standing/mandatory review paths and a hidden LLM stage in Candidate Validation. Those are historical only and are not current policy.
- The old I5 live-smoke validation bypass was later retired.

## 2026-09-18 — OLQ-001 implementation and governance hardening

### DONE
- Created HIGH-risk OLQ-001 for one-time Siemens Open Library V19 `.zal19` -> native V21 qualification.
- PR #21 merged task-gated bounded `src/TiaV21Worker/**` candidate support.
- PR #23 merged bounded HIGH repair support.
- PR #22 exact head `4354bebbf2a3bf745b09589d6abac0938d5b5664` passed CI #164 and independent `chatgpt-secondary` round-4 review, then merged as `5c8c957e6abb7004e4ee9e9382de97347ebfc9c6`.
- PR #24 introduced authorship-based reviewer independence, fail-closed reviewer authorization, deterministic Candidate Validation, fresh review after candidate-changing repair, main-only Windows/TIA manual execution, trusted coding context and methodology telemetry.
- PR #24 exact final head `a7062ac85c7b3c3fbcbe93380ea1c8e2f33d79ac` passed exact-SHA CI and fresh independent `chatgpt-secondary` review, then merged as `ef7e5a00e74a9d3b994c23637aab6fc2ae2546f5`.

## 2026-09-18 — Methodology and trusted-context instrumentation

### DONE
- Added `docs/DEVELOPMENT_METHODOLOGY.md`, `docs/METHODOLOGY_JOURNAL.md`, issue #25 raw telemetry and `.github/workflows/methodology-telemetry.yml`.
- Added `agents/runtime/build-coder-context.py`, `docs/CODING_AGENT_CONTEXT.md`, bounded `contextFiles`, source hashes and shared OpenRouter/DeepSeek rendered context.
- Methodology checkpoints became mandatory primary-ChatGPT orchestration duties.

## 2026-09-18/19 — GOV-CTX-001 and GOV-BOOT-001

### DONE
- GOV-CTX-001 PR #30 repaired prompt/control authority separation and merged as `0260117391abf5f0a8375699dca12caa06bafb8b` after CI #224 and fresh independent review.
- Issue #31 / PR #32 defined the permanent explicit governance-bootstrap lane after multiple independent review rounds F001-F007.
- Positive human provenance uses SSH-signed scope/review attestation commits; negative app attribution is insufficient.
- Permanent attestation schemas are invocation-generic and fail-closed gates are stage-specific.
- PR #32 merged as `60bd20841360998406db59cfb13a61cb33982566`; issue #31 closed.
- Normal task-backed automation remains task-only and fail-closed.

## 2026-09-19 — OLQ trusted qualification execution

### IN PROGRESS
- OLQ-INFRA-002 PR #27 refreshed against post-bootstrap `main`, received fresh exact-SHA secondary approval and merged as `24f3a8fad542132a7aa9369be4451dd6ca0ae23f`.
- First trusted qualification attempt exposed Windows PowerShell execution-policy blocking before worker execution; operator set CurrentUser policy to `RemoteSigned` while MachinePolicy/UserPolicy remained undefined.
- Next trusted run produced sanitized `BLOCKED` because `TIA_OPEN_LIBRARY_V19_ARCHIVE` was not visible to the runner.
- Operator configured runner-local `TIA_OPEN_LIBRARY_V19_ARCHIVE` and `TIA_OPEN_LIBRARY_QUALIFIED_ROOT` and relaunched the user-mode runner.
- Trusted run `35426328232` then reached the real first `qualify-library` invocation and remained there until the outer 45-minute job timeout cancelled it.
- No public evidence JSON was finalized; post-job cleanup terminated remaining Siemens Portal/CrashDetector/FileStorage processes.
- This is not evidence of a vendor-library defect or qualification success.
- Issue #36 / PR #37 (`OLQ-INFRA-003`) adds bounded per-invocation timeout observability while preserving trusted-main/vendor boundaries.
- Current PR #37 head after base resynchronization: `0c9633fe657b8949beea76f87f3a63411cd103e0`; exact-SHA CI #264 / run `35429912399` PASS; fresh secondary review required.
- Historical PR #37 review-ready SHA `cff83ee7284fa1ea2c4d75621ddfe720914a5688` is stale and must not be reused.

## 2026-09-19 — CHAT-HANDOFF-001 repository-first chat transfer

### IN PROGRESS
- Trusted task `tasks/CHAT-HANDOFF-001.json` added to `main`; tracking issue #38 created.
- Candidate branch `chatgpt/chat-handoff-protocol` adds `docs/CHAT_HANDOFF_PROTOCOL.md` and refreshes stale operational snapshots.
- Canonical human triggers include `переходим в другой чат`, `переходим в новый чат`, `готовь handoff`, and equivalent unambiguous transfer requests.
- Handoff is defined as a transaction: freeze discretionary work -> live GitHub freshness audit -> reconcile chat/GitHub -> persist factual checkpoint -> verify stale evidence -> emit compact fresh-chat bootstrap prompt.
- The audit found `PROJECT_STATE.md`/`NEXT_CHAT_HANDOFF.md` materially stale: they still described GOV-BOOT/PR #27 as active although both had advanced.
- During setup, primary ChatGPT twice issued temporary placeholder `create_file` operations against `main` instead of a pre-created candidate branch. Both placeholders were immediately reverted and no placeholder remains in the tree, but `main` history moved and invalidated the previously prepared PR #37 review package.
- PR #37 was explicitly rebuilt against current `main` while preserving its one-workflow diff; fresh CI #264 passed.

### LESSON / GUARD
- Before any authority-bearing repository write, explicitly verify the target ref/branch; do not rely on omitted/default branch behavior.
- After every write that can move `main`, a candidate head, or reviewed base, re-fetch affected PR/ref and reassess exact-SHA/base review freshness.
- Chat handoff must detect and expose stale evidence rather than carrying it into the fresh chat.

## 2026-09-18 — AUTO-001 accepted operating-model direction

### PLANNED
- Issue #35 records the accepted hybrid autonomy model.
- OpenRouter/DeepSeek should run routine tasks autonomously to explicit checkpoints.
- `WAITING_FOR_REVIEW` means primary connected ChatGPT performs full semantic/code/architecture review when independent.
- Primary-authored candidates route to `chatgpt-secondary`.
- AUTO-001 must preserve exact-SHA review, bounded repair and trusted Windows/TIA boundaries.

## Logging rule

After every meaningful infrastructure/methodology change, keep this file concise and factual. Raw high-frequency events belong in methodology issue #25; reusable lessons belong in `docs/DEVELOPMENT_METHODOLOGY.md`; milestone interpretation belongs in `docs/METHODOLOGY_JOURNAL.md`.
