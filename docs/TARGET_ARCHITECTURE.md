# Target Architecture — TIA Automation Factory

Status: **PROPOSED / HIGH-RISK ARCHITECTURE DECISION**

Date: 2026-09-18

This document defines the proposed target architecture for the generator. It is not accepted until the repository HIGH-risk review policy is satisfied. The proposal must receive independent ChatGPT and Gemini architecture review before it is merged to `main` and treated as normative.

## 1. Product goal

Build an engineering system in which an automation engineer models a plant visually and structurally, and the factory deterministically produces a modular Siemens TIA Portal V21 project that uses Siemens Open Library objects through a trusted TIA Portal Openness boundary.

Target flow:

```text
React / React Flow + table/property views
            |
            v
Canonical Automation Project
(vendor-neutral, versioned, no UI-library semantics)
            |
            v
Domain validation + semantic compilation
            |
            v
Vendor-neutral PLC Program IR
            |
            v
Siemens Backend
Open Library catalog + bindings + Siemens lowering
            |
            v
SCL AST / deterministic source emitter
            |
            v
Declarative Generation Package
            |
            v
Trusted TiaV21Worker (.NET Framework 4.8)
            |
            +--> target/profile preparation
            +--> Open Library materialization
            +--> external-source generation
            +--> TIA V21 compile
            +--> save ready project + diagnostics
            |
            v
Ready TIA Portal V21 project
```

The project must remain modular, deterministic, scalable to large plants, reviewable in Git, and reproducible from the same source model + target profile + pinned library versions.

## 2. Architectural principles

### 2.1 Domain first, Siemens second

The canonical project describes automation concepts, not Siemens implementation details.

The Domain and PLC IR must never contain names such as `fbValve_Solenoid`, `udtHMI_ValveControl`, `Siemens.Engineering`, TIA library GUIDs, or TIA object paths. Those belong to the Siemens/Open-Library backend.

A domain concept such as a two-position valve may be implemented by Siemens Open Library today and by another implementation profile later without changing the visual project format.

### 2.2 React Flow is a view/editor, not the language runtime

Raw React Flow node/edge JSON must never be the generator's source language.

React Flow is an adapter over a canonical, versioned `AutomationProject` model. Canvas position, zoom, colors and UI grouping are layout metadata and must not affect generated PLC code.

The frontend may perform fast UX validation, but backend/compiler validation is authoritative.

### 2.3 PLC scan semantics are explicit

The generator is not an event/message engine.

Every graph element must have PLC semantics: signal level, edge, pulse, state, timer, latch, condition, state machine, device command, feedback, etc. Combinational logic is scheduled deterministically. Cycles are rejected unless they cross an explicit stateful element. Multiple writers are rejected by default.

### 2.4 Generated code is deterministic

The same canonical project, target profile, Open Library catalog version and generator version must produce semantically identical artifacts.

Stable identifiers and tags drive generated identifiers. Moving a node on the canvas must not change PLC source.

### 2.5 Open Library is consumed through a versioned implementation catalog

The generator does not scatter Open Library block names through business logic.

The Siemens backend owns a versioned machine-readable catalog describing exact released block/type versions, parameter directions, Siemens data types, dependencies, HMI/Error UDTs, mode/simulation behavior and optional HMI metadata.

The catalog should ultimately be extracted/verified from the actual trusted V21-opened/upgraded library through TIA Openness materialization/export/inspection rather than maintained manually at scale.

### 2.6 TIA Openness is assembly/orchestration, not the semantic compiler

Compiler decisions are made before the Windows/TIA boundary.

`TiaV21Worker` receives a bounded declarative package and performs only trusted operations: prepare target, materialize required library objects, import/generate sources, compile, save, and emit diagnostics.

Candidate source code is never executed on the trusted Windows/TIA host.

### 2.7 Modularity is bounded, not maximal nesting

Open Library's multi-instance guidance should be applied inside meaningful subsystem/unit memory boundaries, not recursively to the entire plant.

A very large root FB whose single instance DB contains every unit/device would increase memory size, recompilation blast radius and coupling. The default scalable boundary is therefore one application instance DB per logical unit/subsystem, with device FBs inside that unit as multi-instances.

## 3. Siemens Open Library rules that shape the design

The supplied Siemens Open Library V19 documentation establishes the following constraints.

### 3.1 A library object is more than an FB

A typical device object is a coordinated contract containing:

- an FB/FC implementation;
- an HMI UDT used as an `IN_OUT` interface;
- an Error UDT for alarms and PLC logic;
- HMI faceplates/popups and related metadata;
- dependencies on helper blocks and types;
- for physical devices, optional simulation behavior.

For example, `fbValve_Solenoid` includes scalar inputs such as `Time`, `Int`, and `Bool`, an `IN_OUT` `udtHMI_ValveControl`, explicit Boolean outputs, and `udtError_Valve` output.

Therefore a Siemens mapping must describe a complete library object contract, not only the FB name.

### 3.2 Multi-instance memory is the default for normal device FBs

Open Library documentation recommends multi-instance FB usage to reduce the number of instance DBs and improve project structure.

The generated architecture therefore uses application/unit FBs whose `VAR_STATIC` section contains Open Library device FB instances. A unit/application FB itself normally has one explicit instance DB, creating a bounded memory domain. Single-instance device DBs are reserved for cases where a library object or Siemens technology explicitly requires them.

### 3.3 Internal FB memory is private

The generator must never read undocumented fields from a library FB instance DB. External behavior is obtained only through documented inputs, outputs, HMI UDTs and Error UDTs.

### 3.4 `iStatus` and `iErrorCode` are not PLC-control contracts

Open Library documents `iStatus` and scrolling `iErrorCode` as HMI display values. Generated PLC logic must use explicit Boolean outputs and Error UDT status bits, not these presentation integers.

### 3.5 Modes are subsystem concepts

Open Library defines Stop, Auto, Manual and Independent modes. For medium/large systems it recommends modes per subsystem and passing the mode into the customer-created subsystem FB.

The domain therefore needs explicit `ModeDomain`/unit-level mode ownership; mode should not be duplicated independently on every graph node.

### 3.6 Simulation is a subsystem-controlled PLC concept

Simulatable device blocks expose `bInSimulate`. Open Library recommends distributing simulation from a higher-level system FB. Simulation is not an HMI bypass/maintenance mechanism.

The domain therefore needs explicit `SimulationDomain` ownership, propagated down to devices by compiler lowering.

### 3.7 Library constants and initial setup are mandatory dependencies

Open Library uses constants from the `Open Library` PLC tag-table master copy and depends on CPU System/Clock memory bits. These are target/project prerequisites and belong to the TIA/Open-Library materialization stage, not to every generated device call.

### 3.8 Interlock and permissive semantics are first-class

`fbInterlock` and `fbPermissive` model named condition sets and expose HMI state, not merely a Boolean AND.

The canonical model should preserve condition identity/name and condition-set semantics so a Siemens backend can map them to library objects and HMI data instead of reducing them prematurely to anonymous logic.

### 3.9 Sequencers are stateful high-level semantics

`fbStepSequencer` is intentionally used multiple times while sharing one instance memory allocation for a sequence. This is not equivalent to a generic stateless node. Sequencers/state machines are later, higher-risk compiler features and must receive explicit semantic design before implementation.

Detailed generator implications are maintained in `docs/OPEN_LIBRARY_INTEGRATION_RULES.md` within this proposal.

## 4. Canonical automation model

The first stable project schema should evolve toward this conceptual shape:

```text
AutomationProject
  metadata / schemaVersion
  controllers[]
  areas[] / units[]
  devices[]
  connections[]
  logic[]
  conditionSets[]
  ioBindings[]
  modeDomains[]
  simulationDomains[]
  layouts[]             # UI-only metadata
```

### 4.1 Stable identity and human naming

Every semantic object has:

- immutable/stable technical ID;
- human engineering tag/name;
- optional description;
- hierarchy parent/reference.

Generated source uses deterministic naming rules derived from these stable values. UI internal IDs must not become PLC contracts accidentally.

### 4.2 Device template versus device instance

A template defines a semantic equipment type and its public contract; an instance represents a physical/logical object in one project.

Example semantic template:

```text
TwoPositionValve
  Inputs/commands:
    Enable
    CommandWork
    Reset
    SignalHome
    SignalWork
  Outputs/status:
    CommandHome
    CommandWork
    ActiveHome
    ActiveWork
    Error
  Parameters:
    Timeout
  Context:
    Mode
    Simulation
```

No Siemens block name appears here.

### 4.3 Ports versus parameters

Ports represent runtime signal/data flow and have:

- stable port ID;
- direction: normally Input or Output at the domain level; bidirectional shared data is used only where it is a real domain concept, not merely because a Siemens FB uses `IN_OUT`;
- type;
- semantic role;
- required/optional cardinality.

Parameters are compile-time engineering configuration such as timeout/default/scaling values. A parameter must not be represented as a graph wire simply because the UI can draw one.

Siemens-only `IN_OUT` implementation details such as an Open Library HMI UDT belong to the Siemens binding, not the visual device contract by default.

### 4.4 Connections

A connection references `source object + source port -> target object + target port`.

Compiler rules:

- output-to-input direction;
- type compatibility;
- required port completeness;
- single writer by default;
- no implicit merge of multiple command sources;
- explicit conversion nodes for non-trivial conversions;
- stable diagnostics that reference domain object/port IDs.

### 4.5 Logic nodes

Initial logic primitives should remain small and explicit:

- AND / OR / NOT;
- compare;
- rising/falling edge;
- TON / TOF or semantic timers;
- latch/reset;
- selector/arbitration.

Stateful primitives mark legal scan-cycle feedback boundaries.

### 4.6 Condition sets

Interlocks/permissives are modeled as named condition collections with a final OK state and individual condition identity/text. This allows later mapping to Open Library `fbInterlock`/`fbPermissive` HMI structures.

### 4.7 I/O bindings

Physical I/O mapping is separate from logical device topology. A device port can bind to a controller/channel/tag without polluting device semantics with Siemens addresses.

Initial product scope should support S7-1500 standard PLC logic. F-safety program generation is explicitly out of scope until separately designed and validated.

### 4.8 Project persistence and UI layout

The canonical project format is a versioned portable engineering representation, initially JSON-based and Git-friendly.

Recommended separation:

```text
project.json / semantic project files
  schemaVersion
  hierarchy
  devices
  connections
  logic
  IO/configuration

layout.json / layout section
  canvas coordinates
  viewport/group visual state
  purely presentational metadata
```

For very large projects the same logical schema may be split into deterministic files by controller/unit, but storage partitioning must not change semantics.

SQLite may later be used as a local index/cache for search/cross-reference/performance. It should not become the only opaque source format during the early product phase.

## 5. Compiler architecture

The compiler should be multi-pass and deterministic.

```text
Project DTO
  1. Schema validation / migration
  2. Symbol table + identifier validation
  3. Template/port resolution
  4. Type checking
  5. Connection/cardinality validation
  6. Scan-semantic validation
     - multiple writers
     - combinational cycles
     - stateful boundaries
  7. Partition by controller / area / unit
  8. Normalize expressions/conditions
  9. Deterministic scheduling
 10. Lower to vendor-neutral PlcProgramIr
```

### 5.1 PLC IR must become independent of Domain DTO types

The current early `PlcIrField` carries `AutomationType` directly. That is acceptable only for the smoke baseline.

Before complex generation, PLC IR should own a target-independent PLC type system such as:

```text
PlcTypeRef
  Scalar(Bool, Int, DInt, Real, Time, String...)
  NamedType(symbol)
  Array(...)
  Struct(...)
```

This prevents the backend from depending on frontend/domain enum growth and allows target lowering to introduce target-specific named types without contaminating the domain.

### 5.2 Proposed PLC Program IR

Conceptually:

```text
PlcProgramIr
  dataTypes[]
  globalData[]
  functionBlocks[]
  functions[]
  entryPoints[]

PlcFunctionBlockIr
  interface
  staticInstances
  temporaries
  statements
  sourceRefs
```

It is a compiler IR, not a serialized UI format.

## 6. Siemens backend architecture

The Siemens backend lowers PLC IR + selected implementation profile into a Siemens-specific program model.

```text
PlcProgramIr
 + SiemensTargetProfile
 + OpenLibraryCatalog
        |
        v
SiemensProgramModel
        |
        +--> required library objects/types
        +--> generated UDTs
        +--> generated FBs
        +--> generated DBs/instance DB plan
        +--> OB/root orchestration plan
        +--> source map
        v
SCL AST
        v
Deterministic SCL emitter
```

### 6.1 Open Library catalog

A catalog entry should eventually contain at least:

```text
semanticImplementationKey
library identity + archive/source hash
library type name
exact version/GUID/release state
block kind
memory model exception (if any)
parameters[]:
  name
  direction
  Siemens type (scalar or named UDT)
  required/default semantics
related HMI UDT
related Error UDT
mode support
simulation support
dependencies
optional HMI/SiVArc metadata
```

The catalog is versioned and pinned. Generation must not silently bind to "latest available" library types.

### 6.2 Catalog extraction/verification

At scale, the catalog should be derived/verified from the actual library through trusted TIA V21 Openness operations.

The supplied V19 archive should be upgraded/retrieved into V21 in a trusted/cached step, then exact released type versions and dependencies inspected. If direct metadata is insufficient for parameter-interface extraction, the trusted process may instantiate/export the object and derive the descriptor from the resulting supported source/SimaticML representation. The extraction mechanism itself must be tested against known Open Library documentation.

### 6.3 Library materialization

TIA V21 supports creating project instances from specific global-library type versions and synchronizes required dependent elements into the project library. Openness also supports creating supported objects from master copies, including PLC tag tables/user constants.

The worker should use exact versions and explicit conflict/path behavior, never arbitrary drag/drop-like "current default" behavior.

## 7. SCL generation strategy

SCL remains the primary generated language for the application layer.

The current direct `StringBuilder` UDT emitter is acceptable for the smoke slice only. Before generated FB calls, introduce a minimal SCL AST with deterministic formatting:

- compilation unit;
- type declaration;
- FB/FC/DB declaration;
- variable sections;
- assignments;
- block calls and named parameter bindings;
- literals and variable/member expressions;
- comments/regions where useful.

Templates may be used for harmless boilerplate, but expressions and calls should not become a large collection of string concatenations.

One generated `.scl` file may contain multiple declarations. Keeping a single bounded source artifact as long as practical avoids unnecessary CI/TIA infrastructure expansion while the compiler matures.

## 8. Modular generated TIA project structure

The recommended application layout is hierarchical but uses **bounded subsystem instance memory**, not one recursively nested plant-wide instance DB.

```text
Program blocks / Generated
  OB_Main                         # minimal entry point
  FC_GeneratedRoot                # optional stateless orchestration

  FB_WaterSystem
  FB_AirSystem
  FB_ConveyorArea

  DB_WaterSystem                  # instance DB of FB_WaterSystem
  DB_AirSystem                    # instance DB of FB_AirSystem
  DB_ConveyorArea                 # instance DB of FB_ConveyorArea

PLC data types / Generated
  project-owned generated UDTs

Global data / Generated
  DB_HMI_WaterSystem
  DB_Errors_WaterSystem
  DB_Config_WaterSystem           # only where needed
  ... equivalent per unit

Library objects/types
  Open Library/...                # materialized from pinned library versions

User/
  manually maintained extension blocks; generator never overwrites
```

### 8.1 Unit/subsystem is the default memory boundary

`OB_Main` should contain only orchestration, either direct unit calls or a small stateless/root orchestration FC.

Each logical unit/subsystem has an application FB with its own instance DB. That application FB contains the ordinary Open Library device FBs as multi-instance statics.

Example:

```text
OB_Main
  -> DB_WaterSystem   (instance of FB_WaterSystem)
  -> DB_AirSystem     (instance of FB_AirSystem)

FB_WaterSystem / DB_WaterSystem
  VAR_STATIC
    V101 : fbValve_Solenoid
    V102 : fbValve_Solenoid
    P101 : <Open Library motor FB>
```

This preserves Open Library's multi-instance benefits while preventing a single enormous recursive root instance DB.

### 8.2 Reusable unit types may share code but not state

If multiple units are truly identical, they may share one generated/reusable FB type while each unit receives a distinct instance DB.

Unique units may receive distinct generated FBs. The compiler should not force artificial type reuse when logic differs, but must avoid generating a unique wrapper FB per individual field device.

### 8.3 Do not create one wrapper FB per device instance

A generated wrapper per physical valve/motor would explode project size and reduce maintainability.

Prefer unit/application FB types and multi-instance device instances. If semantic device wrappers are required, create them per reusable device/application type/pattern, not per physical instance.

### 8.4 HMI and Error DB grouping

Each subsystem/unit should normally own structured global DBs containing per-device HMI/Error UDT variables, for example:

```text
DB_HMI_WaterSystem
  V101 : udtHMI_ValveControl
  P101 : udtHMI_...

DB_Errors_WaterSystem
  V101 : udtError_Valve
  P101 : udtError_...
```

This aligns with Open Library examples, makes operator/HMI integration predictable, and keeps FB internal memory private.

### 8.5 Modes and simulation

Each unit/subsystem owns or references its mode domain and passes mode to all relevant Open Library instances.

Simulation is propagated from unit/system context to each simulatable device's `bInSimulate` input.

### 8.6 Generated versus manual ownership

Generated paths are fully generator-owned and may be replaced deterministically.

Manual/user paths are never rewritten. Cross-boundary integration must happen through documented interfaces, not by editing generated code manually.

### 8.7 Partitioning remains explicit for very large projects

Controller and unit partitioning is a compiler concern. A future target profile may impose practical limits on unit size, instance DB size, number of generated objects or scan-time budget. The compiler should be able to report those limits rather than silently collapsing everything into one block/DB.

## 9. TIA V21 worker evolution

The current worker is intentionally a smoke-test implementation: it creates one hard-coded S7-1516 project, imports one SCL source and compiles it. It must not be incrementally turned into business logic.

The target worker is a trusted `ProjectAssembler` driven by a declarative package.

### 9.1 Target generation package

Conceptual package:

```text
manifest.json
  schemaVersion
  generatorVersion
  sourceProjectHash
  artifact hashes

target-profile.json
  TIA version
  controller/profile
  base-template identity

library-requirements.json
  exact Open Library identity/hash
  required type versions/master copies

generated/application.scl
source-map.json
```

No executable/script payload from candidate code is accepted on Windows.

### 9.2 Worker responsibilities

1. Validate package schema and hashes.
2. Create/copy the trusted target project profile/template.
3. Ensure CPU/library prerequisites such as System/Clock memory and the Open Library tag-table master copy.
4. Open/retrieve the pinned Open Library; if source archive is V19, perform controlled V21 upgrade/cache keyed by archive hash.
5. Materialize exact required released library type versions and dependent elements.
6. Import the bounded generated SCL source.
7. Generate blocks from source.
8. Compile the entire PLC software.
9. Save the final project.
10. Return structured diagnostics with stage, TIA object path, severity and source/domain reference where a source map permits it.

### 9.3 Hardware strategy

Do not make arbitrary hardware-catalog engineering a prerequisite for the generator MVP.

For production, prefer versioned trusted target profiles/base TIA projects containing known-good hardware/network configuration. Later, optional hardware-from-scratch profiles can be added when there is a concrete requirement.

The current hard-coded S7-1516 is a smoke fixture, not the product model.

## 10. Diagnostics and traceability

Every generated object should retain a source reference back to domain IDs. The Siemens backend emits a source map connecting generated symbols/sections to project objects.

Diagnostics should eventually be translatable from:

```text
TIA: FB_WaterSystem / line or object path
```

back to:

```text
Unit WaterSystem -> Valve V101 -> port Enable
```

This is essential for a usable visual engineering product.

## 11. Frontend/API boundary

Target product stack remains:

```text
React + TypeScript + React Flow
        |
ASP.NET Core / .NET 10 API
        |
Domain + compiler
```

Required frontend views should share one underlying model:

- Equipment/plant hierarchy;
- device table;
- graph/connectivity;
- I/O mapping;
- control logic;
- properties;
- diagnostics/cross references.

The graph is not expected to show the entire plant as one canvas. Hierarchical/unit views and filtered graphs are required for scale.

Frontend implementation should begin only after the canonical model and first real Open Library vertical slice are proven.

## 12. HMI and alarm strategy

HMI generation is a later phase, but architecture must not block it.

The Siemens catalog may retain HMI faceplate/type/SiVArc metadata. The first PLC generator should create HMI/Error structures compatible with Open Library contracts. Direct HMI screen generation, SiVArc automation and alarm generation are separate versioned features with their own TIA acceptance.

Legacy Open Library alarm-generation assumptions such as non-optimized Error DB layout must not be adopted blindly. Alarm strategy must be made explicit when that phase starts.

## 13. Deliberate non-goals for the first production vertical slice

Do not implement yet:

- F-safety logic generation;
- arbitrary LAD/FBD layout generation;
- full HMI screen generation;
- PID/complex sequencer generation;
- a generic multi-vendor plugin framework;
- TIA Software Units/namespaces unless a measured need appears;
- automatic arbitrary hardware configuration from scratch;
- a huge generated wrapper class hierarchy.

The architecture keeps boundaries that allow these later, but the first goal is a reliable Siemens/Open-Library PLC generator.

## 14. First production vertical slice

The architecture is considered proven when the repository can deterministically execute this scenario:

```text
Canonical project
  WaterSystem unit
    V101 : TwoPositionValve
    mode + simulation context
    typed command/feedback signals
        |
        v
compiler + PLC IR
        |
        v
Open Library mapping -> fbValve_Solenoid
        |
        v
Generated FB_WaterSystem
  multi-instance V101 : fbValve_Solenoid
Generated DB_WaterSystem
  instance DB of FB_WaterSystem
Generated DB_HMI_WaterSystem
Generated DB_Errors_WaterSystem
Minimal OB/root orchestration call
        |
        v
trusted TIA V21 worker
  exact pinned Open Library dependencies
  project prerequisites
  import generated SCL
  compile
  save
        |
        v
ready project, 0 TIA errors
```

That vertical slice should be completed before broadening the UI or library object catalog.

## 15. Architecture acceptance criteria

This proposal should be accepted only if independent review agrees that:

- Domain remains independent of React Flow and Siemens;
- PLC scan semantics are explicit and testable;
- Open Library versions/dependencies are reproducible;
- generated TIA project organization follows multi-instance guidance inside bounded unit/subsystem instance memory;
- project partitioning can scale without one giant plant-wide instance DB;
- the Windows/TIA trust boundary stays narrow and declarative;
- generated/manual ownership is clear;
- diagnostics can map back to engineering objects;
- roadmap can reach a real compiled valve vertical slice through small versioned tasks without speculative infrastructure growth.
