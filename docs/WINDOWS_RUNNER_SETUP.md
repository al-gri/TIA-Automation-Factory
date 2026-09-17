# Windows runner setup for TIA Portal V21

The Windows machine is used only for Siemens verification. AI inference and normal build/test work stay in the cloud.

## Required software

- Windows with TIA Portal V21 installed.
- TIA Portal Openness V21 (installed with TIA Portal V21).
- GitHub self-hosted Actions runner.
- .NET Framework 4.8 developer tools when the real `TiaV21Worker` is introduced.

## Required GitHub runner label

Configure the runner with the custom label:

`tia-v21`

The workflow expects:

`[self-hosted, Windows, X64, tia-v21]`

## First verification

Run the GitHub Actions workflow **TIA V21 Verification** manually. At this stage it performs only non-destructive checks:

1. verifies `C:\Program Files\Siemens\Automation\Portal V21`,
2. verifies the V21 Openness `PublicAPI\V21\net48` directory,
3. lists the available API assemblies.

## Next milestone

After the runner passes these checks, implement a dedicated `TiaV21Worker` that works only with a disposable test TIA project. The worker will import generated artifacts, compile the PLC software, and emit machine-readable diagnostics.
