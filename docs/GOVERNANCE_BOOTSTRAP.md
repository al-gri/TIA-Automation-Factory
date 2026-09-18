# Governance Bootstrap Lane

Status: **normative, exceptional path**.

This document defines the only allowed non-recursive authorization path for maintainer/primary-ChatGPT-authored governance-authority repairs when no pre-existing trusted `tasks/*.json` can authorize the work without depending on the same authorization semantics being repaired.

The normal path remains a trusted versioned task on `main`. This lane is an exception for governance bootstrap only; it is not an alternate implementation workflow and it is never a generic missing-task fallback.

## 1. When this lane may be used

All of the following must be true:

1. The work repairs or defines the repository's **normative authority/review-control model itself**: for example reviewer authorization, trusted-context authority, review-state authority, merge authority, bootstrap authority, or a trust-boundary rule that decides who may authorize those actions.
2. The need for bootstrap is **semantic, not path-name based**. Physical inclusion in the coding-agent protected-path list is neither required nor sufficient.
3. A normal trusted task cannot be established first without relying on the same authorization semantics being repaired. A merely absent, inconvenient, stale, or badly scoped task does not satisfy this condition.
4. One GitHub issue contains the bounded scope contract required by section 2 and is frozen for authorization.
5. Positive human provenance required by section 2 exists for that exact frozen issue body.

If any condition is false, use the normal trusted-task path.

Ordinary product/generator/TIA work, routine documentation, normal task creation, or ordinary protected-path maintenance must not use this lane.

## 2. Human scope authorization

Before a bootstrap candidate can be accepted, one GitHub issue must be the bounded scope authority.

The issue body must state at least:

- task identity;
- problem/goal;
- acceptance requirements;
- HIGH risk classification;
- governance surfaces in scope;
- explicit out-of-scope boundaries;
- required reviewer slot `chatgpt-secondary` for a primary-authored candidate;
- whether Windows/TIA execution is allowed or required;
- repository boundary.

Primary connected ChatGPT may propose or edit that issue body, but **candidate text, connector-authored text, comments, reactions, labels and owner attribution are not human authorization**.

After the body is frozen, compute SHA-256 over the exact UTF-8 issue-body bytes as returned by GitHub.

The human operator must then create an **SSH-signed Git commit outside ChatGPT/Codex/project automation** using a human-controlled SSH signing private key that is not available to repository automation. The signed commit must contain a canonical scope-attestation file on a dedicated non-merged branch.

Required canonical payload fields:

```text
artifactType: governance-bootstrap-scope-v1
repository: al-gri/TIA-Automation-Factory
issue: 31
task: GOV-BOOT-001
purpose: scope-authorization
issueBodySha256: <exact lowercase SHA-256>
statement: I authorize implementation and independent review only within the exact bounded scope of this issue body.
```

The verifier must fetch the exact commit through the GitHub REST API and require all of the following:

- `commit.verification.verified == true`;
- `commit.verification.reason == "valid"`;
- `commit.verification.signature` begins with `-----BEGIN SSH SIGNATURE-----`;
- GitHub resolves both commit `author.login` and `committer.login` to the authorized repository owner `al-gri`;
- the attestation file content exactly matches the required payload and current issue-body hash;
- the live issue body still hashes to that exact value;
- verification binds the exact signed commit SHA, not merely the mutable attestation-branch head.

GitHub web-flow signatures are not sufficient for this lane. Unsigned connector/API commits are not sufficient. `performed_via_github_app` may be inspected as supplementary evidence but is never the positive authenticator.

Trust-root assumption: the human signing private key is controlled by the human operator and unavailable to ChatGPT, Codex, GitHub App credentials, PATs used by project automation, CI, runners and coding providers. If this assumption cannot be maintained, bootstrap is `BLOCKED`.

The attestation branch is evidence transport only and must never be merged into `main`.

Any material issue-body change invalidates prior scope authorization and requires a fresh SSH-signed scope-attestation commit for the new exact body hash.

## 3. Candidate restrictions

A bootstrap candidate:

- may implement only the signed human-authorized governance scope;
- may not widen its own authorization;
- may not treat files added or changed in the candidate branch as trusted-main authorization for that same candidate;
- may not weaken reviewer independence, exact-SHA binding, deterministic CI, protected Windows/TIA boundaries, or repository scope;
- may not execute candidate source on trusted Windows/TIA;
- may not touch `IndustrialMDE`;
- may not use this lane to authorize ordinary product/generator work or to avoid normal task establishment.

## 4. Independent review and provenance

Because primary connected ChatGPT authored the bootstrap candidate, the required independent reviewer is a fresh isolated `chatgpt-secondary` session.

The review package must contain:

1. this bootstrap policy;
2. the frozen authorized issue body and exact SHA-256;
3. the exact signed scope-attestation commit SHA and its raw GitHub verification metadata;
4. exact PR and candidate SHA;
5. complete bounded diff/source context;
6. exact-SHA deterministic CI evidence;
7. prior findings and repair round when applicable;
8. explicit bootstrap acceptance objectives;
9. the standard machine-readable review response contract.

The reviewer must independently verify the live issue-body hash, signed human authorization, candidate scope, exact candidate SHA and CI evidence from GitHub.

### Provenance-separated secondary verdict

An `APPROVE` can satisfy the bootstrap merge gate only if the exact secondary-review JSON is contained in a **separate SSH-signed review-attestation commit** created outside ChatGPT/Codex/project automation under the same human-controlled signing key rule.

Required review-attestation payload:

```text
artifactType: governance-bootstrap-review-v1
repository: al-gri/TIA-Automation-Factory
issue: 31
task: GOV-BOOT-001
purpose: review-attestation
reviewRequestId: <exact request id>
reviewerSlot: chatgpt-secondary
candidateSha: <exact candidate SHA>
reviewRound: <exact integer>
reviewJsonSha256: <SHA-256 of exact UTF-8 JSON>
reviewJson:
<full exact JSON payload>
```

The verifier must require the same GitHub commit verification properties as for scope authorization, recompute the JSON hash, validate schema and identity, and verify the candidate SHA is still current.

Primary ChatGPT may persist an unsigned connector-authored copy for traceability, but that copy can never satisfy the authority-bearing APPROVE gate. A non-provenanced `CHANGES_REQUIRED` or `BLOCKED` may be acted on conservatively because it grants no authority.

Any candidate change invalidates the review exactly as in the normal task path.

## 5. Repair budget

Bootstrap repair is bounded even though no trusted task exists.

Default budget: **at most 2 candidate-changing repair rounds after the first reviewed candidate** while the authorized issue scope remains bounded.

Each repair produces a new candidate SHA and requires fresh deterministic CI plus a new isolated `chatgpt-secondary` review round. If repair materially changes the issue-body authority contract, all prior scope attestations are invalid and a fresh signed scope-attestation commit is required.

The human may authorize a lower limit. Increasing the default or allowing any candidate-changing repair after the two-repair budget is exhausted requires a fresh explicit human decision. Without it, state becomes `BLOCKED`; primary ChatGPT may not silently extend the budget.

## 6. Automation boundary

Existing trusted external-review automation remains **task-only and fail-closed**. It must continue to reject review responses that lack trusted `tasks/*.json` authorization.

Bootstrap must never be inferred from a missing task, issue prose, branch name, candidate files, review text, ordinary owner comments, or connector-authored claims.

Bootstrap authorization and signed review evidence are validated manually by primary connected ChatGPT against exact GitHub commit verification metadata. This manual lane must not be fed through task-only automation by inventing or inferring a trusted task.

The project must not add the human signing private key to repository secrets, CI, runners, coding-provider context or any connected-agent credential.

## 7. Merge gate

Primary connected ChatGPT may execute the delegated merge only when all of the following are true for the exact current head:

- a valid SSH-signed human scope-attestation commit exists for the exact current issue-body hash;
- the candidate diff remains within that signed authorized governance scope;
- exact-SHA deterministic CI is green;
- fresh independent `chatgpt-secondary` review is `APPROVE` with no unresolved critical/major finding;
- that exact APPROVE JSON is contained in a valid separate SSH-signed review-attestation commit and passes content-hash/schema/identity validation;
- no `BLOCKED` or `REVIEW_CONFLICT` remains;
- no unauthorized Windows/TIA execution occurred;
- repository boundary and protected-machine rules remain intact.

No additional routine human merge confirmation is required after the human has already signed both the bounded scope authorization and the exact independent APPROVE attestation.

A risk waiver, scope widening, repair-budget extension, material architecture change outside the authorized issue, destructive external action, or unresolved conflict still requires a new human decision.

## 8. Completion

After a bootstrap governance merge:

- perform the normal methodology checkpoint;
- update operational state/handoff documents;
- close the bootstrap issue only when the permanent ambiguity is actually resolved;
- keep human attestation branches out of `main`;
- return subsequent ordinary implementation work to trusted versioned tasks.

## 9. Fail-closed rule

State is `BLOCKED` if any of these cannot be established from live GitHub evidence:

- exact frozen issue-body fingerprint;
- valid human SSH-signed scope attestation;
- bounded semantic bootstrap eligibility;
- reviewer independence;
- valid human SSH-signed authority-bearing review attestation;
- exact-SHA deterministic evidence;
- candidate scope;
- remaining repair budget.

Do not repair a bootstrap authorization gap by letting the candidate, its author, an author-controlled connector, or an unsigned/weakly attributed owner action manufacture authority.
