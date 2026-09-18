# TIA Automation Factory

Automation engineering software factory targeting Siemens TIA Portal V21 through TIA Portal Openness.

## Start here — mandatory for AI / fresh chats

GitHub is the only durable source of truth for this project. No prior chat history is required or authoritative.

Before doing project work, read:

1. `AGENTS.md` — mandatory repository-first operating contract, ChatGPT / coding-provider / Gemini roles, and user command semantics.
2. `docs/PROJECT_STATE.md` — current authoritative operational state, active blocker, and next action.
3. `docs/AI_COLLABORATION_MODEL.md` — collaboration and risk policy.
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

- OpenRouter free coding model is the first autonomous coding provider while its daily allowance is available.
- DeepSeek official API with `deepseek-flash` is the paid coding fallback and repair provider after OpenRouter is exhausted or unavailable.
- ChatGPT is the Senior Architect, connected GitHub operator, and primary external reviewer.
- Gemini is an independent reviewer / red-team escalation only when repository risk policy requires it; ChatGPT must provide a complete ready-to-paste Gemini prompt rather than silently substituting Gemini for its own review slot.
- AI candidate source runs only on disposable Linux runners.
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
