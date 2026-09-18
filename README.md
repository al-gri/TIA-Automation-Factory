# TIA Automation Factory

Automation engineering software factory targeting Siemens TIA Portal V21 through TIA Portal Openness.

## Start here — mandatory for AI / fresh chats

GitHub is the only durable source of truth for this project. No prior chat history is required or authoritative.

Before doing project work, read:

1. `AGENTS.md` — mandatory repository-first operating contract, ChatGPT / coding-provider / Gemini roles, user command semantics, and Cline policy.
2. `docs/PROJECT_STATE.md` — current authoritative operational state, active blocker, and next action.
3. `docs/AI_COLLABORATION_MODEL.md` — collaboration, provider cascade, and risk policy.
4. `docs/EXTERNAL_REVIEW_PROTOCOL.md` — external review protocol.

A brand-new ChatGPT session should be able to inspect this repository and continue safely without any context copied from another chat.

## Architecture

`Automation input` → Automation Domain → PLC IR → Siemens SCL → trusted Windows self-hosted runner → TIA Portal V21 → diagnostics.

- `src/Domain` — vendor-neutral automation model.
- `src/PlcCompiler` — validation and conversion to vendor-neutral PLC IR.
- `src/SiemensBackend` — Siemens-specific SCL generation.
- `src/GeneratorCli` — CLI used by CI to generate artifacts from JSON.
- `src/TiaV21Worker` — Windows/.NET Framework 4.8 trust boundary for TIA Portal V21 Openness.
- `tests` — deterministic unit and output tests.
- `tasks` — trusted versioned task specifications.
- `reviews` — external-review templates and response schema.

## AI operating model

- Coding provider order: **OpenRouter first**, then official **DeepSeek `deepseek-flash`** when the OpenRouter daily allowance is exhausted/unavailable.
- ChatGPT is the Senior Architect and primary connected external reviewer.
- Gemini is an independent reviewer / red-team escalation, not a routine coding fallback.
- Optional Cline-style interactive development must use the same repository context, branch/review rules, and protected boundaries; it is not the production orchestrator.
- AI candidate source runs only on disposable Linux runners in the autonomous path.
- TIA Portal V21 compile through trusted infrastructure remains authoritative.
- No automatic merge.

See `AGENTS.md` and `docs/AI_COLLABORATION_MODEL.md` for the normative rules.

## Local cloud-independent check

```bash
dotnet test tests/SiemensBackend.Tests/SiemensBackend.Tests.csproj
dotnet run --project src/GeneratorCli/GeneratorCli.csproj -- examples/motor.json artifacts/generated
```

## TIA V21 machine

The TIA worker is intentionally isolated from the rest of the system. The Windows account running it must be allowed to use TIA Portal Openness and the machine must have TIA Portal V21 installed.

See `docs/WINDOWS_RUNNER_SETUP.md`.
