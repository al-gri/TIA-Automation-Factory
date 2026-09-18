# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-18

This file is the first operational state document to read after root `AGENTS.md`.

It exists so a brand-new ChatGPT / reviewer session can recover the project without any prior chat history. GitHub is authoritative; if this snapshot conflicts with newer PR comments, workflow runs, or commits, inspect the newer GitHub evidence and update this file.

## Project

Repository: `al-gri/TIA-Automation-Factory`

Purpose: build an automation engineering software factory that converts vendor-neutral automation models into Siemens PLC artifacts and verifies them through a trusted TIA Portal V21 / TIA Openness boundary.

Architecture:

```text
Automation input
  -> Domain Model
  -> PLC Compiler / PLC IR
  -> Siemens Backend
  -> generated PLC artifact
  -> trusted TIA V21 Worker
  -> TIA Portal V21
```

Modern Domain / compiler / backend code must not depend on `Siemens.Engineering`. TIA Openness stays isolated in `src/TiaV21Worker` targeting .NET Framework 4.8.

`IndustrialMDE` is outside this repository's scope and must not be modified.

## Current phase

Infrastructure baseline through I6 is complete and frozen.

Active work is Phase 2 tracked by GitHub issue #7: DeepSeek primary coder + external ChatGPT / Gemini review protocol.

Current AI operating model:

- DeepSeek API: primary autonomous implementer.
- Primary coding model: `deepseek-flash` through repository provider configuration (`deepseek/deepseek-flash` in OpenCode audit naming).
- ChatGPT: Senior Architect and primary connected external reviewer.
- Gemini: independent verification / red-team reviewer when risk policy requires it.
- TIA Portal V21: deterministic Siemens acceptance authority.
- GitHub: sole durable source of project context and state.

## Proven DeepSeek integration

Autonomous Agent run `35318239703` successfully used the real repository `DEEPSEEK_API_KEY` with no fallback.

Provider audit:

- provider: `deepseek`
- model: `deepseek/deepseek-flash`
- step finishes: 14
- input tokens: 16,324
- output tokens: 2,271
- reasoning tokens: 1,490
- cache read tokens: 187,264
- reported cost: `$0.005266992`

It created PR #13 with a deterministic test-only duplicate-field validation regression test.

## Connected ChatGPT review proof

Task: `tasks/PHASE2-001.json`

Candidate PR: #13

Candidate SHA: `178f1dc376bc347a4dbb533a5efd43e4e6996dc3`

The trusted External Review Request workflow produced a `WAITING_FOR_EXTERNAL_REVIEW` package for reviewer slot `chatgpt`.

A connected ChatGPT session recovered the request entirely from GitHub, independently checked the task, candidate diff, source context, Linux evidence, and generated artifact, and returned `APPROVE`.

The External Review Response workflow run `35319434427` successfully:

- parsed the reviewer JSON;
- bound it to the trusted pending review state;
- validated task / SHA / review type / round / reviewer slot;
- published the approved external-review state;
- dispatched the unchanged candidate into Candidate Validation.

This proves the intended interaction pattern: the human can simply ask ChatGPT to check DeepSeek; ChatGPT reads GitHub, performs its authorized review slot, and writes the result back to GitHub.

## Current blocker

Candidate Validation run `35319445985` failed before deterministic candidate testing.

Root cause: `.github/workflows/candidate-validation.yml` checks out the candidate SHA first and then the `Resolve trusted dispatch task` step uses `test -f "$TASK_PATH"` against the candidate checkout.

For `PHASE2-001`, the trusted task was added to `main` after candidate PR #13 was created, so the task file correctly exists in trusted `main` but not in the older candidate tree.

This exposes a trust-boundary bug in the old validation workflow: task specification must be resolved from trusted `main`, not from candidate source.

The failure is orchestration / trust-state related, not a DeepSeek candidate defect and not a ChatGPT review defect.

## Immediate next action

Fix Candidate Validation so trusted task content is always read from `main` (for example by fetching / checking out trusted main task state separately or using `git show origin/main:$TASK_PATH`) while candidate source / tests continue to come from the candidate SHA.

The trusted task copy must then be used consistently by:

- requirements review;
- generator input / expected artifact resolution;
- package manifest / task evidence;
- bounded repair state.

Do not weaken the protected-path or Windows/TIA boundaries while making this change.

After the fix:

1. rerun / redispatch Candidate Validation for PR #13 and candidate SHA `178f1dc376bc347a4dbb533a5efd43e4e6996dc3` using trusted `tasks/PHASE2-001.json`;
2. require Linux deterministic gates to pass;
3. transfer the exact generated `UDT_Motor.scl` artifact through the trusted Windows boundary;
4. require TIA Portal V21 compile with zero errors;
5. record final proof in this file / issue #7 / infrastructure log as appropriate.

## Risk / Gemini state

`PHASE2-001` is LOW risk and currently does not require a Gemini review under the accepted risk policy.

If a future task is HIGH risk, or MEDIUM risk is escalated, ChatGPT must not simulate Gemini. It must provide the human a complete ready-to-paste Gemini prompt built from current GitHub context and the same independent review package rules.

## Open PR interpretation

At this snapshot, the active Phase 2 proof candidate is PR #13.

Older open AI PRs #4, #5, and #10 are historical infrastructure / smoke candidates. Do not infer that they are current work merely because they remain open. Inspect their task and latest workflow state before acting on them.

No automatic merge is allowed.

## Required documents

- `AGENTS.md` — mandatory clean-chat / agent entry point and command semantics.
- `docs/PROJECT_STATE.md` — current operational snapshot (this file).
- `docs/AI_COLLABORATION_MODEL.md` — roles, risk policy, AI collaboration architecture.
- `docs/EXTERNAL_REVIEW_PROTOCOL.md` — normative external-review protocol.
- `docs/INFRASTRUCTURE_LOG.md` — chronological infrastructure history.
- `docs/GENERATOR_CHAT_HANDOFF.md` — technical architecture / workflow handoff background.
- `tasks/*.json` — trusted executable task specifications.
- `reviews/` — review templates and machine-readable response schema.
- `.github/workflows/` — trusted orchestration implementation.

## Human-facing response policy

When the user asks ChatGPT to check the repository or DeepSeek, the chat response should contain only useful operational information:

- what is currently waiting / broken / completed;
- what ChatGPT checked or changed;
- the decision (`APPROVE`, `CHANGES_REQUIRED`, `BLOCKED`) when applicable;
- whether the user must do anything.

Do not paste routine logs or large diffs into chat.

If Gemini is needed, include the complete ready-to-paste Gemini message directly in the chat response.
