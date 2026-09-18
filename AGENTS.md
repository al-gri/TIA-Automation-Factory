# TIA Automation Factory — Repository-First AI Operating Contract

This file is the mandatory entry point for any AI assistant, reviewer, coding agent, or fresh chat working on this repository.

## Source of truth

GitHub is the only durable source of truth for this project.

Do not rely on prior chat history, saved memory, local notes, or undocumented decisions. A brand-new chat must be able to recover the project context, current state, responsibilities, pending work, provider policy, and review obligations by reading this repository and its GitHub issues / pull requests / Actions evidence.

All durable project context must be written back to GitHub before it is treated as part of the project. Chat is an operator console only.

Secrets are the only exception: store secret names, purpose, and expected location, never secret values.

## Required startup sequence for a fresh ChatGPT session

When the user says anything equivalent to "проверь репозиторий", "проверь DeepSeek", "что сейчас происходит", or asks to continue project work from a clean chat:

1. Read this file first.
2. Read `docs/PROJECT_STATE.md` for the current authoritative state and immediate blocker / next action.
3. Read `docs/AI_COLLABORATION_MODEL.md` for role boundaries and provider policy.
4. Read `docs/EXTERNAL_REVIEW_PROTOCOL.md` before handling any external review.
5. Read the current task file under `tasks/` referenced by the active PR / workflow state.
6. Inspect relevant open PRs, their latest comments, candidate SHA, changed files, and GitHub Actions evidence.
7. Use GitHub state, not chat history, to decide what action is required.
8. After a meaningful decision or infrastructure change, persist the durable result in GitHub.

Do not ask the user to manually assemble context that already exists in GitHub.

## ChatGPT role

ChatGPT is the Senior Architect and primary connected external reviewer.

For normal repository checks, the user expects a short operational report containing only the most useful information: current state, important failure / risk, action taken, and whether the user must do anything.

Do not dump logs, long diffs, or background explanations unless they are needed for a decision or explicitly requested.

## User command semantics

### "Проверь репозиторий"

Inspect the authoritative repository state, active tasks, open AI candidate PRs, pending review states, and relevant failing / running workflows. Perform any safe reviewer / orchestration action that is already authorized by repository policy. Return only the important result and next action.

### "Проверь DeepSeek" / "Проверь запросы DeepSeek"

Find active coding-agent candidate PRs and pending external review requests. For every request assigned to the `chatgpt` reviewer slot:

- verify the review request is bound to the current PR head SHA;
- read the trusted task from `main`;
- inspect the complete bounded candidate diff and relevant source context;
- inspect deterministic Linux / TIA evidence that is available;
- apply the repository review protocol;
- submit the structured ChatGPT review response back to GitHub;
- report only the important result to the user.

Possible outcomes are `APPROVE`, `CHANGES_REQUIRED`, or `BLOCKED` as defined by the versioned protocol.

Never approve AI-authored work merely because tests are green. Verify the task and acceptance criteria independently.

## Coding provider policy

Routine coding uses a cost-first bounded cascade:

1. **OpenRouter first** using the configured free coding model while its daily allowance is available.
2. **DeepSeek second** using the official API model `deepseek-flash` when OpenRouter is unavailable, rate-limited, or daily quota is exhausted.
3. If OpenRouter exhausts its allowance after already changing the disposable workspace, DeepSeek continues from that partial candidate instead of throwing away useful work.
4. If the paid DeepSeek fallback also fails late after producing real repository changes, deterministic acceptance may evaluate the preserved candidate; provider success alone is never the DONE criterion.
5. **Gemini is not a routine coding fallback.** It is reserved for independent review / red-team escalation so it remains independent from the implementer path.

Provider selection, model, token/cost usage, fallback reason, and outcome must be recorded in the provider audit attached to the candidate PR / workflow evidence.

## Gemini escalation

ChatGPT must never impersonate the independent Gemini reviewer.

When repository policy requires Gemini, ChatGPT must return to the user a complete, ready-to-paste Gemini message. The user must not have to collect context manually.

That Gemini message must be self-contained and generated from GitHub source of truth, including as applicable:

- project / architecture boundaries;
- exact task and acceptance criteria;
- risk class and requested review type;
- candidate SHA and PR identity;
- bounded diff and relevant source context;
- Linux test / generator evidence;
- TIA diagnostics / artifact evidence when available;
- prior reviewer findings when relevant;
- explicit red-team objectives;
- exact required structured response format.

For HIGH-risk work, ChatGPT and Gemini remain independent. Do not show one reviewer's conclusion to the other before both independent reviews are complete.

## Coding agent role

The coding agent may implement, test, prepare PRs, and perform bounded repairs through the OpenRouter -> DeepSeek provider cascade. It may not approve its own work, change protected infrastructure from a candidate task, bypass deterministic gates, or merge automatically.

DeepSeek `deepseek-flash` is the paid fallback and continuity provider when the OpenRouter daily allowance is exhausted.

## Cline / interactive agent policy

Cline or a similar editor/terminal agent may be used as an **optional human-in-the-loop development interface**, not as a new source of truth and not as a replacement for the trusted GitHub pipeline.

If used, it must:

- start from this repository and read `AGENTS.md`, `docs/PROJECT_STATE.md`, the active `tasks/*.json`, and relevant architecture docs;
- work on a branch, never directly on `main`;
- obey the same protected paths, review requirements, and deterministic tests as the cloud coding agent;
- never receive unrestricted authority over the trusted Windows/TIA machine;
- never bypass external review or TIA acceptance;
- write durable decisions/results back to GitHub.

Cline is most useful for interactive prototyping, local debugging, and fast edit/test loops when a human is actively supervising. The autonomous production path remains GitHub Actions + bounded coding providers + external review + deterministic TIA acceptance.

## Trust and acceptance boundaries

The repository's existing deterministic trust boundary remains authoritative:

- AI candidate source executes only on disposable Linux runners in the autonomous path;
- protected infrastructure is not candidate-editable;
- Windows checks out trusted `main`;
- Windows receives only bounded PLC artifacts;
- trusted `src/TiaV21Worker` is the only TIA Openness execution path;
- real TIA Portal V21 compilation is authoritative for Siemens acceptance;
- review does not replace deterministic testing or TIA compilation;
- no automatic merge.

## Persistence rule

A fact or decision that matters to future work is not considered durable until it exists in GitHub in one of these forms:

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
