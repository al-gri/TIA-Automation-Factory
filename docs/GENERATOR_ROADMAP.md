# Generator Roadmap

Status: **PROPOSED — review round 3 required**

Goal: reach a production-capable `React Flow -> canonical model -> PLC compiler -> Siemens Open Library -> TIA Openness -> ready TIA Portal V21 project` through small versioned tasks.

The roadmap is risk-first: prove real Open Library/TIA constraints before broad UI or catalog work.

## Work-order rules

1. Finish/merge the current bounded task before starting overlapping implementation.
2. No speculative infrastructure expansion.
3. Every task has explicit deterministic/TIA acceptance.
4. HIGH-risk architecture/PLC semantics/TIA/library migration requires independent ChatGPT + Gemini review.
5. Broad React Flow UI starts only after the first real valve vertical slice is green.

---

## Phase 0 — Proven automation infrastructure

Status: **DONE / frozen**.

Proven: Linux generation/tests, coding-agent PR, connected ChatGPT review, bounded repair, trusted Windows boundary, real TIA V21 compile/diagnostics.

`PLC-001` has also proven `TIME` through JSON -> Domain -> PLC IR -> Siemens SCL -> exact TIA V21 artifact compile with 0 errors / 0 warnings. PR #16 remains a human merge decision.

---

## Phase 1 — Accept generator architecture

### ARCH-001 — Target generator architecture

Risk: **HIGH**

Documents:

- `TARGET_ARCHITECTURE.md`
- `OPEN_LIBRARY_INTEGRATION_RULES.md`
- `ENGINEERING_RULES.md`
- this roadmap

Review history:

- Round 1 Gemini returned `CHANGES_REQUIRED`; its four findings were incorporated into the proposal.
- Round 2 ChatGPT returned `CHANGES_REQUIRED` for exact candidate `7367a0bbc22987bc68275feab59e7f198b73b314`, requiring explicit temporal-dependency semantics for stateful primitives, controller-global handling of cross-unit same-scan dependencies and roadmap clarification around target-profile ownership.
- Round 2 Gemini attempts that could not inspect the exact GitHub candidate or did not satisfy the external-review schema are evidence only and do not fill the independent reviewer slot.

Any architecture change creates a new candidate SHA and requires fresh independent ChatGPT + Gemini reviews bound to that exact SHA.

Gate: no architecture-dependent implementation task starts until ARCH-001 is accepted.

---

## Phase 2 — Qualify the real Siemens Open Library for V21

This is the first priority after architecture acceptance because the actual library is the largest external dependency.

### OLQ-001 — Qualify supplied Open Library V19 for TIA Portal V21

Risk: **HIGH**

Goal: create one deterministic, reusable native V21 qualified library profile from the supplied V19 archive.

Acceptance:

- record source `.zal19` SHA256;
- record exact TIA Portal V21 version/build;
- trusted qualification code performs supported V19 -> V21 upgrade/retrieval;
- save/archive/cache resulting native V21 library under deterministic identity;
- reference project/library compiles successfully;
- qualification manifest records resulting library identity/hash;
- human accepts the qualification result;
- normal ProjectAssembler rejects raw V19 archive input.

No candidate executable runs on Windows; qualification logic is trusted/reviewed code.

### OL-001 — Inspect qualified V21 valve contract

Risk: **HIGH initially / MEDIUM once qualification mechanism is established**

Goal: extract/verify the first actual implementation contract.

Acceptance:

- exact `fbValve_Solenoid` type/version/GUID/state discovered;
- exact `udtHMI_ValveControl` identity discovered;
- exact `udtError_Valve` identity discovered;
- helper FB/FC/UDT dependencies recorded;
- `Open Library` constants/tag-table master copy located;
- machine-readable valve catalog seed produced;
- documentation-derived interface is reconciled with actual qualified V21 evidence.

### OL-002 — Initial target profile + preflight + clean valve materialization

Risk: **HIGH**

Goal: introduce the minimum production target-profile/preflight mechanism needed to prove that one trusted/reference V21 target can receive the qualified valve dependencies before generated application logic exists.

Acceptance:

- one target/base project identity/hash pinned;
- one target profile declares System memory enabled/address requirement;
- one target profile declares Clock memory enabled/address requirement;
- ProjectAssembler validates actual CPU settings before materialization;
- mismatch produces deterministic failure;
- Open Library constants/tag-table present;
- exact valve dependencies materialized from qualified native V21 library;
- clean PLC compile has 0 errors;
- diagnostics record exact target/library identities.

This task **introduces** target profiles and CPU prerequisite preflight for the first trusted V21 target. Later `TIA-001` generalizes/hardens this already-proven mechanism; it does not introduce it for the first time.

---

## Phase 3 — Compiler foundations required by a real FB call

### PLC-002 — PLC IR owns its type system

Risk: **MEDIUM**

Goal: remove `Domain.AutomationType` from the backend-facing IR contract.

Acceptance:

- `PlcTypeRef`/scalar types introduced;
- Domain -> PLC IR lowering explicit;
- Motor/ValveConfig output remains semantically equivalent;
- unsupported lowering fails explicitly;
- regression TIA compile remains green.

### PLC-003 — Minimal structural SCL AST

Risk: **MEDIUM**

Goal: establish only the syntax tree needed for upcoming data blocks and FB calls.

Required nodes grow incrementally:

- compilation unit;
- type/FB/DB declarations;
- variable sections;
- scalar/named type refs;
- block attributes;
- assignments;
- block calls/named parameter bindings;
- variable/member/literal expressions.

Acceptance: deterministic golden output; no generic parser/framework beyond required nodes.

### PLC-004 — Explicit DB access mode

Risk: **MEDIUM**

Goal: make Siemens DB optimization policy an explicit model property.

Acceptance:

- `Optimized` and `Standard` represented in Siemens model/SCL AST;
- SCL emitter produces a Standard/non-optimized DB using the supported optimized-access attribute;
- exact test Error DB compiles in TIA V21;
- real TIA evidence confirms required access behavior;
- no SimaticML/YAML backend introduced unless SCL proves insufficient.

---

## Phase 4 — First canonical device and Open Library binding

### DM-001 — Canonical `TwoPositionValve`

Risk: **HIGH** because it establishes the first real canonical project schema slice.

Minimum semantics:

- stable device/unit IDs;
- timeout parameter;
- Enable/interlock input;
- CommandWork / Reset inputs;
- Home/Work feedback inputs;
- command/status/error outputs;
- mode context reference;
- simulation context reference;
- safety/E-stop status consumed as an externally engineered input only.

Acceptance:

- no Siemens names in Domain;
- schemaVersion defined;
- typed directions/cardinality explicit;
- layout excluded from semantics;
- validation diagnostics use canonical IDs.

### OL-003 — Data-driven valve binding

Risk: **MEDIUM**

Goal: bind `TwoPositionValve` to the exact qualified `fbValve_Solenoid` contract.

Acceptance:

- exact parameter names/directions/types from OL-001;
- HMI/Error UDT names stay Siemens-specific;
- required/default bindings explicit;
- memory-model metadata explicit;
- descriptor validation catches missing/duplicate/incompatible bindings.

---

## Phase 5 — First real generator milestone

### GEN-001 — Generate one `WaterSystem` valve application

Risk: **HIGH**

Input:

```text
WaterSystem
  V101 : TwoPositionValve
  mode context
  simulation context
  typed command/feedback signals
```

Expected TIA structure:

```text
FB_WaterSystem
  VAR_STATIC
    V101 : fbValve_Solenoid

DB_WaterSystem            # instance DB for unit FB
DB_HMI_WaterSystem
  V101 : udtHMI_ValveControl
DB_Errors_WaterSystem
  V101 : udtError_Valve
OB_Main / small orchestration
```

Acceptance:

- V101 is a multi-instance;
- unit has bounded instance DB;
- all required FB parameters bound explicitly;
- mode uses documented Open Library semantics/constants;
- simulation comes from unit context;
- no internal library-memory access;
- no `iStatus`/`iErrorCode` control logic;
- Error DB access mode matches selected alarm target profile;
- target CPU preflight passes;
- exact qualified library versions used;
- full real TIA V21 compile has 0 errors;
- saved project contains expected objects;
- diagnostics/source map can identify V101.

**This is M3: first real PLC generator. Do not begin broad React Flow UI before this is green.**

---

## Phase 6 — Modular scale

### GEN-002 — Multiple units/areas

Generate at least two units. Each unit/application FB has its own instance DB and ordinary device FBs are multi-instances inside it. No plant-wide giant instance DB.

This task establishes memory/ownership scaling only; unit boundaries must not be treated as implicit scan-delay boundaries.

### GEN-003 — HMI/Error/config grouping

Formalize deterministic per-unit global data layout and access-mode policy.

### GEN-004 — Source map and diagnostic identity

Map generated symbols/TIA diagnostics back to canonical unit/device/port IDs.

Acceptance for Phase 6 includes deterministic regeneration, no duplicate objects and full TIA compile 0 errors.

---

## Phase 7 — Typed graph / PLC logic compiler

### GRAPH-001 — Typed connections and single-writer rules

Validate direction, type compatibility, required ports and multiple-writer conflicts.

### GRAPH-002 — Temporal dependency and stateful primitive contract

Risk: **HIGH**

Goal: define scan-accurate state semantics before allowing stateful elements to participate in graph scheduling.

Acceptance:

- scheduling dependencies explicitly distinguish `SameScan` from `PreviousState`;
- only `PreviousState` dependencies cut same-scan cycles;
- stateful nodes are not treated as generic one-scan registers;
- edge primitive contract identifies current-input and remembered-state dependencies;
- timer contract identifies current-input, elapsed/state and current-output dependencies;
- latch/reset contract defines deterministic set/reset priority and state commit behavior;
- explicit feedback/state primitive defines what is read from the previous scan;
- lowering follows observable `read previous state -> compute current outputs -> commit next state` semantics;
- unit tests cover current-input feed-through plus previous-state behavior;
- equivalent semantic input order/layout produces equivalent temporal IR.

### GRAPH-003 — Controller-global combinational scheduling

Risk: **HIGH**

Goal: deterministically schedule all same-controller `SameScan` dependencies without allowing area/unit ownership partitions to change scan behavior.

Acceptance:

- build a controller-level graph of all `SameScan` dependencies;
- SCC analysis runs on that controller-level graph;
- same-scan SCC/self-loop is rejected unless actually cut by `PreviousState`;
- remaining controller same-scan graph is proven DAG;
- stable topological sort with deterministic semantic tie-breaker;
- same-scan propagation documented/tested;
- cross-unit same-controller dependency determines deterministic unit/application invocation order;
- cross-unit same-scan cycles are rejected unless explicitly delayed;
- ordinary cross-controller runtime connections are rejected until an explicit communication primitive/profile defines transport and latency;
- UI/layout/input/unit declaration ordering does not alter semantic schedule;
- TIA reference scenarios compile.

### GRAPH-004 — Interlock/permissive condition sets

Preserve named operator/HMI semantics and map eligible Siemens targets to Open Library `fbInterlock`/`fbPermissive`.

---

## Phase 8 — I/O and practical device coverage

### IO-001 — Vendor-neutral I/O binding

Separate physical addresses/channels from logical device topology.

### DEV-001 — Open Library motor

Prove architecture is not valve-specific.

### DEV-002 — Analog device

Exercise REAL/scaling/config/HMI/Error contracts.

### DEV-003 — richer motor/VFD

Only after simple devices are stable.

Engineering MVP = valve + motor + analog + I/O -> saved modular compile-clean V21 project.

---

## Phase 9 — Production project delivery

### PKG-001 — Versioned declarative generation package

Add only when real output can no longer be safely represented by current bounded artifact flow. No executable payload.

### TIA-001 — Target-profile generalization and delivery hardening

Build on the minimal profile/preflight mechanism proven by `OL-002`.

Generalize from one trusted valve reference target to versioned production profiles/base projects with explicit hardware/profile identities, reusable validation rules and migration/version policy. Do not reintroduce target profiles as a new concept here.

### TIA-002 — Idempotent project assembly

Same input can regenerate without duplicates, preserve manual ownership, compile fully and save a deliverable TIA V21 project with manifest.

---

## Phase 10 — Product UI

### API-001 — ASP.NET Core compiler API

Expose canonical validation/generation operations.

### UI-001 — Equipment/table/properties

Prove UI adapter over canonical model before graph complexity.

### UI-002 — React Flow typed graph adapter

Layout metadata stays separate; backend compiler remains authoritative.

### UI-003 — Diagnostics/cross-reference

Attach compiler/TIA diagnostics to canonical engineering objects.

### UI-004 — I/O view

Dedicated mapping/table view rather than forcing everything onto one graph.

---

## Phase 11 — HMI, alarms, advanced Open Library

- `HMI-001`: HMI/SiVArc metadata/instantiation proof.
- `ALARM-001`: legacy/modern alarm strategy with proven Error DB access rules.
- `OL-004+`: expand qualified catalog object-family by object-family.
- `SEQ-001`: independently design state machine/`fbStepSequencer` shared-instance semantics.
- PID/technology objects only after their documented memory/technology constraints are proven.

---

## Release milestones

- **M1 Infrastructure** — done/frozen.
- **M2 Qualified Open Library V21 profile** — OLQ-001 + OL-001 + OL-002 green.
- **M3 First real generator** — GEN-001 green in real TIA V21.
- **M4 Modular multi-unit generator** — GEN-002..004.
- **M5 Visual PLC logic compiler** — GRAPH-001..004.
- **M6 Engineering MVP** — I/O + valve + motor + analog -> ready V21 project.
- **M7 Visual product MVP** — React/table/graph editor over proven generator.
- **M8 Production expansion** — HMI/alarms/more devices/advanced sequences.

## Definition of Done

A Siemens-generating feature is complete only after applicable gates pass:

```text
versioned trusted task
 -> deterministic tests/golden output
 -> required independent review
 -> qualified library/target profile
 -> target preflight
 -> exact bounded artifact/package
 -> real TIA V21 assembly/import
 -> full PLC compile (0 errors)
 -> required object/version/access-mode assertions
 -> reproducibility/source-map evidence
 -> human merge decision
```