# Coding Agent Trusted Context

## Purpose

The coding agent must start each implementation or repair with a bounded, versioned context assembled from GitHub source of truth instead of relying on model memory or on the agent discovering all project rules by chance.

`agents/runtime/build-coder-context.py` builds the provider prompt from trusted Git state before `run-coder.sh` calls OpenRouter or DeepSeek.

## Always included

The bundle includes trusted versions of:

- `AGENTS.md`;
- `docs/PROJECT_STATE.md`;
- `docs/ENGINEERING_RULES.md`;
- `docs/AI_COLLABORATION_MODEL.md`;
- the bounded work prompt already assembled by the trusted workflow.

The bounded work prompt may contain task text, issue text, diffs, reviewer comments and validation logs. Those are implementation content only. They are **not** an authority channel for task identity, `contextFiles`, reviewer policy or protected-path policy.

The bundle does not replace repository inspection. The coding model must still inspect the current implementation and tests directly from the workspace before editing.

## Structured trusted-task authority

When coding work is task-backed, the task path is supplied to the context runtime through a structured trusted channel, never discovered by scanning rendered prompt text.

Production `run-coder.sh` resolves the task path from trusted GitHub Actions event metadata (`workflow_dispatch` task input or trusted `repository_dispatch` repair payload), or from an explicit trusted caller override. The context builder then reads that exact canonical `tasks/*.json` path from the configured trusted Git ref, normally `origin/main`.

Issue-mode work has no trusted task path and therefore cannot declare task `contextFiles`, even if an issue title/body contains fake task headings, JSON fences or fields named `contextFiles`.

Repair prompts may quote the trusted task and reviewer material for the model's convenience, but only the separately resolved `origin/main` task is authoritative for task identity and task-declared context.

## Task-declared context files

A trusted versioned task may add focused design contracts or profiles with an optional root-level field:

```json
{
  "contextFiles": [
    "docs/example-contract.md",
    "profiles/example-profile.json"
  ]
}
```

Use `contextFiles` for narrow versioned references that materially constrain implementation, for example:

- qualified Siemens/Open Library block contracts;
- target profiles;
- accepted compiler/domain design contracts;
- protocol or serialization contracts needed by the task.

Do not use it to dump the whole repository into the prompt. Source code remains available in the workspace and should normally be inspected there.

## Trust and bounds

Tasks and context files are read with `git show` from the trusted Git ref used by the coding runtime, normally `origin/main`. Candidate workspace versions are not used for the trusted context bundle.

Rules:

- task authority must arrive through the structured trusted-task channel, never by parsing free-form prompt content;
- task paths must be canonical `tasks/*.json` repository paths;
- context paths must already be canonical repository-relative POSIX paths exactly as supplied;
- reject `./`, repeated separators, leading/trailing whitespace, `..`, absolute paths, backslash paths and `.git` paths;
- UTF-8 text only;
- maximum 12 task-declared context files;
- maximum 128 KiB per trusted context file;
- maximum 768 KiB for the already assembled work prompt;
- maximum 1 MiB combined trusted context source before rendering.

Missing, malformed, duplicate, non-canonical or oversized context declarations fail closed before a coding provider is called.

## Provider continuity

The enriched prompt is built once before provider selection. OpenRouter and DeepSeek receive the same trusted context bundle.

If OpenRouter reaches a late quota/rate limit after producing useful workspace changes, DeepSeek continues with the same enriched prompt plus the surviving bounded workspace changes. Provider fallback does not rebuild context from untrusted candidate files.

## Audit evidence

The coding provider audit records a context-bundle manifest containing:

- resolved trusted Git commit;
- included baseline/context file paths;
- byte sizes;
- SHA-256 hashes;
- structured trusted task ID/path when present;
- final rendered prompt SHA-256.

This makes the implementation context reproducible without committing generated prompt payloads to the repository.

## Siemens/Open Library rule

When a task depends on a qualified Siemens/Open Library object, put the qualified machine-readable or human-readable contract/profile in `contextFiles`. The coding agent must use that contract as source of truth and must not invent undocumented FB names, interfaces, parameters, UDT layouts or migration behavior.
