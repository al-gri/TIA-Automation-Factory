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

The work prompt contains the current versioned task or GitHub issue. Repair prompts also contain the trusted task, current diff and bounded review/validation evidence.

The bundle does not replace repository inspection. The coding model must still inspect the current implementation and tests directly from the workspace before editing.

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

Context files are read with `git show` from the trusted Git ref used by the coding runtime, normally `origin/main`. Candidate workspace versions are not used for the trusted context bundle.

Rules:

- repository-relative normalized paths only;
- no `..`, absolute paths, backslash paths or `.git` paths;
- UTF-8 text only;
- maximum 12 task-declared context files;
- maximum 128 KiB per trusted context file;
- maximum 768 KiB for the already assembled work prompt;
- maximum 1 MiB combined trusted context source before rendering.

Missing, malformed, duplicate or oversized context declarations fail closed before a coding provider is called.

## Provider continuity

The enriched prompt is built once before provider selection. OpenRouter and DeepSeek receive the same trusted context bundle.

If OpenRouter reaches a late quota/rate limit after producing useful workspace changes, DeepSeek continues with the same enriched prompt plus the surviving bounded workspace changes. Provider fallback does not rebuild context from untrusted candidate files.

## Audit evidence

The coding provider audit records a context-bundle manifest containing:

- resolved trusted Git commit;
- included baseline/context file paths;
- byte sizes;
- SHA-256 hashes;
- task ID when recoverable;
- final rendered prompt SHA-256.

This makes the implementation context reproducible without committing generated prompt payloads to the repository.

## Siemens/Open Library rule

When a task depends on a qualified Siemens/Open Library object, put the qualified machine-readable or human-readable contract/profile in `contextFiles`. The coding agent must use that contract as source of truth and must not invent undocumented FB names, interfaces, parameters, UDT layouts or migration behavior.
