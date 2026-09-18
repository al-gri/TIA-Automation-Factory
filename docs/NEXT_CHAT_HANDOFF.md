# TIA Automation Factory — Fresh Chat Handoff

Updated: 2026-09-18

GitHub is the sole durable source of truth. Do not use old chat history as project state.

## Mandatory startup

Read, in order:

1. `AGENTS.md`
2. `docs/PROJECT_STATE.md`
3. this file
4. `docs/AI_COLLABORATION_MODEL.md`
5. `docs/EXTERNAL_REVIEW_PROTOCOL.md`

Then inspect current PRs, Actions, versioned tasks, issue #19 and pending external-review requests.

`IndustrialMDE` is outside scope and must not be touched.

## Roles and authority

- coding provider: OpenRouter first, official DeepSeek `deepseek-flash` fallback;
- ChatGPT: Senior Architect + primary connected reviewer + delegated technical merge authority;
- Gemini: independent reviewer/red-team when authorship or escalation requires it;
- TIA Portal V21 real compile/Openness execution: authoritative Siemens acceptance.

Reviewer independence is authorship-based. One independent external reviewer is the default even for HIGH risk. Coding-agent-authored candidate -> ChatGPT; ChatGPT-authored/co-authored candidate -> Gemini. Add a second reviewer only by escalation or explicit human request.

The human has delegated routine technical merge/no-merge decisions to connected ChatGPT after exact-SHA gates pass. Human input is reserved for strategic/materially irreversible decisions, risk waivers, destructive external actions, licensing/vendor-distribution choices, or unresolved review conflict/ambiguity.

## Current operational state

### Architecture

PR #17 is merged. Architecture merge commit: `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`. Issue #18 is closed.

Accepted direction remains:

```text
React/React Flow/table views
 -> canonical vendor-neutral AutomationProject
 -> Domain
 -> deterministic PLC Compiler / PLC IR
 -> SiemensBackend
 -> qualified Open Library catalog/bindings
 -> deterministic SCL AST/emitter
 -> bounded declarative package
 -> trusted TiaV21Worker / ProjectAssembler
 -> TIA Portal V21
```

### PLC-001

PR #16 is merged at merge commit `64daa260416b7a4163f7627b668ae155db694919`.

Exact reviewed candidate: `c4999464457eb5715c5b8590cb6b4d077002640f`.

`TIME` support passed deterministic Linux tests, connected ChatGPT review and exact trusted TIA Portal V21 compile: **0 errors / 0 warnings**.

### OLQ-001 workflow unblock

PR #21 is merged.

Exact Gemini-reviewed candidate: `3a66224e361c68ca065d3b3946a6278760b59138`.

Merge commit: `7430f83140de4bdf4d4b53c564373b15d14fb378`.

The merged change safely supports:

- non-GeneratorCli tasks;
- trusted task opt-in for bounded `src/TiaV21Worker/**` source changes;
- no candidate changes to workflows/tasks/prompts;
- one independent reviewer by authorship policy;
- no pre-merge Windows/TIA execution of candidate worker code.

### OLQ-001 versioned task

Canonical task exists on `main`:

`tasks/OLQ-001.json`

Task commit: `48b7ae7e80505ef75fc05ceef3e48bef2cd836e8`.

It requires an explicit `qualify-library` path in `TiaV21Worker`, source `.zal19` SHA256, exact V21 build identity, `GlobalLibraries.RetrieveWithUpgrade(...)`, save + compressed explicit `.zal21` archive, deterministic qualification identity, native reopen with current-version `Retrieve(...)`, mismatch failure, and machine-readable manifest/diagnostics. Vendor library payload must never be committed.

## Immediate next action

Start `.github/workflows/agent.yml` (**Autonomous Agent**) in task mode with:

- `source = task`
- `task_path = tasks/OLQ-001.json`
- `issue_number` empty

The current connected GitHub API surface does not expose creation of a `workflow_dispatch` event. This is an interface limitation, not a repository/workflow blocker. Do not bypass the trust model by using issue-mode `agent-ready`, because issue mode correctly lacks the trusted `TiaV21Worker` opt-in.

After the task-mode run exists, connected ChatGPT should continue without separate merge prompts:

1. inspect the coding-agent run and candidate PR;
2. perform independent ChatGPT review of the exact coding-agent SHA;
3. dispatch/handle bounded repair if needed;
4. merge autonomously once review + deterministic gates satisfy policy;
5. prepare/run trusted-main qualification on TIA V21 with the external `.zal19`;
6. persist qualification manifest/diagnostics;
7. request human input only when the qualified library profile itself requires final business/licensing acceptance before becoming a normal generator dependency.

## Roadmap after OLQ-001

`OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001` WaterSystem/fbValve_Solenoid vertical slice.

Do not add broad UI, generic plugin infrastructure, F-safety generation, or unrelated hardware generation ahead of this sequence.

## Fresh-chat behavior

When the user says `проверь репозиторий`, `проверь запросы`, `что ждёт review?`, etc., inspect GitHub directly and process authorized pending ChatGPT review/orchestration/merge actions without asking the user to collect context.

If Gemini is required, provide a complete ready-to-paste request bound to the exact current SHA; never impersonate Gemini.
