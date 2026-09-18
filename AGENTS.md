# TIA Automation Factory — Repository-First AI Operating Contract

This file is the mandatory entry point for any AI assistant, reviewer, coding agent, or fresh chat working on this repository.

## Source of truth

GitHub is the only durable source of truth for this project.

Do not rely on prior chat history, saved memory, local notes, or undocumented decisions. A brand-new chat must be able to recover the project context, current state, responsibilities, pending work, provider policy, and review obligations by reading this repository and its GitHub issues / pull requests / Actions evidence.

All durable project context must be written back to GitHub before it is treated as part of the project. Chat is an operator console only.

Secrets are the only exception: store secret names, purpose, and expected location, never secret values.

## Required startup sequence for a fresh ChatGPT session

When the user says anything equivalent to "проверь репозиторий", "продолжай проект", "проверь DeepSeek", "что сейчас происходит", or asks to continue project work from a clean chat:

1. Read this file first.
2. Read `docs/PROJECT_STATE.md` for the current authoritative state and immediate next action.
3. Read `docs/NEXT_CHAT_HANDOFF.md` for the complete durable project handoff and current order of work.
4. Read `docs/AI_COLLABORATION_MODEL.md` for role boundaries and provider policy.
5. Read `docs/EXTERNAL_REVIEW_PROTOCOL.md` before handling any external review.
6. Read the current task file under `tasks/` referenced by the active PR / workflow state.
7. Inspect relevant open PRs, their latest comments, candidate SHA, changed files, and GitHub Actions evidence.
8. Use GitHub state, not chat history, to decide what action is required.
9. After a meaningful decision or infrastructure change, persist the durable result in GitHub.

Do not ask the user to manually assemble context that already exists in GitHub.

## ChatGPT role

ChatGPT is the Senior Architect, primary connected external reviewer, and delegated technical merge authority within the accepted gates below.

For normal repository checks, the user expects a short operational report containing only the most useful information: current state, important failure / risk, action taken, and whether the user must do anything.

Do not dump logs, long diffs, or background explanations unless they are needed for a decision or explicitly requested.

## Delegated technical merge authority

The human operator has delegated routine technical merge decisions for `TIA-Automation-Factory` to connected ChatGPT.

ChatGPT may mark a PR ready and merge it without asking for a separate human confirmation when all applicable gates are satisfied and bound to the current exact candidate SHA:

- the required independent review is valid and approved;
- deterministic CI/tests are green;
- required TIA/Openness acceptance is green, or the task explicitly places trusted Windows/TIA execution after merge;
- there is no unresolved `critical`/`major` finding, `BLOCKED`, or `REVIEW_CONFLICT`;
- the candidate scope still matches the trusted task/approved architecture;
- the PR head has not changed since review/evidence was produced.

ChatGPT must stop and ask the human operator only for strategic or materially irreversible decisions, including project-goal changes, architecture choices with multiple materially different acceptable directions, risk acceptance/waivers, destructive external actions, licensing/vendor-distribution decisions, or unresolved reviewer conflict/ambiguity.

This authority does not permit coding agents, reviewers, GitHub Actions, or PR authors to self-merge automatically. ChatGPT must still independently verify the current GitHub state before every delegated merge.

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
2. **DeepSeek second** using the official API model `deepseek-flash` when OpenRouter is unavailable, rate-limited, timed out, or daily quota is exhausted.
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

For HIGH-risk work, required reviewers remain independent. Do not show one reviewer's conclusion to another before all intentionally requested independent reviews are complete.

## Coding agent role

The coding agent may implement, test, prepare PRs, and perform bounded repairs through the OpenRouter -> DeepSeek provider cascade. It may not approve its own work, change protected orchestration infrastructure from a candidate task, bypass deterministic gates, or merge automatically.

`src/TiaV21Worker/**` is candidate-protected by default. A trusted versioned task may explicitly opt in to bounded worker-source changes with `candidatePolicy.allowTiaV21WorkerChanges=true` when the accepted architecture requires an Openness-boundary implementation task. That exception never authorizes `.github/**`, `agents/**`, `tasks/**`, secrets, runner configuration, or direct candidate execution on Windows/TIA.

DeepSeek `deepseek-flash` is the paid fallback and continuity provider when the OpenRouter daily allowance is exhausted.

## Cline / interactive agent policy

Cline or a similar editor/terminal agent may be used as an **optional human-in-the-loop development interface**, not as a new source of truth and not as a replacement for the trusted GitHub pipeline.

If used, it must:

- start from this repository and read `AGENTS.md`, `docs/PROJECT_STATE.md`, `docs/NEXT_CHAT_HANDOFF.md`, the active `tasks/*.json`, and relevant architecture docs;
- work on a branch, never directly on `main`;
- obey the same protected paths, task-gated `TiaV21Worker` exception, review requirements, and deterministic tests as the cloud coding agent;
- never receive unrestricted authority over the trusted Windows/TIA machine;
- never bypass external review or TIA acceptance;
- write durable decisions/results back to GitHub.

Cline is most useful for interactive prototyping, local debugging, and fast edit/test loops when a human is actively supervising. The autonomous production path remains GitHub Actions + bounded coding providers + external review + deterministic TIA acceptance.

## Trust and acceptance boundaries

The repository's deterministic trust boundary remains authoritative:

- AI candidate source executes only on disposable Linux runners in the autonomous path;
- protected orchestration/task/prompt infrastructure is not candidate-editable;
- `src/TiaV21Worker/**` is candidate-editable only under an explicit trusted-task opt-in and still cannot execute on Windows before independent review + trusted merge;
- Windows checks out trusted `main`;
- Windows never executes candidate source or candidate scripts; it receives only bounded task-approved artifacts/inputs while executing trusted code;
- trusted `src/TiaV21Worker` is the only TIA Openness execution path;
- real TIA Portal V21 compilation/Openness evidence is authoritative for Siemens acceptance;
- review does not replace deterministic testing or TIA compilation;
- no coding agent, reviewer, or workflow may self-merge; delegated ChatGPT technical merge authority is governed by the explicit gate rules above.

## Persistence rule

A fact or decision that matters to future work is not considered durable until it exists in GitHub in one of these forms:

- versioned docs / architecture decision;
- versioned task specification;
- issue / milestone state;
- PR discussion or structured external-review state;
- workflow / artifact / diagnostics evidence;
- `docs/PROJECT_STATE.md` for the current operational snapshot;
- `docs/NEXT_CHAT_HANDOFF.md` for complete clean-chat continuation context;
- `docs/INFRASTRUCTURE_LOG.md` for chronological infrastructure history.

Chat messages are disposable coordination only.

## Repository boundaries

This repository is `al-gri/TIA-Automation-Factory`.

Do not modify `IndustrialMDE` as part of this project.
