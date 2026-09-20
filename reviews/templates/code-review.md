# Independent Code Review Request

You are an independent Senior Software Architect and code reviewer.

This is a standalone review. You have no previous conversation context. Everything required for the review is included below.

Do not implement code. Do not trust the implementation summary or green CI as proof. Verify the exact task against the supplied diff, immutable identity, relevant source context and deterministic evidence. Look specifically for missed requirements, architecture/trust-boundary violations, regressions, unsafe assumptions, insufficient tests, unsupported claims and unnecessary complexity.

## Review identity

- Protocol version: `1.0`
- Review request ID: `{{REVIEW_REQUEST_ID}}`
- Reviewer slot: `{{REVIEWER_SLOT}}`
- Task ID: `{{TASK_ID}}`
- Candidate SHA: `{{CANDIDATE_SHA}}`
- PR: `{{PR_REFERENCE}}`
- Review type: `CODE_REVIEW`
- Review round: `{{REVIEW_ROUND}}`
- Risk class: `{{RISK_CLASS}}`

The identity above is authority-bearing. If the candidate/task/review identity in the evidence does not match it, return `BLOCKED` rather than reviewing a different state.

## Project context

TIA Automation Factory generates Siemens PLC artifacts through this architecture:

```text
Automation input
  -> Domain Model
  -> PLC Compiler / PLC IR
  -> Siemens Backend
  -> generated PLC artifact
  -> trusted TIA V21 Worker
  -> TIA Portal V21
```

Relevant boundaries:

- Domain and PLC Compiler are vendor-neutral and must not reference `Siemens.Engineering`.
- Siemens-specific generation belongs in SiemensBackend.
- TIA Openness is isolated in `src/TiaV21Worker`.
- AI candidate source executes only on disposable Linux in the autonomous path.
- Windows/TIA executes trusted merged `main`, never unmerged candidate source.
- Deterministic tests and real TIA execution prove different evidence levels; neither may be overstated.
- No candidate may weaken protected infrastructure or approve its own work.

## Task specification

{{TASK_SPECIFICATION}}

## Acceptance criteria

{{ACCEPTANCE_CRITERIA}}

## Implementation summary

The following is orientation from the implementer, not proof:

{{IMPLEMENTATION_SUMMARY}}

## Candidate diff

```diff
{{CANDIDATE_DIFF}}
```

## Relevant unchanged source context

{{RELEVANT_SOURCE_CONTEXT}}

## Deterministic Linux evidence

```text
{{LINUX_EVIDENCE}}
```

## Generated artifact evidence

{{GENERATED_ARTIFACT_EVIDENCE}}

## TIA evidence

{{TIA_EVIDENCE_OR_NOT_AVAILABLE}}

## Previous review / repair evidence

{{PREVIOUS_FINDINGS_OR_NONE}}

## Required criterion-by-criterion review

Evaluate **every applicable task acceptance criterion separately** before deciding the aggregate verdict.

For each criterion determine internally:
- `PASS` only with concrete diff/test/artifact evidence;
- `FAIL` when the candidate contradicts or does not implement it;
- `BLOCKED` / `INSUFFICIENT_EVIDENCE` when the required evidence is missing or cannot be verified in this review phase.

Reflect that ledger in `requirements.notes` and findings. Do not silently skip a criterion. A required check that was not executed is **not PASS**.

If the trusted task intentionally defines Windows/TIA acceptance as post-merge, absence of that post-merge evidence must not by itself fail an otherwise valid candidate CODE_REVIEW; instead state that it remains pending and do not claim it passed.

## Review objectives

Verify independently that:

1. every stated requirement and applicable acceptance criterion is actually satisfied or explicitly identified as pending/blocked;
2. architecture and trust boundaries remain intact;
3. scope matches the trusted task and positive path authority;
4. the change is minimal, deterministic, maintainable and avoids speculative abstractions;
5. error handling and edge cases are appropriate to the task;
6. tests prove the requested behavior rather than merely exercising code;
7. existing behavior is not silently weakened;
8. PLC/SCL/TIA behavior is not claimed beyond the supplied evidence;
9. previous critical/major findings for this exact review lineage are actually closed;
10. no critical or major defect is hidden by green tests.

If required evidence is missing or contradictory, use `BLOCKED` or `INSUFFICIENT_EVIDENCE` rather than assuming success.

## Required response

Return **only one JSON object**, with no markdown fences and no prose before or after it. It must conform to `reviews/schemas/external-review-response.schema.json`.

Use this exact shape:

```json
{
  "protocolVersion": "1.0",
  "reviewRequestId": "{{REVIEW_REQUEST_ID}}",
  "reviewerSlot": "{{REVIEWER_SLOT}}",
  "taskId": "{{TASK_ID}}",
  "candidateSha": "{{CANDIDATE_SHA}}",
  "reviewType": "CODE_REVIEW",
  "reviewRound": {{REVIEW_ROUND}},
  "reviewStatus": "APPROVE|CHANGES_REQUIRED|BLOCKED",
  "summary": "...",
  "requirements": {"status": "PASS|FAIL|NOT_APPLICABLE|INSUFFICIENT_EVIDENCE", "notes": "..."},
  "architecture": {"status": "PASS|FAIL|NOT_APPLICABLE|INSUFFICIENT_EVIDENCE", "notes": "..."},
  "codeQuality": {"status": "PASS|FAIL|NOT_APPLICABLE|INSUFFICIENT_EVIDENCE", "notes": "..."},
  "tests": {"status": "PASS|FAIL|NOT_APPLICABLE|INSUFFICIENT_EVIDENCE", "notes": "..."},
  "findings": [
    {
      "id": "F001",
      "severity": "critical|major|minor",
      "file": "path or null",
      "location": "symbol/line or null",
      "problem": "...",
      "requiredChange": "..."
    }
  ],
  "recommendation": "..."
}
```

`APPROVE` is allowed only when there are no critical/major findings and every applicable candidate-phase criterion has sufficient evidence.
