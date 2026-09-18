# Generator Engineering Rules

Status: **PROPOSED — review round 2 required**

These rules become the development contract only after the architecture proposal is independently approved and merged.

## 1. Repository and task discipline

1. GitHub is the durable source of truth.
2. One versioned task has one bounded objective.
3. Every task defines scope, protected paths, deterministic acceptance, risk class and bounded repairs.
4. Do not expand infrastructure without a concrete generator blocker.
5. No automatic merge; human merge decision remains separate from technical acceptance.

## 2. Layer boundaries

```text
Frontend adapter
 -> Domain
 -> PlcCompiler / PLC IR
 -> SiemensBackend
 -> declarative package
 -> trusted TiaV21Worker
```

Forbidden:

- Domain -> React Flow dependency;
- Domain/PlcCompiler -> `Siemens.Engineering`;
- UI layout -> PLC semantics;
- TiaV21Worker -> business/domain semantic decisions;
- AI-authored executable payloads on the trusted Windows/TIA host.

## 3. Canonical model rules

- React Flow JSON is not the source language.
- Canonical model is versioned and migration-controlled.
- Semantic IDs are stable and independent of canvas/layout IDs.
- Device ports are typed/directional.
- Engineering parameters are not disguised as graph wires.
- Siemens/Open Library names stay outside Domain.
- Generated/manual project ownership zones are explicit.

## 4. PLC scan-semantic rules

The compiler must make PLC scan behavior explicit.

- single writer by default;
- no silent coercion/merge/latch;
- explicit stateful primitives define scan-to-scan boundaries;
- SCC analysis is mandatory for combinational partitions;
- combinational cycles/self-loops are compilation errors unless cut by an explicit stateful boundary;
- after stateful edges are removed, each combinational partition must be a DAG;
- executable combinational statements are emitted from a **stable topological sort**;
- scheduling ties use deterministic semantic IDs/order, never UI position or accidental JSON order;
- same-scan propagation semantics are documented and tested;
- equivalent semantic models with different layout/order produce equivalent schedules.

PLC semantic changes are HIGH risk when they establish/change scheduling or state behavior.

## 5. Open Library version rules

- Never use an unspecified/default/latest Open Library version.
- Normal generation uses only a **qualified native V21 library profile**.
- A V19 -> V21 upgrade is a separate HIGH-risk qualification/migration event, never an ordinary generation step.
- Qualification records source V19 archive SHA, TIA V21 version, resulting qualified V21 identity/hash, exact type versions/dependencies and reference-compile evidence.
- A new qualified library version requires explicit review/migration, not silent replacement.

## 6. Open Library integration rules

- Object mappings are data-driven through a versioned Siemens catalog.
- Normal device FBs use multi-instance memory inside bounded unit/application FBs unless documented otherwise.
- Do not generate one wrapper/instance DB per ordinary physical device by default.
- Never consume undocumented internal FB instance memory.
- Do not use `iStatus`/scrolling `iErrorCode` as PLC-control state.
- Mode and simulation are subsystem contexts.
- Reuse Open Library constants and documented semantics rather than duplicating magic values/frameworks.
- Preserve named interlock/permissive semantics.
- Treat sequencer/shared-instance behavior as a separately designed HIGH-risk feature.

## 7. DB access-mode rules

SiemensBackend must model DB access mode explicitly (`Optimized` or `Standard`).

If the selected Open Library alarm profile uses the legacy alarm-generator workflow, generated Error DBs must be Standard/non-optimized and proven in TIA V21.

Use the simplest supported representation first: SCL block attributes when sufficient. Do not add SimaticML or Simatic Source Document solely because they exist.

Do not place required standard-access Error DBs in a TIA Software Unit context that forces optimized access.

## 8. Target-profile and hardware prerequisite rules

Target profiles/base projects are versioned and hash-bound.

Before library/application materialization, ProjectAssembler must preflight the actual target and verify required Open Library CPU prerequisites, including System memory and Clock memory enabled state plus expected addresses.

The exact V21 Openness read/validation mechanism must be established by a dedicated trusted TIA test. Missing/mismatched settings are deterministic errors; do not silently guess or mutate semantic assumptions.

## 9. SCL generation rules

- SCL is the primary application-generation language while it is sufficient.
- Before non-trivial FB calls, use a minimal structural SCL AST/emitter.
- Do not grow a compiler from ad-hoc string concatenation for calls/expressions.
- Golden output tests are mandatory.
- Add SimaticML/YAML only when a concrete required TIA feature cannot be expressed safely in SCL.

## 10. Determinism rules

Unchanged canonical project + generator version + target profile + qualified library profile must produce semantically identical output.

- no random generated symbol names;
- naming is centralized/tested;
- layout changes cannot alter PLC output;
- explicit ordering for maps/collections;
- manifests record generator/library/target identities;
- regeneration must not duplicate generated TIA objects.

## 11. TIA trust-boundary rules

Windows/TIA receives only validated/hash-bound declarative inputs and trusted code from `main`.

Normal ProjectAssembler:

1. validates package;
2. opens/copies trusted target profile;
3. preflight-validates target prerequisites;
4. opens qualified native V21 library;
5. materializes exact objects/dependencies;
6. imports/generates sources;
7. compiles full PLC;
8. saves project;
9. returns structured diagnostics/evidence.

It does not perform Domain compilation or major-version library upgrade during a normal build.

## 12. Diagnostics rules

- compiler diagnostics reference canonical object/port IDs;
- backend emits source-map metadata;
- TIA diagnostics are correlated back to generated symbol and semantic source where possible;
- errors are not hidden by automatic repair/coercion in the generator itself.

## 13. Safety boundary

The generator may consume an already-engineered E-stop/safety status as a normal input required by standard Open Library logic. It does not generate, validate or certify Siemens F-safety logic.

## 14. Test pyramid

1. Domain/compiler unit tests;
2. schema/type/graph validation;
3. SCC/topological scheduler tests;
4. PLC IR golden tests;
5. Siemens SCL AST/emitter golden tests;
6. GeneratorCli/package tests on Linux;
7. exact candidate artifact/package in real TIA V21;
8. later HMI/reference-project tests when applicable.

Use the cheapest authoritative layer, but never omit real TIA acceptance for changed Siemens behavior.

## 15. Review risk

- LOW: isolated scalar/format/data mapping with established semantics.
- MEDIUM: internal type/AST representation preserving established behavior.
- HIGH: canonical schema, scan semantics, Open Library qualification/materialization, target preflight, TIA trust boundary, safety scope, major architecture.

HIGH-risk work requires independent ChatGPT and Gemini review of the same exact candidate SHA. Reviewer conflict blocks acceptance.

## 16. Definition of Done

A Siemens-generating task is done only when all applicable machine-verifiable gates pass:

```text
trusted versioned task
 -> protected-path checks
 -> deterministic tests
 -> generated artifact/package
 -> required external review
 -> trusted target preflight
 -> real TIA V21 assembly/import
 -> full PLC compile (errors == 0)
 -> required object/version/access-mode checks
 -> reproducibility evidence
 -> human merge decision
```

An LLM saying `done` is never acceptance evidence.