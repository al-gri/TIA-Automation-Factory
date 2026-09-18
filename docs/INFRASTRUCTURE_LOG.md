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

## 2026-09-18 — OLQ trusted execution harness

### PAUSED
- Issue #26 / PR #27 adds the trusted-main Windows/TIA harness for real OLQ-001 qualification twice with sanitized evidence only.
- Repaired exact head `86b1b3f1b2cabca227d2976fa537ba00f572d960` passed CI #223 / run `35387244950`.
- PR #27 is primary-ChatGPT-authored and must be re-checked against the post-governance `main` before any merge gate.

## 2026-09-18 — GOV-CTX-001: prompt/control authority separation

### DONE
- Historical F011/F012 findings were re-tested against then-current `main` and confirmed live despite the old verdict being stale.
- Issue #28 / PR #30 removed task-authority discovery from mixed prompts and made repository path declarations fail closed on non-canonical spelling.
- Exact candidate `e3d2912d74cf83256aafe1bd597f49f360411d34` passed CI #224 and fresh independent `chatgpt-secondary` review.
- Governance then exposed a bootstrap ambiguity: no trusted `tasks/GOV-CTX-001.json` existed to authorize the secondary slot, while adding one in the same candidate would be self-authorization.
- Human operator granted a one-time exact-SHA waiver for PR #30.
- PR #30 merged as `0260117391abf5f0a8375699dca12caa06bafb8b`; issue #28 closed.
- M-013 records the control-plane/data-plane separation rule.

## 2026-09-18 — GOV-BOOT-001: explicit governance bootstrap lane

### IN PROGRESS
- Issue #31 created to remove the protected-governance authorization recursion exposed by PR #30.
- Human operator explicitly authorized the bounded #31 scope after PR #30 merge.
- Authorized issue-body SHA-256: `6ef39216ad77793837a1184323a6b70d0e19edaf47300d0966fc962122b7c9ee`.
- Branch `chatgpt/gov-boot-001-bootstrap-lane` adds `docs/GOVERNANCE_BOOTSTRAP.md` and binds the lane into AGENTS/review/collaboration/methodology/state/handoff docs.
- Normal external-review automation remains task-only and fail-closed; bootstrap evidence is manual and cannot be inferred from a missing task.
- A bootstrap candidate cannot authorize itself; material scope changes invalidate human authorization.
- M-014 records the reusable bootstrap rule.

### NEXT GATE
- Run exact-SHA CI for the bootstrap-policy PR.
- Because primary ChatGPT authored the candidate, obtain fresh isolated `chatgpt-secondary` review against the authorized #31 body fingerprint and exact candidate SHA.
- Merge only if CI and review pass and the fingerprint/scope remain unchanged.
- Then re-check PR #27 against the new `main` and resume OLQ qualification work.

## Logging rule

After every meaningful infrastructure/methodology change, keep this file concise and factual. Raw high-frequency events belong in methodology issue #25; reusable lessons belong in `docs/DEVELOPMENT_METHODOLOGY.md`; milestone interpretation belongs in `docs/METHODOLOGY_JOURNAL.md`.
