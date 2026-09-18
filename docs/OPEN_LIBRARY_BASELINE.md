# Siemens Open Library baseline for generator development

Status: accepted research baseline for the next generator tasks.

Date: 2026-09-18

GitHub is the durable source of truth for the conclusions in this file. The Siemens Open Library V19 vendor archive was used only as a reference artifact during analysis; the archive itself is not committed here.

## Relevant first target

The first concrete Open Library object selected for architecture analysis is the two-state solenoid valve function block `fbValve_Solenoid`.

Its documented interface establishes several requirements that the current generator baseline does not yet model:

- inputs include `tInTimeout : Time`;
- inputs include `iInMode : Int` and multiple `Bool` signals/commands;
- `HMI_ValveControl` is an `IN_OUT` parameter using `udtHMI_ValveControl`;
- outputs include multiple `Bool` values plus `ERROR_Valve : udtError_Valve`.

The library naming convention also treats `TIME` as a normal PLC scalar type (prefix `t`) and distinguishes parameter data-flow direction (`In`, `Out`, temporary/static/internal use).

## Memory / encapsulation rules relevant to generation

The library documentation recommends multi-instance usage for normal function-block integration in order to reduce the number of instance DBs and improve project structure.

The generator must not depend on internal instance-memory fields of Open Library FBs. Integration should use the documented FB interface, HMI/Error UDTs, and explicit outputs.

## Architectural implications

The intended progression is incremental:

1. add missing vendor-neutral scalar prerequisites such as `Time`;
2. introduce an explicit Siemens/Open-Library block-signature model with parameter direction and named UDT references;
3. represent one real mapping for `fbValve_Solenoid`;
4. generate a bounded multi-instance SCL call/wrapper;
5. validate the exact generated artifact through trusted TIA Portal V21.

Do not jump directly from the current `AutomationDevice -> PlcIrDataType -> UDT SCL` baseline to a large generic Open Library framework.

## Boundary rules

- `src/Domain` and `src/PlcCompiler` remain vendor-neutral and must not reference `Siemens.Engineering`.
- Siemens/Open-Library-specific mapping belongs in `src/SiemensBackend` or a later Siemens-specific catalog layer.
- TIA Openness remains isolated in trusted `src/TiaV21Worker`.
- Infrastructure is frozen unless a real generator task exposes a concrete blocker.

## Immediate task consequence

The smallest missing prerequisite visible in the real valve interface is the PLC `TIME` scalar. The next task is therefore `tasks/PLC-001.json`, which adds `Time` end-to-end and proves the generated artifact in real TIA Portal V21 before introducing block-signature/catalog abstractions.
