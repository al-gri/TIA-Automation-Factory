# Independent Repository Audit — accepted execution record

Date: 2026-09-19
Audited baseline: `f94f132c0bdab4439b739262c96069754e3e3842`
Acceptance decision by primary architect: accepted with live-GitHub verification; recommendations are prioritized by confirmed risk and are not treated as automatic authority overrides.

## Confirmed immediate findings

- **C01 / P0 confirmed:** current autonomous implementation and repair workflows execute candidate-controlled code and later perform privileged GitHub publication in the same job/workspace. Protected-path diff checks do not create a process-isolation boundary.
- **M03 confirmed:** the trusted repair wrapper materializes `run-coder.sh` from trusted main, but that script invokes `agents/runtime/build-coder-context.py` from the candidate workspace.
- **M04 confirmed:** publication uses `git add -A` after candidate execution; issue #50 records an actual byproduct leakage incident.
- **M02 confirmed:** LOW/MEDIUM approval still dispatches candidate validation that transfers unmerged candidate PLC source to the Windows/TIA runner.
- **M01 confirmed:** trusted run `35461905726` failed to build `TiaV21Worker` with CS0019 before qualification; issue #58 and `OLQ-DIAG-BUILD-001` remain the correct narrow repair once the unsafe autonomous publication lane is contained.
- `main` was unprotected at the audited/live-verified baseline. Platform hardening is useful but is not substituted for the process-isolation fix.

## Execution order adopted

1. `FACTORY-ISOLATION-001`: isolate candidate execution from GitHub publication, pin the full trusted helper chain, enforce positive final patch scope, and remove premerge candidate-to-TIA processing.
2. Resume `OLQ-DIAG-BUILD-001` through the contained lane (or another independently reviewed safe route), then rerun trusted postmerge Windows build/qualification.
3. Establish one truthful Open Library V21 valve profile and reference compile.
4. Generate one real valve slice with explicit input/symbol/ownership semantics and deterministic artifacts.
5. Complete trusted TIA import/compile/save/reopen acceptance for that valve.

## Non-blocking audit recommendations

Review-package compaction (#49), bootstrap idempotence (#53), prompt/document consolidation, telemetry cleanup, legacy asset cleanup, broader UI/graph/catalog work, and audit-cadence refinements remain follow-up work. They must not displace the P0/P1 product route unless a new concrete blocker appears.

## Operating constraint

Until `FACTORY-ISOLATION-001` is independently reviewed and merged, do not use the current Autonomous Agent / Agent Repair workflows as a trusted unattended publication lane, and do not send unmerged candidate artifacts to Windows/TIA.
