# Project State — Authoritative Operational Snapshot

Last updated: 2026-09-18

This is the first operational state document to read after root `AGENTS.md`.

GitHub is the only durable source of truth. A fresh ChatGPT / Gemini / coding-agent session must recover the project from this repository, issues, pull requests, Actions evidence, and versioned tasks without relying on any previous chat.

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

## Current operating model

Infrastructure through I6 is complete and frozen. Phase 2 repository-first AI orchestration is now active.

Coding provider order is authoritative and implemented in `agents/runtime/run-coder.sh` plus the agent/repair workflows:

1. **OpenRouter first** using the configured free coding model while quota is available.
2. **DeepSeek second** using official API model `deepseek-flash` when OpenRouter is unavailable, rate-limited, timed out, or its daily allowance is exhausted.
3. If OpenRouter hits quota after producing real workspace changes, those changes are preserved and DeepSeek continues the same bounded task instead of restarting from zero.
4. Gemini is not a routine coding fallback; it is reserved for independent review / red-team escalation.

ChatGPT is the Senior Architect and primary connected external reviewer. TIA Portal V21 remains the deterministic Siemens acceptance authority. No automatic merge is allowed.

## Fresh-chat operator contract

Root `AGENTS.md` is mandatory. It defines the commands a clean ChatGPT session must understand.

When the user says `проверь репозиторий`, `проверь DeepSeek`, `проверь запросы DeepSeek`, `что ждёт review?`, or equivalent, connected ChatGPT must inspect GitHub itself, perform any authorized ChatGPT review/orchestration action, write durable results back to GitHub, and report only the operationally important result to the user.

The user must not be asked to manually collect context already present in GitHub.

If Gemini is required by risk policy, ChatGPT must not impersonate Gemini. It must provide a complete ready-to-paste Gemini message generated from GitHub source of truth.

## Provider implementation proof

OpenRouter is already a proven working coding provider from the earlier I6 path. DeepSeek official API is also proven independently.

DeepSeek proof run `35318239703`:

- provider: `deepseek`
- model: `deepseek/deepseek-flash`
- step finishes: 14
- input tokens: 16,324
- output tokens: 2,271
- reasoning tokens: 1,490
- cache read tokens: 187,264
- reported cost: `$0.005266992`

Repository-first / OpenRouter-first policy implementation was merged in PR #15. Main implementation commit: `77d383a8073fb04286f54d47a3fa87b2653dbf83`.

That change also fixed Candidate Validation so trusted task JSON is always resolved from `main` rather than from the candidate checkout.

The exact new OpenRouter -> DeepSeek continuation path is configured and ready; the next normal coding task should be used as the live audit of this ordering rather than creating another artificial infrastructure-only smoke unless needed.

## Connected ChatGPT -> TIA proof

Task: `tasks/PHASE2-001.json`

Candidate PR: #13

Candidate SHA: `178f1dc376bc347a4dbb533a5efd43e4e6996dc3`

Connected ChatGPT recovered the pending external review entirely from GitHub, checked the trusted task, candidate SHA, bounded diff, source context, Linux test evidence, and generated artifact, then submitted a schema-valid `APPROVE` back to GitHub.

External Review Response run `35319434427` validated and bound that response to the exact request/task/SHA/reviewer slot and dispatched Candidate Validation.

The first Candidate Validation attempt exposed the old task-source trust bug. PR #15 fixed it by resolving task state from trusted `main`.

Fresh Candidate Validation run `35322785146` then completed successfully end to end:

- trusted task resolution from `main`: PASS;
- protected-path check: PASS;
- deterministic Linux evidence: PASS;
- Requirements Reviewer: PASS;
- candidate PLC artifact generation/package: PASS;
- trusted Windows/TIA V21 acceptance: PASS;
- PLC/TIA Reviewer: PASS;
- `repair-or-finish`: PASS with no repair required.

TIA V21 diagnostics for the exact `UDT_Motor.scl` candidate artifact:

```json
{
  "success": true,
  "state": "Success",
  "warnings": 0,
  "errors": 0
}
```

`UDT_Motor (UDT)` and `Main (OB1)` both compiled successfully.

This proves the intended human interaction pattern: the user can ask ChatGPT in a clean connected chat to inspect the repository / pending AI work; ChatGPT can independently review and write its decision back to GitHub; the unchanged approved candidate then continues through trusted deterministic gates and real TIA Portal V21.

## Risk / Gemini policy

LOW risk: OpenRouter/DeepSeek coder -> ChatGPT external review -> deterministic/TIA gates as applicable.

MEDIUM risk: OpenRouter/DeepSeek coder -> ChatGPT review -> Gemini only if findings/uncertainty justify escalation -> deterministic/TIA gates.

HIGH risk / architecture / PLC semantics / security: independent ChatGPT and Gemini reviews are required. Reviewer disagreement produces `REVIEW_CONFLICT` and blocks automatic acceptance.

`PHASE2-001` is LOW risk and did not require Gemini.

## Current phase and next work

Core Phase 2 plumbing is operational. The project can now move from infrastructure work to actual generator development.

For normal work:

1. create / use a versioned task under `tasks/`;
2. coding agent attempts OpenRouter first and DeepSeek second;
3. candidate passes deterministic Linux checks;
4. connected ChatGPT external review is required according to risk policy;
5. `CHANGES_REQUIRED` resumes bounded repair on the same PR;
6. `APPROVE` continues the unchanged candidate through trusted Candidate Validation / TIA;
7. no automatic merge.

Near-term measurement goal: record provider/model, calls/steps, tokens/cache, fallback reason, cost, duration, and repair count over several real generator tasks before considering LiteLLM or additional routing complexity.

## Required documents

- `AGENTS.md` — mandatory clean-chat / agent entry point and user-command semantics.
- `README.md` — repository overview and mandatory start links.
- `docs/PROJECT_STATE.md` — current authoritative operational snapshot.
- `docs/AI_COLLABORATION_MODEL.md` — roles, provider cascade, risk policy.
- `docs/EXTERNAL_REVIEW_PROTOCOL.md` — normative external-review protocol.
- `docs/INFRASTRUCTURE_LOG.md` — chronological infrastructure history.
- `docs/GENERATOR_CHAT_HANDOFF.md` — technical architecture / workflow background.
- `tasks/*.json` — trusted executable task specifications.
- `reviews/` — review templates and machine-readable response schema.
- `.github/workflows/` — trusted orchestration implementation.

## Human-facing response policy

When the user asks ChatGPT to check the repository or AI requests, the chat response should contain only:

- what is currently waiting / broken / completed;
- what ChatGPT checked or changed;
- the review decision when applicable;
- the next gate;
- whether the user must do anything.

Do not dump routine logs or large diffs into chat. If Gemini is needed, include the complete ready-to-paste Gemini message directly in the response.
