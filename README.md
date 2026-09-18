# TIA Automation Factory

Automation engineering software factory targeting Siemens TIA Portal V21 through TIA Portal Openness.

The project deliberately builds two things in parallel:

1. the PLC/TIA automation generator;
2. a reusable, evidence-driven methodology for developing software with AI coding agents, independent review and trusted execution environments.

## Start here — mandatory for AI / fresh chats

GitHub is the only durable source of truth for this project. No prior chat history is required or authoritative.

Before doing project work, read:

1. `AGENTS.md` — mandatory repository-first operating contract and delegated authority.
2. `docs/PROJECT_STATE.md` — current authoritative operational state, active blocker and next action.
3. `docs/NEXT_CHAT_HANDOFF.md` — complete fresh-chat handoff and current order of work.
4. `docs/AI_COLLABORATION_MODEL.md` — collaboration/provider/reviewer model.
5. `docs/DEVELOPMENT_METHODOLOGY.md` — living reusable AI-assisted development methodology.
6. `docs/METHODOLOGY_JOURNAL.md` — curated milestone lessons.
7. `docs/EXTERNAL_REVIEW_PROTOCOL.md` — external review protocol.

A brand-new ChatGPT session should be able to inspect this repository and continue safely without any context copied from another chat.

Raw automated methodology telemetry is stored in GitHub issue #25 after the telemetry workflow is merged.

## Architecture

`Automation input` → Automation Domain → PLC IR → Siemens SCL → trusted Windows self-hosted runner → TIA Portal V21 → diagnostics.

- `src/Domain` — vendor-neutral automation model.
- `src/PlcCompiler` — validation and conversion to vendor-neutral PLC IR.
- `src/SiemensBackend` — Siemens-specific SCL generation.
- `src/GeneratorCli` — CLI used by CI to generate artifacts from JSON.
- `src/TiaV21Worker` — Windows/.NET Framework 4.8 trust boundary for TIA Portal V21 Openness.
- `tests` — deterministic unit/output/governance tests.
- `tasks` — trusted versioned task specifications.
- `reviews` — external-review templates and response schema.
- `agents` — trusted coding/reviewer runtime helpers and prompts.

## AI operating model

- Coding provider order: **OpenRouter first**, then official **DeepSeek `deepseek-flash`**.
- Primary connected ChatGPT is Senior Architect, orchestrator, normal coding-agent reviewer, methodology curator and delegated technical merge authority.
- A fresh isolated `chatgpt-secondary` session is the independent reviewer when primary ChatGPT materially authored/co-authored the candidate.
- Gemini has no standing project role.
- Coding agents receive a bounded trusted context bundle from Git source of truth; task `contextFiles` may add focused design/vendor contracts.
- Optional Cline-style interactive development must use the same repository context, branch/review rules and protected boundaries; it is not the production orchestrator.
- AI candidate source runs only on disposable Linux runners in the autonomous path.
- Self-hosted Windows/TIA source is trusted-main-only.
- TIA Portal V21 compile/Openness diagnostics remain authoritative Siemens acceptance.
- No coding agent/reviewer/workflow may self-merge. Primary connected ChatGPT may perform delegated routine technical merges only after exact-SHA gates are satisfied.

See `AGENTS.md`, `docs/NEXT_CHAT_HANDOFF.md`, `docs/AI_COLLABORATION_MODEL.md`, and `docs/DEVELOPMENT_METHODOLOGY.md` for normative operating rules.

## Methodology capture

- `docs/DEVELOPMENT_METHODOLOGY.md` — current rules, experiments and metrics.
- `docs/METHODOLOGY_JOURNAL.md` — curated chronological lessons.
- GitHub issue #25 — append-only raw automated workflow/PR diary.
- `.github/workflows/methodology-telemetry.yml` — automatic event capture.

Primary connected ChatGPT is required to update methodology at logical milestones without waiting for the user to request documentation.

## Local cloud-independent check

```bash
dotnet test tests/SiemensBackend.Tests/SiemensBackend.Tests.csproj
dotnet run --project src/GeneratorCli/GeneratorCli.csproj -- examples/motor.json artifacts/generated
```

## TIA V21 machine

The TIA worker is intentionally isolated from the rest of the system. The Windows account running it must be allowed to use TIA Portal Openness and the machine must have TIA Portal V21 installed.

See `docs/WINDOWS_RUNNER_SETUP.md`.
