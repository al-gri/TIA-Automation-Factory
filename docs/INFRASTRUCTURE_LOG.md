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
- PR #22 coding/repair rounds resolved F001-F008; exact head `4354bebbf2a3bf745b09589d6abac0938d5b5664` passed CI #164 and independent `chatgpt-secondary` round-4 review, then merged as `5c8c957e6abb7004e4ee9e9382de97347ebfc9c6`.
- PR #24 introduced authorship-based `chatgpt` / `chatgpt-secondary`, fail-closed reviewer authorization, deterministic Candidate Validation, fresh review after candidate-changing repair, one trusted validation dispatch source, main-only Windows/TIA manual execution, trusted coding context and methodology telemetry.
- Independent rounds F001-F010 drove workflow/governance repairs.
- PR #24 exact final head `a7062ac85c7b3c3fbcbe93380ea1c8e2f33d79ac` passed exact-SHA CI and fresh independent `chatgpt-secondary` review, then merged as `ef7e5a00e74a9d3b994c23637aab6fc2ae2546f5`.

## 2026-09-18 — Methodology and trusted-context instrumentation

### DONE
- Added `docs/DEVELOPMENT_METHODOLOGY.md`, `docs/METHODOLOGY_JOURNAL.md`, issue #25 raw telemetry and `.github/workflows/methodology-telemetry.yml`.
- Added `agents/runtime/build-coder-context.py`, `docs/CODING_AGENT_CONTEXT.md`, bounded `contextFiles`, source hashes and shared OpenRouter/DeepSeek rendered context.
- Methodology checkpoints became mandatory primary-ChatGPT orchestration duties.
- Issue #25 telemetry recorded PR #24/#22 merges and subsequent governance milestones.

## 2026-09-18 — OLQ trusted execution harness

### IN PROGRESS
- Issue #26 / PR #27 adds the smallest trusted-main Windows/TIA harness for real OLQ-001 qualification twice with sanitized evidence only.
- First candidate `9ce3963…` failed repository trust-boundary regression because the main-only self-hosted guard did not use the exact audited invariant.
- Repaired exact head `86b1b3f1b2cabca227d2976fa537ba00f572d960` passed CI #223 / run `35387244950`.
- PR #27 is primary-ChatGPT-authored and reached independent-review gate, but is now paused behind GOV-CTX-001.

## 2026-09-18 — GOV-CTX-001: prompt/control authority separation

### LIVE DEFECT CONFIRMED
- A historical secondary-review response for PR #24 old SHA `450dca6608f0526d00370595fc5a928f7bbfbd71` reported F011/F012.
- The old verdict itself was stale, but current `main` inspection confirmed the findings were still live after PR #24 merge.
- `build-coder-context.py` parsed task-looking headings from mixed `--prompt-input` text, allowing free-form issue/reviewer content to select trusted-Git `contextFiles`.
- `validate_repo_path()` silently canonicalized some malformed raw paths rather than requiring canonical spelling.
- Issue #28 was created as HIGH-risk governance repair; downstream PR #27 is paused.

### REPAIR CANDIDATE
- Branch `chatgpt/gov-ctx-001-trusted-task-channel` removes prompt parsing as a task-authority mechanism.
- Production `run-coder.sh` resolves task path from structured GitHub Actions event metadata (`workflow_dispatch` task input or trusted `repository_dispatch` repair payload), with optional explicit trusted caller override.
- `build-coder-context.py` loads only canonical `tasks/*.json` from the trusted Git ref for task identity/`contextFiles`.
- Issue mode cannot declare task context even when issue text contains fake headings/JSON/contextFiles.
- Context paths must already be canonical repository-relative POSIX spellings.
- New regressions cover issue/repair prompt spoofing, trusted-task precedence and malformed canonical paths.
- Methodology rule M-013 records control-plane/data-plane separation.

### NEXT GATE
- Open repair PR from issue #28 branch.
- Require exact-live-head green CI.
- Because primary ChatGPT authored the repair, obtain fresh independent `chatgpt-secondary` review for the exact SHA.
- Merge under delegated technical authority only if all exact-SHA gates pass.
- Re-check PR #27 on the repaired `main`, refresh independent review if needed, then continue real OLQ-001 Windows/TIA qualification.

## Logging rule

After every meaningful infrastructure/methodology change, keep this file concise and factual. Raw high-frequency events belong in methodology issue #25; reusable lessons belong in `docs/DEVELOPMENT_METHODOLOGY.md`; milestone interpretation belongs in `docs/METHODOLOGY_JOURNAL.md`.
