# Methodology Journal

Curated chronological diary of lessons learned while developing both TIA Automation Factory and the AI-assisted development methodology itself. Raw automated telemetry is recorded in GitHub issue #25; this file records milestone-level interpretation.

## 2026-09-17 — Establish the trusted execution split

### Evidence
Disposable Linux runners were sufficient for candidate generation/tests, while real TIA Portal V21 execution required a self-hosted Windows machine with Siemens Openness prerequisites. The Linux -> trusted Windows/TIA path was proven end-to-end.

### Lessons
Candidate source and trusted vendor tooling must be separated physically/logically; protected-path enforcement is required; green Linux tests do not prove Siemens/TIA acceptance.

### Methodology effect
Introduced deterministic gates, trusted Windows boundary, task acceptance split and independent review.

## 2026-09-17 — Repair must be bounded and observable

### Evidence
An intentionally incomplete candidate was rejected, repaired on the same PR and revalidated.

### Lessons
Same-PR repair preserves evidence; repair budgets must be trusted-task data; failed validation should enter repair context.

### Methodology effect
Adopted bounded same-PR repair with visible blocked state after exhaustion.

## 2026-09-18 — Provider continuity is useful only when acceptance is provider-independent

### Evidence
OpenRouter quota behavior and official DeepSeek `deepseek-flash` both executed bounded tasks; useful workspace changes could exist before provider failure.

### Lessons
Fallback should preserve task identity/workspace where safe; provider success is never acceptance; provider/model/usage/fallback telemetry matters.

### Methodology effect
Adopted OpenRouter -> DeepSeek coding cascade and provider audit.

## 2026-09-18 — Repository-first context removes chat dependency

### Evidence
Fresh connected sessions recovered tasks, reviews and project state from GitHub rather than prior chats.

### Lessons
Chat is a poor project database; roles become transferable when architecture/state/tasks/reviews are versioned; durable decisions must return to GitHub.

### Methodology effect
Adopted repository-first operating contracts and GitHub as sole durable source of truth.

## 2026-09-18 — Exact-SHA review is non-negotiable

### Evidence
Multiple repairs changed candidate SHAs after earlier evidence; semantic reviews found defects not caught by Linux CI.

### Lessons
Approval belongs to an immutable candidate; any candidate-changing repair invalidates prior review.

### Methodology effect
Every candidate-changing repair returns to fresh exact-SHA external review.

## 2026-09-18 — Reviewer independence must follow authorship, not model branding

### Evidence
Primary ChatGPT became material co-author during repair; Gemini availability was unreliable.

### Lessons
A session must not independently approve its own material work; independence is authorship/evidence separation; a fresh isolated secondary ChatGPT is a practical replacement reviewer.

### Methodology effect
Introduced `chatgpt-secondary` and one-independent-reviewer-by-default governance.

## 2026-09-18 — Governance defects are software defects

### Evidence
Independent review found fail-open reviewer authorization, stale approvals, hidden model stages, legacy validation bypasses and non-main trusted Windows paths.

### Lessons
Workflow state machines need production-code rigor; trusted Windows needs repository-wide main-only invariants.

### Methodology effect
Added fail-closed reviewer authorization, deterministic candidate validation, repair/review transitions and main-only TIA guards.

## 2026-09-18 — Coding context should be explicit, bounded and reproducible

### Evidence
Important repository rules were initially merely discoverable instead of intentionally assembled into provider context.

### Lessons
Large context windows do not guarantee the right files are read; trusted context should be assembled before provider selection; vendor interfaces should enter through versioned qualified contracts.

### Methodology effect
Added trusted context bundling, hashes and task `contextFiles`.

## 2026-09-18 — Methodology became a first-class artifact

The project explicitly treats the reusable development methodology as a second product. `docs/DEVELOPMENT_METHODOLOGY.md` carries curated rules, this journal carries milestone lessons, and issue #25 carries raw automated telemetry. Methodology checkpoints are mandatory orchestration work.

## 2026-09-18 — Prompt content must never become control-plane authority

### Evidence
A stale historical finding was re-tested against then-current source and confirmed that mixed model-visible prompt content could still influence trusted task/context metadata.

### Lessons
Stale verdicts and defect hypotheses are different; prompt text may contain untrusted quoted content; authority must attach to structured trusted fields, not the enclosing text blob; path declarations must fail closed.

### Methodology effect
Introduced M-013 and GOV-CTX-001 control-plane/data-plane separation.

## 2026-09-18/19 — Governance needs explicit positive human provenance

### Evidence
Governance bootstrap review showed that candidate/self-authorized tasks, owner attribution and `performed_via_github_app == null` were insufficient as root authority. Successive reviews required semantic bootstrap eligibility, complete frozen scope, invocation-generic schemas, stage-specific gates and positive SSH-signed human provenance. PR #32 ultimately merged.

### Lessons
A fail-closed system still needs an explicit root-authority lane for repairing its own authorization mechanism. Positive authentication requires a capability unavailable to the conflicted actor. Permanent schemas describe relationships, not one incident's identifiers. Fail-closed gates apply when evidence can exist; requiring future-stage evidence early can deadlock the state machine.

### Methodology effect
Introduced M-014 and the bounded SSH-signed governance bootstrap protocol.

## 2026-09-19 — Windows security/API overload semantics require operation-level acceptance

### Evidence
TIA-AUTH produced two distinct post-merge Windows-only defects despite green Linux gates:

1. `RegistryAccessRule` was constructed through a string identity overload using SID text. Windows treated the string as an account name and failed with `Some or all identity references could not be translated`. Replacing the string with a typed `SecurityIdentifier` fixed rule construction.
2. After the narrow ACL was granted, a non-elevated probe could successfully call `OpenSubKey` with `SetValue | QueryValues`, yet the subsequent `SetValue` failed with `Cannot write to the registry key`. The worker used the .NET Framework rights-only `OpenSubKey` overload, which retained a non-writable `RegistryKey` state. Explicit `RegistryKeyPermissionCheck.ReadWriteSubTree` fixed the intended mutation without broadening native rights.

Trusted run `35458909671` after PR #55 then passed whitelist synchronization/Openness admission and reached the independent `RetrieveWithUpgrade` migration failure.

### Lessons
- Typed security identities are safer than overloads that reinterpret security identifiers as names.
- A successful resource/handle acquisition is not sufficient acceptance for a state-changing requirement; acceptance must exercise the intended mutation (`SetValue`, write, save, etc.) under the real non-elevated/trusted identity.
- Least-privilege tests must distinguish native access rights from higher-level framework object state/permission modes.
- Linux deterministic gates cannot replace post-merge platform acceptance for Windows/.NET Framework/TIA behavior.
- When a trusted workflow reaches a later independent phase, that phase progression is strong evidence that earlier trust-boundary gates were actually crossed, not merely mocked.

### Methodology effect
Treat this as a reusable rule candidate for future promotion: platform-bound permission/API tasks should include an operation-level acceptance probe under the exact intended identity/token, not only existence/open/constructor checks. Keep the probe bounded and least-privilege; never widen permissions merely to make a diagnostic pass.

## 2026-09-19 — Diagnose opaque vendor wrappers before repairing behavior

### Evidence
After TIA-AUTH was fixed, repeated trusted qualification reached the same Siemens wrapper failure: `phase:retrieve-with-upgrade type:EngineeringTargetInvocationException hresult:0x80131500`. OLQ-DIAG-001 correctly hid arbitrary raw Siemens text, but phase/type/HRESULT alone cannot distinguish product/version/content/archive/precondition causes. Full exception text remains runner-local and is deliberately removed by the public workflow.

### Lessons
- Do not implement speculative fallback APIs or migration workarounds from a generic vendor wrapper exception.
- Diagnostic enrichment should be a separate bounded task that preserves behavior and privacy.
- When vendor APIs expose structured reason/detail surfaces, derive fixed allowlisted categories and deterministic fingerprints rather than publishing arbitrary messages.
- Diagnostic evidence should be sufficient to select the next small task without turning logs into a vendor-payload or privacy leak.

### Methodology effect
Created issue #56 and trusted task `OLQ-DIAG-002` as a diagnostic-only gate before any migration behavior change.

## 2026-09-20/21 — Repair exhaustion is also a task-decomposition signal

### Evidence
`OLQ-VALVE-PROFILE-001` combined qualification transaction/reuse repair, V19->V21 migration truth, Valve contract/dependency discovery and a reference compile/save/reopen proof. PR #81 went through two bounded repairs and still ended with MAJOR defects at exact SHA `8101d1e197cf33eaf96874acb4a9933fb5256656`, including cross-file compile drift, missing required tests and incorrect qualification/reference lifecycle. The trusted repair budget was exhausted and the PR was closed unmerged.

A follow-up audit of the fresh coding-agent context showed that a new initial Autonomous Agent run receives the trusted task and baseline repository files, but not prior issue comments/review findings. It also showed that `docs/PROJECT_STATE.md`, one of the baseline context files, had become materially stale relative to live GitHub.

### Lessons
- Bounded repair exhaustion should trigger a decomposition review, not an automatic fresh rerun of the same large task.
- A fresh agent cannot be assumed to learn from prior review history unless those findings are deliberately represented in trusted task/context inputs.
- Large tasks that couple an uncertain external/vendor prerequisite with downstream feature semantics amplify repair drift. Split at the evidence boundary: first establish the prerequisite/transaction truth, then implement semantics that depend on it.
- Operational snapshot files included in every coding context must be maintained as real inputs, not treated as harmless documentation debt.
- Closing an exhausted candidate while preserving its exact-SHA evidence reduces accidental-merge risk without weakening the human decision gate for further candidate-changing work.

### Methodology effect
For #61, replacement planning now separates qualification transaction/native-V21-open evidence from Valve contract/reference work. No new candidate is authorized until the explicit repair-budget decision is made. `PROJECT_STATE`/handoff/log are refreshed as coding-context inputs, and future replacement tasks should be smaller and self-contained enough that a fresh agent does not depend on hidden review history.
