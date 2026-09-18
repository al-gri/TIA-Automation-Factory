# Target Architecture — TIA Automation Factory

Status: **PROPOSED / HIGH-RISK ARCHITECTURE DECISION — review round 3 required**

Date: 2026-09-18

This document defines the proposed target architecture for the generator. It is not normative until the repository HIGH-risk review policy is satisfied for the exact current proposal SHA.

## 1. Product goal

Build an engineering system in which an automation engineer models a plant visually and structurally and the factory deterministically produces a modular Siemens TIA Portal V21 project using qualified Siemens Open Library objects through a trusted TIA Portal Openness boundary.

```text
React / React Flow + table/property views
        |
        v
Canonical AutomationProject
(vendor-neutral, versioned, UI-independent)
        |
        v
Domain validation + semantic compilation
        |
        v
Vendor-neutral PLC Program IR
        |
        v
Siemens Backend
qualified Open Library catalog + bindings
        |
        v
SCL AST / deterministic emitter
        |
        v
Declarative generation package
        |
        v
Trusted TiaV21Worker / ProjectAssembler
        |
        v
TIA Portal V21 -> compile -> save -> diagnostics
```

## 2. Non-negotiable boundaries

- React Flow is an editor adapter, never the canonical PLC language.
- Domain and PLC IR contain no `Siemens.Engineering`, Open Library FB names, TIA GUIDs or TIA paths.
- PLC compiler decisions are complete before the Windows/TIA boundary.
- Siemens-specific implementation belongs in SiemensBackend.
- TIA Openness remains isolated in trusted `src/TiaV21Worker` targeting .NET Framework 4.8.
- AI-authored executable code never runs on the trusted Windows/TIA host.
- Real TIA Portal V21 compile is authoritative for Siemens integration.
- F-safety generation is out of scope.

## 3. Canonical engineering model

The canonical source evolves toward:

```text
AutomationProject
  metadata / schemaVersion
  controllers[]
  areas[] / units[]
  devices[]
  signals[]
  connections[]
  logic[]
  conditionSets[]
  ioBindings[]
  modeDomains[]
  simulationDomains[]
  layouts[]            # UI-only
```

Every semantic object has a stable technical ID plus a human engineering tag/name. Canvas position, zoom, colors and UI grouping are layout metadata and cannot affect PLC output.

### Devices, ports and parameters

A semantic device such as `TwoPositionValve` exposes vendor-neutral typed runtime ports, engineering parameters and context references. Siemens implementation details such as `fbValve_Solenoid` or `udtHMI_ValveControl` are not part of the Domain contract.

Ports and parameters are different concepts. Parameters such as timeout/scaling are configuration, not graph wires merely because the UI can draw a connection.

## 4. PLC compiler and deterministic scan semantics

The compiler is multi-pass:

```text
1. schema validation / migration
2. symbol and identifier validation
3. template/port resolution
4. type checking
5. connection/cardinality/single-writer validation
6. temporal-dependency resolution for stateful primitives
7. controller-level same-scan dependency graph construction
8. SCC validation + executable-container scheduling
9. ownership partitioning / normalization by controller / area / unit
10. lower to PlcProgramIr
```

Ownership partitioning may shape generated FBs/DBs, but it must not erase dependencies that determine PLC scan behavior.

### 4.1 Temporal dependency contract

A stateful object is **not** automatically a one-scan delay and is not removed wholesale from cycle analysis. The compiler models dependency latency explicitly.

Every executable dependency relevant to scheduling is classified as one of:

```text
SameScan       # consumer needs a value produced from current-scan inputs/state
PreviousState  # consumer reads state committed by an earlier scan
```

Rules:

1. Only `PreviousState` dependencies cut a same-scan dependency cycle.
2. A stateful primitive may still have `SameScan` dependencies from current inputs to current outputs.
3. Each stateful primitive contract declares which outputs depend on current inputs, which depend on previous state, and what state is committed for the next scan.
4. The compiler conceptually separates **read previous state -> compute current outputs -> commit next state**. Lowering may optimize this representation only if observable scan semantics are unchanged.
5. Edge detectors, timers, latches/reset elements and explicit feedback/state primitives require dedicated semantic tests; they must not inherit an assumed generic one-scan latency merely because they are stateful.

Example: a rising-edge primitive can read its remembered input from the previous scan while its current output still depends on the current input in the same scan. The current-input dependency therefore remains in the same-scan graph.

This contract prevents a stateful FB with current-input feed-through from accidentally hiding an algebraic loop.

### 4.2 Controller-global scheduling and executable boundaries

Deterministic scheduling is a formal compiler invariant, not an implementation detail.

For each controller:

1. Build one logical dependency graph containing all `SameScan` dependencies, including dependencies that cross area or unit ownership boundaries.
2. Compute strongly connected components on that controller-level same-scan graph.
3. Any SCC containing more than one node, or a self-loop, is a compilation error unless the cycle is actually cut by an explicit `PreviousState` dependency.
4. After delayed/previous-state dependencies are excluded, the remaining semantic graph must be a DAG.
5. Map scheduled semantic nodes into their generated executable containers. In the initial modular architecture, each generated unit/application FB is an **atomic once-per-scan invocation boundary**.
6. Build a quotient/dependency graph between those atomic executable containers: an edge `UnitA -> UnitB` exists when any `SameScan` value produced inside `UnitA` is required by `UnitB` in the same scan.
7. The executable-container graph must also be a DAG. A container-level cycle is a compilation error even if the finer semantic-node graph is acyclic, because realizing `UnitA(part) -> UnitB -> UnitA(part)` would require splitting/interleaving an atomic FB invocation.
8. Perform stable topological scheduling at both levels: deterministic order inside each container and deterministic order of container invocations.
9. When several nodes or containers are simultaneously schedulable, use deterministic semantic IDs/order, never UI position, declaration order or accidental JSON array order.
10. Same-scan propagation means a downstream consumer observes the value produced earlier in that realizable deterministic schedule.

`area` and `unit` are ownership, naming and memory boundaries. A unit becomes a scheduling boundary only because the generated architecture emits it as one atomic application-FB invocation. Areas are organizational by default; if a future design emits an area as another atomic callable container, the same quotient-DAG rule applies.

Cross-unit same-scan data crosses explicit generated unit/application interfaces or orchestration signals. Generated code must not wire directly into another unit FB's private multi-instance memory. A future multi-phase/split-unit execution contract is possible only as a separate HIGH-risk design; it is not inferred automatically to make an otherwise unrealizable graph compile.

A normal canonical connection must not claim same-scan semantics across different controllers. Cross-controller data flow requires an explicit communication primitive/profile with defined transport, buffering, stale-data/error behavior and update latency. Until such a primitive is implemented, ordinary cross-controller runtime connections are compilation errors.

Equivalent canonical models with different UI layout, JSON order or unit declaration order must produce the same realizable semantic and container schedules.

### 4.3 PLC IR owns its own type system

The current smoke IR carrying `Domain.AutomationType` directly is temporary. Before complex generation, PLC IR owns target-independent types such as:

```text
PlcTypeRef
  Scalar(Bool, Int, DInt, Real, Time, String...)
  NamedType(symbol)
  Array(...)
  Struct(...)
```

PLC IR also retains the compiler-resolved execution/dependency semantics and container invocation order needed by the backend; SiemensBackend must not rediscover graph scheduling from source/UI order.

## 5. Siemens/Open Library backend

SiemensBackend lowers PLC IR using:

```text
PlcProgramIr
 + SiemensTargetProfile
 + QualifiedOpenLibraryCatalog
        |
        v
SiemensProgramModel
        |
        + required qualified library objects/types
        + generated UDTs
        + generated FBs
        + generated DBs + DB access mode
        + instance-DB plan
        + entry/orchestration plan
        + source map
        v
SCL AST
```

The Open Library catalog pins:

- qualified V21 library identity/hash;
- originating V19 source archive hash for provenance;
- exact released type version/GUID/state where available;
- block kind and memory-model exceptions;
- parameter names, directions and Siemens types;
- HMI/Error UDTs;
- mode/simulation support;
- dependencies and master copies/constants;
- optional HMI/SiVArc metadata.

Generation never binds to an unspecified `latest` library type.

## 6. Library qualification is separate from production generation

TIA V21 can upgrade/retrieve older library archives, but a major-version upgrade is **not** part of a normal project build.

Two distinct operations exist:

### 6.1 Library qualification / migration event

A dedicated HIGH-risk trusted operation may accept the source V19 `.zal19` and:

1. record source SHA256 and TIA V21 build/version;
2. perform the supported V19 -> V21 retrieve/open-with-upgrade path;
3. save/archive a native V21 qualified library artifact/cache;
4. inspect exact released type versions, dependencies and required master copies;
5. compile a clean reference project;
6. produce a machine-readable qualification manifest/catalog seed;
7. require human acceptance before the result becomes an approved library profile.

A library upgrade is therefore an explicit versioned migration event.

### 6.2 Normal ProjectAssembler operation

Normal generation accepts only an already qualified native V21 library identity/artifact/profile. It must reject an unqualified V19 archive as a production input.

The same qualified library artifact/profile is reused until an explicit migration creates a new version.

## 7. SCL generation and DB access mode

SCL remains the primary generated application language. Before real FB calls the current direct StringBuilder approach evolves into a minimal deterministic SCL AST supporting declarations, variable sections, assignments, block calls, named bindings and expressions.

The Siemens program model must carry an explicit per-DB access policy:

```text
DbAccessMode
  Optimized
  Standard
```

For the Open Library legacy alarm-generation compatibility profile, generated Error DBs are **Standard / non-optimized** and the emitter must express this explicitly, e.g. via the supported SCL block attribute (`S7_Optimized_Access := 'FALSE'`) and prove it in real TIA V21.

This requirement does **not** imply introducing SimaticML or Simatic Source Document now. SCL remains preferred while it can represent the required metadata. A second exchange backend is introduced only when a concrete TIA feature cannot be represented safely through SCL.

Generated standard-access Error DBs must not be placed inside a TIA Software Unit configuration that forces optimized access.

## 8. Modular generated TIA project structure

Use bounded subsystem memory, not one recursively nested plant-wide instance DB.

```text
Program blocks / Generated
  OB_Main
  FB_WaterSystem
  FB_AirSystem
  DB_WaterSystem       # instance DB of FB_WaterSystem
  DB_AirSystem         # instance DB of FB_AirSystem

Global data / Generated
  DB_HMI_WaterSystem
  DB_Errors_WaterSystem
  DB_Config_WaterSystem   # only where needed

Library objects/types
  qualified Open Library objects

User/
  manually maintained extension objects; generator never overwrites
```

Inside a unit application FB, ordinary Open Library field-device FBs are multi-instances:

```text
FB_WaterSystem
  VAR_STATIC
    V101 : fbValve_Solenoid
    V102 : fbValve_Solenoid
    P101 : <Open Library motor FB>
```

Do not generate one wrapper FB or one instance DB per ordinary physical device by default. Reusable unit types may share code while each physical unit has separate state/instance DB.

A generated unit/application FB is called atomically once per scan in the initial architecture. Same-controller cross-unit dependencies must therefore form an acyclic unit-invocation graph and are passed through explicit unit interfaces/orchestration signals; bounded memory domains never authorize direct access to another unit's private multi-instance state.

## 9. Open Library runtime contracts

The generator must preserve the library's documented contracts:

- internal FB instance memory is private;
- PLC logic uses documented inputs/outputs/HMI/Error contracts only;
- `iStatus` and scrolling `iErrorCode` are presentation values, not PLC-control state;
- mode is normally owned per subsystem and propagated to devices;
- simulation is a subsystem/system context propagated to `bInSimulate` where supported;
- Open Library constants/tag-table master copies are project prerequisites;
- `fbInterlock` / `fbPermissive` preserve named condition/HMI semantics;
- `fbStepSequencer` has special shared-instance semantics and requires separate explicit design.

## 10. Target profiles and CPU prerequisite preflight

Production generation prefers trusted versioned TIA base projects/target profiles rather than arbitrary hardware-from-scratch engineering.

A target profile declares at minimum:

- TIA version;
- trusted base project identity/hash;
- controller/profile identity;
- expected Open Library qualified profile;
- required System memory enabled state/address;
- required Clock memory enabled state/address;
- required Open Library constants/tag-table identity.

The ProjectAssembler performs a **preflight validation** of the actual target CPU and project before library/program materialization. It must verify the required System/Clock memory configuration and addresses match the target profile. Missing/mismatched prerequisites produce a deterministic failure; the assembler must not silently guess addresses or patch semantics.

The exact V21 Openness attribute/service mechanism for this check is proven by a dedicated trusted TIA task before production use.

## 11. Trusted ProjectAssembler

The normal assembler receives a bounded declarative package containing hashes, target profile, qualified library requirements, generated sources and source map.

Responsibilities:

1. validate package schema and hashes;
2. open/copy the trusted target/base project;
3. preflight CPU and Open Library prerequisites;
4. open the already-qualified native V21 Open Library profile;
5. materialize exact required released type versions/dependencies/master copies;
6. import/generate bounded SCL;
7. compile the entire PLC software;
8. save the project;
9. return structured diagnostics and evidence.

It does not perform Domain semantics or a V19->V21 major-version upgrade during a normal build.

## 12. Diagnostics and source mapping

Every generated object retains a source reference back to canonical Domain IDs. The generation package carries a source map so a TIA diagnostic can eventually map:

```text
TIA: FB_WaterSystem / generated symbol
 -> Unit WaterSystem
 -> Device V101
 -> semantic port/parameter
```

## 13. Frontend boundary

Target product stack remains React + TypeScript + React Flow over ASP.NET Core/.NET 10. Equipment/table/properties, connectivity, I/O, logic and diagnostics views operate on the same canonical model.

Do not show the entire plant as one canvas. Use hierarchy/unit views and filtered graphs.

Broad UI work begins only after the first real Open Library vertical slice is green.

## 14. First production vertical slice

Architecture proof target:

```text
WaterSystem
  V101 : TwoPositionValve
  mode + simulation context
        |
        v
Domain -> PLC IR
        |
        v
qualified Open Library binding -> fbValve_Solenoid
        |
        v
FB_WaterSystem
  V101 multi-instance
DB_WaterSystem
DB_HMI_WaterSystem
DB_Errors_WaterSystem (explicit access policy)
        |
        v
trusted target-profile preflight
qualified V21 library materialization
SCL generation/import
full TIA V21 compile
        |
        v
0 errors + saved project + traceable diagnostics
```

That proof precedes broad React Flow UI and broad catalog expansion.

## 15. Deliberate non-goals

Not in the first production slice:

- F-safety logic generation;
- arbitrary LAD/FBD graphical generation;
- PID/complex sequencer generation;
- full HMI screen generation;
- generic multi-vendor plugin framework;
- arbitrary hardware-from-scratch generation;
- wrapper hierarchy per device;
- multi-phase/interleaved execution of one unit FB within a scan;
- SimaticML/YAML backend without a demonstrated SCL blocker.

## 16. Architecture acceptance criteria

This proposal is acceptable only when independent review agrees that:

- Domain is independent of React Flow and Siemens;
- dependency latency is explicit: only `PreviousState` dependencies cut same-scan cycles;
- stateful primitives declare current-input/output and previous-state dependencies rather than acting as generic cycle breakers;
- same-controller semantic scheduling includes dependencies across unit/area ownership boundaries;
- generated unit/application FBs are atomic once-per-scan execution containers whose cross-unit dependency quotient graph is also a stable DAG;
- cross-unit data uses explicit public interfaces/orchestration signals rather than private multi-instance memory;
- ordinary cross-controller runtime connections are rejected unless represented by an explicit communication primitive with defined latency;
- library upgrade is a separate qualification event, not normal build behavior;
- normal builds consume an exact qualified native V21 library profile;
- DB optimization/standard-access policy is explicit and testable;
- System/Clock memory prerequisites are preflight-validated;
- Open Library versions/dependencies are reproducible;
- unit/subsystem memory boundaries scale without one giant plant DB;
- Windows/TIA remains a narrow declarative trust boundary;
- generated/manual ownership is explicit;
- diagnostics retain source identity;
- the roadmap reaches a real compiled valve slice through small versioned tasks.