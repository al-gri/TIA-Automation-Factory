# Independent Repository Audit

Status: active review procedure.

## Purpose

Periodically obtain a read-only independent review of the whole `TIA-Automation-Factory` repository, not only an individual PR. The audit must evaluate both products being built here:

1. the Siemens/TIA Portal V21 PLC generator;
2. the AI-assisted development methodology and autonomous coding/review system.

The audit is intended to detect architectural drift, stale or contradictory documentation, test gaps, unsafe trust-boundary assumptions, unnecessary process complexity, prompt degradation and places where the human operator is being used as a manual message bus.

## Cadence

Run a full audit at the earliest of:

- before entering a new roadmap release milestone;
- after every 5 merged implementation PRs since the previous full audit.

Run an out-of-cycle audit after any material change to:

- `.github/workflows/**` trust/review/coding orchestration;
- `agents/**` prompts/runtime/context construction;
- reviewer authority / merge policy;
- the trusted Windows/TIA execution boundary;
- the canonical generator architecture.

A repeated infrastructure failure with the same root-cause class is also a reason for an out-of-cycle audit.

The audit itself is advisory by default and must not become a standing blocker for normal product work. Only a concrete `critical` or `major` finding that affects the active development path blocks that path until disposition is recorded.

## Authority and evidence

GitHub is the sole source of truth. The reviewer is read-only and must bind the report to the audited default-branch SHA. Live repository/PR/issue/Actions state overrides stale snapshots in documentation.

The reviewer must clearly distinguish:

- observed GitHub facts;
- engineering inference;
- recommendation.

`IndustrialMDE` is outside scope and must not be modified.

## Mandatory audit scope

Review at minimum:

- root repository structure and current default-branch tree;
- `README.md`, `AGENTS.md`, project-state/handoff documents and roadmap/architecture documents;
- `src/Domain`, `src/PlcCompiler`, `src/SiemensBackend`, `src/GeneratorCli`, `src/TiaV21Worker`;
- all deterministic tests and examples;
- all `.github/workflows/**` relevant to CI, coding, repair, external review, TIA and methodology telemetry;
- `agents/prompts/**` and `agents/runtime/**`;
- `tasks/**`, `reviews/**`, external-review schemas/protocol;
- current open PRs/issues and representative recent Actions/merge evidence;
- methodology documents and telemetry issue #25;
- Siemens Open Library qualification/target-profile strategy and vendor-data boundary.

## Required analysis dimensions

The report must cover:

1. product architecture and roadmap coherence;
2. domain/IR/compiler/backend separation and deterministic generation;
3. Siemens/TIA/Open Library correctness and trusted execution boundary;
4. code quality, maintainability and unnecessary abstractions;
5. test quality, missing coverage and whether tests prove the important contracts;
6. CI/CD and autonomous-agent reliability;
7. security, authority, reviewer independence and candidate/trusted-main separation;
8. documentation consistency and stale state;
9. AI collaboration efficiency, including human toil and unnecessary ceremony;
10. prompt quality for coder, repair and reviewers;
11. methodology quality and whether process complexity is justified by evidence;
12. near-term sequencing: what should block generator development and what should explicitly not block it.

## Prompt review requirement

The reviewer must inspect every file under `agents/prompts/**` plus the runtime/context code that consumes those prompts. It must not review prompt prose in isolation.

For each prompt, report:

- what is already effective and should remain;
- ambiguity, contradiction, duplication or missing context;
- instructions that are unenforceable or conflict with workflow policy;
- token/context waste;
- missing finish criteria or evidence requirements;
- opportunities to make coding/repair more autonomous without weakening trust controls.

Where improvement is justified, provide complete replacement prompt text, not only comments. At minimum cover:

- `agents/prompts/coder.md`;
- `agents/prompts/repair.md`;
- `agents/prompts/reviewer-requirements.md`;
- `agents/prompts/reviewer-tia.md`.

Also recommend targeted changes to `AGENTS.md`, `docs/AI_COLLABORATION_MODEL.md` or context-building logic when prompt effectiveness depends on them.

## Required report structure

1. **Audit identity** — audited repository, default-branch SHA, date, relevant live PR/issues/Actions inspected.
2. **Executive assessment** — concise verdict and overall health.
3. **What is strong** — practices/architecture worth preserving.
4. **Findings** — `critical`, `major`, `minor`, each with concrete file/workflow/evidence references, impact and recommended action.
5. **Architecture review** — current architecture vs stated product goal and roadmap.
6. **Code and tests review** — maintainability/correctness/test gaps.
7. **TIA / Siemens / Open Library review** — integration and trust boundary.
8. **AI operating model review** — autonomy, review independence, process overhead and human burden.
9. **Prompt audit** — per-prompt assessment.
10. **Recommended replacement prompts** — complete proposed texts.
11. **Simplification plan** — controls to keep, automate, remove or defer.
12. **Prioritized action plan** — P0/P1/P2, explicitly marking which items must block generator development and which must not.
13. **Next 5 bounded tasks** — suggested task sequence toward the product goal.
14. **Periodic-audit feedback** — whether this audit cadence/scope should change based on evidence.

## Persistence

Keep one tracking issue open for periodic audits. Each completed audit is recorded there with:

- audited main SHA;
- reviewer identity/type;
- link or full report;
- critical/major finding summary;
- follow-up issues/tasks created;
- count of merged implementation PRs since the prior audit;
- next milestone/cadence trigger.

Material methodology lessons are also linked from methodology telemetry issue #25 and may later be promoted into `docs/DEVELOPMENT_METHODOLOGY.md` after architectural evaluation.
