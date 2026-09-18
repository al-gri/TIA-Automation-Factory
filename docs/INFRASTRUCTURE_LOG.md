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
- Issue #31 / PR #32 define the permanent bootstrap lane for genuine authorization recursion in the repository's normative authority/review-control model.
- Round 1 exact candidate `2d69e0bba51bb5de2672aa7f453bbe812575f0e6` -> `CHANGES_REQUIRED` with major F001-F004.
- Round 2 exact candidate `6bd2860c49713f49cc7a30ac6ec2eceb1db4a1d4` -> `CHANGES_REQUIRED` with major F005.
- F001-F004 forced semantic eligibility, complete fingerprinted issue scope, no candidate self-authorization and provenance separation for human/reviewer authority.
- F005 established that owner authorship plus `performed_via_github_app == null` is only negative attribution and does not positively prove human origin against non-App API credentials.
- The earlier direct owner authorization comment `5736439687` is historical evidence only under the repaired model.
- Issue #31 was rewritten for the second/final candidate-changing repair. Current exact UTF-8 body SHA-256: `c037d2a568644813cbeaa0c626761c845c2ca046c8a6d7ba557573491ae49099`.
- Candidate policy now requires positive cryptographic human provenance: SSH-signed Git scope/review attestation commits under a human-controlled key unavailable to project automation.
- Raw GitHub verification must report `verified=true`, `reason=valid`, SSH signature type and repository-owner author/committer identity; web-flow signatures/comments/app metadata are insufficient by themselves.
- Normal external-review automation remains task-only and fail-closed; no missing-task fallback was added.
- The default two candidate-changing bootstrap repairs are now consumed.
- M-014 was strengthened: neither the candidate nor an author-controlled connector/credential path may manufacture authority.

### NEXT GATE
- Freeze repaired PR #32 exact head and require exact-SHA CI PASS.
- Human creates SSH-signed scope-attestation commit for issue-body hash `c037d2a568644813cbeaa0c626761c845c2ca046c8a6d7ba557573491ae49099` on the dedicated non-merged human-attestation branch.
- Primary verifies exact commit SHA, raw GitHub SSH-signature metadata and payload.
- Obtain fresh isolated `chatgpt-secondary` round-3 review against the frozen candidate and signed scope authority.
- If APPROVE, human creates a separate SSH-signed review-attestation commit containing the exact JSON and its hash.
- Primary verifies schema/identity/exact SHA/signature and delegated-merges only if every gate remains green.
- Any further candidate-changing repair requires fresh explicit human authorization; otherwise `BLOCKED`.
- Then re-check PR #27 against the new `main` and resume OLQ qualification work.

## 2026-09-18 — AUTO-001 accepted operating-model direction

### PLANNED
- Issue #35 records the accepted hybrid autonomy model.
- OpenRouter/DeepSeek should run routine tasks autonomously to explicit checkpoints.
- `WAITING_FOR_REVIEW` means primary connected ChatGPT performs full semantic/code/architecture review when independent.
- Primary-authored candidates route to `chatgpt-secondary`.
- AUTO-001 implementation is downstream of GOV-BOOT-001 and must not weaken exact-SHA review, bounded repair or trusted Windows/TIA boundaries.

## Logging rule

After every meaningful infrastructure/methodology change, keep this file concise and factual. Raw high-frequency events belong in methodology issue #25; reusable lessons belong in `docs/DEVELOPMENT_METHODOLOGY.md`; milestone interpretation belongs in `docs/METHODOLOGY_JOURNAL.md`.
