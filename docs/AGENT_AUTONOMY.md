# Autonomous coding-agent loop

Milestone 2 adds OpenHands on a GitHub-hosted Linux runner. The agent edits the repository in an ephemeral cloud runner, runs deterministic Linux checks, and opens a pull request. It does not receive RDP, WinRM, SSH, or generic shell access to the Windows TIA V21 runner.

## LLM

Initial provider: Google Gemini Developer API.

Model: `gemini/gemini-3.8-flash`.

Store the API key only as the repository Actions secret `GEMINI_API_KEY`. Never commit it.

## Security boundary

In the first autonomous phase the agent is not allowed to change:

- `.github/**`
- `src/TiaV21Worker/**`

The workflow rejects the run if these paths are changed. This prevents the coding agent from rewriting the workflows or the executable boundary that talks to TIA Portal.

The Windows self-hosted runner remains an acceptance environment. Do not add `pull_request` workflows that execute arbitrary PR code directly on the Windows runner.

## Flow

1. Create a clear GitHub Issue with acceptance criteria.
2. Trigger `Autonomous Agent` with the issue number (initially by `workflow_dispatch`).
3. OpenHands checks out `main` on `ubuntu-latest`, creates an `agent/issue-*` branch and works on the task.
4. The repository OpenHands stop hook runs the Linux quality gate.
5. The workflow independently repeats deterministic tests and rejects protected-path changes.
6. If changes are valid, the workflow commits, pushes the branch and opens a PR.
7. Normal CI runs on the PR.
8. TIA V21 remains a separate trusted acceptance gate.

## Bounded execution

The agent workflow sets `MAX_ITERATIONS=30` and a GitHub job timeout. A failed task must become a failed/blocked run instead of an infinite retry loop.

## First agent task

Use a deliberately small cloud-only task that does not require changing the Windows worker. Example: add validation for duplicate field names in the vendor-neutral PLC IR and cover it with tests.
