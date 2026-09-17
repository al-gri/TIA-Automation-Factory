# Generator development handoff

This document is the operating handoff from infrastructure setup to generator development.

## Goal

Develop the Siemens PLC code generator through bounded autonomous Git tasks while keeping real TIA Portal V21 as the final deterministic acceptance gate.

Target loop:

```text
versioned task in Git
  -> coding agent on GitHub Linux
  -> candidate branch + PR
  -> Requirements Reviewer
  -> Linux PLC artifact package
  -> trusted Windows/TIA V21 acceptance
  -> PLC/TIA Reviewer
  -> PASS
     or bounded repair -> same PR -> revalidation
     or BLOCKED after maxRepairAttempts
```

## Repository boundary

Work only in `al-gri/TIA-Automation-Factory`.

Do not modify `al-gri/IndustrialMDE`.

Vendor-neutral projects must not reference `Siemens.Engineering`. Siemens Openness integration remains isolated in `src/TiaV21Worker`.

## Current software path

```text
src/Domain
  -> src/PlcCompiler
  -> src/SiemensBackend
  -> src/GeneratorCli
  -> generated SCL
  -> src/TiaV21Worker on trusted Windows runner
  -> TIA Portal V21
```

The current generator slice can deserialize an `AutomationDevice`, compile it to PLC IR, and generate Siemens SCL UDT source.

## Trusted automation

### `.github/workflows/agent.yml` — Autonomous Agent

Starts implementation from either:
- a versioned task under `tasks/*.json`; or
- a GitHub Issue.

For the full TIA-validated path, prefer versioned Git tasks.

The coding agent runs only on a disposable GitHub-hosted Linux runner. It may edit task-scoped source/test/example files but may not edit trusted infrastructure or `src/TiaV21Worker`.

### `.github/workflows/candidate-validation.yml` — Candidate Validation

For a task candidate it performs, in order:
1. protected-path guard;
2. deterministic Linux tests/generator evidence;
3. independent Requirements Reviewer;
4. generation and hashing of the candidate PLC artifact;
5. trusted Windows/TIA V21 acceptance;
6. independent PLC/TIA Reviewer;
7. PASS / repair / BLOCKED decision.

The Windows job checks out trusted `main`; it never executes candidate C# code. Only the generated PLC package is transferred to Windows.

### `.github/workflows/agent-repair.yml` — Autonomous Repair

Triggered only after a failed candidate validation while repair budget remains.

The repair agent receives:
- trusted task specification;
- current candidate diff;
- recent reviewer comments;
- failed validation logs;
- prior `tia-diagnostics.json` when available;
- repair attempt number and maximum.

It edits and pushes the same candidate branch/PR, then dispatches a fresh trusted validation.

The task field `maxRepairAttempts` bounds the loop. Once exhausted, the candidate becomes `BLOCKED`; no infinite retry loop is permitted.

### `.github/workflows/i5-repair-smoke.yml`

Manual regression smoke for the repair mechanism. It intentionally creates an incomplete candidate. Do not use it for normal generator development.

## Agent prompts

Trusted prompts are versioned under:

```text
agents/prompts/coder.md
agents/prompts/repair.md
agents/prompts/reviewer-requirements.md
agents/prompts/reviewer-tia.md
```

Do not embed task-specific implementation instructions into workflow YAML. Put work intent and acceptance criteria in the versioned task.

## Task format

Use `tasks/INFRA-001.json` as the current schema example.

Important fields:

```json
{
  "id": "...",
  "title": "...",
  "goal": "...",
  "scope": [],
  "generator": {
    "input": "examples/...json",
    "expectedArtifact": "...scl"
  },
  "acceptance": {
    "requirements": [],
    "tia": []
  },
  "protectedPaths": [],
  "maxRepairAttempts": 3
}
```

`acceptance.requirements` is evaluated before Windows/TIA. `acceptance.tia` is evaluated only after the exact candidate PLC artifact has been tested by real TIA Portal V21.

## LLM providers

Coder runtime is provider-resilient:
1. Gemini CLI is attempted first with requested model `gemini-3.8-flash`;
2. actual Gemini usage is audited because Google may serve another model;
3. quota/provider failure discards that attempt's workspace edits;
4. fallback uses OpenRouter/OpenCode.

Current fallback model:

```text
nvidia/nemotron-3-ultra-550b-a55b:free
```

Required repository Actions secrets currently configured:

```text
GEMINI_API_KEY
OPENROUTER_API_KEY
```

Do not put API keys in tasks, prompts, issues, commits, logs, or chat messages.

## Windows / TIA boundary

Runner labels:

```text
self-hosted
Windows
X64
tia-v21
```

Runner name observed during acceptance: `TIA-V21-PC`.

`TiaV21Worker` targets .NET Framework 4.8 and loads TIA Portal V21 Openness assemblies from the V21 PublicAPI installation.

The trusted worker currently creates a temporary project, creates an S7-1500 station, imports the candidate SCL source, compiles the PLC software, and emits `tia-diagnostics.json`.

A successful acceptance requires at minimum:

```json
{
  "success": true,
  "errors": 0
}
```

Warnings are reported separately.

## Proven infrastructure milestones

- Real TIA V21 E2E Motor smoke: PASS, 0 errors / 0 warnings.
- Git task + coder + provider fallback: PASS.
- Requirements Reviewer: PASS in clean smoke and correctly rejected an incomplete smoke candidate.
- Safe Linux artifact -> trusted Windows/TIA bridge: PASS.
- PLC/TIA Reviewer: PASS.
- Bounded repair loop: PASS. PR #6 was intentionally incomplete, was rejected, repaired in attempt 1/3 on the same PR, then passed real TIA V21 with 0 errors / 0 warnings.

See `docs/INFRASTRUCTURE_LOG.md` for chronological details.

## Normal operating procedure for generator development

1. Create or update one small versioned task in `tasks/` with explicit acceptance criteria.
2. Keep the task narrow enough to review and test deterministically.
3. Start `Autonomous Agent` with `source=task` and the task path.
4. Do not manually edit the candidate branch while autonomous validation/repair is running.
5. Read the PR comments from Requirements Reviewer and PLC/TIA Reviewer.
6. Treat `PASS` only as the state where deterministic checks and required TIA criteria pass.
7. If the loop reaches `BLOCKED`, diagnose the blocker before increasing the attempt budget or changing infrastructure.
8. Merge only after human review of a passing candidate; autonomous workflows do not self-merge.

## Infrastructure freeze rule

After the final I6 clean smoke, treat the infrastructure as frozen. Change workflows, runtimes, prompts, provider plumbing, or the Windows/TIA boundary only when a concrete generator-development task is blocked by infrastructure.

The next chat should focus on the generator/domain/compiler/Open Library model rather than expanding orchestration for its own sake.
