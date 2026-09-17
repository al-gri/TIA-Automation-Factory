# TIA Automation Factory

Minimal end-to-end foundation for an automation engineering software factory targeting Siemens TIA Portal V21 through TIA Portal Openness.

## First vertical slice

`examples/motor.json` → Automation Domain → PLC IR → Siemens SCL → Windows self-hosted runner → TIA Portal V21 → diagnostics.

The first milestone deliberately keeps AI out of the critical path. We first make generation and verification deterministic; an autonomous coding agent is added only after the pipeline is proven.

## Projects

- `src/Domain` — vendor-neutral automation model.
- `src/PlcCompiler` — validation and conversion to vendor-neutral PLC IR.
- `src/SiemensBackend` — Siemens-specific SCL generation.
- `src/GeneratorCli` — small CLI used by CI to generate artifacts from JSON.
- `src/TiaV21Worker` — Windows/.NET Framework 4.8 boundary for TIA Portal V21 Openness.
- `tests` — deterministic unit and golden-output tests.

## Local cloud-independent check

```bash
dotnet test tests/SiemensBackend.Tests/SiemensBackend.Tests.csproj
dotnet run --project src/GeneratorCli/GeneratorCli.csproj -- examples/motor.json artifacts/generated
```

## TIA V21 machine

The TIA worker is intentionally isolated from the rest of the system. The Windows account running it must be allowed to use TIA Portal Openness and the machine must have TIA Portal V21 installed.

See `docs/WINDOWS_RUNNER_SETUP.md`.
