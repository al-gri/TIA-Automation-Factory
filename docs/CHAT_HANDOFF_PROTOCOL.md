# Primary ChatGPT Chat Handoff Protocol

Status: candidate under trusted task `tasks/CHAT-HANDOFF-001.json`.

GitHub is the sole durable source of truth. This protocol transfers the **primary connected ChatGPT role**, not chat memory.

## 1. Trigger

Activate this procedure on any clear transfer request, including:

- `переходим в другой чат`;
- `переходим в новый чат`;
- `готовь handoff`;
- `сделай handoff`;
- equivalent unambiguous wording.

Do not ask the human to reconstruct context available in GitHub.

## 2. Transaction

```text
freeze discretionary work
  -> live GitHub freshness audit
  -> reconcile chat claims with GitHub
  -> finish only safe atomic orchestration already in flight
  -> persist factual checkpoint
  -> verify stale-evidence status
  -> emit compact fresh-chat bootstrap prompt
```

Handoff is complete only when the durable checkpoint and bootstrap prompt identify the same next safe gate.

## 3. Freeze rule

On trigger:

1. stop new discretionary implementation/research/refactors;
2. finish only already-authorized atomic bookkeeping needed to leave coherent state;
3. do not merge/start TIA/consume repair budget merely to make the handoff cleaner;
4. preserve exact-SHA review semantics and do not casually move an in-flight candidate.

## 4. Mandatory live audit

Re-read live GitHub:

1. `AGENTS.md`;
2. `docs/PROJECT_STATE.md`;
3. `docs/NEXT_CHAT_HANDOFF.md`;
4. `docs/AI_COLLABORATION_MODEL.md`;
5. `docs/DEVELOPMENT_METHODOLOGY.md` and latest relevant journal entries;
6. `docs/EXTERNAL_REVIEW_PROTOCOL.md` when review work is active;
7. active trusted `tasks/*.json`;
8. relevant open PRs/issues and exact head/base SHA;
9. review request identity/round/verdict and repair budget;
10. relevant CI/Actions/TIA evidence and sanitized issue evidence;
11. methodology telemetry when it affects the checkpoint;
12. newly created work not yet reflected in snapshot docs.

## 5. Reconciliation

- GitHub wins over chat memory and older handoff documents.
- Unverified chat-only claims remain coordination, not durable fact.
- Stale snapshots are corrected or explicitly marked stale/pending.
- Review approval for SHA-A is never authority for SHA-B.
- If a persistence merge would stale an in-flight candidate base/head/review, defer that merge or explicitly require a fresh exact-SHA gate.
- Do not invent a clean story when GitHub evidence is inconsistent.

## 6. Persistence surfaces

Synchronize factual state when changed:

- `docs/PROJECT_STATE.md` — durable completed milestones, active work, blockers, order, boundaries;
- `docs/NEXT_CHAT_HANDOFF.md` — latest transfer checkpoint with main SHA, active PR/task, exact gates, blockers and next action;
- `docs/INFRASTRUCTURE_LOG.md` — concise chronology;
- `docs/METHODOLOGY_JOURNAL.md` and methodology rules when a reusable lesson exists;
- PR/issue state for traceability.

A handoff checkpoint must not smuggle new architecture, authority, reviewer policy, risk waiver, repair-budget extension, workflow semantics or project goals.

## 7. Write precondition

Before every handoff write:

1. name the target branch/ref explicitly;
2. verify that ref and current main;
3. determine whether the write moves main or a reviewed candidate/base;
4. avoid silent exact-SHA invalidation;
5. re-fetch after the write.

Never rely on an omitted branch parameter for a handoff write.

## 8. Completion check

Before telling the user to open the new chat, verify:

- current trusted main SHA;
- active PR exact head/base and scope;
- latest CI/review/TIA state;
- pending reviewer/user action;
- stale review packages that must not be reused;
- whether handoff docs are merged or pending review;
- no secret/vendor payload/private signing material entered handoff artifacts.

## 9. Old-chat output

The old chat returns:

1. confirmation that the audit ran;
2. trusted main SHA;
3. active exact candidate/review/blocker;
4. remaining human action;
5. one ready-to-paste bootstrap prompt.

Do not paste the old conversation.

## 10. Fresh-chat contract

The bootstrap prompt instructs the new chat to:

- act as primary connected ChatGPT / Senior Architect / orchestrator / methodology curator / delegated routine technical merge authority;
- treat GitHub as sole source of truth;
- execute `AGENTS.md` startup and read this protocol;
- independently verify live tasks, PRs, exact SHAs, reviews, Actions/TIA and pending agent work;
- continue from the current safe gate, not from pasted claims;
- preserve reviewer independence and exact-SHA semantics;
- never touch `IndustrialMDE`;
- never request an old-chat transcript when GitHub contains the needed state.

Recommended minimal form:

```text
Продолжай `al-gri/TIA-Automation-Factory` как primary connected ChatGPT / Senior Software Architect / orchestrator / methodology curator / delegated routine merge authority. GitHub — единственный источник истины. Выполни startup sequence из `AGENTS.md`, прочитай `docs/CHAT_HANDOFF_PROTOCOL.md` и `docs/NEXT_CHAT_HANDOFF.md`, затем самостоятельно перепроверь live tasks/PRs/Actions/review requests и продолжай с текущего безопасного gate. Не используй старый чат как authority и не трогай `IndustrialMDE`.
```
