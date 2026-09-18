# Siemens Open Library Integration Rules

Status: **PROPOSED — review round 3 required**

Date: 2026-09-18

These rules translate the supplied Siemens Open Library V19 documentation into generator invariants for a TIA Portal V21 target.

## 1. Library object contract

A device object is not merely an FB. It can include:

```text
FB/FC
+ HMI UDT
+ Error UDT
+ helper FB/FC/UDTs
+ constants/master copies
+ simulation/mode semantics
+ HMI assets / optional SiVArc metadata
```

Domain models the engineering object. SiemensBackend maps it to a complete qualified implementation descriptor. TIA materialization supplies the pinned library objects and dependencies.

## 2. Naming and interfaces

Exact Open Library names such as `tInTimeout`, `iInMode`, `bOutError`, `udtHMI_ValveControl` belong only to Siemens catalog/binding code. Vendor-neutral Domain port names must remain semantic.

The catalog exposes documented public interface members only. Internal instance memory must never become a generated API.

## 3. Versions and dependency integrity

Open Library UDT/FB/HMI versions form a coherent dependency graph. The generator must:

- pin a qualified native V21 library identity/hash;
- retain the originating V19 archive SHA for provenance;
- pin exact released type versions/GUID/state where available;
- materialize dependent types/master copies coherently;
- reject unspecified/default/latest version selection;
- treat a library change as an explicit migration/version event.

## 4. V19 -> V21 qualification strategy

TIA V21 upgrade support is used only in a dedicated HIGH-risk qualification/migration event, never in a normal project build.

### Stage A — Qualification

Trusted qualification code may:

1. hash the source `.zal19` archive;
2. record exact TIA Portal V21 build/version;
3. perform the supported V19 -> V21 retrieve/open-with-upgrade operation;
4. save/archive a native V21 qualified library artifact/cache;
5. inspect exact released type versions, dependencies and master copies;
6. compile a clean reference project;
7. emit a qualification manifest/catalog seed;
8. require human acceptance.

### Stage B — Normal generation

The normal ProjectAssembler accepts only the qualified native V21 library profile/artifact. It rejects the V19 source archive as a runtime generation input.

This separates migration risk from deterministic project generation.

## 5. CPU prerequisites

Open Library requires CPU System memory and Clock memory configuration plus its user constants/tag table.

The target profile declares the required enabled state and expected byte addresses. ProjectAssembler preflight validates the actual target CPU/project before materializing generated application logic.

A missing or mismatched System/Clock memory prerequisite is a deterministic error. Do not guess or silently patch addresses.

The exact V21 Openness mechanism used to read/validate these properties must be proven in a dedicated trusted TIA task before production use.

## 6. Multi-instance memory

Normal field-device FBs are multi-instances inside bounded application/unit FBs:

```text
FB_WaterSystem
  VAR_STATIC
    V101 : fbValve_Solenoid
    V102 : fbValve_Solenoid
    P101 : <Open Library motor FB>
```

Each unit/application FB normally has its own instance DB. Do not create one ordinary device instance DB per field object, and do not recursively fold the entire plant into one giant root instance DB.

These unit/application boundaries are memory/ownership boundaries and, in the initial generated architecture, each unit/application FB is one atomic once-per-scan invocation. Same-controller cross-unit dependencies therefore must form an acyclic unit-invocation graph in addition to the finer semantic graph.

Cross-unit values pass through explicit unit/application interfaces or orchestration signals. Generated code must never reach into another unit's private Open Library multi-instance memory to satisfy a dependency.

Documented exceptions such as technology/PID-specific memory requirements are metadata on the Open Library descriptor rather than compiler special cases.

## 7. HMI UDT contract

Typical device FBs expose HMI data through an `IN_OUT` UDT such as:

```text
HMI_ValveControl : udtHMI_ValveControl
```

Allocate deterministic HMI UDT storage by unit/subsystem and bind it explicitly. The HMI UDT is a Siemens implementation concern, not a generic visual graph port by default.

## 8. Error UDT and DB access policy

Error-capable blocks expose typed Error UDTs, for example:

```text
ERROR_Valve : udtError_Valve
```

Error storage is grouped deterministically by unit/subsystem.

The supplied Open Library V19 documentation explicitly requires the Error DB to be **non-optimized / standard access** when using the legacy Open Library alarm-generator workflow.

Therefore SiemensBackend must carry an explicit per-DB access mode:

```text
DbAccessMode.Optimized
DbAccessMode.Standard
```

For an `OpenLibraryLegacyAlarm` target/profile, `DB_Errors_<Unit>` MUST be Standard/non-optimized. The SCL emitter should express that through the supported SCL optimized-access block attribute and prove it in TIA V21.

Do not introduce SimaticML or Simatic Source Document solely for this requirement while SCL can represent it. Add another exchange format only after a concrete SCL limitation is demonstrated.

Do not place standard-access Error DBs inside a TIA Software Unit context that forces optimized access.

## 9. Status/error semantics

`iStatus` and scrolling `iErrorCode` are presentation/HMI values. PLC control logic must use explicit documented Boolean outputs and Error UDT state rather than infer control state from numeric display codes.

## 10. Mode semantics

For medium/large systems, mode belongs to a system/unit/subsystem and is propagated to participating devices. Do not create unrelated mode state on every visual device.

Generated Siemens logic references documented Open Library constants instead of duplicating magic integer values when practical.

## 11. Simulation semantics

Simulation is PLC-side and propagated from a higher-level unit/system context to `bInSimulate` on supported devices. It is intended for controlled testing, not as a generic maintenance/safety bypass.

## 12. Two-position valve reference object

The first production object is `fbValve_Solenoid`, because its interface exercises the required architecture.

Expected documented contract includes scalar inputs such as:

```text
tInTimeout : Time
iInMode : Int
bInEstop : Bool
bInSignalHome : Bool
bInSignalWork : Bool
bInEnable : Bool
bInCommandWork : Bool
bInResetError : Bool
bInSimulate : Bool
```

and:

```text
HMI_ValveControl : udtHMI_ValveControl   # IN_OUT
ERROR_Valve      : udtError_Valve        # output
```

Exact names, directions, types, versions and helper dependencies must be verified against the qualified V21 library, not assumed from documentation alone.

## 13. Interlock/permissive semantics

`fbInterlock` and `fbPermissive` preserve named condition/HMI semantics. Canonical Domain condition sets must preserve condition identity and names rather than collapse them prematurely into anonymous Boolean AND logic.

## 14. Sequencer semantics

`fbStepSequencer` has shared-instance semantics across steps of one sequence. Do not map each visual step to an independent generic FB instance. Sequence/state-machine behavior is a later HIGH-risk semantic design.

## 15. Generated project ownership

- generated application blocks/DBs are generator-owned and replaceable deterministically;
- manual extension objects are human-owned and never overwritten;
- internal Open Library FB memory is private;
- cross-boundary integration uses documented interfaces only.

## 16. Layer ownership

### Domain
Owns semantic devices, typed ports, hierarchy, parameters, connections, named condition sets, mode/simulation concepts.

### PlcCompiler / PLC IR
Owns type checking, explicit `SameScan` versus `PreviousState` dependency semantics, controller-global SCC/cycle validation, stable scheduling of semantic dependencies, mapping to atomic generated unit/application execution containers, quotient-DAG validation/order across those containers, state and target-independent executable/data representation. Ordinary cross-controller runtime connections are rejected until represented by an explicit communication primitive/profile with defined latency.

### SiemensBackend
Owns qualified Open Library catalog/bindings, Siemens lowering, multi-instance model, HMI/Error DB model including `DbAccessMode`, constants references, SCL AST/emission and source maps. It consumes compiler-resolved internal and unit/application invocation order rather than deriving scheduling from generated object order.

### Library qualification operation
Owns explicit V19 -> V21 migration/qualification, reference compile and qualification manifest. It is not a normal build operation.

### TiaV21Worker / ProjectAssembler
Owns target-profile preflight, opening the qualified V21 library, exact type/master-copy materialization, SCL generation/import, compile/save and diagnostics. It does not perform semantic compilation or normal-build major-version library upgrades.

## 17. Evidence required before broad catalog expansion

Do not expand to many Open Library objects until the valve proof demonstrates:

- a qualified and repeatable native V21 library profile exists;
- exact valve FB/HMI/Error versions and dependencies are recorded;
- System/Clock memory preflight works;
- Open Library constants/master copies materialize correctly;
- Standard Error DB access compiles where legacy alarm compatibility is selected;
- multi-instance valve call compiles;
- HMI/Error UDT binding compiles;
- mode/simulation binding compiles;
- full TIA compile has zero errors;
- regeneration remains deterministic.