# Generator Engineering Rules

Status: **PROPOSED with target architecture**

These rules are intended to become the non-negotiable development contract for the generator after architecture review and merge.

## Rule 1 — GitHub is the source of truth

Architecture, current state, versioned tasks, acceptance criteria, review decisions and meaningful evidence must be durable in this repository/GitHub.

Chat, local notes and agent history are coordination only.

## Rule 2 — One task, one bounded objective

Every autonomous implementation step must have a versioned task with:

- exact goal;
- bounded scope;
- explicit protected paths;
- deterministic requirements;
- TIA acceptance when Siemens behavior changes;
- risk class;
- bounded repair attempts.

Do not combine unrelated refactors, architecture changes and feature work in one coding-agent task.

## Rule 3 — Do not expand infrastructure without a concrete generator blocker

The current autonomous/review/TIA path is frozen.

Infrastructure changes require a demonstrated blocker from a real generator task and separate review. Do not create framework layers, provider routing, workflow features or Windows capabilities merely because they may be useful later.

## Rule 4 — Maintain strict layer boundaries

```text
Frontend adapter
  -> Domain
  -> PlcCompiler / PLC IR
  -> SiemensBackend
  -> declarative package
  -> trusted TiaV21Worker
```

Forbidden dependencies:

- Domain -> React Flow;
- Domain -> `Siemens.Engineering`;
- PlcCompiler -> `Siemens.Engineering`;
- TiaV21Worker -> domain/business semantic decisions;
- UI layout -> generated PLC semantics.

## Rule 5 — Canonical project model is independent of React Flow

React Flow nodes/edges are not persisted as the canonical source language.

The product stores a versioned automation model plus separate layout metadata. React Flow adapts to/from that model.

Changing visual position/style must not change generated PLC artifacts.

## Rule 6 — Canonical domain is independent of Siemens Open Library

Domain concepts are `Motor`, `TwoPositionValve`, `Interlock`, `Permissive`, signals, ports, units, parameters, etc.

Names such as `fbValve_Solenoid`, `udtHMI_ValveControl`, library GUIDs and TIA paths exist only in Siemens-specific mapping/catalog code.

## Rule 7 — PLC scan semantics must be explicit

No implicit Node-RED/event semantics.

For each logical construct define whether it represents level, edge, pulse, memory, timer, state or combinational expression.

Compiler requirements:

- deterministic execution order;
- single-writer default;
- type checking;
- combinational cycle rejection;
- feedback only through explicit stateful elements;
- defined same-scan behavior.

PLC-semantic changes are at least MEDIUM risk; safety-relevant semantics are HIGH risk.

## Rule 8 — Stable IDs and deterministic names

Semantic objects have stable technical IDs and human engineering tags separately.

Generated symbols must be deterministic. No random GUID should appear in emitted SCL names. Regeneration from unchanged inputs must not churn source.

Naming rules must be centralized and tested.

## Rule 9 — Generated and manual code are separate ownership zones

Generated project groups/files are generator-owned and may be replaced.

Manual extension groups are human-owned and never overwritten.

Users must not be required to edit generated sources to complete normal engineering work.

## Rule 10 — Pin Open Library identity and type versions

Never silently use an unspecified/default/latest library version.

A reproducible target records:

- source library identity/archive hash;
- upgraded V21 library identity/cache key;
- exact released FB/UDT type versions or equivalent stable identifiers;
- generator/Open-Library catalog version.

Library upgrades are explicit migration events.

## Rule 11 — Open Library object mappings are data-driven

Do not spread FB parameter names through arbitrary compiler classes.

Use a versioned Siemens/Open-Library descriptor/catalog for interfaces, directions, types, UDTs and dependencies. Handwritten descriptors are acceptable only for a small proof slice and must be replaceable by trusted extraction/verification.

## Rule 12 — Multi-instance is the default Open Library memory model

Generated unit/application FBs should contain normal Open Library FB instances in static/multi-instance memory unless a documented object requires a different memory model.

Do not generate one instance DB per ordinary field device by default.

## Rule 13 — Never use library internal instance memory as an API

Only documented block inputs, outputs, HMI UDTs and Error UDTs may be consumed.

`iStatus` and scrolling `iErrorCode` are display information, not PLC-control signals.

## Rule 14 — Mode and simulation are subsystem contexts

Mode and simulation should be owned at an appropriate system/unit level and propagated to devices.

Do not create unrelated local copies for each visual node.

Open Library constants should be used rather than duplicating magic integer values in generated SCL.

## Rule 15 — Library/project prerequisites are centralized

CPU System/Clock memory configuration, the Open Library tag-table master copy, required library types and dependency synchronization are TIA assembly concerns.

They must not be repeated manually in every device generator.

## Rule 16 — Keep SCL generation structural

Before non-trivial FB calls, use a minimal SCL AST/structured emitter.

Do not grow a large compiler from arbitrary `StringBuilder.Append`/string interpolation fragments for expressions and block calls.

All emitters require deterministic golden/snapshot tests.

## Rule 17 — Prefer one generated SCL compilation unit until it becomes a blocker

Multiple TIA artifacts can be declared in one source. Keep the candidate package minimal while this is sufficient.

Expand workflow/package infrastructure only when a real feature cannot be represented safely as the bounded source artifact.

## Rule 18 — TIA compilation is authoritative for Siemens syntax/integration

Unit tests and golden SCL are necessary but do not prove TIA correctness.

Any task that changes generated Siemens artifacts or Open Library integration must ultimately pass real TIA Portal V21 compile with zero errors for the exact candidate artifact/package.

Warnings may be allowed only when explicitly accepted by the task; production target is zero generated warnings where practical.

## Rule 19 — Windows/TIA accepts declarative bounded inputs only

AI-authored executables/scripts never run on the trusted TIA machine.

The Windows runner checks out trusted `main` infrastructure and receives only validated/hash-bound generation inputs/artifacts.

`TiaV21Worker` is the single Openness execution path.

## Rule 20 — Diagnostics must retain source identity

Every compiler diagnostic references domain IDs/ports, not only free-text generated names.

As the backend matures, emitted Siemens source must carry a source map so TIA errors can be traced back to the visual engineering object.

## Rule 21 — Do not hide errors through automatic coercion

No silent type conversions, implicit multiple-writer ORs, implicit latches or guessed parameter binding.

If compiler intent is ambiguous, fail with a deterministic diagnostic and require the model to be explicit.

## Rule 22 — Safety has a hard scope boundary

The generator may consume a normal signal representing an already-engineered safety state/E-stop status, but must not claim to generate or validate Siemens F-safety logic until a separate safety architecture, certification/risk process and acceptance strategy are approved.

## Rule 23 — Reuse Open Library semantics instead of duplicating them

When Open Library already defines mode, simulation, error/HMI contracts, interlock/permissive behavior or device control, the Siemens backend should integrate with those contracts rather than reimplementing a parallel Siemens-specific framework.

The vendor-neutral Domain may be richer, but the Siemens lowering should avoid unnecessary wrapper layers.

## Rule 24 — Avoid wrapper explosion

Do not create a unique generated wrapper FB for every physical device instance.

Use reusable unit/application FBs and multi-instances. Semantic wrappers, if required, are per reusable type/pattern.

## Rule 25 — Large plants are hierarchical

Do not design the product around one global canvas or one giant application FB.

Project model, generated FBs, HMI/Error DBs, modes, simulation and visual navigation must support Areas/Units/subsystems.

## Rule 26 — Every architectural abstraction must earn its existence

Add interfaces/layers only when they isolate a real variability axis or enforce a boundary:

- frontend adapter;
- domain;
- compiler/IR;
- target backend;
- trusted TIA assembler.

Do not add generic plugin frameworks, CQRS/event buses, distributed services or other infrastructure without a measured requirement.

## Rule 27 — Test at the cheapest authoritative layer

Preferred test pyramid:

1. pure Domain/compiler unit tests;
2. type/graph validation tests;
3. PLC IR golden tests;
4. Siemens SCL AST/emitter golden tests;
5. GeneratorCli/package tests on Linux;
6. exact artifact/package compile in TIA V21;
7. later integration tests against reference projects/HMI where required.

Do not use TIA for behavior that can be proven deterministically on Linux, but do not omit TIA for Siemens integration.

## Rule 28 — Schema evolution is explicit

Canonical project JSON has `schemaVersion` and controlled migrations.

Breaking model changes require migration tests and architecture review appropriate to risk. The UI and compiler must not rely on undocumented JSON shapes.

## Rule 29 — Review risk is based on semantics, not line count

Examples:

- LOW: isolated scalar type mapping, small output formatting fix, data descriptor addition with established semantics.
- MEDIUM: PLC IR behavior, type system, graph validation, Open Library binding for a known object, SCL AST behavior.
- HIGH: canonical project schema, scan semantics, TIA worker/trust boundary, library upgrade/materialization mechanism, safety behavior, major architecture changes.

Follow `docs/AI_COLLABORATION_MODEL.md` for reviewer requirements.

## Rule 30 — Definition of Done is machine-verifiable

A task is DONE only when all task-specific gates pass. For generated PLC behavior this normally means:

```text
trusted task resolved from main
protected paths unchanged
build/tests pass
requirements pass
artifact/package generated deterministically
external review passes
real TIA V21 compile passes
PLC/TIA review passes
human merge decision remains separate
```

An LLM stating that work is complete is never acceptance evidence.
