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