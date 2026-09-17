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

### CURRENT
- I1.5 provider resilience is implemented but not yet runtime-accepted because `OPENROUTER_API_KEY` is not configured in GitHub Actions.
- I2-I4 remain implemented but not accepted until the full `INFRA-001` chain completes.

### NEXT
- Add repository Actions secret `OPENROUTER_API_KEY` and repeat `tasks/INFRA-001.json`.
- Confirm automatic Gemini quota failure -> clean workspace -> OpenRouter/OpenCode coder fallback.
- Confirm Requirements Reviewer -> Linux artifact package -> trusted TIA V21 -> PLC/TIA Reviewer completes without manual source-code intervention.
- Fix any defects found by the full INFRA-001 smoke test and then mark I1.5 and I2-I4 DONE.
- I5: bounded repair loop using structured reviewer and TIA diagnostics, maximum attempts from the task.
- I6: repeat a clean end-to-end Git-task smoke run with no manual source-code intervention and prepare handoff documentation for the generator-development chat.

## Logging rule
After every meaningful infrastructure change, append one short bullet under DONE/CURRENT/NEXT. Do not turn this file into design documentation; design belongs in `docs/INFRASTRUCTURE_PLAN.md`.
