# Phase 2 proof — 2026-09-18

This document records the completed repository-first AI operating proof.

## Operating policy

- GitHub is the sole durable project context.
- Fresh ChatGPT sessions start from `AGENTS.md` and `docs/PROJECT_STATE.md`.
- Routine coding provider order is OpenRouter first, then DeepSeek `deepseek-flash` when OpenRouter is unavailable, rate-limited, timed out, or daily quota is exhausted.
- If OpenRouter produces real workspace changes before exhausting quota, DeepSeek continues the same bounded workspace.
- Gemini is reserved for independent review / red-team escalation, not routine coding fallback.
- No automatic merge.

## Implementation proof

PR #15 merged the repository-first context contract, OpenRouter-first provider policy, repair alignment, and trusted-task Candidate Validation fix.

Main implementation commit: `77d383a8073fb04286f54d47a3fa87b2653dbf83`.

DeepSeek official API had already been proven independently in run `35318239703` using `deepseek/deepseek-flash` with reported cost `$0.005266992`.

## Connected ChatGPT review proof

Task: `tasks/PHASE2-001.json`

Candidate PR: #13

Candidate SHA: `178f1dc376bc347a4dbb533a5efd43e4e6996dc3`

Connected ChatGPT recovered the pending request entirely from GitHub, independently checked the trusted task, candidate SHA, bounded diff, source context, Linux evidence, and generated PLC artifact, and submitted `APPROVE` back to GitHub.

External Review Response run `35319434427` bound the response to the exact request/task/SHA/reviewer slot and dispatched Candidate Validation.

## Trusted validation / TIA proof

Fresh Candidate Validation run `35322785146` completed successfully:

- trusted task resolution from `main`: PASS;
- protected-path check: PASS;
- deterministic Linux tests/generation: PASS;
- Requirements Reviewer: PASS;
- candidate PLC package: PASS;
- trusted Windows/TIA V21 acceptance: PASS;
- PLC/TIA Reviewer: PASS;
- `repair-or-finish`: PASS, no repair required.

TIA diagnostics for the exact `UDT_Motor.scl` artifact:

```json
{
  "success": true,
  "state": "Success",
  "warnings": 0,
  "errors": 0
}
```

`UDT_Motor (UDT)` and `Main (OB1)` both compiled successfully.

## Result

The intended operator path is proven:

```text
versioned Git task
  -> OpenRouter / DeepSeek bounded coder
  -> candidate PR
  -> WAITING_FOR_EXTERNAL_REVIEW
  -> connected ChatGPT reads GitHub and submits its bound review
  -> APPROVE or bounded repair
  -> trusted Candidate Validation
  -> exact PLC artifact
  -> trusted Windows/TIA V21
  -> independent PLC/TIA review
  -> DONE
```

For HIGH-risk work, independent Gemini review remains a separate required slot. ChatGPT must provide a complete ready-to-paste Gemini message when that escalation is required.
