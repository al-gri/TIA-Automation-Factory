# Autonomous coding-agent loop

The autonomous implementation agent runs on a disposable GitHub-hosted Linux runner. TIA Portal V21 remains behind a trusted Windows acceptance boundary.

The infrastructure design and current milestones are tracked in:
- `docs/INFRASTRUCTURE_PLAN.md`
- `docs/INFRASTRUCTURE_LOG.md`

## Agent runtime

Provider: Google Gemini Developer API.

CLI: pinned stable Google Gemini CLI (`@google/gemini-cli`).

Requested coding model: `gemini-3.8-flash`.

Authentication: repository Actions secret `GEMINI_API_KEY`; never commit the key.

Gemini CLI can route/fallback internal calls. Therefore each run must record both the requested model and the actual model names reported in the CLI result statistics. In the first successful autonomous run, the session was initialized as `gemini-3.8-flash`, while final usage statistics attributed tokens to `gemini-3.5-flash`.

## Versioned instructions

Agent behavior is not defined only inside workflow YAML.

- Coder prompt: `agents/prompts/coder.md`
- Requirements reviewer: `agents/prompts/reviewer-requirements.md`
- PLC/TIA reviewer: `agents/prompts/reviewer-tia.md`
- Machine-readable tasks: `tasks/*.json`

The target smoke task is `tasks/INFRA-001.json`.

## Security boundary

Candidate/AI-authored code may execute only on disposable GitHub-hosted Linux runners.

The coding agent may not modify:
- `.github/**`
- `agents/**`
- `tasks/**`
- `.gemini/**`
- `.openhands/**`
- `.gitignore`
- `src/TiaV21Worker/**`

The Windows self-hosted runner must not check out or execute code from an AI candidate branch. A candidate branch is allowed to produce bounded PLC artifacts in Linux. The Windows gate consumes those artifacts while running a trusted `TiaV21Worker` checked out from `main`.

This prevents a modified `GeneratorCli` or test project from becoming arbitrary code execution on the Windows/TIA workstation.

## Review model

Two independent read-only review sessions are planned:

1. Requirements Reviewer — task specification + diff + Linux test/generator evidence.
2. PLC/TIA Reviewer — task specification + generated PLC artifact + real `tia-diagnostics.json`.

Reviewers do not replace deterministic tests or TIA compile. TIA diagnostics are authoritative for real Siemens import/compile status.

## Bounded execution

The future repair loop is bounded by the task's `maxRepairAttempts`. A failed candidate receives structured reviewer/TIA feedback and may be repaired on the same branch. Exhausted attempts become `BLOCKED`; no unbounded `while (!success) askAI()` loop is permitted.
