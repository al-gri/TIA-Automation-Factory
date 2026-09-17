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
- Added trusted `.gemini/settings.json` that pins `gemini-3.8-flash` and disables built-in subagents; future runs still record actual usage instead of assuming the requested model was used.
- I1 complete: coder prompt is versioned at `agents/prompts/coder.md`, reviewer prompts are versioned under `agents/prompts/`, and smoke task `tasks/INFRA-001.json` is stored in Git.
- Updated `Autonomous Agent` workflow to run from a Git task path or GitHub Issue and to record requested/initialized/actual model information.
- CI remained green after I1 changes.

### CURRENT
- I2: add independent read-only Requirements Reviewer and structured criterion-by-criterion result.

### NEXT
- I3: safe Linux artifact -> trusted Windows/TIA acceptance bridge; never execute candidate branch code on the Windows runner.
- I4: independent read-only PLC/TIA Reviewer.
- I5: bounded repair loop using reviewer and TIA diagnostics.
- I6: final infrastructure smoke test driven only by a Git task/prompt.

## Logging rule
After every meaningful infrastructure change, append one short bullet under DONE/CURRENT/NEXT. Do not turn this file into design documentation; design belongs in `docs/INFRASTRUCTURE_PLAN.md`.
