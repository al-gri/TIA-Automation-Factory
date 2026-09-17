# Infrastructure target

This phase is complete only when a fresh development chat can hand a Git-versioned task to the repository and the infrastructure can execute the full candidate loop without manually editing source code.

## Target flow

```text
Git task spec + versioned prompts
        |
        v
Coder agent on GitHub-hosted Linux
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

## Model policy

Requested coding model: `gemini-3.8-flash` through pinned Gemini CLI stable release.

Every run must record both:
- requested model;
- actual models reported by Gemini CLI usage statistics.

A run is auditable even if Gemini CLI routes internal/sub-agent calls to a different model.

## Infrastructure milestones

### I0 — proven foundations — DONE
- Linux CI builds/tests generator.
- Windows self-hosted runner connected.
- TIA V21 and Openness detected.
- Trusted worker imports generated SCL and compiles in TIA with zero errors.
- Gemini coding agent can create a branch and PR.

### I1 — Git-versioned task/prompt — CURRENT
- Move coder instructions out of workflow YAML into `agents/prompts/coder.md`.
- Add a machine-readable smoke task under `tasks/`.
- Workflow reads prompt and task from Git.
- Record requested and actual model usage.

### I2 — requirements reviewer
- Add read-only reviewer prompt.
- Reviewer consumes task + candidate diff + deterministic test report.
- Structured result: `PASS` or `CHANGES_REQUIRED` with criterion-by-criterion evidence.

### I3 — safe candidate-to-TIA bridge
- Linux generates PLC artifact and uploads it with a manifest.
- Windows job checks out trusted `main` only.
- Windows downloads candidate artifact; it never executes candidate branch code.
- Trusted worker sends artifact to disposable TIA V21 project and uploads diagnostics.

### I4 — PLC/TIA reviewer
- Add independent read-only reviewer for PLC artifact + `tia-diagnostics.json`.
- Structured result: `PASS` / `CHANGES_REQUIRED`.

### I5 — bounded repair loop
- Feed reviewer/TIA failures back to coder on the same candidate branch.
- Maximum attempts configured (initially 3 or 4).
- Exhausted attempts become `BLOCKED`, never an infinite loop.

### I6 — infrastructure handoff smoke test
A Git task (no manually authored code changes) causes the agent system to:
1. read its prompt and task from Git;
2. create/modify a small Siemens generator example;
3. pass Linux tests;
4. generate a PLC artifact;
5. pass Requirements Reviewer;
6. pass real TIA V21 compile with zero errors;
7. pass PLC/TIA Reviewer;
8. leave an auditable PR, reports, artifacts, and logs.

When I6 passes, this infrastructure phase is considered ready for handoff to the generator-development chat.
