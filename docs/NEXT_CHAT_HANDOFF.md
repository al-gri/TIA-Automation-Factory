# TIA Automation Factory — Fresh Chat Handoff

This document is the durable handoff for continuing the project from a brand-new ChatGPT conversation.

GitHub is the only durable source of truth. Do not rely on previous chat history or memory. A clean ChatGPT session must recover the project from the repository, issues, pull requests, Actions evidence, and versioned tasks.

## What the project is

Repository: `al-gri/TIA-Automation-Factory`

Do not modify `al-gri/IndustrialMDE`.

Goal: build a software factory that converts vendor-neutral automation models into Siemens PLC artifacts and verifies those artifacts through real TIA Portal V21 using a trusted Windows/TIA Openness boundary.

Current architecture:

```text
Automation input / future visual designer
  -> Domain Model
  -> PLC Compiler / PLC IR
  -> Siemens Backend
  -> generated SCL / PLC artifact
  -> trusted TiaV21Worker
  -> TIA Portal V21
  -> diagnostics
```

Vendor-neutral projects must not reference `Siemens.Engineering`. TIA Openness remains isolated in `src/TiaV21Worker` targeting .NET Framework 4.8.

## Mandatory startup sequence in a fresh ChatGPT chat

When the user says `проверь репозиторий`, `продолжай проект`, `проверь DeepSeek`, `проверь запросы DeepSeek`, or equivalent:

1. Read root `AGENTS.md`.
2. Read `docs/PROJECT_STATE.md`.
3. Read this `docs/NEXT_CHAT_HANDOFF.md`.
4. Read `docs/AI_COLLABORATION_MODEL.md`.
5. Read `docs/EXTERNAL_REVIEW_PROTOCOL.md` if review is involved.
6. Read the active `tasks/*.json` task referenced by the current PR/workflow.
7. Inspect current open candidate PRs, their current head SHA, latest review state, relevant Actions runs, artifacts, and TIA diagnostics.
8. Use GitHub state, not chat history, to decide the next action.
9. Persist any durable decision, blocker, architecture change, or meaningful result back to GitHub.

The user must not be asked to manually gather context that already exists in GitHub.

## Human interaction model

Chat is only the operator console.

Normal user commands are intentionally short:

- `проверь репозиторий`
- `проверь запросы DeepSeek`
- `что ждёт review?`
- `проверь PR #N`

For these commands ChatGPT should independently inspect GitHub, perform its authorized review/orchestration actions, write the result back to GitHub when needed, and return only useful operational information to the user.

Do not dump routine logs or large diffs into chat unless needed for a decision.

## Coding-provider policy

Routine coding uses this order:

1. **OpenRouter first** while the configured free coding model is available.
2. **DeepSeek second**, official API model `deepseek-flash`, when OpenRouter is unavailable, rate-limited, timed out, or daily quota is exhausted.
3. If OpenRouter exhausts quota after already producing real workspace changes, preserve those changes and let DeepSeek continue the same bounded workspace.
4. If DeepSeek later fails after producing a valid partial candidate, deterministic validation may still evaluate the preserved candidate when repository policy allows it.
5. **Gemini is not a routine coding fallback.** It is reserved for independent review / red-team escalation.

Current OpenRouter coding model:

```text
nvidia/nemotron-3-ultra-550b-a55b:free
```

DeepSeek OpenCode audit name:

```text
deepseek/deepseek-flash
```

Required GitHub Actions secrets include:

```text
OPENROUTER_API_KEY
DEEPSEEK_API_KEY
```

Reviewer infrastructure may also use configured reviewer-provider secrets. Never commit secret values.

## Proven provider behavior

OpenRouter has already been proven in the autonomous coding path, including quota/fallback handling.

DeepSeek official API was proven in Autonomous Agent run `35318239703` with:

- provider: `deepseek`
- model: `deepseek/deepseek-flash`
- 14 completed agent steps
- 16,324 input tokens
- 2,271 output tokens
- 1,490 reasoning tokens
- 187,264 cache-read tokens
- reported cost: `$0.005266992`

The repository now implements OpenRouter-first -> DeepSeek-second coding in `agents/runtime/run-coder.sh` and aligned agent/repair workflows.

## Review roles

### ChatGPT

ChatGPT is the Senior Software Architect and primary connected external reviewer.

For pending review requests assigned to the `chatgpt` reviewer slot, ChatGPT must:

- verify the request matches the current PR head SHA;
- read the trusted task from `main`;
- inspect the bounded candidate diff and relevant unchanged context;
- inspect Linux tests / generator evidence and TIA evidence when available;
- independently decide `APPROVE`, `CHANGES_REQUIRED`, or `BLOCKED`;
- submit the schema-valid structured review response back to GitHub;
- never approve merely because tests are green.

### Gemini

Gemini is an independent reviewer / red-team escalation path.

ChatGPT must never impersonate Gemini or fill a `gemini` reviewer slot.

If Gemini is required, ChatGPT must give the user one complete ready-to-paste Gemini message containing all necessary GitHub-derived context, evidence, candidate identity, review objectives, and required response format. The user should not have to assemble anything manually.

### Risk policy

LOW risk:

```text
OpenRouter/DeepSeek coder
  -> deterministic Linux checks
  -> ChatGPT external review
  -> TIA when applicable
```

MEDIUM risk:

```text
OpenRouter/DeepSeek coder
  -> ChatGPT review
  -> Gemini only if findings/uncertainty justify escalation
  -> deterministic/TIA gates
```

HIGH risk / architecture / PLC semantics / security:

```text
independent ChatGPT review
+
independent Gemini review
  -> aggregate decision
```

If the reviewers disagree, use `REVIEW_CONFLICT`. Do not auto-accept.

## External-review protocol

Normative document: `docs/EXTERNAL_REVIEW_PROTOCOL.md`.

Review requests are self-contained and bound to:

- `reviewRequestId`
- `reviewerSlot`
- `taskId`
- `candidateSha`
- `reviewType`
- `reviewRound`

Validated outcomes:

```text
APPROVE
CHANGES_REQUIRED
BLOCKED
REVIEW_CONFLICT
```

`CHANGES_REQUIRED` resumes a bounded repair on the same candidate PR. Task `maxRepairAttempts` remains authoritative. There is no infinite repair loop.

## Trusted validation path

For a versioned task candidate the trusted path is:

```text
versioned task in main
  -> coding agent on disposable GitHub Linux
  -> candidate branch + PR
  -> external ChatGPT review
  -> Candidate Validation
  -> trusted task resolved from main
  -> protected-path guard
  -> deterministic Linux tests/generation
  -> Requirements Reviewer
  -> exact candidate PLC artifact package
  -> trusted Windows runner checks out main
  -> TiaV21Worker
  -> TIA Portal V21 compile
  -> PLC/TIA Reviewer
  -> PASS / bounded repair / BLOCKED
```

Candidate C# is never executed on the trusted Windows/TIA machine. Windows receives only the bounded PLC package and runs trusted infrastructure from `main`.

No automatic merge is allowed.

## Completed infrastructure milestones

- Real TIA V21 Motor smoke: PASS, 0 errors / 0 warnings.
- Autonomous Git task -> coder -> PR: proven.
- Requirements Reviewer: proven.
- Safe Linux artifact -> trusted Windows/TIA bridge: proven.
- PLC/TIA Reviewer: proven.
- Bounded repair loop: proven on an intentionally incomplete candidate.
- I6 clean production infrastructure smoke: PASS with real TIA V21.
- Self-contained external review packages: implemented.
- Connected ChatGPT review from GitHub without manual user context transfer: proven.
- DeepSeek `deepseek-flash`: proven as real paid API coder.
- Repository-first clean-chat contract: implemented.
- Candidate Validation trusted-task bug fixed: task JSON is resolved from trusted `main`, not candidate checkout.
- End-to-end Phase 2 proof after the trust fix: PASS.

## Phase 2 proof

Task: `tasks/PHASE2-001.json`

Candidate PR: `#13`

Candidate SHA: `178f1dc376bc347a4dbb533a5efd43e4e6996dc3`

Connected ChatGPT recovered the review entirely from GitHub and returned `APPROVE` through the structured review protocol.

External Review Response run: `35319434427`.

Fresh Candidate Validation run after the trusted-task fix: `35322785146`.

That run passed:

- trusted task resolution from `main`;
- protected-path checks;
- deterministic Linux tests/generation;
- Requirements Reviewer;
- PLC artifact packaging;
- trusted Windows/TIA V21 acceptance;
- PLC/TIA Reviewer;
- final `repair-or-finish` with no repair required.

TIA diagnostics for exact `UDT_Motor.scl`:

```json
{
  "success": true,
  "state": "Success",
  "warnings": 0,
  "errors": 0
}
```

`UDT_Motor (UDT)` and `Main (OB1)` compiled successfully.

See `docs/PHASE2_PROOF_2026-09-18.md` for the compact proof record.

## Current code baseline

Main generator slice:

```text
src/Domain
src/PlcCompiler
src/SiemensBackend
src/GeneratorCli
src/TiaV21Worker
```

Current capabilities include:

- JSON `AutomationDevice` input;
- vendor-neutral `AutomationType` / device fields;
- compiler validation including duplicate field rejection;
- PLC IR generation;
- Siemens SCL UDT generation;
- CLI generation from examples;
- real TIA V21 import/compile diagnostics.

This is still an early generator baseline. The next phase should focus on the actual automation/domain/compiler/Open Library model rather than expanding orchestration without a concrete need.

## Normal task format

Versioned generator work should be placed under `tasks/*.json` with explicit requirements and TIA acceptance criteria.

Representative structure:

```json
{
  "id": "PLC-...",
  "title": "...",
  "goal": "...",
  "scope": [],
  "generator": {
    "input": "examples/...json",
    "expectedArtifact": "...scl"
  },
  "acceptance": {
    "requirements": [],
    "tia": []
  },
  "review": {
    "riskClass": "LOW|MEDIUM|HIGH",
    "reviewType": "CODE_REVIEW|PLC_REVIEW",
    "reviewerSlots": ["chatgpt"]
  },
  "protectedPaths": [],
  "maxRepairAttempts": 3
}
```

For HIGH risk, reviewer slots must include independent `chatgpt` and `gemini` review according to repository policy.

## Current next steps

Infrastructure is considered frozen unless a real generator-development blocker requires a change.

Next work should be:

1. return to generator architecture and Siemens Open Library analysis;
2. define the first real generator-domain task as a versioned `tasks/*.json` item;
3. run that task through OpenRouter first, with automatic DeepSeek `deepseek-flash` fallback when needed;
4. let connected ChatGPT review the resulting candidate directly through GitHub;
5. invoke Gemini only when risk policy requires it;
6. require deterministic Linux + real TIA V21 acceptance;
7. collect provider/token/cache/cost/duration/repair metrics across several real tasks;
8. consider LiteLLM or more complex provider pooling only if measured usage justifies it.

## Recommended first action in the next chat

The user can simply say:

> Проверь репозиторий `al-gri/TIA-Automation-Factory`, изучи `AGENTS.md`, `docs/PROJECT_STATE.md` и `docs/NEXT_CHAT_HANDOFF.md`. Продолжаем разработку PLC-генератора. Сначала определи текущий активный контекст и предложи следующий небольшой versioned generator task. Ничего не меняй в `IndustrialMDE`.

The assistant should then inspect GitHub and continue from repository state rather than asking the user to repeat project history.
