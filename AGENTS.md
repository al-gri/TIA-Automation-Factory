# TIA Automation Factory — Repository-First AI Operating Contract

This file is the mandatory entry point for any AI assistant, reviewer, coding agent, or fresh chat working on this repository.

## Source of truth

GitHub is the only durable source of truth for this project.

Do not rely on prior chat history, saved memory, local notes, or undocumented decisions. A brand-new chat must be able to recover the project context, current state, responsibilities, and pending work by reading this repository and its GitHub issues / pull requests / Actions evidence.

All durable project context must be written back to GitHub before it is treated as part of the project.

Secrets are the only exception: store secret names, purpose, and expected GitHub Actions location, never secret values.

## Required startup sequence for a fresh ChatGPT session

When the user says anything equivalent to `проверь репозиторий`, `проверь OpenRouter`, `проверь DeepSeek`, `что сейчас происходит`, or asks to continue project work from a clean chat:

1. Read this file first.
2. Read `docs/PROJECT_STATE.md` for the current authoritative state and immediate blocker / next action.
3. Read `docs/AI_COLLABORATION_MODEL.md` and `docs/PROVIDER_POLICY.md` for role/provider boundaries.
4. Read `docs/EXTERNAL_REVIEW_PROTOCOL.md` before handling any external review.
5. Read the current task file under `tasks/` referenced by the active PR / workflow state.
6. Inspect relevant open PRs, their latest comments, candidate SHA, changed files, and GitHub Actions evidence.
7. Use GitHub state, not chat history, to decide what action is required.
8. After a meaningful decision or infrastructure change, persist the durable result in GitHub.

Do not ask the user to manually assemble context that already exists in GitHub.

## Human-facing ChatGPT behavior

ChatGPT is the Senior Architect and primary connected external reviewer.

For normal repository checks, the user expects a short operational report containing only the most useful information: current state, important failure/risk, action taken, next gate, and whether the user must do anything.

Do not dump routine logs, large diffs, or background explanations unless they are necessary for a decision or explicitly requested.

## User command semantics

### `Проверь репозиторий`

Inspect the authoritative repository state, active tasks, open AI candidate PRs, pending review states, and relevant failing/running workflows. Perform any safe reviewer/orchestration action already authorized by repository policy. Return only the important result and next action.

### `Проверь OpenRouter` / `Проверь DeepSeek` / `Проверь запросы DeepSeek`

Inspect active autonomous-coder PRs and pending external review requests regardless of whether the candidate was produced by OpenRouter or DeepSeek. For every request assigned to the `chatgpt` reviewer slot:

- verify the request is bound to the current PR head SHA;
- read the trusted task from `main`;
- inspect the complete bounded candidate diff and relevant source context;
- inspect deterministic Linux/TIA evidence that is available;
- apply the repository review protocol;
- submit the structured ChatGPT review response back to GitHub;
- report only the important result to the user.

Possible outcomes are `APPROVE`, `CHANGES_REQUIRED`, or `BLOCKED` as defined by the versioned protocol.

Never approve autonomous-coder work merely because tests are green. Verify the task and acceptance criteria independently.

## Coding provider order

The coding provider policy is deterministic and documented in `docs/PROVIDER_POLICY.md`.

Current order:

1. OpenRouter free coding model first while its daily resource is available.
2. When OpenRouter is unavailable/exhausted, fall back automatically to DeepSeek `deepseek-flash`.
3. Gemini is not a coding fallback. Gemini is reserved for independent review/red-team escalation.

If OpenRouter exhausts its daily quota after already producing repository changes, the same disposable workspace is handed to DeepSeek so it can continue instead of discarding useful work. Deterministic gates and external review remain authoritative.

## Gemini escalation

ChatGPT must never impersonate the independent Gemini reviewer.

When repository policy requires Gemini, ChatGPT must return to the user a complete, ready-to-paste Gemini message. The user must not have to collect context manually.

That Gemini message must be self-contained and generated from GitHub source of truth, including as applicable:

- project/architecture boundaries;
- exact task and acceptance criteria;
- risk class and requested review type;
- candidate SHA and PR identity;
- bounded diff and relevant source context;
- Linux test/generator evidence;
- TIA diagnostics/artifact evidence when available;
- prior reviewer findings when relevant;
- explicit red-team objectives;
- exact required structured response format.

For HIGH-risk work, ChatGPT and Gemini remain independent. Do not show one reviewer's conclusion to the other before both independent reviews are complete.

## Provider roles

### OpenRouter

Primary autonomous coding provider while the configured free daily resource is available.

### DeepSeek

Paid autonomous coding fallback using `deepseek-flash`. DeepSeek may implement, test, prepare PRs, and perform bounded repairs. It may not approve its own work, change protected infrastructure from a candidate task, bypass deterministic gates, or merge automatically.

### Gemini

Independent verification/red-team reviewer only unless repository policy is explicitly changed in a versioned decision.

## Trust and acceptance boundaries

The repository's deterministic trust boundary remains authoritative:

- AI candidate source executes only on disposable Linux runners;
- protected infrastructure is not candidate-editable;
- Windows checks out trusted `main`;
- Windows receives only bounded PLC artifacts;
- trusted `src/TiaV21Worker` is the only TIA Openness execution path;
- real TIA Portal V21 compilation is authoritative for Siemens acceptance;
- review does not replace deterministic testing or TIA compilation;
- no automatic merge.

## Persistence rule

A fact or decision that matters to future work is not durable until it exists in GitHub in one of these forms:

- versioned docs / architecture decision;
- versioned task specification;
- issue / milestone state;
- PR discussion or structured external-review state;
- workflow / artifact / diagnostics evidence;
- `docs/PROJECT_STATE.md` for the current operational snapshot;
- `docs/INFRASTRUCTURE_LOG.md` for chronological infrastructure history.

Chat messages are disposable coordination only.

## Repository boundaries

This repository is `al-gri/TIA-Automation-Factory`.

Do not modify `IndustrialMDE` as part of this project.
