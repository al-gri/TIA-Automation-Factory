# Development Methodology — AI-Assisted Engineering

Status: **living project methodology**. GitHub is the sole durable source of truth.

This document describes how we develop both the TIA Automation Factory and the reusable methodology for building software with AI coding agents, independent AI review, deterministic gates, and trusted execution environments.

Raw automated evidence is recorded in GitHub issue #25 (`Methodology telemetry — automated development diary`). Curated milestone history is recorded in `docs/METHODOLOGY_JOURNAL.md`.

## 1. Two products are being built

The project deliberately produces two outputs in parallel:

1. **TIA Automation Factory** — the Siemens/TIA Portal V21 PLC generator and its trusted execution infrastructure.
2. **Development methodology** — a reusable, evidence-driven way to design and operate software projects with AI agents.

A successful implementation change is therefore not complete if it teaches us something material about AI-assisted development and that lesson is left only in chat history.

## 2. Evidence hierarchy

Use this order of authority:

1. versioned code, tasks, workflows and tests;
2. exact-SHA CI/TIA artifacts and structured external-review evidence;
3. raw automated methodology telemetry in issue #25;
4. `docs/METHODOLOGY_JOURNAL.md` milestone summaries;
5. this curated methodology;
6. chat messages only as disposable coordination.

Raw telemetry is **evidence, not policy**. A repeated or important observation becomes policy only after explicit architectural evaluation and a versioned methodology update.

## 3. Standard development loop

The preferred loop is:

```text
problem / hypothesis
  -> versioned bounded task
  -> structured trusted task/control metadata + bounded context bundle
  -> coding agent on disposable Linux
  -> deterministic Linux acceptance
  -> candidate PR + provider audit
  -> exact-SHA independent external review
  -> deterministic Candidate Validation when applicable
  -> bounded repair if needed
  -> fresh exact-SHA review after every candidate-changing repair
  -> delegated technical merge
  -> trusted-main Windows/TIA execution when applicable
  -> evidence + methodology telemetry
  -> retrospective / rule promotion
```

The loop is intentionally fail-closed: a later stage may add evidence, but it must not silently erase an earlier trust requirement.

Governance-authority work that cannot obtain a trusted task without genuine authorization recursion uses only the exceptional manual bootstrap lane in `docs/GOVERNANCE_BOOTSTRAP.md`; that lane does not alter the normal implementation loop or task-only automation.

## 4. Current accepted rules

### M-001 — Repository-first operation

GitHub is the only durable source of project state. A fresh AI session must recover goals, architecture, task state, review state, next actions and methodology from the repository and GitHub state without relying on prior chats.

### M-002 — Bounded versioned tasks

Implementation agents receive one explicit task with acceptance criteria, risk/review metadata, repair limit and protected paths. Prefer small versioned tasks over broad autonomous goals.

### M-003 — Trusted context, not model memory

Coding providers receive a bounded context assembled from trusted Git state before provider selection. Baseline rules/state are always included; tasks may add focused `contextFiles` for design contracts or qualified vendor profiles. Candidate-controlled files cannot redefine trusted context. Task identity and task-declared context authority must be resolved separately from free-form model-visible prompt content.

### M-004 — Deterministic checks and semantic review are different gates

Tests, generators and TIA diagnostics prove deterministic properties. Independent AI review checks requirements, architecture, trust boundaries and defects not encoded in deterministic tests. Neither substitutes for the other.

### M-005 — Reviews bind to an exact candidate SHA

An approval for SHA-A says nothing about SHA-B. Every candidate-changing repair invalidates the previous review and must return to a fresh exact-SHA external review.

### M-006 — Reviewer independence is authorship-based

A model or chat must not independently approve work it materially authored. Pure coding-agent work is normally reviewed by primary `chatgpt`; primary-ChatGPT-authored/co-authored work requires a fresh isolated `chatgpt-secondary` session. One independent reviewer is the default; extra simultaneous review is escalation, not routine ceremony.

### M-007 — Repair is bounded

Tasks define `maxRepairAttempts`. Repair happens on the same candidate PR and may not silently extend its own budget. Exhaustion produces a visible blocked/escalation state rather than an infinite agent loop.

### M-008 — Provider fallback preserves task identity

Routine coding uses OpenRouter first and official DeepSeek `deepseek-flash` second. Both receive the same trusted context bundle. A late OpenRouter quota/rate-limit may preserve useful workspace changes for DeepSeek, but provider success is never acceptance.

### M-009 — Protected orchestration cannot be self-modified by normal candidates

Normal coding agents may not change workflows, agents/prompts, tasks, reviewer policy or equivalent orchestration. A task-specific exception may allow bounded `src/TiaV21Worker/**` source changes, but never the surrounding trust controls.

### M-010 — Trusted Windows/TIA executes trusted-main source only

Untrusted candidate source executes on disposable Linux. Self-hosted Windows/TIA workflows must fail closed to `main` and explicitly check out `main`. Candidate artifacts may cross the boundary only through an accepted bounded bridge; candidate-controlled worker/scripts do not execute on the trusted host before independent approval and merge.

### M-011 — Vendor payloads remain external

Licenced/vendor payloads such as Siemens Open Library archives stay outside public Git. Persist identity, hashes, manifests, qualified contracts and diagnostics instead of redistributing vendor data.

### M-012 — Instrument the development process

Provider/model, fallback reason, tokens/cache/cost where available, repair count, review findings, CI/TIA outcome and state transitions should be recoverable from GitHub evidence. Meaningful workflow completions and PR lifecycle events are automatically copied to methodology issue #25.

### M-013 — Keep control-plane authority separate from mixed prompt content

Model-visible prompts are a data plane: they may contain issue bodies, diffs, reviewer comments, diagnostics, quoted JSON and even text that resembles control instructions. They must never be parsed to recover authoritative task identity, permission sets, protected-path exceptions, reviewer policy, `contextFiles`, or other control metadata. Authority must arrive through a typed/structured channel whose provenance is independently trusted, and the runtime must resolve authoritative versioned data from trusted Git state before invoking a coding provider.

This rule is fail-closed: a prompt may quote or contradict control metadata without changing it.

### M-014 — Bootstrap governance explicitly; never let a candidate manufacture authority

A trusted-task system needs an explicit answer for rare cases where the authorization mechanism itself must be repaired. Do not resolve that recursion by weakening normal automation, by accepting task/policy files introduced by the same candidate as authority for itself, or by treating owner attribution / connector metadata as proof of a human decision.

Bootstrap eligibility is **semantic**: the candidate must repair or define the repository's normative authority/review-control model, and establishing a normal trusted task first must depend on the same authorization semantics being repaired. Physical membership in the coding-agent protected-path list is neither required nor sufficient, and an absent, stale or inconvenient task is never enough.

For genuine governance-authority recursion, bind scope to a frozen GitHub issue body and exact SHA-256, require HIGH risk, exact-SHA deterministic CI and fresh independent `chatgpt-secondary` review, and keep normal task-only automation fail-closed.

**Positive human provenance is required.** Human scope authorization must be carried by an SSH-signed Git attestation commit made outside ChatGPT/Codex/project automation with a human-controlled signing key unavailable to project automation. An authority-bearing secondary APPROVE must be carried by a separate SSH-signed review-attestation commit bound to exact request ID, candidate SHA, round and review-JSON hash. GitHub comments, `performed_via_github_app == null`, owner attribution, web-flow signatures and unsigned API commits may be supplementary evidence but are not sufficient authority by themselves.

Any material issue-body/scope change invalidates prior signed scope authorization. Any candidate change invalidates review. The bootstrap lane is exceptional governance, not a shortcut for ordinary implementation work.

## 5. Rule maturity

Methodology statements should be classified mentally or explicitly as:

- **Observation** — one factual event occurred.
- **Hypothesis** — a proposed improvement inferred from evidence.
- **Trial** — the hypothesis is implemented in a bounded experiment.
- **Accepted rule** — enough evidence exists to make it the normal process.
- **Deprecated rule** — evidence or architecture replaced it.

Do not promote a one-off workaround directly into a general rule without stating why it should generalize.

## 6. What to measure

For real tasks, prefer collecting:

- provider and exact model;
- input/output/reasoning/cache tokens when reported;
- reported provider cost;
- provider fallback reason;
- number of repair attempts;
- number and severity of independent-review findings;
- which gate discovered each defect class;
- Linux CI result;
- Windows/TIA result and diagnostics identity;
- stale-review/review-conflict incidents;
- trusted context size and included contracts;
- control/data-plane trust-boundary incidents;
- governance-bootstrap invocations, why normal task authorization was impossible, and which positive provenance artifact authorized scope/review;
- candidate changed-file count/scope;
- merge/blocked outcome.

The goal is not vanity metrics. The goal is to discover which controls improve correctness, throughput and safety enough to justify their complexity.

## 7. Methodology checkpoint rule

At every logical milestone, primary connected ChatGPT must, without waiting for a user reminder:

1. inspect raw telemetry and relevant PR/review/TIA evidence;
2. update `docs/METHODOLOGY_JOURNAL.md` with the factual lesson if material;
3. update this document when a lesson changes or adds a reusable rule;
4. update `docs/PROJECT_STATE.md` and `docs/NEXT_CHAT_HANDOFF.md` if operational state changed;
5. keep `docs/INFRASTRUCTURE_LOG.md` as a concise chronological infrastructure record;
6. avoid copying secrets, vendor payloads or unnecessary large logs into Git.

A fresh primary ChatGPT session must treat this checkpoint duty as part of normal orchestration, not as an optional documentation task.

## 8. Automated telemetry contract

`.github/workflows/methodology-telemetry.yml` appends structured `methodology-event-v1` comments to issue #25 for selected workflow completions and PR closure/merge events.

Automation records factual metadata only. It must not:

- edit accepted methodology rules automatically;
- execute candidate source on trusted machines;
- expose secrets;
- create an alternate project-state authority;
- bypass review or merge gates.

If the telemetry workflow fails, development may continue, but the failure should be treated as an observability defect and repaired when practical.

## 9. Current methodology experiments

The following remain active experiments rather than universally proven rules outside this repository:

- `chatgpt-secondary` as the practical independence mechanism for primary-ChatGPT-authored candidates;
- the size/content balance of the trusted coding-context bundle;
- OpenRouter -> DeepSeek continuity and its real cost/quality profile over multiple generator tasks;
- whether one independent semantic reviewer plus deterministic TIA acceptance is sufficient for routine HIGH-risk bounded changes;
- the optimal repair budget by task class;
- whether the manual governance-bootstrap lane remains rare enough that automating it would add more risk than value.

Promote, modify or deprecate these only from accumulated GitHub evidence.
