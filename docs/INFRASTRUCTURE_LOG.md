# Infrastructure progress log

Short, chronological record of infrastructure work. Keep entries concise and factual.

## 2026-09-17

### DONE
- Created isolated repository `al-gri/TIA-Automation-Factory`; `IndustrialMDE` remains untouched.
- Added minimal Domain -> PLC IR -> Siemens SCL generator path.
- Added Linux GitHub Actions CI for tests and SCL generation.
- Registered Windows x64 self-hosted runner with label `tia-v21`.
- Verified TIA Portal V21 installation and V21 Openness assemblies.
- Built `TiaV21Worker` against real V21 `net48` assemblies.
- Verified Windows account membership in `Siemens TIA Openness`.
- Completed real TIA V21 E2E smoke test: generated `UDT_Motor.scl`, imported it through Openness, compiled with 0 errors / 0 warnings, emitted `tia-diagnostics.json`.
- Added first autonomous coding workflow on GitHub-hosted Linux.
- Added protected-path checks and `.gitignore` to stop build artifacts from becoming AI commits.
- Enabled GitHub Actions PR creation.
- Completed first successful autonomous candidate run and created PR #4.
- Learned that green deterministic tests do not prove all acceptance criteria were implemented; reviewer stage is required.
- Versioned coder/reviewer prompts and `tasks/INFRA-001.json`.
- Split task acceptance into `acceptance.requirements` and `acceptance.tia`.
- Implemented independent Requirements Reviewer and PLC/TIA Reviewer stages.
- Implemented safe candidate bridge: candidate code executes only on Linux; Windows checks out trusted `main` and receives only the generated PLC artifact package.
- Implemented `Candidate Validation`: requirements review -> Linux PLC package -> trusted TIA V21 -> PLC/TIA review.
- Replaced bot-PR implicit chaining with explicit trusted `repository_dispatch`.
- Gemini free-tier quota failure exposed the need for provider fallback and provider audit.
- Implemented provider-resilient coder/reviewer runtimes and pinned OpenCode `1.18.31`.
- Configured OpenRouter and proved fallback to `nvidia/nemotron-3-ultra-550b-a55b:free`.
- Corrected OpenCode invocation to supported `--pure --auto --agent build --format json --model ...` flags.
- I2 complete: autonomous coder created PR #5 with Valve input/tests; Requirements Reviewer passed.
- I3 complete: exact `UDT_Valve.scl` was packaged on Linux and compiled by trusted TIA V21 with 0 errors / 0 warnings.
- I4 complete: PLC/TIA Reviewer passed.
- I5 implemented and proven: intentionally incomplete PR #6 was rejected, repaired on the same PR at attempt 1/3, revalidated, and passed TIA V21.
- Added reusable I5 repair smoke and documented generator handoff.
- I6 run #8 exposed a late OpenRouter quota case after useful candidate changes had already been produced.
- Hardened `run-coder.sh` so late quota/rate-limit with real repository changes preserves the partial candidate for deterministic acceptance instead of discarding it.
- Added disposable `output/` to `.gitignore`.

## 2026-09-18

### DONE
- I6 clean production smoke completed in run `35315016121`; PR #10 candidate passed deterministic Linux acceptance.
- I6 Candidate Validation run `35315441338` passed Requirements Reviewer, candidate packaging, trusted Windows/TIA V21 acceptance, PLC/TIA Reviewer, and final `repair-or-finish` at attempt 0/3.
- I6 TIA diagnostics for exact `UDT_Valve.scl`: `success=true`, 0 errors, 0 warnings; `UDT_Valve (UDT)` and `Main (OB1)` both Success.
- Infrastructure baseline through I6 was declared complete/frozen except for concrete generator/Phase-2 blockers.
- Accepted risk-based external review: LOW -> one ChatGPT review; MEDIUM -> ChatGPT with Gemini escalation if justified; HIGH/architecture/PLC semantics/security -> independent ChatGPT + Gemini.
- Added self-contained external review packages that work in brand-new reviewer chats with zero prior context.
- Added strict external-review response schema and binding to request ID, reviewer slot, task ID, candidate SHA, review type, and round.
- Added explicit `WAITING_FOR_EXTERNAL_REVIEW`, `APPROVED_EXTERNAL_REVIEW`, `REVIEW_CHANGES_REQUIRED`, `BLOCKED`, and `REVIEW_CONFLICT` state handling.
- Added trusted review-package renderer/validator and CI tests.
- Added workflow path from validated `CHANGES_REQUIRED` into bounded repair on the same PR.
- Added workflow path from validated LOW/MEDIUM `APPROVE` into trusted Candidate Validation/TIA.
- Connected ChatGPT review mode proven: ChatGPT can recover the pending request from GitHub, inspect task/SHA/diff/tests/artifact evidence, submit its structured review response to the PR, and continue orchestration without the user manually opening GitHub.
- Added DeepSeek official API support through `opencode.json` as an OpenAI-compatible provider using `deepseek-flash`; repository secret `DEEPSEEK_API_KEY` was proven accessible to Actions.
- DeepSeek smoke run `35318239703` succeeded with no fallback: provider `deepseek`, model `deepseek/deepseek-flash`, 14 steps, 16,324 input tokens, 2,271 output tokens, 1,490 reasoning tokens, 187,264 cache-read tokens, reported cost `$0.005266992`.
- DeepSeek smoke created PR #13 with a deterministic duplicate-field regression test and no production behavior change; Linux tests passed.
- Created versioned task `tasks/PHASE2-001.json` to prove connected ChatGPT external review and trusted TIA continuation.
- Connected ChatGPT reviewed PR #13 entirely from GitHub and returned `APPROVE`; External Review Response run `35319434427` validated/bound the response and dispatched Candidate Validation.
- That first validation exposed a trust-boundary defect: task JSON was read from candidate checkout instead of trusted `main`.
- Fixed Candidate Validation so trusted task content is always resolved from `main`; candidate source/tests remain bound to candidate SHA.
- Adopted repository-first operation: root `AGENTS.md`, `docs/PROJECT_STATE.md`, `docs/AI_COLLABORATION_MODEL.md`, and review protocol contain all durable context required by a clean ChatGPT/Gemini/coding-agent session.
- Accepted GitHub as the sole durable source of project context. Chat history is only an operator console and is never required to continue work.
- Added user-command semantics: `проверь репозиторий`, `проверь DeepSeek`, `проверь запросы DeepSeek`, `что ждёт review?`, and `проверь PR #N` instruct connected ChatGPT to inspect GitHub and perform its authorized actions itself.
- Accepted final routine coding-provider order: **OpenRouter first -> DeepSeek `deepseek-flash` second**. Gemini is removed from routine coding fallback and reserved for independent review/red-team escalation.
- Implemented OpenRouter-first -> DeepSeek-second logic in `agents/runtime/run-coder.sh` and aligned normal/repair workflows.
- If OpenRouter reaches quota after useful workspace changes, those changes are preserved and DeepSeek continues the same bounded workspace.
- PR #15 merged the repository-first contract, OpenRouter-first provider policy, repair alignment, and trusted-task Candidate Validation fix. Infrastructure implementation commit for that slice: `77d383a8073fb04286f54d47a3fa87b2653dbf83`.
- Fresh Candidate Validation run `35322785146` proved the fixed end-to-end path: trusted task from `main`, Linux checks, Requirements Reviewer, candidate PLC package, trusted Windows/TIA V21, PLC/TIA Reviewer, final finish gate — all PASS.
- TIA V21 compiled exact `UDT_Motor.scl` in run `35322785146` with `success=true`, 0 errors, 0 warnings; `UDT_Motor (UDT)` and `Main (OB1)` both Success.
- Recorded compact proof in `docs/PHASE2_PROOF_2026-09-18.md` and updated issue #7.
- Updated `docs/PROJECT_STATE.md` to state Phase 2 orchestration is operational and generator development is the next focus.
- Added `docs/NEXT_CHAT_HANDOFF.md` as the complete durable handoff for a brand-new ChatGPT session.
- Refreshed `docs/GENERATOR_CHAT_HANDOFF.md` to remove stale Gemini-first provider information and describe the current OpenRouter -> DeepSeek workflow.
- Updated `README.md` so fresh AI sessions are directed to `AGENTS.md`, `PROJECT_STATE.md`, and `NEXT_CHAT_HANDOFF.md` before project work.

### CURRENT
- Infrastructure and repository-first AI orchestration are operational.
- No known infrastructure blocker is active.
- Phase 2 issue #7 remains open primarily to collect real-task provider/cost/throughput measurements before deciding whether more routing complexity is justified.
- Project focus should move to real PLC generator/domain/compiler and Siemens Open Library work.

### NEXT
- Analyze Siemens Open Library and the required vendor-neutral domain/PLC IR for real automation systems.
- Define the first small real generator-development task under `tasks/` with explicit requirements, risk class, and TIA acceptance criteria.
- Run normal work using OpenRouter first and DeepSeek `deepseek-flash` as automatic continuity fallback.
- Use connected ChatGPT review directly from GitHub; invoke Gemini only when risk policy requires independent escalation.
- Record provider/model/tokens/cache/cost/duration/repair count over several real tasks.
- Consider LiteLLM/additional provider pooling only if measured usage demonstrates a concrete need.

## Logging rule
After every meaningful infrastructure change, append one short factual bullet under DONE/CURRENT/NEXT. Design details belong in dedicated documents; this file remains a chronological log.
