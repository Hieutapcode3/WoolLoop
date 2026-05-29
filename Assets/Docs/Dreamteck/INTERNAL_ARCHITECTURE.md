# Dreamteck Splines Internal Architecture

## System Flow

```text
SplinePoint[] -> Spline -> SplineComputer sample cache
                         -> SampleCollection
                         -> SplineUser clipped samples
                         -> Sample modifiers
                         -> Component Build
                         -> Component PostBuild
                         -> Transform / Mesh / Collider / Objects / Events
```

## Core Ownership

`SplineComputer` owns the authoritative scene spline. Its internal `Spline` stores local or world point data depending on `space`. The computer owns raw samples, transformed samples, trigger groups, node links, and subscriber references.

`SplineUser` derivatives do not own the source spline. They subscribe to a `SplineComputer`, copy or reference sample collection data, apply clip ranges and sample modifiers, then generate their own output.

## Dependency Graph

```text
Dreamteck.Utilities
  -> ArrayUtility, DMath, MeshUtility, TS_Mesh, TS_Bounds, TransformUtility

Dreamteck.Splines.Core
  -> SplinePoint
  -> SplineSample
  -> Spline
  -> SampleCollection
  -> SplineTrigger / TriggerGroup
  -> TransformModule
  -> SplineUtility
  -> IO / Primitives

Dreamteck.Splines.Components
  -> SplineComputer
  -> SplineUser
      -> SplineTracer
          -> SplineFollower / SplinePositioner / SplineProjector
      -> MeshGenerator
          -> Tube / Surface / SplineMesh / Path / Waveform / Renderer / ComplexSurface
      -> Collider generators
      -> ObjectController
      -> ParticleController
      -> LengthCalculator
  -> Node
  -> SplineMorph
```

## Update Loop

`SplineComputer`:

```text
Awake
  ResampleTransform

Update/FixedUpdate/LateUpdate depending on updateMode
  ResampleTransformIfNeeded
  if queued resample:
    CalculateSamples
  if transform changed:
    TransformSamples
  if queued rebuild:
    RebuildUsers
```

`SplineUser`:

```text
Awake
  CacheTransform
  GetSamples
  RebuildImmediate if configured

Update/FixedUpdate/LateUpdate depending on updateMethod
  Run
  if rebuild queued:
    GetSamples if needed
    Build or threaded Build
  PostBuild on main thread
  LateRun
```

## Data Flow

Point mutation:

```text
SetPoint(s)
  optional world/local transform conversion
  point dirty marking
  node propagation if point is connected
  Rebuild(true)
```

Sampling:

```text
Spline.Evaluate/EvaluateUniform
  -> raw samples
  -> TransformSamples if computer is in local space or transform changed
  -> SampleCollection
```

User clipping:

```text
SplineUser.GetSamples
  -> get collection from computer
  -> evaluate clipFrom and clipTo boundary samples
  -> calculate clipped sample count
```

Modifiers:

```text
GetSample / Evaluate
  -> raw clipped sample
  -> OffsetModifier
  -> RotationModifier
  -> ColorModifier
  -> SizeModifier
```

## Render Flow

There is no custom render pipeline hook. Mesh components generate Unity meshes.

```text
MeshGenerator.Build
  -> CreateMesh if needed
  -> BuildMesh override fills TS_Mesh

MeshGenerator.PostBuild
  -> Transform TS_Mesh into local space
  -> double-sided or flip faces
  -> calculate tangents if enabled
  -> write TS_Mesh into Unity Mesh
  -> MarkDynamic if enabled
  -> recalculate normals if selected
  -> assign MeshFilter.sharedMesh
  -> queue MeshCollider update
```

Material and shader selection is external through `MeshRenderer.sharedMaterials`.

## Event Flow

Triggers:

```text
SplineFollower.Move or SplineTracer.SetPercent(checkTriggers: true)
  -> CheckTriggers(start, end)
  -> TriggerGroup.Check
  -> SplineTrigger.Check
  -> SplineTrigger.Invoke(user)
  -> UnityEvent<SplineUser>
```

Nodes:

```text
SplineTracer.CheckNodes
  -> computer.GetJunctions interval
  -> queue NodeConnection
  -> InvokeNodes
  -> onNode(List<NodeConnection>)
```

Rebuild:

```text
SplineComputer.Rebuild
  -> onRebuild
  -> queued user rebuilds
SplineUser.PostBuild
  -> onPostBuild
```

## Manager/Service Patterns

There is no global service locator for splines. Coordination is object-reference based:

- `SplineComputer` has subscribers.
- `SplineUser` has one `SplineComputer`.
- `Node` owns connections to multiple `SplineComputer` points.
- `SplineUtility.Merge` rewires subscribers and nodes during merge.

Threading is handled through `SplineThreading`, a small internal static dispatcher with delegates, not through Unity Jobs.

## Important Utility Classes

| Class | Role |
|---|---|
| `ArrayUtility` | Adds/removes/shifts arrays used throughout source. |
| `DMath` | Double-precision clamp/lerp/move helpers. |
| `MeshUtility` | Tangents, face flipping, double-sided meshes, OBJ output. |
| `TS_Mesh` | Temporary mesh data container before writing Unity `Mesh`. |
| `TS_Bounds` | Bounds data used by mesh bending/channel systems. |
| `TransformUtility` | Editor/runtime transform math helpers. |
| `AsyncJobSystem` | Coroutine-like async job helper in utilities. |

## Serialization Notes

Most inspector fields are `[SerializeField] [HideInInspector]` and drawn by custom editors. This means values are serialized even if not visible in the default inspector.

`SplinePoint` uses `FormerlySerializedAs` for backward compatibility. `SplineComputer` also has former serialized names for old data.

`ObjectController` stores spawned references. Prefab/editor retention is intentionally handled with `EditorUtility.SetDirty` to avoid losing spawned prefab instances.

## Extension Points

Best-supported extension points:

- Derive from `SplineUser` for custom spline consumers.
- Derive from `MeshGenerator` for custom mesh output.
- Use `ObjectControllerCustomRuleBase` for custom placement behavior.
- Use `SplineSampleModifier` pattern for custom sample modification if editor integration is added.

Thread-safe extension rule: pure math in `Build`, Unity API writes in `PostBuild`.
