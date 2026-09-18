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
- Replaced stale OpenHands CLI approach with pinned Gemini CLI `0.60.0`.
- Added protected-path checks and `.gitignore` to stop build artifacts from becoming AI commits.
- Enabled GitHub Actions PR creation.
- Completed first successful autonomous candidate run and created PR #4.
- Learned that green deterministic tests do not prove all acceptance criteria were implemented; reviewer stage is required.
- Model audit: workflow requested `gemini-3.8-flash`; successful run init reported `gemini-3.8-flash`, while final usage statistics attributed tokens to `gemini-3.5-flash`.
- Added trusted `.gemini/settings.json` that pins `gemini-3.8-flash`, disables built-in subagents, and disables hidden Plan Mode model routing; each run still records actual usage.
- I1 complete: coder prompt is versioned at `agents/prompts/coder.md`, reviewer prompts are versioned under `agents/prompts/`, and smoke task `tasks/INFRA-001.json` is stored in Git.
- Updated `Autonomous Agent` workflow to run from a Git task path or GitHub Issue and to record requested/initialized/actual model information.
- Split task acceptance into `acceptance.requirements` and `acceptance.tia` so reviewers have non-circular responsibilities.
- Implemented Requirements Reviewer and PLC/TIA Reviewer prompts as independent read-only sessions.
- Implemented safe candidate bridge: candidate code executes only on Linux; Windows checks out trusted `main` and receives only the generated PLC artifact package.
- Implemented `Candidate Validation` orchestration: requirements review -> Linux PLC package -> trusted TIA V21 -> PLC/TIA review.
- Replaced bot-PR implicit chaining with explicit trusted `repository_dispatch` from `Autonomous Agent` to `Candidate Validation`, avoiding manual approval as part of the intended path.
- CI remained green after the Git-task agent changes.
- Started full `INFRA-001` smoke run. Git task resolution, prompt loading, branch creation and Gemini session initialization all worked.
- `INFRA-001` was blocked before source edits by Gemini free-tier daily quota: API returned HTTP 429 / daily quota exhausted, limit 20 requests for actual model `gemini-3.5-flash`; workflow correctly failed and model audit recorded requested `gemini-3.8-flash`, initialized `gemini-3.8-flash`, actual usage `gemini-3.5-flash`.
- Implemented provider-resilient coder runtime at `agents/runtime/run-coder.sh`: Gemini is attempted first; failed provider/quota attempts are discarded before OpenRouter/OpenCode fallback runs in the disposable Linux workspace.
- Implemented provider-resilient read-only reviewer runtime at `agents/runtime/run-reviewer.py`: OpenRouter direct API is preferred when configured, with Gemini fallback; reviewers receive no repository editing tools through the OpenRouter path.
- Pinned OpenCode fallback runtime to `opencode-ai@1.18.31` and free fallback model `nvidia/nemotron-3-ultra-550b-a55b:free`.
- Updated coder and both reviewer workflows to record selected provider/model and fallback audit information.
- Added CI syntax validation for trusted agent runtime scripts and upgraded CI checkout/setup-dotnet actions to current majors.
- Configured `OPENROUTER_API_KEY` in GitHub Actions and proved runtime fallback detection: Gemini quota failure -> clean Git workspace -> OpenRouter/OpenCode invocation.
- Found OpenCode `1.18.31` CLI incompatibility with the `--standalone` flag before any candidate source edits; corrected fallback invocation to supported `--pure --auto --agent build --format json --model ...` flags.
- I1.5 complete: `INFRA-001 #7` proved automatic Gemini quota failure -> clean workspace -> successful OpenRouter/OpenCode fallback using `nvidia/nemotron-3-ultra-550b-a55b:free`.
- I2 complete: autonomous coder created PR #5 with `examples/valve.json` and deterministic Valve generation tests; Requirements Reviewer independently returned `PASS`.
- I3 complete: Linux generated and packaged exact `UDT_Valve.scl`; Windows checked out trusted `main`, received only the PLC package, ran trusted `TiaV21Worker`, and TIA Portal V21 compiled with 0 errors / 0 warnings.
- I4 complete: PLC/TIA Reviewer independently verified the artifact manifest plus TIA diagnostics and returned `PASS`.
- Full Git-task path is now proven end-to-end: versioned task/prompt -> coder -> PR -> reviewer -> PLC artifact -> trusted Windows/TIA -> diagnostics -> second reviewer.
- I5 implemented: `Candidate Validation` now dispatches bounded `candidate-repair` attempts using `maxRepairAttempts`; `Autonomous Repair` edits the same candidate branch/PR and redispatches trusted validation.
- I5 repair prompt consumes trusted task state, current diff, recent reviewer comments, failed validation logs, and prior TIA diagnostics when available; protected infrastructure remains non-editable.
- I5 proven with PR #6: intentionally incomplete Valve was rejected by Requirements Reviewer, repair attempt 1/3 fixed the same PR, fresh validation passed requirements, TIA V21 compiled `UDT_Valve` with 0 errors / 0 warnings, and PLC/TIA Reviewer passed.
- Added reusable manual `I5 Repair Smoke` workflow for regression testing the bounded repair loop.
- Closed smoke PR #6 without merge after proving I5.
- Prepared `docs/GENERATOR_CHAT_HANDOFF.md` with architecture, workflows, task schema, provider fallback, Windows/TIA boundary, repair semantics, and normal operating procedure for the next chat.
- I6 run #8 exposed a late OpenRouter daily-limit case: the agent had already created Valve changes, passed 2/2 tests, generated `UDT_Valve.scl`, and rechecked Motor before OpenRouter returned 429 at 50/50 free requests.
- Hardened `run-coder.sh` so a late quota/rate-limit with real repository changes preserves the partial candidate for deterministic Linux acceptance and independent reviewers instead of discarding it; audit records `partial_candidate_after_rate_limit`.
- Added disposable `output/` to `.gitignore` so agent-generated verification artifacts cannot enter candidate PRs.

## 2026-09-18

### DONE
- Accepted the Phase 2 hybrid AI operating model: DeepSeek API as primary autonomous implementer, ChatGPT as Senior Architect / primary external reviewer, and Gemini as independent verification/red-team reviewer.
- Accepted risk-based external review: one reviewer for low-risk changes, ChatGPT-led review for medium risk, and independent ChatGPT + Gemini review for high-risk/architecture/PLC-semantic/security changes.
- Accepted self-contained review packages designed for brand-new reviewer chats with zero prior context; GitHub remains the source of truth and transfer mechanism.
- Accepted explicit review states `APPROVE`, `CHANGES_REQUIRED`, `BLOCKED`, plus `REVIEW_CONFLICT` for disagreement between independent reviewers.
- Documented the operating model in `docs/AI_COLLABORATION_MODEL.md` and opened tracking issue #7.
- I6 DONE: clean production `INFRA-001` run `35315016121` started from `main` without fault injection or manual candidate edits; Gemini failed on demand/quota and the trusted fallback successfully completed the candidate through OpenRouter/OpenCode.
- I6 created PR #10 at candidate `91210d2da2a5a361aebb8f8077d715b9d893553d`; deterministic Linux acceptance passed 2/2 tests, Motor regression generation, and `UDT_Valve.scl` generation.
- I6 Candidate Validation run `35315441338` passed Requirements Reviewer, candidate packaging, trusted Windows/TIA acceptance, PLC/TIA Reviewer, and final `repair-or-finish` at attempt 0/3.
- I6 trusted TIA Portal V21 compiled the exact `UDT_Valve.scl` candidate artifact with `success=true`, 0 errors, and 0 warnings; `UDT_Valve (UDT)` and `Main (OB1)` both reported Success.
- Infrastructure baseline through I6 is complete and frozen; future baseline changes require a concrete generator/Phase-2 blocker.
- Phase 2 external-review protocol is implemented with versioned code/architecture/PLC templates, strict bound reviewer JSON, trusted package generation, and explicit `WAITING_FOR_EXTERNAL_REVIEW` / result / `REVIEW_CONFLICT` states.
- Connected ChatGPT may now act as the operator-facing review console: on request it can read pending GitHub review packages and evidence and write the authorized structured review response back to GitHub; HIGH-risk Gemini review remains independent.
- Added DeepSeek API as the primary autonomous coder with provider order DeepSeek -> Gemini -> OpenRouter, a 16-step OpenCode bound, 30-minute DeepSeek timeout, and per-run token/cache/cost audit.
- Pinned OpenCode `1.18.31` lacked a built-in DeepSeek catalog entry, so `opencode.json` now declares the official `https://api.deepseek.com` endpoint explicitly as an OpenAI-compatible provider using `deepseek-flash`.
- DeepSeek primary path proven in Autonomous Agent run `35318239703`: selected provider `deepseek`, model `deepseek/deepseek-flash`, no fallback, 14 steps, 16,324 input tokens, 2,271 output tokens, 187,264 cache-read tokens, reported cost `$0.005266992`.
- DeepSeek smoke produced PR #13 with only `AutomationCompilerTests.cs`; it independently mutation-checked the duplicate-name test, restored production code, and deterministic Linux tests passed.

### CURRENT
- Phase 2 / issue #7: connect validated external `CHANGES_REQUIRED` results to the existing bounded repair dispatcher, then exercise the full DeepSeek -> ChatGPT review -> repair/approve loop on a versioned task.

### NEXT
- Add automatic bounded repair/resume from validated external reviewer findings without changing the Windows/TIA boundary.
- Run one versioned generator-development task through DeepSeek and the connected ChatGPT review path.
- Accumulate per-task calls/tokens/cache/cost before considering LiteLLM or additional provider pooling.

## Logging rule
After every meaningful infrastructure change, append one short bullet under DONE/CURRENT/NEXT. Do not turn this file into design documentation; design belongs in dedicated design docs.
