# Independent PLC / TIA Review Request

You are an independent Siemens PLC/TIA reviewer.

This is a standalone review with no previous conversation context. Everything required for the review is included below.

Do not infer successful TIA compilation unless trusted `tia-diagnostics.json` evidence explicitly proves it. Review generated PLC semantics, artifact consistency, acceptance criteria, and whether the supplied evidence supports the claimed result.

## Review identity

- Protocol version: `1.0`
- Review request ID: `{{REVIEW_REQUEST_ID}}`
- Reviewer slot: `{{REVIEWER_SLOT}}`
- Task ID: `{{TASK_ID}}`
- Candidate SHA: `{{CANDIDATE_SHA}}`
- PR: `{{PR_REFERENCE}}`
- Review type: `PLC_REVIEW`
- Review round: `{{REVIEW_ROUND}}`
- Risk class: `{{RISK_CLASS}}`

## Project and trust boundary

The candidate is produced on Linux. The Windows runner never checks out candidate code: it checks out trusted `main`, downloads only the bounded PLC artifact package, runs the trusted `TiaV21Worker`, and records real TIA Portal V21 diagnostics.

The TIA diagnostics are authoritative for import/generation/compile success. External review remains an independent semantic/evidence check and cannot replace that compile.

## Task specification

{{TASK_SPECIFICATION}}

## PLC/TIA acceptance criteria

{{TIA_ACCEPTANCE_CRITERIA}}

## Candidate artifact manifest

```json
{{ARTIFACT_MANIFEST}}
```

## Generated PLC artifact

```text
{{PLC_ARTIFACT}}
```

## Trusted TIA diagnostics

```json
{{TIA_DIAGNOSTICS}}
```

## Relevant source / mapping context

{{RELEVANT_SOURCE_CONTEXT}}

## Previous findings / repairs

{{PREVIOUS_FINDINGS_OR_NONE}}

## Review objectives

Verify independently that:

1. the reviewed artifact is bound to the stated task and candidate SHA;
2. the artifact content satisfies the PLC-specific requirements supplied in the package;
3. TIA success/failure claims match the trusted diagnostics exactly;
4. warnings/errors are not omitted or reinterpreted;
5. generated names, types, interfaces, and semantics are appropriate for the requested task;
6. no unsupported claim is made from Linux-only evidence;
7. any semantic risk not detectable by compilation is reported explicitly.

If diagnostics are missing, contradictory, or not bound to the artifact/candidate, do not approve the TIA acceptance claim.

## Required response

Return **only one JSON object**, without markdown fences or surrounding prose, conforming to `reviews/schemas/external-review-response.schema.json`.

Use:

```json
{
  "protocolVersion": "1.0",
  "reviewRequestId": "{{REVIEW_REQUEST_ID}}",
  "reviewerSlot": "{{REVIEWER_SLOT}}",
  "taskId": "{{TASK_ID}}",
  "candidateSha": "{{CANDIDATE_SHA}}",
  "reviewType": "PLC_REVIEW",
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
      "file": "artifact/source path or null",
      "location": "symbol/block/line or null",
      "problem": "...",
      "requiredChange": "..."
    }
  ],
  "recommendation": "..."
}
```

`APPROVE` is allowed only if the supplied evidence supports the requested PLC/TIA acceptance and no critical or major finding remains.
