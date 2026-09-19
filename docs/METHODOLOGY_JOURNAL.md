# Methodology Journal

Curated chronological diary of lessons learned while developing both TIA Automation Factory and the AI-assisted development methodology itself.

Raw automated telemetry is recorded in GitHub issue #25. This file records milestone-level interpretation, not every workflow event.

## 2026-09-17 — Establish the trusted execution split

### Evidence

- Disposable Linux runners were sufficient for candidate generation/tests.
- Real TIA Portal V21 execution required a self-hosted Windows machine with Siemens Openness prerequisites.
- Initial autonomous workflows and real TIA smoke proved the complete Linux -> artifact -> trusted Windows/TIA path.

### Lessons

- Candidate source and trusted vendor tooling must be separated physically and logically.
- Protected-path enforcement is required before an AI agent is allowed to publish a branch.
- A green Linux test suite does not prove that Siemens/TIA acceptance or every task requirement is satisfied.

### Methodology effect

Introduced explicit deterministic gates, trusted Windows boundary, task acceptance split and independent review.

## 2026-09-17 — Repair must be bounded and observable

### Evidence

I5 intentionally seeded an incomplete candidate, observed rejection, performed bounded repair on the same PR and revalidated successfully.

### Lessons

- Repairing the same PR preserves evidence and avoids branch proliferation.
- Repair limits must be trusted-task data, not agent discretion.
- Failed validation evidence should be fed back into repair context.

### Methodology effect

Adopted bounded same-PR repair with explicit attempt counters and blocked state after exhaustion.

## 2026-09-18 — Provider continuity is useful only when acceptance is provider-independent

### Evidence

OpenRouter quota behavior and a successful official DeepSeek `deepseek-flash` smoke demonstrated that multiple coding providers can execute the same bounded task. A late OpenRouter quota event showed that useful workspace changes may exist before provider failure.

### Lessons

- Provider fallback should preserve task identity and, where safe, useful partial workspace changes.
- Model/provider success must never be treated as acceptance.
- Provider/model/tokens/cache/cost/fallback reason are useful methodology telemetry.

### Methodology effect

Adopted OpenRouter -> DeepSeek coding cascade and provider audit.

## 2026-09-18 — Repository-first context removes chat dependency

### Evidence

Fresh connected ChatGPT sessions successfully recovered tasks, review requests and project state from GitHub rather than old chats.

### Lessons

- Chat history is a poor project database.
- AI roles become transferable when architecture, current state, handoff, task and review protocols are versioned.
- Durable decisions must be written back to GitHub immediately enough that a fresh session can continue.

### Methodology effect

Adopted `AGENTS.md`, `PROJECT_STATE.md`, `NEXT_CHAT_HANDOFF.md`, collaboration/review protocols and GitHub as sole durable source of truth.

## 2026-09-18 — Exact-SHA review is non-negotiable

### Evidence

During OLQ-001 and governance work, multiple repairs changed candidate SHAs after valid earlier evidence. Review rounds repeatedly found defects that deterministic Linux CI could not detect.

### Lessons

- Approval is a property of an immutable candidate, not of a PR name.
- Any source-changing repair invalidates prior review.
- Fresh review must precede renewed trusted Candidate Validation.

### Methodology effect

The state machine now returns every candidate-changing repair to a fresh external-review gate before deterministic Candidate Validation may resume.

## 2026-09-18 — Reviewer independence must follow authorship, not model branding

### Evidence

Primary ChatGPT became a material co-author of PR #22 after repair budget exhaustion. The original trusted task still authorized only the primary reviewer, exposing a governance conflict. Gemini review access was also operationally unreliable.

### Lessons

- The same AI session must not independently approve code it materially authored.
- Independence should be enforced from Git authorship/evidence separation.
- A second isolated ChatGPT session can serve as reviewer when the primary session is conflicted.
- `reviewerSlots` must be an authorization set, not a command to run every reviewer.

### Methodology effect

Introduced `chatgpt-secondary`, authorship-derived reviewer policy and one-independent-reviewer-by-default governance.

## 2026-09-18 — Governance defects are software defects

### Evidence

Independent review rounds F001-F010 found defects in reviewer eligibility, task authorization ordering, stale approval propagation, hidden Gemini reviewer runtime, a legacy Candidate Validation bypass, and manual Windows/TIA workflows capable of checking out non-main refs.

### Lessons

- Workflow state machines require the same review rigor as production code.
- Fail-open authorization is a major defect even when CI is green.
- Legacy smoke workflows can become security bypasses after architecture evolves.
- Trusted Windows workflows need repository-wide main-only invariants, not informal operator discipline.

### Methodology effect

Added repository-wide workflow regressions, fail-closed reviewer authorization, deterministic Candidate Validation, fresh-review repair transitions and main-only self-hosted TIA guards.

## 2026-09-18 — Coding context should be explicit, bounded and reproducible

### Evidence

The coding workflow originally appended only the task JSON to `coder.md`, while important repository rules/state were merely available for the model to discover. This was adequate for small tasks but fragile for Siemens/Open Library work.

### Lessons

- Large model context windows do not guarantee the model reads the right files.
- Trusted context should be assembled before provider selection.
- Source code should normally remain workspace-readable instead of being dumped wholesale into prompts.
- Vendor-specific interfaces should enter coding context through versioned qualified contracts, not model inference.

### Methodology effect

Added `build-coder-context.py`, baseline repository context, task `contextFiles`, context hashes/manifest and shared OpenRouter/DeepSeek enriched prompts.

## 2026-09-18 — Methodology is now a first-class project artifact

### Decision

The project explicitly treats development methodology as a second product alongside the PLC generator.

### Mechanism

- `docs/DEVELOPMENT_METHODOLOGY.md` contains curated reusable rules and active experiments.
- This journal records milestone-level lessons.
- Issue #25 is the append-only automated raw event diary.
- A trusted telemetry workflow records meaningful workflow completions and PR lifecycle events automatically.
- `AGENTS.md` requires primary ChatGPT to perform methodology checkpoints without waiting for a user reminder.

### Expected future use

After enough real generator tasks, review the accumulated evidence and extract a stable playbook covering task design, context construction, provider routing, review independence, repair budgets, deterministic gates, trusted-machine boundaries and documentation discipline.

## 2026-09-18 — Prompt content must never become control-plane authority

### Evidence

A historical independent review payload for PR #24 identified F011/F012 on candidate `450dca6608f0526d00370595fc5a928f7bbfbd71`. The payload itself was stale for merge purposes, but a fresh inspection of current `main` confirmed that `build-coder-context.py` still parsed task-looking headings from mixed `--prompt-input` content and used the parsed `contextFiles` as trusted authority. Current `validate_repo_path()` also silently canonicalized some malformed spellings. The defect had therefore survived the final #24 merge and downstream OLQ work had already begun.

### Lessons

- Exact-SHA staleness applies to verdicts, not to defect hypotheses: an old finding should be re-tested against current source before being discarded.
- A prompt assembled by a trusted workflow can still contain untrusted data such as issue bodies, diffs, review comments and logs.
- Trust provenance must attach to fields, not to the enclosing text blob.
- Control metadata such as task identity, `contextFiles`, permissions and reviewer policy must travel through a separate structured channel and be resolved from trusted state.
- Context-path normalization must fail closed; canonicalization after input is not equivalent to requiring canonical input.
- A final independent review can miss a previously unpersisted finding if review evidence is not durably bound into the repository workflow.

### Methodology effect

Introduced M-013: keep control-plane authority separate from mixed prompt content. GOV-CTX-001 / issue #28 repairs task authority through structured event metadata plus trusted Git lookup, adds prompt-spoof and canonical-path regressions, and blocks downstream PR #27 until this trust-origin repair passes exact-SHA CI and independent `chatgpt-secondary` review.

## 2026-09-18 — Governance needs an explicit bootstrap path

### Evidence

PR #30 repaired the live GOV-CTX-001 trust-origin defect and passed exact-SHA CI #224 plus fresh independent `chatgpt-secondary` APPROVE, but current governance also required a trusted task on `main` to authorize `chatgpt-secondary`. No `tasks/GOV-CTX-001.json` existed. Adding one inside the same candidate would have been self-authorization; creating a separate protected task PR raised the same recursive authorization question.

The human operator granted a one-time exact-SHA waiver for PR #30 and then explicitly directed the project to eliminate the bootstrap ambiguity under issue #31. PR #30 merged at `0260117391abf5f0a8375699dca12caa06bafb8b`. Issue #31 was used to define a bounded bootstrap scope before the permanent policy candidate was authored.

### Lessons

- A fail-closed task/review system still needs an explicit root-of-authority path for repairing its own authorization mechanism.
- The candidate must never solve recursion by authorizing itself.
- Human strategic authority is appropriate at the bootstrap boundary, but scope and provenance must be independently auditable from GitHub.
- Normal automation should remain task-only and fail-closed; exceptional governance authorization is safer as a visibly manual lane than as an implicit fallback.
- Once the bounded scope is validly human-authorized, exact-SHA CI and an independent reviewer can gate the implementation without requiring a second routine merge confirmation.

### Methodology effect

Introduced M-014 and `docs/GOVERNANCE_BOOTSTRAP.md`: rare governance-authority recursion uses a frozen issue-body fingerprint, HIGH-risk exact-SHA CI and fresh `chatgpt-secondary` review while ordinary task-based automation remains unchanged and fail-closed.

## 2026-09-18 — Authority provenance must be separate from the conflicted actor

### Evidence

The first independent review of PR #32 at candidate `2d69e0bba51bb5de2672aa7f453bbe812575f0e6` returned `CHANGES_REQUIRED` with four major findings:

- F001: connector-authored comments could claim human authorization without proving that the human actually created the authority artifact;
- F002: the live authorization issue did not contain the mandatory bounded authorization fields inside its fingerprinted body;
- F003: bootstrap eligibility incorrectly depended on coding-agent protected paths even though the candidate changed governance docs outside that list;
- F004: primary-persisted secondary JSON proved response contents but not reviewer-evidence provenance.

GitHub API metadata confirmed that the existing authorization comments were created through `chatgpt-codex-connector`, so they could not serve as origin-verifiable human authority.

### Lessons

- A trusted actor identity in the rendered GitHub UI is insufficient when an app can act as that account; provenance metadata matters.
- Human root authorization for an exceptional lane must be represented by an action the conflicted primary cannot create, not by prose saying that the human approved something.
- Bootstrap eligibility should describe the semantic authority recursion, not accidentally mirror one implementation's protected-path list.
- Review identity fields and exact SHA authenticate *what* a verdict refers to, but not *who caused it to enter the trusted record*.
- Conservative findings may be acted on even when relayed through primary, but an authority-bearing APPROVE requires provenance separated from the candidate author.

### Methodology effect

Round-1 repair initially used non-app-mediated direct owner comments as the provenance mechanism. That mechanism was provisional and is superseded by the F005 lesson below.

## 2026-09-18 — Negative attribution is not positive human authentication

### Evidence

Round-2 independent review of PR #32 candidate `6bd2860c49713f49cc7a30ac6ec2eceb1db4a1d4` accepted the semantic bootstrap repair but returned major F005. The reviewer correctly observed that `performed_via_github_app == null` proves only that GitHub did not attribute an action to a GitHub App. A credentialed non-App API path could still act as the owner.

The current ChatGPT connector's own commits were checked through the raw GitHub commit API and are unsigned (`verification.verified=false`, `reason=unsigned`), which gives the project a stronger separable primitive: a human-controlled SSH signing key unavailable to project automation.

### Lessons

- Negative provenance metadata cannot be promoted into positive identity proof.
- The root authenticator must require a secret/capability unavailable to the conflicted actor, not merely a metadata pattern the actor usually does not produce.
- A signed authority artifact should bind the exact semantic payload; mutable branch names and unsigned comments may point to evidence but are not evidence themselves.
- Review APPROVE provenance needs the same strength as scope authorization because both grant authority.

### Methodology effect

M-014 now requires SSH-signed Git attestation commits for both bootstrap scope authorization and authority-bearing secondary APPROVE. GitHub must report a valid SSH signature, repository-owner author/committer identity, and exact attestation payload. Web-flow signatures, owner comments and `performed_via_github_app` checks are supplementary only. Human signing private material must remain outside ChatGPT/Codex/project automation, CI and runner secrets.

## 2026-09-19 — Permanent authority protocols must separate schema from invocation and gates from stages

### Evidence

Round-3 independent review of PR #32 candidate `f078c4b3aba11ebe3dacdf21881dd5b00f5fcedd` confirmed F001-F005 repaired but found two new major defects. F006 showed that the supposedly permanent scope/review attestation payloads hard-coded the current invocation's issue/task values. F007 showed that a global fail-closed list required a signed review attestation before the reviewer could produce the APPROVE JSON that the attestation must contain.

The default bootstrap repair budget was already exhausted. The human explicitly authorized exactly one additional candidate-changing repair round limited to F006/F007 without widening scope, and issue #31 was updated to bind that decision into the exact signed authority contract.

### Lessons

- A permanent governance schema must define field relationships, not bake in one incident's identifiers. Current issue/task values belong in an instance, example or signed evidence artifact.
- Fail-closed does not mean requiring future-stage evidence before that evidence can exist. Gates must fail closed at the stage where the evidence is applicable.
- A missing precondition and a not-yet-applicable artifact are different states; conflating them can deadlock an otherwise conservative state machine.
- Human repair-budget extensions should remain bounded, explicit and invocation-local; they must not silently mutate the permanent default.
- When durable human authority data changes, the exact issue-body fingerprint changes too, so prior signatures must become stale rather than being informally carried forward.

### Methodology effect

The bootstrap policy now uses invocation-generic scope/review attestation schemas with verifier equality to the current repository/frozen issue/task identity, and stage-specific fail-closed semantics for pre-review, CHANGES_REQUIRED/BLOCKED, and post-APPROVE signed evidence. The one additional F006/F007 repair remains an exception for this invocation only; it does not change the default two-repair budget.

## 2026-09-19 — Chat handoff must audit repository freshness before role transfer

### Evidence

While preparing a durable primary-chat transfer protocol, live GitHub inspection showed that `docs/PROJECT_STATE.md` and `docs/NEXT_CHAT_HANDOFF.md` were materially stale: they still described GOV-BOOT-001/PR #32 and PR #27 as active blockers even though both had already completed/advanced.

During the same work, primary ChatGPT twice issued a temporary `create_file` operation against `main` before creating/verifying the intended candidate branch. Both placeholder files were immediately reverted and no placeholder remained in the tree, but the `main` history moved. That base movement made the previously prepared PR #37 review package stale despite no lasting placeholder content.

PR #37 therefore had to be explicitly resynchronized with current `main`; its new exact head `0c9633fe657b8949beea76f87f3a63411cd103e0` required fresh exact-SHA CI (#264 PASS) and a fresh secondary review request.

### Lessons

- Repository-first operation is insufficient if state snapshots are not refreshed before a chat boundary; a fresh chat can faithfully read stale GitHub documents.
- Handoff must be a transaction: freeze discretionary work, inspect live GitHub, reconcile stale/chat-only claims, persist factual state, then transfer the role.
- Exact-SHA review validity depends on surrounding base/ref state as well as candidate intent. A reverted accidental write can still invalidate a prepared review package because history moved.
- Every authority-bearing repository write needs an explicit verified target ref/branch precondition. Omitted/default branch behavior is not acceptable during handoff or protected orchestration.
- The new chat should receive a compact navigation prompt, not the old transcript; it must independently verify the checkpoint before exercising primary authority.
- Operational handoff updates may synchronize facts but must not be used to smuggle normative architecture/authority/risk changes around normal task/review gates.

### Methodology effect

CHAT-HANDOFF-001 / issue #38 introduces `docs/CHAT_HANDOFF_PROTOCOL.md` and M-015. The primary-role transfer becomes a repository freshness checkpoint with explicit stale-evidence handling and write-target verification.
