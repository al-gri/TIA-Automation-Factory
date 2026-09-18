# Methodology Journal

Curated chronological diary of lessons learned while developing both TIA Automation Factory and the AI-assisted development methodology itself.

Raw automated telemetry is recorded in GitHub issue #25. This file records milestone-level interpretation, not every workflow event.

## 2026-09-17 — Establish the trusted execution split

### Evidence

- Disposable Linux runners were sufficient for candidate generation/tests.
- Real TIA Portal V21 execution required a self-hosted Windows machine with Siemens Openness prerequisites.
- Initial autonomous workflows and real TIA smoke proved the complete Linux -> artifact -> trusted Windows/TIA path.

### Lessons

- Candidate source and trusted vendor tooling must be separated physically and logically.
- Protected-path enforcement is required before an AI agent is allowed to publish a branch.
- A green Linux test suite does not prove that Siemens/TIA acceptance or every task requirement is satisfied.

### Methodology effect

Introduced explicit deterministic gates, trusted Windows boundary, task acceptance split and independent review.

## 2026-09-17 — Repair must be bounded and observable

### Evidence

I5 intentionally seeded an incomplete candidate, observed rejection, performed bounded repair on the same PR and revalidated successfully.

### Lessons

- Repairing the same PR preserves evidence and avoids branch proliferation.
- Repair limits must be trusted-task data, not agent discretion.
- Failed validation evidence should be fed back into repair context.

### Methodology effect

Adopted bounded same-PR repair with explicit attempt counters and blocked state after exhaustion.

## 2026-09-18 — Provider continuity is useful only when acceptance is provider-independent

### Evidence

OpenRouter quota behavior and a successful official DeepSeek `deepseek-flash` smoke demonstrated that multiple coding providers can execute the same bounded task. A late OpenRouter quota event showed that useful workspace changes may exist before provider failure.

### Lessons

- Provider fallback should preserve task identity and, where safe, useful partial workspace changes.
- Model/provider success must never be treated as acceptance.
- Provider/model/tokens/cache/cost/fallback reason are useful methodology telemetry.

### Methodology effect

Adopted OpenRouter -> DeepSeek coding cascade and provider audit.

## 2026-09-18 — Repository-first context removes chat dependency

### Evidence

Fresh connected ChatGPT sessions successfully recovered tasks, review requests and project state from GitHub rather than old chats.

### Lessons

- Chat history is a poor project database.
- AI roles become transferable when architecture, current state, handoff, task and review protocols are versioned.
- Durable decisions must be written back to GitHub immediately enough that a fresh session can continue.

### Methodology effect

Adopted `AGENTS.md`, `PROJECT_STATE.md`, `NEXT_CHAT_HANDOFF.md`, collaboration/review protocols and GitHub as sole durable source of truth.

## 2026-09-18 — Exact-SHA review is non-negotiable

### Evidence

During OLQ-001 and governance work, multiple repairs changed candidate SHAs after valid earlier evidence. Review rounds repeatedly found defects that deterministic Linux CI could not detect.

### Lessons

- Approval is a property of an immutable candidate, not of a PR name.
- Any source-changing repair invalidates prior review.
- Fresh review must precede renewed trusted Candidate Validation.

### Methodology effect

The state machine now returns every candidate-changing repair to a fresh external-review gate before deterministic Candidate Validation may resume.

## 2026-09-18 — Reviewer independence must follow authorship, not model branding

### Evidence

Primary ChatGPT became a material co-author of PR #22 after repair budget exhaustion. The original trusted task still authorized only the primary reviewer, exposing a governance conflict. Gemini review access was also operationally unreliable.

### Lessons

- The same AI session must not independently approve code it materially authored.
- Independence should be enforced from Git authorship/evidence separation.
- A second isolated ChatGPT session can serve as reviewer when the primary session is conflicted.
- `reviewerSlots` must be an authorization set, not a command to run every reviewer.

### Methodology effect

Introduced `chatgpt-secondary`, authorship-derived reviewer policy and one-independent-reviewer-by-default governance.

## 2026-09-18 — Governance defects are software defects

### Evidence

Independent review rounds F001-F010 found defects in reviewer eligibility, task authorization ordering, stale approval propagation, hidden Gemini reviewer runtime, a legacy Candidate Validation bypass, and manual Windows/TIA workflows capable of checking out non-main refs.

### Lessons

- Workflow state machines require the same review rigor as production code.
- Fail-open authorization is a major defect even when CI is green.
- Legacy smoke workflows can become security bypasses after architecture evolves.
- Trusted Windows workflows need repository-wide main-only invariants, not informal operator discipline.

### Methodology effect

Added repository-wide workflow regressions, fail-closed reviewer authorization, deterministic Candidate Validation, fresh-review repair transitions and main-only self-hosted TIA guards.

## 2026-09-18 — Coding context should be explicit, bounded and reproducible

### Evidence

The coding workflow originally appended only the task JSON to `coder.md`, while important repository rules/state were merely available for the model to discover. This was adequate for small tasks but fragile for Siemens/Open Library work.

### Lessons

- Large model context windows do not guarantee the model reads the right files.
- Trusted context should be assembled before provider selection.
- Source code should normally remain workspace-readable instead of being dumped wholesale into prompts.
- Vendor-specific interfaces should enter coding context through versioned qualified contracts, not model inference.

### Methodology effect

Added `build-coder-context.py`, baseline repository context, task `contextFiles`, context hashes/manifest and shared OpenRouter/DeepSeek enriched prompts.

## 2026-09-18 — Methodology is now a first-class project artifact

### Decision

The project explicitly treats development methodology as a second product alongside the PLC generator.

### Mechanism

- `docs/DEVELOPMENT_METHODOLOGY.md` contains curated reusable rules and active experiments.
- This journal records milestone-level lessons.
- Issue #25 is the append-only automated raw event diary.
- A trusted telemetry workflow records meaningful workflow completions and PR lifecycle events automatically.
- `AGENTS.md` requires primary ChatGPT to perform methodology checkpoints without waiting for a user reminder.

### Expected future use

After enough real generator tasks, review the accumulated evidence and extract a stable playbook covering task design, context construction, provider routing, review independence, repair budgets, deterministic gates, trusted-machine boundaries and documentation discipline.

## 2026-09-18 — Prompt content must never become control-plane authority

### Evidence

A historical independent review payload for PR #24 identified F011/F012 on candidate `450dca6608f0526d00370595fc5a928f7bbfbd71`. The payload itself was stale for merge purposes, but a fresh inspection of current `main` confirmed that `build-coder-context.py` still parsed task-looking headings from mixed `--prompt-input` content and used the parsed `contextFiles` as trusted authority. Current `validate_repo_path()` also silently canonicalized some malformed spellings. The defect had therefore survived the final #24 merge and downstream OLQ work had already begun.

### Lessons

- Exact-SHA staleness applies to verdicts, not to defect hypotheses: an old finding should be re-tested against current source before being discarded.
- A prompt assembled by a trusted workflow can still contain untrusted data such as issue bodies, diffs, review comments and logs.
- Trust provenance must attach to fields, not to the enclosing text blob.
- Control metadata such as task identity, `contextFiles`, permissions and reviewer policy must travel through a separate structured channel and be resolved from trusted state.
- Context-path normalization must fail closed; canonicalization after input is not equivalent to requiring canonical input.
- A final independent review can miss a previously unpersisted finding if review evidence is not durably bound into the repository workflow.

### Methodology effect

Introduced M-013: keep control-plane authority separate from mixed prompt content. GOV-CTX-001 / issue #28 repairs task authority through structured event metadata plus trusted Git lookup, adds prompt-spoof and canonical-path regressions, and blocks downstream PR #27 until this trust-origin repair passes exact-SHA CI and independent `chatgpt-secondary` review.

## 2026-09-18 — Governance needs an explicit bootstrap path

### Evidence

PR #30 repaired the live GOV-CTX-001 trust-origin defect and passed exact-SHA CI #224 plus fresh independent `chatgpt-secondary` APPROVE, but current governance also required a trusted task on `main` to authorize `chatgpt-secondary`. No `tasks/GOV-CTX-001.json` existed. Adding one inside the same candidate would have been self-authorization; creating a separate protected task PR raised the same recursive authorization question.

The human operator granted a one-time exact-SHA waiver for PR #30 and then explicitly directed the project to eliminate the bootstrap ambiguity under issue #31. PR #30 merged at `0260117391abf5f0a8375699dca12caa06bafb8b`. Issue #31 was used to define a bounded bootstrap scope before the permanent policy candidate was authored.

### Lessons

- A fail-closed task/review system still needs an explicit root-of-authority path for repairing its own authorization mechanism.
- The candidate must never solve recursion by authorizing itself.
- Human strategic authority is appropriate at the bootstrap boundary, but scope and provenance must be independently auditable from GitHub.
- Normal automation should remain task-only and fail-closed; exceptional governance authorization is safer as a visibly manual lane than as an implicit fallback.
- Once the bounded scope is validly human-authorized, exact-SHA CI and an independent reviewer can gate the implementation without requiring a second routine merge confirmation.

### Methodology effect

Introduced M-014 and `docs/GOVERNANCE_BOOTSTRAP.md`: rare governance-authority recursion uses a frozen issue-body fingerprint, HIGH-risk exact-SHA CI and fresh `chatgpt-secondary` review while ordinary task-based automation remains unchanged and fail-closed.

## 2026-09-18 — Authority provenance must be separate from the conflicted actor

### Evidence

The first independent review of PR #32 at candidate `2d69e0bba51bb5de2672aa7f453bbe812575f0e6` returned `CHANGES_REQUIRED` with four major findings:

- F001: connector-authored comments could claim human authorization without proving that the human actually created the authority artifact;
- F002: the live authorization issue did not contain the mandatory bounded authorization fields inside its fingerprinted body;
- F003: bootstrap eligibility incorrectly depended on coding-agent protected paths even though the candidate changed governance docs outside that list;
- F004: primary-persisted secondary JSON proved response contents but not reviewer-evidence provenance.

GitHub API metadata confirmed that the existing authorization comments were created through `chatgpt-codex-connector`, so they could not serve as origin-verifiable human authority.

### Lessons

- A trusted actor identity in the rendered GitHub UI is insufficient when an app can act as that account; provenance metadata matters.
- Human root authorization for an exceptional lane must be represented by an action the conflicted primary cannot create, not by prose saying that the human approved something.
- Bootstrap eligibility should describe the semantic authority recursion, not accidentally mirror one implementation's protected-path list.
- Review identity fields and exact SHA authenticate *what* a verdict refers to, but not *who caused it to enter the trusted record*.
- Conservative findings may be acted on even when relayed through primary, but an authority-bearing APPROVE requires provenance separated from the candidate author.

### Methodology effect

M-014 and the bootstrap protocol now require direct non-app-mediated human GitHub authorization bound to the frozen issue-body hash, plus provenance-separated direct human relay/attestation of any authority-bearing `chatgpt-secondary` APPROVE. Normal task-only automation remains unchanged and fail-closed.
