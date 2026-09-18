# Governance Bootstrap Lane

Status: **normative, exceptional path**.

This document defines the only allowed non-recursive authorization path for maintainer/primary-ChatGPT-authored protected governance repairs when no pre-existing trusted `tasks/*.json` can authorize the work without depending on the governance mechanism being repaired.

The normal path remains a trusted versioned task on `main`. This lane is an exception for governance bootstrap only; it is not an alternate implementation workflow.

## 1. When this lane may be used

All of the following must be true:

1. The work repairs or defines protected governance/control-plane behavior such as reviewer policy, trusted context construction, review state transitions, protected-path enforcement, or the governance protocol itself.
2. The candidate must be maintainer/primary-authored because normal coding agents are not allowed to change the relevant protected paths.
3. A normal trusted task cannot be established first without creating the same authorization recursion the work is intended to repair.
4. The human operator explicitly authorizes the bounded bootstrap scope.

If any condition is false, use the normal trusted-task path.

This lane must never be used merely because creating a normal task is inconvenient.

## 2. Human scope authorization

Before the bootstrap candidate is accepted, the human operator must explicitly authorize one GitHub issue as the bounded scope authority.

The issue body must state at least:

- task identity;
- problem/goal;
- acceptance requirements;
- HIGH risk classification;
- protected paths or governance surfaces in scope;
- explicit out-of-scope boundaries;
- required reviewer slot `chatgpt-secondary` for a primary-authored candidate;
- whether Windows/TIA execution is allowed or required.

Primary connected ChatGPT must persist the authorization to that issue and record the SHA-256 of the exact authorized issue body. The authorization record must identify the issue, task ID, risk, reviewer slot, body hash, Windows/TIA policy and repository boundary.

The human authorization is strategic authority; the GitHub record is the durable audit trail.

Any material issue-body change after the recorded hash invalidates the bootstrap authorization until the human explicitly re-authorizes the new scope and a new hash is recorded.

## 3. Candidate restrictions

A bootstrap candidate:

- may implement only the authorized governance scope;
- may not widen its own authorization;
- may not treat files added in the candidate branch as trusted-main authorization for that same candidate;
- may not weaken reviewer independence, exact-SHA binding, deterministic CI, protected Windows/TIA boundaries, or repository scope;
- may not execute candidate source on trusted Windows/TIA;
- may not touch `IndustrialMDE`;
- may not use this lane to authorize ordinary product/generator work.

## 4. Review and evidence

Because the primary maintainer authored the protected governance candidate, the required independent reviewer is a fresh isolated `chatgpt-secondary` session.

The review package must contain:

1. this bootstrap policy;
2. the authorized issue and recorded issue-body SHA-256;
3. the durable human-authorization comment;
4. exact PR and candidate SHA;
5. complete bounded diff/source context;
6. exact-SHA deterministic CI evidence;
7. explicit bootstrap acceptance objectives;
8. the standard machine-readable review response contract.

The reviewer must verify that the live issue body still hashes to the authorized fingerprint and that the candidate remains within that scope.

Any candidate change invalidates the review exactly as in the normal task path.

## 5. Automation boundary

Existing trusted external-review automation remains **task-only and fail-closed**. It must continue to reject review responses that lack a trusted `tasks/*.json` authorization.

Bootstrap review evidence is therefore recorded manually in the PR/issue by primary connected ChatGPT after validating response identity and exact SHA. It must not be fed through task-only automation by inventing or inferring a trusted task.

This separation is deliberate: the exceptional human-authorized bootstrap lane must not silently broaden normal autonomous permissions.

## 6. Merge gate

Primary connected ChatGPT may execute the delegated merge only when all of the following are true for the exact current head:

- the human-authorized issue/body fingerprint is still valid;
- the candidate diff remains within the authorized governance scope;
- exact-SHA deterministic CI is green;
- fresh independent `chatgpt-secondary` review is `APPROVE` with no unresolved critical/major finding;
- no `BLOCKED` or `REVIEW_CONFLICT` remains;
- no unauthorized Windows/TIA execution occurred;
- repository boundary and protected-machine rules remain intact.

No additional routine human merge confirmation is required when the human already authorized the bounded bootstrap scope and all exact-candidate gates pass.

A risk waiver, scope widening, material architecture change outside the authorized issue, destructive external action, or unresolved conflict still requires a new human decision.

## 7. Completion

After a bootstrap governance merge:

- perform the normal methodology checkpoint;
- update operational state/handoff documents;
- close the bootstrap issue only when the permanent ambiguity is actually resolved;
- return subsequent ordinary implementation work to trusted versioned tasks.

## 8. Fail-closed rule

If the human authorization is missing, its issue-body fingerprint no longer matches, reviewer independence cannot be established, deterministic evidence is stale, or candidate scope is ambiguous, state is `BLOCKED`.

Do not repair a bootstrap authorization gap by letting the candidate authorize itself.
