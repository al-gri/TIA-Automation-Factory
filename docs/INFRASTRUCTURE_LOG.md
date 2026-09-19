# Infrastructure Progress Log

Short chronological record of infrastructure work. Current policy is defined by `AGENTS.md`, `docs/PROJECT_STATE.md` and the living methodology documents. Raw high-frequency evidence belongs in methodology issue #25.

## 2026-09-17 — Initial autonomous/TIA infrastructure

### DONE
- Created isolated `al-gri/TIA-Automation-Factory`; `IndustrialMDE` stayed outside scope.
- Added minimal Domain -> PLC IR -> Siemens SCL path and Linux CI.
- Registered Windows x64 self-hosted runner with `tia-v21` and verified TIA Portal V21 / Openness / net48 prerequisites.
- Built `TiaV21Worker` against real V21 assemblies and completed real TIA smoke with 0 errors / 0 warnings.
- Added disposable-Linux autonomous coding, protected paths, versioned tasks/prompts and trusted Windows/TIA boundary.

## 2026-09-18 — Repository-first orchestration and governance

### DONE
- Added exact-SHA external-review protocol, bounded repair, provider audit and OpenRouter -> DeepSeek continuity.
- Adopted GitHub as sole durable source of truth and authorship-based reviewer independence.
- PR #17 merged ARCH-001; PR #16 merged PLC-001 after exact TIA V21 PASS.
- Added methodology documents, issue #25 telemetry and trusted coder-context construction.
- Repaired control-plane/prompt authority separation (GOV-CTX-001).
- Defined the exceptional SSH-signed governance bootstrap lane; PR #32 ultimately merged at `60bd20841360998406db59cfb13a61cb33982566`.

## 2026-09-19 — OLQ trusted harness and diagnostic path

### DONE
- Trusted OLQ harness PR #27 merged at `24f3a8fad542132a7aa9369be4451dd6ca0ae23f`.
- Trusted qualification workflow runs `main` only, keeps vendor payloads runner-local and publishes sanitized hashes/status.
- OLQ-DIAG-001 added stable public-safe phase/type/HRESULT diagnostics while raw exception details stay runner-local.
- Trusted runs consistently localized the remaining library blocker to `RetrieveWithUpgrade` with `EngineeringTargetInvocationException`, HRESULT `0x80131500`.

## 2026-09-19 — TIA-AUTH whitelist automation

### DONE
- Added worker self-synchronization of its application-specific Siemens Openness whitelist identity plus one-time narrow elevated bootstrap.
- Trusted execution exposed a Windows ACL API overload defect: passing a SID as a string to `RegistryAccessRule` attempted account-name translation. PR #52 repaired it by using `SecurityIdentifier`.
- A second trusted diagnostic proved `OpenSubKey(..., SetValue|QueryValues)` could open the Entry key while `SetValue` still failed. Root cause was .NET Framework `RegistryKey` writeability state when the rights-only overload inherited `RegistryKeyPermissionCheck.Default`.
- PR #55 changed only the key-open overload to explicitly use `RegistryKeyPermissionCheck.ReadWriteSubTree` while preserving exact `SetValue | QueryValues` and Registry64.
- Trusted OLQ run `35458909671` then passed whitelist synchronization/Openness admission and reached `RetrieveWithUpgrade`. Issues #46 and #54 closed completed.

### REMAINING CLEANUP
- Issue #53: bootstrap existing-rule detection is not idempotent; trusted task `tasks/TIA-AUTH-IDEMP-001.json` is prepared. Functional Openness admission is already accepted, so this stays separate from OLQ migration work.

## 2026-09-19 — OLQ-DIAG-002 prepared

### ACTIVE NEXT TASK
- Issue #56 and `tasks/OLQ-DIAG-002.json` define a HIGH-risk diagnostic-only change in `src/TiaV21Worker/Program.cs`.
- It preserves migration semantics and derives only allowlisted safe tags, detail count and SHA-256 fingerprints from `EngineeringException.MessageData/DetailMessageData`.
- Arbitrary Siemens text, paths and vendor/library contents remain non-public.
- Operational checkpoint `main@4b27064e19395a350bb0e2879eb959640cc0775d` passed CI #317 / run `35459383614`.
- Next mechanical action is task-mode Autonomous Agent dispatch for `tasks/OLQ-DIAG-002.json`; candidate Windows/TIA execution remains prohibited.

## Logging rule

Keep this file concise and factual. Raw workflow events belong in issue #25; reusable lessons belong in `docs/DEVELOPMENT_METHODOLOGY.md`; milestone interpretation belongs in `docs/METHODOLOGY_JOURNAL.md`.
