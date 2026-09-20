# Autonomous coding-agent loop

`AGENTS.md` is the normative repository operating contract. This document is a concise implementation overview of the current autonomous coding/review loop and must not override `AGENTS.md` or the trusted task.

## Provider/runtime policy

Routine coding uses the bounded provider cascade defined by `AGENTS.md`:

1. OpenRouter first with the configured coding model.
2. Official DeepSeek API `deepseek-flash` as fallback when OpenRouter is unavailable, rate-limited, timed out, or exhausted.

Provider completion is not acceptance. Provider/model/fallback evidence is recorded separately from deterministic and independent review evidence.

## Versioned instructions

Active instruction surfaces are:

- coding agent: `agents/prompts/coder.md`;
- bounded repair agent: `agents/prompts/repair.md`;
- canonical CODE_REVIEW: `reviews/templates/code-review.md`;
- canonical PLC_REVIEW: `reviews/templates/plc-review.md`;
- shared external-review response schema: `reviews/schemas/external-review-response.schema.json`;
- machine-readable authority/acceptance: `tasks/*.json`.

`agents/prompts/reviewer-requirements.md` and `agents/prompts/reviewer-tia.md` are retired compatibility markers, not active review protocols.

## Candidate / publication authority boundary

AI candidate implementation and tests execute only on disposable GitHub-hosted Linux runners without GitHub publication authority.

Candidate execution produces bounded patch/evidence data. A separate fresh publisher job:

- starts from a trusted clean checkout;
- never executes candidate code/runtime;
- validates the patch against the immutable baseline and trusted task `candidatePolicy.allowedPaths`;
- rejects path escapes, protected paths, unsupported symlinks/modes and unexpected byproducts;
- stages/publishes only validated paths.

Repair uses the same execution/publication split.

## Protected paths and TIA worker exception

Ordinary candidates do not modify protected orchestration/governance paths listed in `AGENTS.md` and the trusted task.

`src/TiaV21Worker/**` is protected by default but may be changed by a normal candidate only when the trusted task explicitly sets `candidatePolicy.allowTiaV21WorkerChanges=true` **and** positively authorizes the exact worker path through `candidatePolicy.allowedPaths`.

That exception never authorizes workflows, prompts, task files, secrets, runner configuration or direct candidate execution on Windows/TIA.

## Windows / TIA boundary

Unmerged candidate source/scripts never execute on the trusted Windows/TIA machine.

Windows workflows fail closed to trusted `main`, explicitly check out `main`, and run the trusted `src/TiaV21Worker` path. Real TIA Portal V21 evidence is authoritative only for the exact import/compile/save/reopen operations actually executed.

## Independent review model

One independent reviewer is the default, selected by material authorship:

- coding-agent-authored candidate with no material primary authorship -> primary connected `chatgpt`;
- primary-authored/co-authored candidate -> fresh isolated `chatgpt-secondary`;
- additional simultaneous reviewers are escalation only.

Review type is task-defined (`CODE_REVIEW`, `PLC_REVIEW`, or `ARCHITECTURE_REVIEW`). Review instructions come from the canonical templates and all external verdicts use the shared response schema.

Deterministic tests/TIA evidence and semantic review are complementary; neither substitutes for the other.

## Bounded repair

Repair attempts are limited by the trusted task's `maxRepairAttempts`. Repair remains inside the same task, exact finding/round and positive path authority.

Candidate defects may be repaired. Infrastructure failures, missing evidence and external TIA/vendor prerequisites fail closed as blockers rather than consuming code repairs or triggering unrelated edits.

Exhausted repair budget becomes `BLOCKED`; there is no unbounded retry loop.
