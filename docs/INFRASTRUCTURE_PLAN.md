# Infrastructure target

This phase is complete only when a fresh development chat can hand a Git-versioned task to the repository and the infrastructure can execute the full candidate loop without manually editing source code.

## Target flow

```text
Git task spec + versioned prompts
        |
        v
Provider-agnostic coder agent on GitHub-hosted Linux
  - primary model/provider
  - independent fallback provider
        |
        +--> source changes
        +--> unit tests
        +--> generated PLC artifact
        |
        v
Requirements reviewer (read-only, independent context)
        |
        v
Trusted Windows TIA gate
  - never executes candidate branch code
  - checks out trusted main worker only
  - downloads generated PLC artifact
        |
        v
TIA Portal V21 / Openness
        |
        v
tia-diagnostics.json
        |
        v
PLC/TIA reviewer (read-only, independent context)
        |
        v
PASS / CHANGES_REQUIRED / BLOCKED
```

## Security boundary

Candidate code runs only on disposable GitHub-hosted Linux runners. The Windows self-hosted runner must not check out or execute code from an AI candidate branch. It may consume bounded PLC artifacts (`.scl`, later controlled XML) produced in Linux and run the trusted `TiaV21Worker` from `main`.

The coding agent must not modify `.github/**`, agent infrastructure, `.gitignore`, or `src/TiaV21Worker/**`.

## Agent roles

1. **Coder** — implements one Git task, adds tests, and produces a candidate branch.
2. **Requirements reviewer** — read-only review of task acceptance criteria, diff, and deterministic Linux test results.
3. **PLC/TIA reviewer** — read-only review of generated PLC artifact plus TIA diagnostics.

The two reviewers use separate prompts and separate model sessions. They do not modify the repository. TIA compile is a deterministic gate and is not replaced by either reviewer.

## Model/provider policy

The infrastructure must not depend on a single LLM provider or free-tier quota.

Current Gemini path requests `gemini-3.8-flash`, but model audit proved that actual usage can be attributed to `gemini-3.5-flash`. Every run must record:
- requested provider/model;
- initialized model when the harness exposes it;
- actual model/provider reported by usage telemetry when available;
- fallback activation and reason.

Provider credentials are GitHub Actions secrets only. Task specs and prompts never contain API keys.

A provider quota failure is an infrastructure condition, not a coding-task failure. The orchestration layer should either use an approved fallback provider or mark the run `BLOCKED_PROVIDER` rather than pretending the task failed acceptance.

## Infrastructure milestones

### I0 — proven foundations — DONE
- Linux CI builds/tests generator.
- Windows self-hosted runner connected.
- TIA V21 and Openness detected.
- Trusted worker imports generated SCL and compiles in TIA with zero errors.
- Cloud coding agent can create a branch and PR.

### I1 — Git-versioned task/prompt — DONE
- Coder instructions live in `agents/prompts/coder.md`.
- Machine-readable smoke task lives under `tasks/`.
- Workflow reads prompt and task from Git.
- Requested and actual model usage are recorded.

### I1.5 — provider resilience — CURRENT
- Replace the hard dependency on Gemini CLI with a provider-agnostic headless agent layer.
- Keep Gemini as one provider.
- Add at least one independent cloud fallback provider.
- Detect quota/provider failures separately from implementation/test failures.
- Log provider/model actually used for coder and reviewers.
- Never retry indefinitely or rotate accounts to bypass provider limits.

### I2 — requirements reviewer — IMPLEMENTED, NOT YET ACCEPTED
- Read-only reviewer prompt.
- Reviewer consumes task + candidate diff + deterministic test report.
- Structured result: `PASS` or `CHANGES_REQUIRED` with criterion-by-criterion evidence.

### I3 — safe candidate-to-TIA bridge — IMPLEMENTED, NOT YET ACCEPTED
- Linux generates PLC artifact and uploads it with a manifest.
- Windows job checks out trusted `main` only.
- Windows downloads candidate artifact; it never executes candidate branch code.
- Trusted worker sends artifact to disposable TIA V21 project and uploads diagnostics.

### I4 — PLC/TIA reviewer — IMPLEMENTED, NOT YET ACCEPTED
- Independent read-only reviewer for PLC artifact + `tia-diagnostics.json`.
- Structured result: `PASS` / `CHANGES_REQUIRED`.

### I5 — bounded repair loop
- Feed reviewer/TIA failures back to coder on the same candidate branch.
- Maximum attempts configured by task.
- Exhausted attempts become `BLOCKED`, never an infinite loop.

### I6 — infrastructure handoff smoke test
A Git task (no manually authored code changes) causes the agent system to:
1. read its prompt and task from Git;
2. select an available approved provider without manual source edits;
3. create/modify a small Siemens generator example;
4. pass Linux tests;
5. generate a PLC artifact;
6. pass Requirements Reviewer;
7. pass real TIA V21 compile with zero errors;
8. pass PLC/TIA Reviewer;
9. leave an auditable PR, provider/model audit, reports, artifacts, and logs.

When I6 passes, this infrastructure phase is considered ready for handoff to the generator-development chat.
