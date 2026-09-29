# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-29
Checkpoint trusted main: `920c4e1a276c77af95288f42da94ddfa3ce414d3`

GitHub is the only durable source of truth. Re-fetch live refs/tasks/PRs/reviews/Actions before acting; this file is a recovery aid, not a substitute for live evidence.

## Project and operating boundaries

Repository: `al-gri/TIA-Automation-Factory`.

Goal: convert a vendor-neutral automation model into deterministic Siemens PLC artifacts and eventually assemble/verify the selected vertical slice through trusted TIA Portal V21 Openness.

Architecture:

```text
Automation input
 -> Domain
 -> PlcCompiler / PLC IR
 -> SiemensBackend
 -> GeneratorCli
 -> deterministic PLC/source artifacts
 -> trusted TiaV21Worker on merged main
 -> TIA Portal V21
```

Domain/compiler stay vendor-neutral. `Siemens.Engineering` stays inside `src/TiaV21Worker`. Candidate source executes on disposable Linux; Windows/TIA executes merged trusted main only. `IndustrialMDE` is out of scope.

## Generator foundation status

### CORE — complete

`GEN-VALVE-FOUNDATION-CORE-001` is merged and green. Trusted generator foundation includes:

- versioned unambiguous canonical parsed-model identity in PlcCompiler;
- SiemensBackend-only bounded emitted-name/symbol policy;
- Motor/Time compatibility and collision regressions.

### OUTPUT — merged and independently accepted

Trusted task: `tasks/GEN-VALVE-FOUNDATION-OUTPUT-001.json`.

PR #102 merged exact reviewed candidate `e4b0165fc86af24a147c10c8be7ee8d25422acdb` as `main@920c4e1a276c77af95288f42da94ddfa3ce414d3`.

Accepted behavior:

- deterministic generator manifest with stable generator/input/profile identities;
- exact artifact SHA-256 over emitted bytes;
- additive optional profile binding while preserving legacy two-argument Motor CLI;
- strict fail-closed AutomationType tokens;
- fail-closed duplicate semantic JSON properties;
- path-aware output containment including pre-existing symlink/reparse-point rejection;
- stable bounded diagnostics;
- no unqualified Valve facts.

Review/repair evidence:

- round 1 on `e7ecea464e4c003fd2e139e44030616d955ebe5e`: CHANGES_REQUIRED for F001 symlink/reparse containment and F002 duplicate semantic JSON properties;
- bounded repair round 1/2;
- CI #497 / `36536698825`: SUCCESS, net10 106/106 PASS, net48 assembly compile PASS, Motor generation PASS;
- round 2 request `ER-102-e4b0165fc86a-CODE_REVIEW-2-chatgpt-secondary-manual`: APPROVE with no findings;
- post-merge CI #500 / `36547750103`: SUCCESS, net10 106/106, net48 assembly PASS, Motor generation PASS.

Trusted-main TIA V21 End-to-End #16 / `36547750239`: **SUCCESS** on `main@920c4e1a276c77af95288f42da94ddfa3ce414d3`.

Observed trusted Windows/TIA evidence:
- generated `UDT_Motor.scl` from merged main;
- TiaV21Worker build PASS;
- TIA Portal V21 Openness import/update PASS;
- `UDT_Motor (UDT)`: successfully updated;
- block/project compile PASS;
- final diagnostics: `success=true`, `state=Success`, **0 errors / 0 warnings**.

This is post-merge Siemens acceptance for the generic Motor foundation path only; it does not qualify the blocked Open Library Valve contract in #61.

## Open Library qualification (#61)

Trusted qualification run #320 / `36273653086` on earlier trusted main failed with sanitized evidence:

`phase:retrieve-with-upgrade type:EngineeringTargetInvocationException hresult:0x80131500 tags:unknown detail-count:3 msgfp:eefb8fcdb933339b dtlfp:f85ef7ebe21aad89`

The classifier did not prove support-package/software-product/unsupported-library-element or another safe prerequisite category. #61 remains **BLOCKED / unknown**.

Do not speculate, change migration APIs, claim a qualified Valve profile, or trigger another qualification run without new reviewed evidence-backed authority.

## Future trusted Valve acceptance (#63)

Planning-only. Do not start implementation until #61 provides a reviewed qualified Valve contract/profile identity. The #62 deterministic foundation is now merged, so #61 is the remaining product-semantic prerequisite for the real Valve vertical slice.

## Governance/review state

PR #104 resolved the reviewer-slot authorization recursion through the exceptional SSH-signed governance-bootstrap lane and merged as `d814919304b43f58ac6d82045ac7051eb7b1226c`. Post-merge CI #493 succeeded.

Normal task-backed rules are restored:

- coding-agent-only candidates -> primary `chatgpt` when independent;
- primary-authored/co-authored candidates -> fresh isolated `chatgpt-secondary` when explicitly authorized by trusted task;
- approvals bind exact current SHA;
- repairs are bounded by trusted task;
- routine technical merge is delegated to primary when all exact gates pass.

## Handoff-process state

Trusted task `CHAT-HANDOFF-001` is active through PR #103.

Current docs candidate contains the repository-first handoff protocol plus persisted operator-local Git/Windows context, including:

- canonical clone `C:\Users\user\Documents\GitHub\TIA-Automation-Factory`;
- repository/origin/default branch;
- PowerShell workflow;
- Git identity;
- current human-only SSH signing-key path/fingerprint and old-key disqualification;
- private-key/passphrase non-disclosure rules;
- correct signed-evidence command order and GitHub re-verification requirements.

This local context is convenience only; GitHub remains authority.

PR #103 is primary-authored and requires fresh isolated `chatgpt-secondary` exact-SHA review before merge. It must be refreshed/reviewed only after its snapshot reflects the latest product/TIA state.

## Hard boundaries

- no unqualified Valve parameter/dependency/version/DB/ownership/target facts;
- no candidate source on trusted Windows/TIA;
- no vendor archive/project payload or raw vendor diagnostics in GitHub;
- no F-safety generation/validation;
- no broad UI/HMI/multi-vendor expansion before the vertical slice is stable;
- never modify `IndustrialMDE`.
