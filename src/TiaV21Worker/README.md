# TiaV21Worker

This folder is the Windows-only boundary for Siemens TIA Portal V21 Openness integration.

The first implementation will run on a GitHub self-hosted Windows runner and will:

1. read a generated PLC artifact,
2. open a dedicated TIA Portal V21 test project,
3. import the artifact through TIA Openness,
4. compile the PLC software,
5. write `tia-diagnostics.json`,
6. return a non-zero exit code when compilation contains errors.

No Siemens Openness assemblies are referenced by Domain, PlcCompiler, or SiemensBackend.
