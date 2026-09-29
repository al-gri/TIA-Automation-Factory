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

## 2026-09-20/21 — review transport, trusted product tasks and exhausted OLQ candidate

### DONE
- Added bounded connected-primary large-diff review fallback in PR #79 without weakening exact-SHA review or isolated-secondary fail-closed behavior.
- Merged trusted task authority for TIA V21 version-independent AllowList correction (`TIA-AUTH-V21-ALLOWLIST-001`) and generator foundation (`GEN-VALVE-FOUNDATION-001`).
- Confirmed initial `Autonomous Agent` execution remains `workflow_dispatch`-only; connected GitHub can drive review/repair continuation but cannot originate a fresh task run.
- PR #81 `OLQ-VALVE-PROFILE-001` consumed repair attempts 1/2 and 2/2. Final exact head `8101d1e197cf33eaf96874acb4a9933fb5256656` still had MAJOR compile/lifecycle/test defects and was validated BLOCKED. It was closed unmerged as historical evidence.
- Fresh coding-agent context was audited: it receives trusted task + baseline repository files, but not prior issue comments/review findings. `docs/PROJECT_STATE.md` was also found materially stale while being injected into every coding context.
- Issue #61 now records a non-authoritative replacement plan: qualification transaction/native-V21-open gate first, Valve contract/reference gate only after real native V21 open.
- Issue #63 now records the future trusted-main import/compile/save/reopen truth-gate shape without inventing Valve/TIA facts.

### CURRENT PRODUCT QUEUE
- #80: launch coding-agent for the trusted V21 AllowList task, independently review, merge and prove repeated non-interactive trusted-main admission.
- #61: no candidate-changing continuation until explicit human authorization after exhausted repair budget; then prefer the two-stage replacement decomposition.
- #62: launch contract-independent generator-foundation task in parallel; preserve the tiny current pipeline and forbid unqualified Valve facts.
- #63: start only after reviewed #61 contract/profile and #62 generated manifest/artifacts exist.

## Logging rule

Keep this file concise and factual. Raw workflow events belong in issue #25; reusable lessons belong in `docs/DEVELOPMENT_METHODOLOGY.md`; milestone interpretation belongs in `docs/METHODOLOGY_JOURNAL.md`.
## 2026-09-26/27 — bounded OLQ diagnosis and generator foundation split

### DONE / CURRENT
- Merged CORE generator identity/name-policy foundation and proved post-merge CI/TIA baseline remained green.
- Merged privacy-safe OLQ failure projection plus bounded prerequisite-tag harness/classifier.
- Trusted qualification run #320 / `36273653086` still returned the same safe `tags:unknown` RetrieveWithUpgrade wrapper evidence. #61 remains blocked; no specific product/package/library prerequisite is proven.
- OUTPUT foundation candidate PR #102 is green at exact head `cc370d6d19c65c483837e1a8358dc9f931c2dc87` and awaits fresh isolated secondary review.
- Handoff audit found main state snapshots materially stale and the old handoff-protocol PR #39 unmerged. A fresh docs-only handoff candidate is prepared on a separate branch so the active PR #102 exact-SHA gate is not disturbed.

## 2026-09-29 — GEN OUTPUT merge, TIA PASS and handoff local-context hardening

### DONE
- PR #102 `GEN-VALVE-FOUNDATION-OUTPUT-001` merged as `920c4e1a276c77af95288f42da94ddfa3ce414d3` after bounded repair round 1 and fresh exact-SHA secondary APPROVE.
- Post-merge CI #500 / `36547750103` succeeded: net10 106/106, net48 test assembly compile, Motor generation.
- Trusted-main TIA V21 End-to-End #16 / `36547750239` succeeded with `success=true`, `state=Success`, 0 errors / 0 warnings; `UDT_Motor (UDT)` updated and compile completed successfully.
- Handoff PR #103 now persists operator-local Git/Windows context needed for exceptional human-only Git/SSH steps: canonical clone path, origin/main, PowerShell usage, Git identity, SSH signing mode/current dedicated v2 key path + public fingerprint, old-key disqualification and private-key/passphrase non-disclosure rules.
- Local context is explicitly convenience only; GitHub remains authority. Evidence branches must never merge into main.

### CURRENT
- PR #103 remains the active `CHAT-HANDOFF-001` docs/process candidate and requires fresh isolated `chatgpt-secondary` review before merge.
- #61 Open Library qualification remains BLOCKED/unknown.
