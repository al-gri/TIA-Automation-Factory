# Primary ChatGPT Chat Handoff Protocol

Status: candidate under trusted task `tasks/CHAT-HANDOFF-001.json`.

GitHub is the sole durable source of truth. This protocol transfers the **primary connected ChatGPT role**, not chat memory.

## 1. Trigger

The current primary ChatGPT must activate this procedure when the human writes any clear transfer command, including:

- `переходим в другой чат`;
- `переходим в новый чат`;
- `готовь handoff`;
- `сделай handoff`;
- an equivalent unambiguous request to move project work to a fresh chat.

Do not ask the human to reconstruct context that GitHub can provide. Do not require a special exact phrase if intent is clear.

## 2. Handoff is a transaction

The handoff sequence is:

```text
freeze discretionary work
  -> live GitHub freshness audit
  -> reconcile chat claims with GitHub
  -> finish safe atomic orchestration already authorized
  -> persist factual checkpoint
  -> verify checkpoint and stale-evidence status
  -> emit compact fresh-chat bootstrap prompt
```

The transfer is incomplete until the durable GitHub checkpoint and the user-facing bootstrap prompt agree on the next safe gate.

## 3. Freeze rule

On trigger:

1. stop starting new discretionary implementation, research, refactors or unrelated repairs;
2. finish only safe atomic bookkeeping/orchestration already in progress when leaving it half-applied would make repository state ambiguous;
3. do not merge, widen scope, consume a review, start trusted Windows/TIA execution or take another authority-bearing action merely to make the handoff look cleaner;
4. preserve exact-SHA review semantics: if a candidate/review is frozen, do not mutate its head/base casually.

## 4. Mandatory freshness audit

Re-read from live GitHub, not from chat memory:

1. `AGENTS.md`;
2. `docs/PROJECT_STATE.md`;
3. `docs/NEXT_CHAT_HANDOFF.md`;
4. `docs/AI_COLLABORATION_MODEL.md`;
5. `docs/DEVELOPMENT_METHODOLOGY.md`;
6. latest relevant `docs/METHODOLOGY_JOURNAL.md` entries;
7. `docs/EXTERNAL_REVIEW_PROTOCOL.md` when review work is active;
8. `docs/GOVERNANCE_BOOTSTRAP.md` only when a live task truly uses the exceptional bootstrap lane;
9. active trusted task files under `tasks/`;
10. open PRs/issues relevant to active work;
11. exact PR head/base SHA, changed files, review request identity/round/verdict and repair budget;
12. relevant CI/Actions/TIA runs and artifacts/comments;
13. methodology telemetry issue #25 when recent evidence affects the checkpoint.

Also inspect newly created/open work that is not yet mentioned in the versioned state documents.

## 5. Reconciliation rules

During the audit:

- GitHub wins over chat memory or an older handoff document.
- A chat claim that cannot be verified in GitHub is coordination only and must not be promoted to durable project fact.
- A stale `PROJECT_STATE.md` or `NEXT_CHAT_HANDOFF.md` must be corrected before transfer when the correction can be made without violating an in-flight exact-SHA gate.
- Old review verdicts remain historical evidence but are not authority for a different candidate SHA.
- A base/head change that invalidates an external-review package must be recorded explicitly; the new chat must be told that a fresh exact-SHA CI/review package is required.
- If a requested persistence change would itself invalidate a frozen candidate/review, prefer recording the pending persistence work separately rather than silently invalidating the gate. If invalidation is unavoidable, make it explicit before transfer.

## 6. Safe work to finish before handoff

Primary may complete already-authorized non-discretionary orchestration such as:

- reading pending review JSON and validating identity/freshness;
- writing a factual issue/PR trace comment;
- recording an observed blocker;
- closing a state transition already made true by GitHub evidence;
- methodology checkpoint bookkeeping;
- reverting an accidental incomplete connector write.

Primary must not use handoff as authorization for a new product change, risk waiver, repair-budget extension, governance exception, destructive action or vendor/licensing decision.

## 7. Persistence surfaces

When facts changed, synchronize the appropriate durable surfaces:

### `docs/PROJECT_STATE.md`

Keep the authoritative operational snapshot current. It should distinguish:

- durable completed milestones;
- active work;
- current blockers/gates;
- accepted next order of work;
- hard boundaries.

Do not preserve obsolete active-state narratives merely because they were once true.

### `docs/NEXT_CHAT_HANDOFF.md`

This is the **latest machine-usable transfer checkpoint**, not a historical diary. It should include:

- checkpoint timestamp/date and trusted `main` SHA;
- current objective/phase;
- active trusted task(s);
- active PR(s), exact head/base SHA and changed-file scope;
- exact CI/review/TIA state;
- pending external-review request identity and reviewer slot when applicable;
- known stale review packages/evidence that must not be reused;
- blockers and whether human action is required;
- exact next safe action;
- hard boundaries.

### `docs/INFRASTRUCTURE_LOG.md`

Append concise factual infrastructure/process milestones; do not copy raw logs.

### `docs/METHODOLOGY_JOURNAL.md` / `docs/DEVELOPMENT_METHODOLOGY.md`

Run the normal methodology checkpoint. Record material lessons and promote only genuinely reusable rules.

### Issues / PR comments / issue #25

Use them for traceability and raw factual telemetry when appropriate. Comments are not a substitute for normative files or trusted tasks.

## 8. Normative-change boundary

A handoff checkpoint is not a shortcut around normal governance.

Do **not** smuggle any of the following into a factual handoff sync:

- new architecture semantics;
- new permissions/authority;
- reviewer-policy changes;
- merge-gate weakening;
- risk waivers;
- repair-budget extensions;
- protected workflow behavior;
- project-goal changes.

Such changes need their own trusted task, candidate, deterministic evidence and independent review according to normal policy.

## 9. Write precondition

Before every repository write performed during handoff:

1. name the intended target branch/ref explicitly;
2. verify that ref exists and is the intended authority surface;
3. verify whether the write would move `main`, a candidate head or a reviewed base;
4. if it would invalidate an exact-SHA/base review, either avoid the write or record that a fresh review is required;
5. after the write, re-fetch the affected ref/PR and verify the actual result.

Never rely on an omitted branch parameter for a handoff-related write.

## 10. Handoff completion check

Before telling the user to open a new chat, verify from GitHub:

- the trusted `main` SHA used by the checkpoint;
- relevant open PR exact head/base SHA;
- latest CI/review/TIA state;
- pending reviewer/user action;
- whether any review package became stale during the handoff procedure;
- whether the handoff documents themselves are merged, pending review, or intentionally deferred;
- no accidental vendor payload, secret, private signing material or unrelated file entered the transfer artifacts.

If a durable document cannot be safely updated because doing so would invalidate an in-flight gate, state that explicitly in both the handoff checkpoint mechanism available at the time and the user-facing bootstrap package.

## 11. Old-chat output contract

The final response of the old chat should be short and contain:

1. confirmation that the handoff audit ran;
2. trusted `main` SHA at checkpoint;
3. active exact candidate/review/blocker state;
4. any user action still required;
5. one ready-to-paste fresh-chat bootstrap prompt.

Do not paste the full old conversation into the new chat.

## 12. Fresh-chat bootstrap contract

The generated prompt must instruct the new chat to:

- act as primary connected ChatGPT / Senior Architect / orchestrator / methodology curator / delegated technical merge authority;
- treat GitHub as the only durable source of truth;
- execute the startup sequence in `AGENTS.md` and read this protocol;
- independently verify live tasks, PRs, exact SHAs, review requests, Actions/TIA evidence and pending agent requests;
- continue from the current live gate rather than trusting the pasted prompt as authority;
- process already-authorized pending requests itself when safe;
- preserve reviewer independence and exact-SHA semantics;
- never touch `IndustrialMDE`;
- never request old-chat transcript/history when GitHub contains the needed state.

Recommended minimal bootstrap form:

```text
Продолжай `al-gri/TIA-Automation-Factory` как primary connected ChatGPT / Senior Architect. GitHub — единственный источник истины. Выполни startup sequence из `AGENTS.md`, прочитай `docs/CHAT_HANDOFF_PROTOCOL.md` и `docs/NEXT_CHAT_HANDOFF.md`, затем самостоятельно перепроверь live tasks/PRs/Actions/review requests и продолжай с текущего безопасного gate. Не используй старый чат как источник состояния и не трогай `IndustrialMDE`.
```

The old chat may add one checkpoint identifier/SHA and one pending reviewer request identity to this prompt for navigation, but those values must still be re-verified live.

## 13. Failure handling

If the handoff audit discovers inconsistent or unsafe state:

- do not manufacture a clean story;
- mark the exact inconsistency/blocker;
- persist it when authorized;
- tell the new chat the first action is to resolve/re-verify that blocker;
- never carry a stale APPROVE or ambiguous authority across the chat boundary.

The goal is continuity with integrity, not continuity at any cost.
