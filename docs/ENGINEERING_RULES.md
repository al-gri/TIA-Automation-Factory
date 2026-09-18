# Generator Engineering Rules

Status: **ACCEPTED**

These rules are the development contract for the generator architecture.

## 1. Repository and task discipline

1. GitHub is the durable source of truth.
2. One versioned task has one bounded objective.
3. Every task defines scope, protected paths, deterministic acceptance, risk class and bounded repairs.
4. Do not expand infrastructure without a concrete generator blocker.
5. No workflow/bot self-merge. Primary connected ChatGPT may execute a delegated technical merge only after all exact-SHA gates defined by governance are satisfied.

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

### 4.1 Dependency latency

- single writer by default;
- no silent coercion/merge/latch;
- every scheduling dependency is classified as `SameScan` or `PreviousState`;
- only `PreviousState` dependencies cut a same-scan cycle;
- a stateful primitive is not automatically a one-scan delay and is not removed wholesale from SCC analysis;
- each stateful primitive declares which outputs depend on current inputs, which depend on previous state and what state is committed for the next scan;
- semantic lowering follows the observable order `read previous state -> compute current outputs -> commit next state`;
- edge, timer, latch/reset and feedback primitives require dedicated temporal tests rather than a generic `stateful = delayed` assumption.

A stateful FB with current-input feed-through must retain those current-input dependencies in the same-scan graph.

### 4.2 Same-controller semantic scheduling

- build one controller-level graph containing all `SameScan` dependencies;
- area/unit ownership boundaries do not erase semantic dependencies;
- SCC analysis is mandatory on the controller-level same-scan graph;
- SCCs with more than one node and self-loops are compilation errors unless the cycle is actually cut by `PreviousState`;
- after previous-state dependencies are excluded, the controller semantic graph must be a DAG;
- scheduling ties use deterministic semantic IDs/order, never UI position or accidental JSON order;
- same-scan propagation means a downstream consumer observes the value produced earlier in the deterministic schedule.

### 4.3 Atomic executable-container scheduling

The initial modular architecture emits each unit/application FB as one atomic once-per-scan invocation.

Therefore:

- map semantic nodes to generated executable containers after semantic dependency validation;
- derive a quotient/container graph where `UnitA -> UnitB` means a `SameScan` value produced in `UnitA` is required by `UnitB`;
- the container graph must be a DAG independently of the finer semantic-node DAG;
- reject a container-level cycle even when the underlying node graph is acyclic, because `UnitA(part) -> UnitB -> UnitA(part)` cannot be realized by one atomic call per unit;
- emit unit/application FB invocations from a stable topological order of the container graph;
- schedule internal statements deterministically inside each container;
- cross-unit data passes through explicit unit/application interfaces or orchestration signals, never another unit's private multi-instance memory;
- area grouping is organizational unless an area is itself emitted as an atomic callable container; if so, the same quotient-DAG rule applies;
- multi-phase/split-unit execution is not inferred as an automatic repair and requires a separate HIGH-risk design.

Equivalent semantic models with different layout, input order or unit declaration order must produce equivalent semantic and container schedules.

### 4.4 Cross-controller semantics

Ordinary runtime connections do not have same-scan semantics across controllers.

A cross-controller dependency requires an explicit communication primitive/profile defining transport, buffering, stale/error behavior and update latency. Until such a primitive exists, ordinary cross-controller runtime connections are compilation errors.

PLC semantic changes are HIGH risk when they establish/change scheduling, state behavior, execution-container boundaries or communication latency.

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
- unit/area declaration order cannot change scan semantics or generated invocation order;
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
3. temporal-dependency tests for edge/timer/latch/reset/feedback primitives;
4. controller-level SCC/DAG tests;
5. executable-container quotient-DAG tests, including an acyclic semantic graph that would require `UnitA(part) -> UnitB -> UnitA(part)` and therefore must be rejected;
6. PLC IR golden tests;
7. Siemens SCL AST/emitter golden tests;
8. GeneratorCli/package tests on Linux;
9. exact candidate artifact/package in real TIA V21;
10. later HMI/reference-project tests when applicable.

Use the cheapest authoritative layer, but never omit real TIA acceptance for changed Siemens behavior.

## 15. Review risk

- LOW: isolated scalar/format/data mapping with established semantics.
- MEDIUM: internal type/AST representation preserving established behavior.
- HIGH: canonical schema, scan semantics, executable-container scheduling, Open Library qualification/materialization, target preflight, TIA trust boundary, safety scope, cross-controller communication semantics, major architecture.

HIGH-risk work requires one independent reviewer distinct from the candidate author/implementer.

Default reviewer selection:

- coding-agent-authored candidate with primary ChatGPT independent -> `chatgpt`;
- primary-ChatGPT-authored/co-authored candidate -> `chatgpt-secondary` in a fresh isolated ChatGPT session;
- secondary-ChatGPT-authored/co-authored candidate -> primary `chatgpt` if independent.

Gemini has no standing project role. A simultaneous second reviewer is escalation only, not a standing gate. Reviewer conflict blocks acceptance when multiple independent reviews are intentionally requested.

## 16. Definition of Done

A Siemens-generating task is done only when all applicable machine-verifiable gates pass:

```text
trusted versioned task
 -> protected-path checks
 -> deterministic tests
 -> generated artifact/package
 -> required independent external review
 -> trusted target preflight
 -> real TIA V21 assembly/import
 -> full PLC compile (errors == 0)
 -> required object/version/access-mode checks
 -> reproducibility evidence
 -> delegated technical merge decision
```

If the trusted task explicitly defines real Windows/TIA execution as a post-merge trusted-main qualification step, the pre-merge gate is independent source/API review plus deterministic candidate evidence, followed by delegated merge and then authoritative trusted-main TIA acceptance.

An LLM saying `done` is never acceptance evidence.
