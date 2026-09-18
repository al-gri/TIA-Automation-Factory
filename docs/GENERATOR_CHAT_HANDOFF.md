# Generator development handoff

This document is the generator-focused companion to `docs/NEXT_CHAT_HANDOFF.md`.

For a brand-new ChatGPT session, start with:

1. `AGENTS.md`
2. `docs/PROJECT_STATE.md`
3. `docs/NEXT_CHAT_HANDOFF.md`
4. this document when focusing on generator/domain/compiler work

GitHub is the only durable source of truth. Prior chat history is not required.

## Goal

Develop the Siemens PLC code generator through small versioned Git tasks while keeping real TIA Portal V21 as the final deterministic Siemens acceptance gate.

Repository: `al-gri/TIA-Automation-Factory`.

Do not modify `al-gri/IndustrialMDE`.

## Architecture

```text
Automation input / future visual designer
  -> src/Domain
  -> src/PlcCompiler / PLC IR
  -> src/SiemensBackend
  -> src/GeneratorCli
  -> generated PLC/SCL artifact
  -> trusted src/TiaV21Worker on Windows
  -> TIA Portal V21
  -> diagnostics
```

Vendor-neutral code must not reference `Siemens.Engineering`. TIA Openness stays isolated in `src/TiaV21Worker` targeting .NET Framework 4.8.

## Current generator baseline

The current slice can:

- deserialize an `AutomationDevice` from JSON;
- validate basic device structure and reject duplicate field names case-insensitively;
- compile to vendor-neutral PLC IR;
- generate Siemens SCL UDT source;
- generate artifacts through `GeneratorCli`;
- import and compile the exact artifact through real TIA Portal V21;
- emit deterministic TIA diagnostics.

This is only the baseline. The next development focus is the real domain/compiler model and Siemens Open Library integration, not additional orchestration for its own sake.

## Coding provider order

Routine implementation is now:

```text
OpenRouter first
  -> if unavailable/quota/rate limit/timeout
DeepSeek official API: deepseek-flash
```

Current OpenRouter coding model:

```text
nvidia/nemotron-3-ultra-550b-a55b:free
```

If OpenRouter produces useful repository changes before quota exhaustion, those changes are preserved and DeepSeek continues the same bounded workspace.

Gemini is not a routine coding fallback. It is reserved for independent review / red-team escalation.

## Review model

ChatGPT is the Senior Architect and primary connected external reviewer.

The user can simply say `проверь репозиторий` or `проверь запросы DeepSeek`; ChatGPT must recover the task, PR, SHA, diff, tests and TIA evidence from GitHub itself, perform the authorized `chatgpt` review slot, and write the structured result back to GitHub.

Risk policy:

- LOW: coder -> ChatGPT -> deterministic/TIA gates.
- MEDIUM: coder -> ChatGPT -> Gemini only if escalation is justified.
- HIGH / architecture / PLC semantics / security: independent ChatGPT + Gemini review.

If Gemini is required, ChatGPT must provide one complete ready-to-paste Gemini message assembled from GitHub context. ChatGPT must not impersonate the Gemini reviewer.

## Trusted task and validation path

For normal generator work use a versioned task under `tasks/*.json`.

Trusted path:

```text
versioned task in main
  -> coding agent on disposable Linux
  -> candidate PR
  -> connected external ChatGPT review
  -> APPROVE or bounded repair
  -> Candidate Validation
  -> trusted task resolved from main
  -> Linux tests/generation
  -> Requirements Reviewer
  -> exact PLC artifact package
  -> trusted Windows/TIA V21
  -> PLC/TIA Reviewer
  -> DONE / bounded repair / BLOCKED
```

Important trust rule: candidate source is never executed on the Windows/TIA runner. Windows checks out trusted `main` and receives only the bounded PLC artifact package.

No automatic merge.

## Task shape

Use explicit acceptance criteria and risk metadata:

```json
{
  "id": "PLC-001",
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
  "review": {
    "riskClass": "LOW|MEDIUM|HIGH",
    "reviewType": "CODE_REVIEW|PLC_REVIEW",
    "reviewerSlots": ["chatgpt"]
  },
  "protectedPaths": [],
  "maxRepairAttempts": 3
}
```

The task `maxRepairAttempts` is authoritative. Never create an unbounded repair loop.

## Proven Phase 2 path

`tasks/PHASE2-001.json` / PR #13 proved the connected review model.

Connected ChatGPT recovered the review from GitHub and submitted `APPROVE`. After the trusted-task source bug was fixed, Candidate Validation run `35322785146` passed Linux checks, Requirements Reviewer, PLC packaging, trusted Windows/TIA acceptance, PLC/TIA Reviewer and final finish gate.

TIA V21 compiled the exact `UDT_Motor.scl` artifact with 0 errors / 0 warnings.

See:

- `docs/PROJECT_STATE.md`
- `docs/PHASE2_PROOF_2026-09-18.md`
- `docs/NEXT_CHAT_HANDOFF.md`
- `docs/INFRASTRUCTURE_LOG.md`

## Normal operating procedure from now on

1. Define one small real generator task in `tasks/`.
2. Keep acceptance deterministic and explicit.
3. Run the autonomous coder: OpenRouter first, DeepSeek fallback.
4. Let connected ChatGPT review from GitHub; do not manually copy context unless the connector is unavailable.
5. If `CHANGES_REQUIRED`, let bounded repair continue on the same PR.
6. If Gemini is required, use the complete prompt prepared by ChatGPT.
7. Require deterministic Linux evidence and real TIA V21 acceptance where applicable.
8. Merge only after human approval of a passing candidate.
9. Record meaningful decisions/results back to GitHub.
10. Treat infrastructure as frozen unless a concrete generator task exposes a real blocker.

## Next engineering focus

Return to the original product problem:

- model automation devices and relationships;
- define the vendor-neutral domain/IR needed for real systems;
- map Siemens Open Library concepts into the generator backend;
- decide how future visual authoring input maps into the domain model;
- build the next generator features as small versioned tasks with deterministic/TIA acceptance.
