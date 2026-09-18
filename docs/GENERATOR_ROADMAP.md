# Generator Roadmap

Status: **PROPOSED with target architecture**

Goal: reach a production-capable Siemens TIA Portal V21 generator through small versioned tasks, proving each risky boundary before broadening scope.

The roadmap deliberately prioritizes the real PLC/Open-Library vertical slice before UI breadth.

## North-star completion target

A production milestone is reached when an engineer can model a multi-unit automation project, generate it deterministically, and obtain a saved TIA Portal V21 project that:

- uses pinned Siemens Open Library objects and dependencies;
- is organized hierarchically into subsystem/unit application FBs;
- uses multi-instance device memory inside bounded unit instance DBs;
- has explicit HMI/Error structures;
- has deterministic mode/simulation propagation;
- compiles with zero errors;
- preserves a strict generated/manual ownership boundary;
- can trace diagnostics back to model objects;
- can be regenerated without manual patching of generated code.

## Work order rules

1. Finish the active task before starting an overlapping implementation task.
2. Architecture proposal/review may proceed in parallel with a small already-running task, but architecture is not authoritative until merged.
3. Do not start UI implementation before the first real Open Library valve vertical slice is compiling.
4. Do not expand CI/TIA package infrastructure until a task requires more than the existing bounded artifact flow.
5. Every task must state the smallest observable capability it adds.
6. Tasks that change PLC semantics or TIA trust behavior receive MEDIUM/HIGH review according to repository policy.

---

# Phase 0 — Proven infrastructure baseline

Status: **DONE / frozen**

Already proven:

- deterministic .NET generation on Linux;
- autonomous coding-agent PR path;
- OpenRouter-first / DeepSeek fallback policy;
- connected ChatGPT review;
- bounded repair;
- trusted Windows/TIA boundary;
- real TIA Portal V21 compile and diagnostics.

No further infrastructure work unless a generator task exposes a concrete blocker.

---

# Phase 1 — Compiler foundations

Purpose: remove smoke-test shortcuts before real FB generation while preserving current output.

## PLC-001 — TIME scalar support

Status at current architecture proposal: **trusted TIA V21 PASS; candidate PR #16 remains a human merge decision**.

Outcome proven by candidate validation:

- `Time` exists in the proposed vendor-neutral scalar vocabulary change;
- ValveConfig UDT generation passed deterministic Linux checks;
- the exact generated `UDT_ValveConfig.scl` compiled in TIA Portal V21 with 0 errors / 0 warnings.

Acceptance remains defined by `tasks/PLC-001.json`.

## ARCH-001 — Accept target generator architecture

Risk: **HIGH**

Scope:

- review `docs/TARGET_ARCHITECTURE.md`;
- review `docs/OPEN_LIBRARY_INTEGRATION_RULES.md`;
- review `docs/ENGINEERING_RULES.md`;
- review this roadmap;
- independent ChatGPT + Gemini review;
- resolve review conflicts before merge.

Acceptance:

- architecture boundaries are explicit;
- Open Library rules are reflected correctly;
- modular TIA project strategy is accepted;
- subsystem/unit memory boundaries scale without one giant root instance DB;
- first vertical slice and non-goals are agreed;
- no production code changes hidden in the architecture PR.

## PLC-002 — Give PLC IR its own type system

Risk: **MEDIUM**

Goal: stop carrying `Domain.AutomationType` directly into PLC IR.

Small scope:

- introduce `PlcTypeRef`/scalar representation;
- lower Domain scalar types into PLC IR;
- keep generated Motor and ValveConfig SCL semantically unchanged;
- add tests proving unsupported type lowering fails explicitly.

Acceptance:

- PlcCompiler IR no longer exposes Domain enum as the Siemens backend's type contract;
- existing examples generate byte-for-byte or semantically equivalent SCL;
- TIA V21 regression remains green for the task artifact.

## PLC-003 — Minimal structural SCL AST/emitter

Risk: **MEDIUM**

Goal: replace direct string construction as the basis for future FB calls without changing product behavior.

Initial AST supports only what is required now:

- type declaration;
- field declaration;
- scalar/named type references;
- deterministic formatting.

Acceptance:

- current UDT outputs remain deterministic;
- emitter golden tests exist;
- no generic parser/formatter framework beyond required nodes.

Do not add block calls yet unless required by the next task.

---

# Phase 2 — Trusted Open Library V21 proof

Purpose: prove that the supplied Open Library V19 can be consumed reproducibly in the actual TIA V21 environment before designing a large mapping catalog.

## OL-001 — Inspect/upgrade the supplied Open Library through trusted V21 Openness

Risk: **HIGH** because it touches the TIA worker/trust boundary.

Goal: establish exact library identity and prove controlled V19 -> V21 handling.

Required evidence:

- source archive SHA256/identity recorded;
- V21 retrieval/upgrade method proven on trusted Windows;
- upgraded library cache/path policy defined;
- exact `fbValve_Solenoid` type/version identity discovered;
- exact `udtHMI_ValveControl` and `udtError_Valve` identities discovered;
- required helper dependencies recorded;
- `Open Library` tag-table master copy located;
- no candidate executable runs on Windows.

Deliverable:

- machine-readable inspection result/artifact;
- compact `OpenLibraryCatalog` seed for the valve slice.

This task is an inspection/materialization proof, not a generic catalog framework.

## OL-002 — Materialize valve dependencies into a clean/reference V21 project

Risk: **HIGH** initially because TIA assembly behavior changes.

Goal: using trusted code only, prepare a V21 project with everything required for `fbValve_Solenoid` to exist and compile before any generated valve application logic is introduced.

Acceptance:

- CPU System memory bits enabled;
- CPU Clock memory bits enabled;
- `Open Library` constants/tag-table master copy present;
- exact pinned valve FB/type dependencies materialized;
- project compiles with zero errors;
- diagnostics identify exact library versions used.

If a trusted base project/profile is adopted, its identity/hash/version must be explicit.

---

# Phase 3 — First real domain object: TwoPositionValve

Purpose: prove the complete domain -> Open Library -> TIA vertical slice with one real device.

## DM-001 — Canonical TwoPositionValve contract

Risk: **HIGH** if this establishes the first canonical project schema; otherwise MEDIUM after schema architecture is accepted.

Goal: define a vendor-neutral valve model with stable IDs, parameters and typed ports.

Minimum semantics:

- timeout parameter;
- Enable/interlock input;
- CommandWork input;
- Reset input;
- Home/Work feedbacks;
- command/status/error outputs;
- mode context reference;
- simulation context reference.

Acceptance:

- no Siemens names in Domain;
- JSON schema/version defined for the small project slice;
- required/optional ports explicit;
- type/cardinality tests;
- UI/layout data excluded from compiler semantics.

## OL-003 — Valve Open Library descriptor/binding

Risk: **MEDIUM**

Goal: create one data-driven Siemens implementation descriptor for `TwoPositionValve -> fbValve_Solenoid` based on OL-001 evidence.

Acceptance:

- exact parameter names/directions/types match inspected V21 library version;
- named HMI/Error UDTs represented only in Siemens layer;
- required/default mappings explicit;
- memory-model metadata can represent documented exceptions without compiler special cases;
- no arbitrary reflection/string lookup spread through compiler code;
- descriptor validation test catches a missing/duplicate binding.

## GEN-001 — Generate one modular WaterSystem valve application

Risk: **HIGH** because this is the first real PLC semantic/Open Library integration slice.

Input model:

```text
WaterSystem
  V101 : TwoPositionValve
  mode context
  simulation context
  command/feedback signals
```

Generated structure target:

```text
FB_WaterSystem
  VAR_STATIC
    V101 : fbValve_Solenoid   # device multi-instance

DB_WaterSystem                # one instance DB for FB_WaterSystem

DB_HMI_WaterSystem
  V101 : udtHMI_ValveControl

DB_Errors_WaterSystem
  V101 : udtError_Valve

OB_Main / optional stateless FC_GeneratedRoot
  calls DB_WaterSystem
```

Acceptance:

- valve is called as a multi-instance inside `FB_WaterSystem`;
- `FB_WaterSystem` has a bounded subsystem instance DB rather than being recursively folded into one plant-wide root DB;
- all required parameters are bound explicitly;
- mode uses documented Open Library constants/semantics;
- simulation is passed from system context;
- no reads of internal instance memory;
- PLC logic does not use `iStatus` or `iErrorCode` as control state;
- exact generated artifact compiles in TIA V21 with zero errors;
- final saved project contains the expected library objects/UDTs/application blocks/DBs;
- diagnostics can identify V101 if generation/TIA fails.

**Gate:** do not start broad React Flow UI before GEN-001 is green.

---

# Phase 4 — Modular plant/project structure

Purpose: prove scale by hierarchy, bounded memory ownership and deterministic partitioning.

## GEN-002 — Multiple units/areas with bounded unit instance DBs

Risk: **MEDIUM/HIGH**

Goal: model at least two units and generate an application hierarchy without a single recursively nested plant-wide instance DB.

Acceptance:

- OB remains minimal;
- orchestration is direct or through a stateless/small generated root FC;
- each unit/application FB has its own instance DB;
- each unit FB contains ordinary Open Library device FBs as multi-instances;
- identical reusable unit FB types may have multiple separate instance DBs;
- stable deterministic names;
- regeneration does not create duplicate generated objects;
- TIA project compiles with zero errors.

## GEN-003 — Grouped HMI/Error DB ownership

Risk: **MEDIUM**

Goal: formalize unit-level HMI/Error data layout and deterministic field naming.

Acceptance:

- one structured HMI/Error DB per unit or accepted bounded grouping policy;
- Open Library UDT versions pinned;
- no instance-memory aliases;
- deterministic regeneration tests.

## GEN-004 — Source map and diagnostic identity

Risk: **MEDIUM**

Goal: retain `domainId -> generated symbol/section` mapping.

Acceptance:

- compiler errors already return domain/port IDs;
- generation package contains source-map metadata;
- a deliberately broken generated mapping can be correlated back to the originating test device in diagnostics.

---

# Phase 5 — Typed graph and scan semantics

Purpose: turn the equipment model into a real visual PLC logic compiler.

## GRAPH-001 — Typed connections and single-writer validation

Risk: **MEDIUM**

Acceptance:

- source/target direction checked;
- scalar/named type compatibility checked;
- required connections checked;
- multiple writers rejected deterministically;
- diagnostics identify both conflicting sources.

## GRAPH-002 — Combinational logic and deterministic scheduling

Risk: **HIGH** because scan semantics become product behavior.

Start only with:

- AND / OR / NOT;
- simple comparisons;
- explicit constants.

Acceptance:

- deterministic topological scheduling;
- combinational cycles rejected;
- documented same-scan propagation;
- equivalent model order/layout generates equivalent logic;
- TIA compiled reference scenarios.

## GRAPH-003 — Stateful primitives

Risk: **HIGH**

Add explicitly designed:

- rising/falling edge;
- timer;
- latch/reset;
- feedback/state boundaries.

No generic event/message abstraction.

## GRAPH-004 — Interlock and permissive condition sets

Risk: **HIGH** due operator/PLC semantics.

Goal: model named conditions as first-class domain objects and map Siemens target to `fbInterlock` / `fbPermissive` where appropriate.

Acceptance includes preservation of condition names/status for later HMI use and real TIA compile.

---

# Phase 6 — I/O and additional devices

Purpose: make the model useful for real plant engineering.

## IO-001 — Vendor-neutral I/O bindings

Model controller/channel/address bindings separately from logical connections.

Support initial S7-1500 digital/analog tags without embedding TIA hardware objects into Domain.

## DEV-001 — Motor

Select one real Open Library motor object through the same catalog/binding mechanism.

Prove no valve-specific architecture leaked into the compiler.

## DEV-002 — Analog device

Add a real analog measurement/control object to exercise REAL, scaling/config and HMI/Error contracts.

## DEV-003 — VFD or richer motor

Add only after simple motor/analog slices are stable; communication-specific dependencies make this more complex.

---

# Phase 7 — Generation package and final project delivery

Purpose: move from a smoke source import to a production project assembler when real output complexity demands it.

## PKG-001 — Versioned declarative generation package

Trigger condition: a real generator task can no longer be safely represented by the existing single bounded artifact.

Risk: **HIGH** (trust boundary).

Package includes validated hashes, target profile, pinned library requirements, generated source and source map. No executable payload.

## TIA-001 — Target profiles/base project strategy

Risk: **HIGH**

Goal: replace hard-coded smoke CPU with explicit target profiles.

Initial production strategy should prefer trusted versioned base TIA projects for hardware/network configuration. Hardware-from-scratch generation is optional later.

## TIA-002 — Idempotent generated-project assembly

Acceptance:

- same project input can be regenerated without duplicate generated objects;
- generated/manual groups are respected;
- unit/application memory remains partitioned according to target/profile limits;
- full PLC compile zero errors;
- project saved as deliverable `.ap21`/TIA project directory;
- deterministic manifest records generator/library/target versions.

---

# Phase 8 — Product API and visual editor

Start after GEN-001 and core graph/schema decisions are proven.

## API-001 — ASP.NET Core compiler API

Expose versioned project validation/generation operations over canonical DTOs. Do not expose internal compiler/TIA classes directly.

## UI-001 — Equipment/table/properties editor

Implement basic project hierarchy/device editing first. It is simpler than graph semantics and validates the canonical model/UI adapter.

## UI-002 — React Flow typed graph adapter

React Flow displays/edits the canonical model through an adapter.

Acceptance:

- layout metadata separate;
- typed ports;
- invalid connections rejected in UI for UX and again by backend authoritatively;
- reload preserves stable semantic IDs independent of node positions.

## UI-003 — Diagnostics/cross-reference experience

Show compiler/TIA diagnostics attached to equipment/ports/logic elements using source maps.

## UI-004 — I/O view

Dedicated table/mapping view for physical I/O; not forced onto the control-logic canvas.

---

# Phase 9 — HMI, alarms and broader Open Library coverage

Only after PLC generation is stable.

## HMI-001 — Open Library HMI metadata proof

Inspect/instantiate relevant HMI type/faceplate assets and decide whether SiVArc is the preferred automation mechanism for supported targets.

## ALARM-001 — Explicit alarm strategy

Evaluate Open Library Error UDT/alarm conventions, optimized/non-optimized DB implications and V21 Openness capabilities. Do not inherit legacy Excel alarm-generator assumptions without proof.

## OL-004+ — Catalog expansion

Expand catalog object-by-object or family-by-family from trusted extraction/evidence:

- valve variants;
- motors;
- analog devices;
- drives;
- PID;
- process devices;
- supplementary blocks only when explicitly required.

## SEQ-001 — State machine/sequencer semantics

HIGH risk. Design independently before mapping `fbStepSequencer`, because its shared-instance usage across step calls is semantically different from normal device instances.

---

# Release milestones

## M1 — Compiler foundation

Done when PLC-003 is accepted and existing UDT examples still compile in real TIA V21.

## M2 — Open Library ready

Done when OL-002 proves a clean/reproducible V21 project can materialize pinned valve dependencies and prerequisites.

## M3 — First real generator

Done when GEN-001 generates and compiles a real `fbValve_Solenoid` application through the complete trusted path.

This is the most important near-term milestone.

## M4 — Modular multi-unit generator

Done when GEN-002/003/004 support hierarchical project structure, bounded unit instance DBs, grouped HMI/Error data and traceable diagnostics.

## M5 — Visual logic compiler

Done when GRAPH-001..004 provide typed deterministic PLC scan semantics and Open Library interlock/permissive integration.

## M6 — Engineering MVP

Done when I/O plus a small practical device set (valve, motor, analog device) can generate a saved, compile-clean modular TIA project.

## M7 — Visual product MVP

Done when React/React Flow/table views edit the canonical model and invoke the proven generator without owning PLC semantics.

## M8 — Production expansion

HMI/alarms, more library objects, target profiles and advanced sequencing/PID follow measured user needs.

---

# Definition of Done for a production generator task

A Siemens-generating feature is not complete until all applicable checks pass:

```text
versioned trusted task
  -> deterministic unit/compiler tests
  -> deterministic generator/golden output
  -> protected boundary checks
  -> independent review required by risk class
  -> exact bounded candidate artifact/package
  -> trusted TIA V21 assembly/import
  -> full PLC compile
  -> errors == 0
  -> required project objects verified
  -> reproducibility evidence recorded
  -> human merge decision
```

For architecture, PLC semantics, library upgrade/materialization, TIA worker/trust changes and safety-related behavior, apply HIGH-risk review and independent Gemini verification before acceptance.
