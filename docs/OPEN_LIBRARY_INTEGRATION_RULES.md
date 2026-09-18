# Siemens Open Library Integration Rules

Status: **PROPOSED architecture evidence / rules**

Date: 2026-09-18

This document converts the supplied Siemens Open Library V19 documentation into explicit generator design rules. It is deliberately more detailed than `docs/OPEN_LIBRARY_BASELINE.md` and should be reviewed together with `docs/TARGET_ARCHITECTURE.md`.

Reference material in the supplied V19 archive used for this analysis:

- `1 - Overview and Architecture.pdf`
- `2 - Initial Setup.pdf`
- `3 - Example Object Configuration.pdf`
- `4 - Detailed Block Overview.pdf`
- `7 - Customizing Library.pdf`
- `9 - Device Simulation.pdf`
- V19 global-library archives for PLC / Comfort / Professional / Unified variants.

The target environment is TIA Portal V21. Library identity/version handling must therefore be verified against the actual V21-opened/upgraded library through trusted Openness before relying on names alone.

## 1. Open Library object model

The fundamental unit is not merely a PLC function block.

A typical Open Library device object coordinates:

```text
PLC FB/FC
  + HMI control UDT
  + Error UDT
  + helper FB/FC/UDT dependencies
  + constants
  + optional simulation semantics
  + HMI faceplate/popup/type assets
  + optional SiVArc/HMI-generation metadata
```

Generator implication:

- Domain models the engineering device/behavior.
- SiemensBackend maps that device to a complete `OpenLibraryObjectDescriptor`.
- TiaV21Worker/materializer ensures the required released library types/master copies/dependencies exist in the project.
- Generated application SCL binds the documented public FB interface and global HMI/Error storage.

Do not reduce the catalog to `{ semanticType -> blockName }`.

## 2. Naming conventions are evidence, not the canonical domain API

Open Library prefixes encode both type and data flow. Examples from the documentation:

```text
BOOL   -> b
BYTE   -> by
INT    -> i
REAL   -> r
WORD   -> w
DWORD  -> dw
TIME   -> t
STRING -> s
UDT    -> no type prefix
multi-instance FB -> no type prefix

input  -> In
output -> Out
static -> no flow prefix
temp   -> Temp
```

Example: `tInTimeout`, `iInMode`, `bOutError`.

Generator rule:

- retain exact Open Library parameter names in the Siemens catalog/binding layer;
- do not force Open Library prefix names into vendor-neutral Domain port names;
- generated application-owned names may follow a project naming standard, but mapping to library parameters remains explicit.

## 3. Constants are part of the library runtime contract

The library uses PLC user constants from the `Open Library` tag table and explicitly recommends constants instead of hard-coded values.

Important categories include mode values and HMI/status constants.

Generator rule:

- TIA project assembly must ensure the library tag table/user constants are present before compiling library objects;
- generated SCL should reference documented library constants where appropriate instead of duplicating numeric magic values;
- library constants belong to the target/library profile, not the vendor-neutral Domain.

## 4. Initial CPU prerequisites

The library documentation requires CPU System memory bits and Clock memory bits to be enabled. The library may not compile correctly without this setup.

Generator rule:

- target profile/project assembler owns these prerequisites;
- the compiler does not inject workarounds into each device call;
- OL-002 must prove these settings on the actual V21 target/reference project.

## 5. Library versions and dependency integrity

The customizing documentation emphasizes type versioning and ordering: when contracts change, UDTs are updated first, then FBs, then HMI faceplates/types so consumers reference a coherent UDT version.

Generator rule:

- pin the exact library source identity/archive SHA and exact released type versions;
- never bind silently to an arbitrary newest/default type version;
- the generated manifest/catalog records version/GUID or other stable V21 Openness identity discovered from the actual upgraded library;
- dependencies must be materialized through library type/master-copy mechanisms so project-library relationships remain coherent;
- upgrading Open Library is a controlled migration, not an invisible generator update.

## 6. Multi-instance memory is the normal integration model

Open Library documentation recommends multi-instance FB memory because normal FBs retain state across scans and multi-instance usage reduces instance DB count and improves project organization.

The PID interface is called out as an exception where single-instance behavior may be retained to preserve technology functionality.

Generator rule:

- normal field-device FBs are declared in generated application/unit FB `VAR_STATIC` sections and called as multi-instances;
- exceptions are metadata on the library descriptor, never hard-coded compiler special cases scattered through code;
- avoid per-device standalone instance DB explosion.

Recommended generated shape:

```text
FB_WaterSystem
  VAR_STATIC
    V101 : fbValve_Solenoid
    V102 : fbValve_Solenoid
    P101 : <library motor FB>
```

## 7. Internal instance memory is private

Open Library explicitly states that the information needed by external logic is exposed through FB outputs and HMI/Error UDTs and that internal FB instance memory should not be used.

Generator rule:

- generated SCL never references `#V101.<undocumented internal member>` as an external semantic contract;
- catalog exposes only documented interface members;
- future reverse engineering/catalog extraction must not accidentally expose all instance DB internals as valid ports.

This rule is important for future library-version compatibility.

## 8. HMI UDT contract

Typical device FBs expose an HMI control structure as an `IN_OUT` parameter. Example:

```text
HMI_ValveControl : udtHMI_ValveControl
```

The HMI UDT contains mode/status/manual command and visualization values required by Open Library HMI assets.

Generator rule:

- HMI UDT is a Siemens implementation concern and is not a vendor-neutral graph port by default;
- allocate one HMI UDT field per generated device in a subsystem/grouped DB;
- pass that exact field to the FB `IN_OUT` binding;
- preserve compatible HMI UDT type versions for later HMI generation.

Example:

```text
DB_HMI_WaterSystem.V101 : udtHMI_ValveControl
```

## 9. Error UDT contract

Error-capable blocks expose a typed error output. Example:

```text
ERROR_Valve : udtError_Valve
```

Error UDTs are intended for alarms and may also expose deterministic Boolean error state for PLC logic.

Generator rule:

- allocate Error UDT storage grouped by unit/subsystem;
- bind typed Error UDT outputs explicitly;
- normal application logic should use documented Boolean/error fields, not display error codes;
- alarm generation strategy is a later explicit module because legacy Open Library alarm tooling has DB-layout assumptions.

## 10. `iStatus` and `iErrorCode` are presentation values

Open Library documentation says:

- `iStatus` is intended for HMI display/color/status indication;
- `iErrorCode` can scroll between errors and is HMI-display data;
- PLC control logic should use explicit status/error Boolean signals instead.

Generator rule:

- never create semantic Domain outputs such as `Running` by interpreting `iStatus` numeric values;
- never drive PLC interlocks from `iErrorCode`;
- use documented explicit FB outputs/Error UDT members.

## 11. Mode architecture

Open Library uses integer system/device modes:

```text
Stop        = 0
Auto        = 1
Manual      = 2
Independent = 10
```

For medium and large systems the documentation recommends a mode per subsystem, passed into the customer-created subsystem FB and from there to all participating library objects.

Generator rule:

- model mode ownership at system/unit/subsystem level (`ModeDomain`), not as an unrelated field on every device node;
- compile a mode reference into unit/application FB interface/context;
- bind each device's `iInMode` from that unit context;
- use Open Library constants rather than generator-owned copies of the numeric values where feasible.

## 12. Simulation architecture

Open Library device simulation is PLC-side and uses `bInSimulate` on simulatable physical-device blocks.

Documented behavior while simulated includes:

- physical feedback inputs are ignored/substituted with ideal conditions;
- commanded physical outputs remain in safe state;
- error conditions are suppressed for the simulated object;
- it is intended primarily for system testing/hardware unavailability, not maintenance bypass.

The documentation recommends propagating system simulation from a high-level FB to the devices inside that system.

Generator rule:

- model a `SimulationDomain` or unit simulation context;
- distribute the context to all simulatable device bindings;
- do not expose simulation as a generic HMI-maintenance bypass;
- simulation semantics and safety warnings must be explicit in the future UI.

## 13. Two-state valve reference mapping

`fbValve_Solenoid` is the first target object because its interface exercises the core architecture.

Documented inputs include:

```text
tInTimeout        : Time
iInMode           : Int
bInEstop          : Bool
bInSignalHome     : Bool
bInSignalWork     : Bool
bInEnable         : Bool
bInCommandWork    : Bool
bInResetError     : Bool
bInSimulate       : Bool
```

Documented IN_OUT:

```text
HMI_ValveControl  : udtHMI_ValveControl
```

Documented outputs include:

```text
bOutCommandHome
bOutCommandWork
bOutActiveHome
bOutActiveWork
bOutAuto
bOutError
ERROR_Valve : udtError_Valve
```

The example configuration documentation also shows that `fbValve_Solenoid` brings helper dependencies such as HMI/error-related blocks/types with it when inserted through the library mechanism.

Generator implication:

- the first real vertical slice must prove exact dependency materialization from the actual V21 library;
- do not manually duplicate the FB/UDT source into our repository;
- generated project/application logic should instantiate/bind the pinned library block.

## 14. E-stop boundary

Many Open Library device objects accept an E-stop status/input.

Generator rule:

- initial generator treats E-stop as an input from an externally engineered safety layer;
- ordinary generated PLC logic may consume that safe-state status as the Open Library contract requires;
- the project does not claim to generate, validate or replace Siemens F-program safety logic in the first product scope.

## 15. Interlocks and permissives are semantic condition sets

`fbInterlock` and `fbPermissive` expose multiple Boolean condition inputs, an HMI structure describing condition state/name, and a final OK state.

Generator rule:

- canonical model uses named condition sets preserving individual condition identity/operator text;
- do not lower them prematurely to a plain `AND` expression if doing so would lose HMI/operator semantics;
- SiemensBackend may map eligible condition sets to `fbInterlock`/`fbPermissive`;
- the general logic graph can still contain ordinary Boolean logic separately.

This distinction is important for diagnostics, commissioning and HMI generation.

## 16. Sequencer semantics are special

`fbStepSequencer` is used repeatedly for steps/states while calls belonging to one sequence share the same instance memory allocation. Different independent state machines require separate instance memory.

Generator rule:

- do not map every visual step node to an independent generic FB instance;
- define canonical sequence/state-machine semantics first;
- compiler lowering must understand shared sequence instance identity and one-scan enter/exit events;
- sequencer work is HIGH risk and comes after basic device/graph semantics are proven.

## 17. HMI/Error DB project organization

Open Library example configuration groups HMI and Error UDT instances into application/system DBs (for example a water-system HMI DB and a corresponding errors DB).

Generator rule:

- group HMI/Error data by logical unit/subsystem rather than by arbitrary canvas location;
- naming and grouping are deterministic from canonical hierarchy;
- internal FB instance data remains separate/private;
- alarm-specific optimized/non-optimized DB policy is chosen explicitly when alarm generation is implemented.

## 18. Legacy alarm generator constraint

The V19 example documentation notes that the supplied alarm-generator tooling expects a non-optimized Error DB.

Generator rule:

- do not force all generated Error DBs to be non-optimized today solely because of a legacy Excel/executable alarm path;
- define an explicit HMI/alarm target profile later;
- if the legacy generator is selected, enforce its DB constraint through that target profile and prove it in TIA/HMI tests.

## 19. HMI/SiVArc integration is downstream of the PLC contract

Open Library includes HMI faceplates/popups and SiVArc-oriented assets for supported targets.

Generator rule:

- PLC generation must first maintain the exact HMI UDT contracts required by these assets;
- store optional HMI asset/type metadata in the Siemens catalog;
- evaluate SiVArc/type instantiation before inventing custom screen-generation XML;
- HMI generation is a separate acceptance phase, not mixed into the first valve PLC task.

## 20. Library customization/version migration

Open Library customization documentation uses Global Library versioning as the distribution/version mechanism and emphasizes coherent UDT -> FB -> faceplate updates.

Generator rule:

- a customized company library should be treated as a separate implementation profile/catalog identity;
- changes to a UDT or FB contract require a catalog/library-version migration and regression compile, not silent replacement;
- project files record which implementation profile/library version produced them.

## 21. V19 archive to V21 target strategy

The repository owns no right to assume V19 type metadata is identical after V21 upgrade.

Trusted V21 integration should:

1. hash the supplied `.zal19` archive;
2. retrieve/open it with V21 upgrade support on the trusted Windows machine;
3. cache the upgraded library by source hash + V21 version;
4. inspect released type versions/master copies through V21 Openness;
5. record exact identities in a generated inspection/catalog artifact;
6. instantiate required exact versions into the target project;
7. compile a clean reference before using the object in generated application logic.

This proof is `OL-001` / `OL-002` in the roadmap.

## 22. What belongs in each layer

### Domain

Owns:

- Valve/Motor/Sensor/etc semantics;
- typed ports;
- project hierarchy;
- modes/simulation contexts as engineering concepts;
- named interlock/permissive conditions;
- parameters and connections.

Does not own:

- `fbValve_Solenoid`;
- HMI/Error Open Library UDT names;
- TIA paths/GUIDs;
- Open Library parameter prefixes.

### PlcCompiler / PLC IR

Owns:

- validation;
- PLC type system;
- scan semantics;
- scheduling/state;
- target-independent executable/data representation.

Does not own:

- Open Library version selection;
- TIA Openness calls.

### SiemensBackend

Owns:

- Open Library catalog/bindings;
- Siemens-specific lowering;
- multi-instance call model;
- HMI/Error DB generation model;
- Open Library constants references;
- SCL AST/emission;
- source maps.

### TiaV21Worker / ProjectAssembler

Owns:

- trusted V21 library upgrade/open/materialization;
- exact type/master-copy instantiation;
- CPU/library project prerequisites;
- external source import/generation;
- compile/save/diagnostics;
- trusted target profiles/templates.

Does not decide automation semantics.

## 23. Required evidence before catalog expansion

Do not build a broad Open Library catalog until the first valve proof demonstrates:

- exact V19 archive can be upgraded/used from V21;
- exact type/version identities are accessible through Openness;
- helper dependencies are materialized correctly;
- generated multi-instance call compiles;
- HMI/Error UDT storage binds correctly;
- subsystem mode/simulation binding compiles;
- full PLC compile returns zero errors;
- regenerated output remains deterministic.

After this, catalog extraction can expand object families incrementally with much lower architectural risk.
