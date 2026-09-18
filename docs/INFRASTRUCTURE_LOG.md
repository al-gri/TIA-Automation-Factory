# Infrastructure Progress Log

Short chronological record of infrastructure work. Keep entries factual and concise. Historical experiments may remain here, but current policy is defined by `AGENTS.md`, `docs/PROJECT_STATE.md`, and the living methodology documents.

## 2026-09-17 — Initial autonomous/TIA infrastructure

### DONE
- Created isolated repository `al-gri/TIA-Automation-Factory`; `IndustrialMDE` remains untouched.
- Added minimal Domain -> PLC IR -> Siemens SCL generator path.
- Added Linux GitHub Actions CI for tests and SCL generation.
- Registered Windows x64 self-hosted runner with label `tia-v21`.
- Verified TIA Portal V21 installation, Openness assemblies, `.NET Framework 4.8` build prerequisites and Siemens TIA Openness group membership.
- Built `TiaV21Worker` against real V21 `net48` assemblies.
- Completed real TIA V21 E2E smoke: generated/imported/compiled `UDT_Motor.scl` with 0 errors / 0 warnings and emitted machine-readable diagnostics.
- Added first autonomous coding workflow on disposable Linux.
- Added protected-path checks and Git hygiene to prevent build outputs/infrastructure changes from becoming AI candidate commits.
- Added versioned coder/reviewer prompts and task acceptance split.
- Implemented candidate bridge: candidate code executes only on Linux; trusted Windows/TIA uses trusted repository code plus bounded generated artifacts.
- Implemented initial Candidate Validation and explicit trusted `repository_dispatch` chaining.
- Provider failures exposed the need for fallback, provider audit and pinned coding runtime.
- Proved OpenRouter/OpenCode coding provider and corrected invocation to supported `--pure --auto --agent build --format json --model ...` flags.
- I2/I3/I4 proved autonomous candidate generation, deterministic Linux acceptance, trusted TIA compile and reviewer stages.
- I5 intentionally produced an incomplete candidate, rejected it, repaired on the same PR within a bounded budget and revalidated successfully.
- Late OpenRouter quota after useful edits proved that partial workspace changes may be worth preserving for a fallback provider.

## 2026-09-18 — Repository-first orchestration and provider continuity

### DONE
- I6 production smoke and trusted Candidate Validation completed successfully with exact TIA diagnostics 0 errors / 0 warnings.
- Added self-contained external-review packages for brand-new reviewer chats with no prior context.
- Added strict external-review response schema bound to request ID, reviewer slot, task ID, candidate SHA, review type and round.
- Added explicit review states including waiting, approve, changes required, blocked and conflict.
- Added bounded repair path from external review findings.
- Proved connected ChatGPT can recover pending review state from GitHub and continue orchestration without manual GitHub navigation.
- Added official DeepSeek API `deepseek-flash` provider; smoke run `35318239703` proved direct DeepSeek execution and provider telemetry.
- Fixed Candidate Validation so trusted task content is always resolved from `main`, never candidate checkout.
- Adopted repository-first operation through `AGENTS.md`, `PROJECT_STATE.md`, `AI_COLLABORATION_MODEL.md` and `NEXT_CHAT_HANDOFF.md`.
- Adopted GitHub as sole durable project context; chat history became disposable coordination only.
- Adopted final routine coding cascade: **OpenRouter -> DeepSeek `deepseek-flash`**.
- Implemented provider continuity so late OpenRouter quota/rate exhaustion can hand surviving bounded workspace changes to DeepSeek.
- PR #15 merged repository-first/provider/task-trust hardening; later Candidate Validation `35322785146` proved the path end-to-end with trusted TIA V21 PASS.
- PR #17 merged accepted generator architecture (`ARCH-001`).
- PR #16 merged PLC-001 `TIME` support after exact TIA V21 compile PASS.

### SUPERSEDED HISTORICAL POLICY
- Earlier experiments used Gemini in routine/mandatory review paths and hidden Candidate Validation reviewer stages. Those decisions are retained here only as history and are **not current policy**.
- Earlier I5 live smoke workflow directly entered Candidate Validation. That path was useful experimentally but later became incompatible with the exact-SHA review state machine and is retired in PR #24.

## 2026-09-18 — OLQ-001 infrastructure and governance hardening

### DONE
- Created HIGH-risk OLQ-001 task for one-time Siemens Open Library V19 `.zal19` -> native V21 qualification.
- PR #21 merged task-gated bounded `src/TiaV21Worker/**` candidate support while keeping orchestration protected and Windows/TIA trusted-main-only.
- PR #22 coding agent produced the first `qualify-library` implementation.
- Primary ChatGPT review found F001-F005: Siemens Archive API signature, missing `FileVersionInfo` dependency, absolute-path validation, manifest parser/reuse bug, and library lifecycle ordering.
- PR #23 merged bounded HIGH repair support.
- Autonomous repair rounds fixed F001-F007; trusted repair budget `2/2` was then exhausted.
- Primary ChatGPT made one bounded maintainer repair for F008 (`return` inside `finally` / C# CS0157), making primary ChatGPT a material co-author of PR #22.
- PR #22 current code head remains `4354bebbf2a3bf745b09589d6abac0938d5b5664`; CI #164 / `35366781733` PASS.
- Human operator replaced Gemini standing review with a fresh isolated second ChatGPT because Gemini GitHub access was unreliable.
- Governance PR #24 introduced authorship-based reviewer selection (`chatgpt` vs `chatgpt-secondary`).
- Independent secondary review rounds found and drove repairs for reviewer-slot hard-coding, accidental multi-review dispatch, missing authorship enforcement, terminal-state ordering, fail-open slot authorization, hidden Gemini reviewer runtime, stale approval after repair, legacy Candidate Validation bypass and trusted Windows manual-ref exposure.
- Candidate Validation was simplified to deterministic checks/TIA evidence after explicit external review; hidden LLM reviewer runtime was removed.
- Every candidate-changing repair now returns to a fresh exact-SHA external review before Candidate Validation can resume.
- Repository-wide tests now require Candidate Validation to have only one trusted dispatch source: post-APPROVE `external-review-response`.
- Legacy I5 Repair Smoke workflow was retired; `INFRA-001` migrated into current reviewed-task metadata.
- Self-hosted manual `tia-v21.yml` and `tia-v21-e2e.yml` were hardened to fail closed to `refs/heads/main` and explicitly check out `main`; repository-wide regression covers manual self-hosted workflow trust.

## 2026-09-18 — Trusted coding context and methodology-as-a-product

### DONE
- Added `agents/runtime/build-coder-context.py` and `docs/CODING_AGENT_CONTEXT.md` in PR #24.
- Coding providers now receive a bounded trusted context assembled from trusted Git before provider selection.
- Baseline context includes repository operating contract, project state, engineering rules and collaboration model plus the bounded work prompt.
- Trusted tasks may declare bounded `contextFiles` for qualified design/vendor contracts such as Siemens Open Library profiles.
- Context paths, UTF-8/type/size/count are validated fail-closed; candidate workspace cannot redefine trusted context.
- OpenRouter and DeepSeek share the same enriched prompt; provider audit records context manifest and hashes.
- Added dedicated context-bundle CI tests.
- User explicitly defined development methodology as a second project output alongside the PLC generator.
- Added `docs/DEVELOPMENT_METHODOLOGY.md` with accepted rules, experiments, metrics and methodology checkpoint duties.
- Added `docs/METHODOLOGY_JOURNAL.md` with curated lessons from the first two development days.
- Created GitHub issue #25 as append-only raw methodology telemetry diary.
- Added `.github/workflows/methodology-telemetry.yml` to automatically log selected workflow completions and PR lifecycle events to issue #25 without auto-editing policy documents.
- Added CI tests enforcing methodology telemetry permissions, coverage and separation of raw evidence from curated policy.
- Updated `AGENTS.md` so primary ChatGPT must perform methodology checkpoints automatically at logical milestones without user reminders.

### CURRENT
- PR #24 is the active governance/infrastructure candidate and has changed after all previously supplied secondary-review payloads.
- Before merge, inspect the **live exact PR #24 head SHA and live exact-SHA CI**, then obtain a fresh `chatgpt-secondary` review for that exact head.
- PR #22 waits behind PR #24 because its implementation is primary-ChatGPT-co-authored and therefore requires trusted `chatgpt-secondary` authorization/review.
- Raw methodology telemetry automation becomes active after PR #24 is merged; issue #25 already exists as the stable sink.

### NEXT
- Finish PR #24 with fresh exact-SHA independent secondary review and delegated merge.
- Re-review and merge PR #22 if exact-SHA gates pass.
- From trusted `main`, build and run OLQ-001 against the operator-controlled V19 `.zal19`; repeat identical qualification identity to prove deterministic reuse.
- Persist hashes/manifests/diagnostics, never vendor archive payloads.
- Require human acceptance before qualified Open Library profile becomes a normal generator dependency.
- Continue `OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001`.
- Use issue #25 + methodology journal to accumulate provider/review/repair/TIA evidence and refine `docs/DEVELOPMENT_METHODOLOGY.md` at each logical milestone.

## Logging rule

After every meaningful infrastructure/methodology change, keep this file concise and factual. Raw high-frequency events belong in methodology issue #25; reusable lessons belong in `docs/DEVELOPMENT_METHODOLOGY.md`; milestone interpretation belongs in `docs/METHODOLOGY_JOURNAL.md`.
