#!/usr/bin/env bash
set -euo pipefail

cd "${OPENHANDS_PROJECT_DIR:-$PWD}"

echo "[OpenHands stop hook] Running deterministic Linux quality gate..."
dotnet test tests/SiemensBackend.Tests/SiemensBackend.Tests.csproj --configuration Release
rm -rf /tmp/tia-automation-agent-generated
dotnet run --project src/GeneratorCli/GeneratorCli.csproj --configuration Release -- examples/motor.json /tmp/tia-automation-agent-generated

test -f /tmp/tia-automation-agent-generated/UDT_Motor.scl

echo "[OpenHands stop hook] Quality gate passed."
