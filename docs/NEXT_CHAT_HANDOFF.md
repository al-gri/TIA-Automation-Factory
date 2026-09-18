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
- primary connected ChatGPT: Senior Architect + normal coding-agent reviewer + orchestrator + delegated technical merge authority;
- `chatgpt-secondary`: fresh isolated ChatGPT used when primary ChatGPT materially authored/co-authored the candidate or when explicit independent escalation is requested;
- Gemini: no standing project role;
- real TIA Portal V21 compile/Openness execution: authoritative Siemens acceptance.

One independent external reviewer is the default even for HIGH risk. Coding-agent-authored candidate -> primary `chatgpt` when independent. Primary-ChatGPT-authored/co-authored candidate -> `chatgpt-secondary`. Additional simultaneous review is escalation only.

Human input is reserved for strategic/materially irreversible decisions, risk waivers, destructive external actions, licensing/vendor-distribution choices and unresolved reviewer conflict/ambiguity.

## Current state

### Architecture

PR #17 is merged at `db8e1bcacc6febb15fa5817a2d2b22d15ccbe58e`. Issue #18 is closed.

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

PR #16 merged at `64daa260416b7a4163f7627b668ae155db694919` after exact trusted TIA Portal V21 compile: **0 errors / 0 warnings**.

### OLQ infrastructure

- PR #21 merged at `7430f83140de4bdf4d4b53c564373b15d14fb378` — task-gated `TiaV21Worker` candidate support and trusted-main-only Windows/TIA execution.
- PR #23 merged at `6ca057eb5dddf3986e1b00ef8e653c044c998938` — bounded HIGH repair support.

### OLQ-001 candidate

Canonical task: `tasks/OLQ-001.json`.

PR #22 is the active implementation candidate.

Current exact code head after two autonomous repair attempts plus one bounded primary-ChatGPT maintainer repair:

`4354bebbf2a3bf745b09589d6abac0938d5b5664`

CI #164 / run `35366781733`: PASS.

The code-level defects F001-F008 are considered resolved by the latest independent external review. The remaining blocker is reviewer-slot governance: primary ChatGPT became a material co-author and therefore cannot independently approve PR #22.

### Reviewer-policy change

The human operator explicitly replaced Gemini with a second isolated ChatGPT reviewer because Gemini's GitHub access was unreliable for this workflow.

Governance PR #24 is active. It changes the project policy so that:

- coding-agent candidate -> primary `chatgpt` when independent;
- primary-ChatGPT-authored/co-authored candidate -> `chatgpt-secondary`;
- Gemini has no standing role;
- one independent reviewer remains sufficient by default;
- `tasks/OLQ-001.json` authorizes both `chatgpt` and `chatgpt-secondary`, allowing authorship-based reviewer transition without introducing mandatory dual review.

Primary ChatGPT authors PR #24, so #24 itself requires a clean independent `chatgpt-secondary` review before merge.

## Immediate next action

1. Inspect current PR #24 head and CI.
2. Prepare/obtain an independent `chatgpt-secondary` review for exact PR #24 SHA.
3. If APPROVE + CI green, primary ChatGPT performs delegated merge of #24.
4. Re-run independent `chatgpt-secondary` review for unchanged PR #22 code candidate under the now-authorized trusted task slot.
5. If APPROVE + exact-SHA CI green, primary ChatGPT merges #22.
6. Build trusted `main` `TiaV21Worker` on Windows/TIA V21.
7. Run OLQ qualification against the actual external `.zal19` and repeat once for deterministic reuse proof.
8. Persist manifest/diagnostics/hashes only; never commit vendor `.zal19/.zal21` payload.
9. Human accepts the resulting qualified library profile before it becomes a normal generator dependency.

## Roadmap after OLQ-001

`OL-001` -> `OL-002` -> PLC compiler foundations -> `GEN-001` WaterSystem/fbValve_Solenoid vertical slice.

Do not add broad UI, generic plugin infrastructure, F-safety generation or unrelated hardware generation ahead of this sequence.

## Fresh-chat behavior

When asked to inspect the repo/reviews/agent state, primary connected ChatGPT should inspect GitHub directly, process authorized pending work and merge technically accepted PRs without separate merge prompts.

If primary ChatGPT is not independent, it prepares a complete ready-to-paste request for a fresh `chatgpt-secondary` session and never self-approves.
