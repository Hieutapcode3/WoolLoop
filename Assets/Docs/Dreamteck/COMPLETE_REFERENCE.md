# Dreamteck Splines Complete Reference

This reference is source-derived from the package under `Assets/Plugins/Dreamteck`. It favors production meaning over raw source dumping. Defaults are taken from field initializers and property behavior in source.

## Assemblies

| Assembly | Type | References | Runtime |
|---|---|---|---:|
| `Dreamteck.Utilities` | asmdef | none | yes |
| `Dreamteck.Splines` | asmdef | `Dreamteck.Utilities` | yes |
| `Dreamteck.Utilities.Editor` | asmdef | editor utilities | no |
| `Dreamteck.Splines.Editor` | asmdef | spline editor tooling | no |

## Core Data Types

### `Spline`

Type: serializable class. Purpose: raw spline data and direct mathematical evaluation.

Defaults:

| Member | Type | Default | Effect |
|---|---|---|---|
| `points` | `SplinePoint[]` | empty | Control point storage. |
| `type` | `Spline.Type` | `Bezier` | Evaluation algorithm. |
| `linearAverageDirection` | `bool` | `true` | Linear tangent averages adjacent directions. |
| `customValueInterpolation` | `AnimationCurve` | `null` | Interpolates color/size between points. |
| `customNormalInterpolation` | `AnimationCurve` | `null` | Interpolates normals between points. |
| `sampleRate` | `int` | `10` | Sample count density for non-linear splines. |
| `knotParametrization` | `float` | `0` | CatmullRom parametrization, clamped 0..1. |

Enums:

- `Spline.Direction`: `Forward = 1`, `Backward = -1`.
- `Spline.Type`: `CatmullRom`, `BSpline`, `Bezier`, `Linear`.

Important API:

- `CalculateLength(from = 0, to = 1, resolution = 1)`: CPU traversal by `moveStep / resolution`; higher resolution costs more.
- `Project(position, subdivide = 4, from = 0, to = 1)`: recursive closest point search; expensive when called every frame in many objects.
- `Raycast`, `RaycastAll`: line/ray checks along sampled segments; physics cost scales with spline length and resolution.
- `EvaluatePosition(percent)`, `Evaluate(percent)`, `Evaluate(percent, ref sample)`: direct evaluation without `SplineComputer` transform cache.
- `Evaluate(ref SplineSample[] samples, from = 0, to = 1)`: allocates/resizes array when length mismatch.
- `EvaluateUniform(...)`: produces uniformly spaced samples based on length; more CPU than default sampling.
- `Travel(start, distance, direction)`: distance traversal along sampled curve.
- `Break`, `Close`, `CatToBezierTangents`, `FormatFromTo`.

Use when: you need pure spline math or offline conversion. Do not use as the main scene component; use `SplineComputer` for transform-aware cached sampling and subscribers.

Performance: medium CPU for projection/raycast/uniform evaluation; cheap for simple cached position reads is not provided here because this class recalculates directly.

### `SplinePoint`

Type: serializable struct. Purpose: one spline control point.

Fields and defaults:

| Field | Type | Constructor default | Purpose |
|---|---|---|---|
| `type` | `SmoothMirrored`, `Broken`, `SmoothFree` | `SmoothMirrored` except full tangent constructor uses `Broken` | Tangent handle relation. |
| `position` | `Vector3` | input position | Main point. |
| `tangent` | `Vector3` | input or point | Incoming tangent. |
| `tangent2` | `Vector3` | mirrored unless provided | Outgoing tangent. |
| `normal` | `Vector3` | `Vector3.up` | Sample up vector source. |
| `size` | `float` | `1` | Width/scale multiplier for users and generators. |
| `color` | `Color` | `Color.white` | Vertex color/sample color. |
| `isDirty` | `bool` | `false` | Non-serialized dirty flag. Reading does not automatically clear in current source despite comment. |

Important API: `Lerp`, `SetPosition`, `SetTangentPosition`, `SetTangent2Position`, `Flatten`, component setters for position/tangent/normal/color, equality operators.

Dependency: used by `Spline`, `SplineComputer`, `Node`, `SplineMorph`, primitives, IO.

### `SplineSample`

Type: struct. Purpose: evaluated spline state.

Fields: `position`, `up`, `forward`, `color`, `size`, `percent`.

Computed properties:

- `right`: cross product of `up` and `forward`.
- `rotation`: `Quaternion.LookRotation(forward, up)`.

Important API: `FastCopy`, static and instance `Lerp`.

Use when: pass by `ref` in hot paths to avoid allocations. Do not repeatedly call allocating `Evaluate` overloads when a reusable sample is enough.

### `SampleCollection`

Type: class. Purpose: cached sample array helper used by `SplineComputer` and `SplineUser`.

Defaults: `samples = []`, `optimizedIndices = []`, `sampleMode = Default`.

Important API: `GetClippedSampleCount`, `GetSamplingValues`, `EvaluatePosition`, `Evaluate`, `EvaluatePositions`, `Travel`, `TravelWithOffset`, `Project`, `CalculateLength`, `CalculateLengthWithOffset`.

Performance: cheap when evaluating cached samples; projection and travel still scale with sample count.

## Main Runtime Components

### `SplineComputer`

Type: `MonoBehaviour`, `ExecuteInEditMode`, menu `Dreamteck/Splines/Spline Computer`.

Purpose: scene-facing spline owner, sample cache, transform conversion, subscriber manager, trigger owner, node owner.

Defaults:

| Member | Type | Default | Notes |
|---|---|---|---|
| `space` | `World`, `Local` | `Local` | Changing converts current points. |
| `type` | `Spline.Type` | internal spline constructed as `CatmullRom` | Setter rebuilds. |
| `sampleRate` | `int` | `10` | Setter clamps minimum to `2`. |
| `sampleMode` | `Default`, `Uniform`, `Optimized` | `Default` | Optimized uses angle threshold. |
| `optimizeAngleThreshold` | `float` | `0.5` | Clamped minimum `0.001`. |
| `is2D` | `bool` | `false` | Rewrites points when changed. |
| `linearAverageDirection` | `bool` | `true` | Delegates to `Spline`. |
| `knotParametrization` | `float` | `0` | CatmullRom uniform/centripetal/chordal blend. |
| `multithreaded` | `bool` | `false` | Uses `SplineThreading` in play mode. |
| `updateMode` | `Update`, `FixedUpdate`, `LateUpdate`, `AllUpdate`, `None` | `Update` | Controls cache/rebuild processing. |
| `triggerGroups` | `TriggerGroup[]` | empty | Trigger system storage. |

Editor-only defaults: `editorDrawPivot = true`, `editorPathColor = white`, `editorAlwaysDraw = false`, `editorDrawThickness = false`, `editorBillboardThickness = true`, `editorUpdateMode = Default`.

Important API:

- Point access: `GetPoints`, `GetPoint`, `GetPointPosition`, `GetPointNormal`, `GetPointTangent`, `GetPointTangent2`, `GetPointSize`, `GetPointColor`.
- Point mutation: `SetPoints`, `SetPoint`, `SetPointPosition`, `SetPointTangents`, `SetPointNormal`, `SetPointSize`, `SetPointColor`.
- Sampling: indexer `this[int]`, `rawSamples`, `EvaluatePosition`, `Evaluate`, `EvaluatePositions`, `GetSamplingValues`.
- Distance/projection: `Travel`, `Project`, `CalculateLength`, `Raycast`, `RaycastAll`.
- Lifecycle/cache: `ResampleTransform`, `Rebuild`, `RebuildImmediate`.
- Subscribers: `Subscribe`, `Unsubscribe`, `IsSubscribed`, `GetSubscribers`.
- Triggers: `AddTriggerGroup`, `AddTrigger`, `RemoveTrigger`, `CheckTriggers`, `ResetTriggers`.
- Nodes: `ConnectNode`, `DisconnectNode`, `GetNode`, `GetNodes`, `GetJunctions`, `TransferNode`, `ShiftNodes`, `GetConnectedComputers`.
- Transform-safe helpers: `TransformPoint`, `InverseTransformPoint`, `TransformDirection`, `InverseTransformDirection`.

Lifecycle:

1. `Awake` caches transform matrices.
2. `Update`/`FixedUpdate`/`LateUpdate` call `RunUpdate` according to `updateMode`.
3. Transform changes resample world/local matrices and queue subscriber rebuild.
4. Rebuild either runs sample calculation immediately or through `SplineThreading`.
5. Subscribers rebuild after sample changes.

When to use: every scene spline should generally use `SplineComputer`.

When not to use: pure data/offline jobs can use `Spline`, but do not mutate `SplineComputer` points from worker threads.

Side effects: setters rebuild; node updates can propagate point changes to connected computers; transform-space switching rewrites stored point coordinates.

### `SplineUser`

Type: `MonoBehaviour`, `ExecuteInEditMode`, base for most consumers.

Defaults:

| Member | Default | Effect |
|---|---:|---|
| `updateMethod` | `Update` | Where user rebuild scheduler runs. |
| `spline` | `null` | Assigned computer; subscribes/unsubscribes on set. |
| `autoUpdate` | `true` | If false, queued rebuilds are ignored until manual rebuild. |
| `clipFrom` | `0` | Sample clip start. |
| `clipTo` | `1` | Sample clip end. |
| `loopSamples` | `false` | Allows wrapped clip range. |
| `multithreaded` | `false` | Runs `Build` on worker thread. |
| `buildOnAwake` | `true` | Immediate build in play mode. |
| `buildOnEnable` | `false` | Optional rebuild on enable. |
| Modifiers | empty | `RotationModifier`, `OffsetModifier`, `ColorModifier`, `SizeModifier`. |

Events: `onPostBuild`.

Important API: `GetSampleRaw`, `GetSample`, `GetSampleWithAngleCompensation`, `Rebuild`, `RebuildImmediate`, `ApplySampleModifiers`, `SetClipRange`, `ClipPercent`, `UnclipPercent`, `EvaluatePosition`, `Evaluate`, `EvaluatePositions`, `Travel`, `TravelWithOffset`, `Project`, `CalculateLength`, `CalculateLengthWithOffset`.

Lifecycle: `Awake` caches transform and gets samples; update method calls `Run`, rebuild `Build`, then `LateRun`; `PostBuild` is main-thread only.

Extension pattern: override `Build` for calculation and `PostBuild` for Unity object mutation. Do not touch Unity objects from `Build` when `multithreaded = true`.

### `SplineTracer`

Type: `SplineUser` base for motion components.

Defaults: `direction = Forward`, `dontLerpDirection = false`, `physicsMode = Transform`, `applyDirectionRotation = true`, `useTriggers = false`, `triggerGroup = 0`, `motion = TransformModule`, target refs null.

Enums: `PhysicsMode`: `Transform`, `Rigidbody`, `Rigidbody2D`.

Events: `onNode(List<NodeConnection>)`, `onMotionApplied`.

Important API: `SetPercent`, `GetPercent`, `SetDistance`, inherited evaluation/travel APIs.

Dependencies: `TransformModule`, optional `Rigidbody`/`Rigidbody2D`, `SplineTrigger`, `Node`.

Performance: cheap for transform application, medium if trigger/node scanning is enabled on many tracers.

### `SplineFollower`

Type: `SplineTracer`, menu `Dreamteck/Splines/Users/Spline Follower`.

Defaults: `wrapMode = Default`, `followMode = Uniform`, `autoStartPosition = false`, `follow = true`, `startPosition = 0`, `preserveUniformSpeedWithOffset = false`, `followSpeed = 1`, `followDuration = 1`, empty speed modifier.

Enums: `FollowMode`: `Uniform`, `Time`; `Wrap`: `Default`, `Loop`, `PingPong`.

Events: `onEndReached(double)`, `onBeginningReached(double)`, serialized Unity float events.

Important API: `Restart`, `SetPercent`, `SetDistance`, `Move(double percent)`, `Move(float distance)`.

When to use: moving characters, cameras, vehicles, VFX anchors along a spline.

When not to use: strict physics simulation unless the rigidbody mode/velocity handling matches gameplay requirements.

Side effects: negative `followSpeed` flips direction; `PingPong` flips direction at ends; `Loop` checks triggers/nodes across wrap.

### `SplinePositioner`

Type: `SplineTracer`, `ExecuteInEditMode`, menu `Dreamteck/Splines/Users/Spline Positioner`.

Purpose: positions object at a percent or distance, optionally following another tracer target.

Defaults observed: mode enum `Percent`/`Distance`, serialized percent/distance/follow-target settings; overrides `SetPercent` and `SetDistance` so API respects mode.

Use for static placement or editor-time sampled object positioning. Cost is low unless updated continuously or projection target is used.

### `SplineProjector`

Type: `SplineTracer`, `ExecuteInEditMode`, menu `Dreamteck/Splines/Users/Spline Projector`.

Defaults: `mode = Accurate/Cached` enum, offset defaults `Vector2.zero`, rotation offset `Vector3.zero`, events `onEndReached`, `onBeginningReached`.

Purpose: project a target transform/point onto the spline and apply motion to the closest sample.

Performance: `Accurate` projection is more CPU-heavy; `Cached` uses sample collection and is better for runtime crowds.

### `SplineRenderer`

Type: `MeshGenerator`, menu `Dreamteck/Splines/Users/Spline Renderer`, requires `MeshFilter` and `MeshRenderer`.

Defaults: `autoOrient = true`, `updateFrameInterval = 0`, `slices = 1`.

Purpose: camera-facing or oriented ribbon/line mesh along a spline.

Performance: GPU/CPU cost scales with sample count and slices; can rebuild every camera render if `RenderWithCamera` is used.

### `MeshGenerator`

Type: `SplineUser` base for mesh components.

Defaults:

| Field/property | Default | Effect |
|---|---:|---|
| `baked` | `false` | Baked meshes stop rebuilding. |
| `markDynamic` | `true` | Calls `Mesh.MarkDynamic`. |
| `size` | `1` | Global mesh scale. |
| `color` | `Color.white` | Global vertex color. |
| `offset` | `Vector3.zero` | Local sample offset. |
| `normalMethod` | `SplineNormals` | Else recalculates normals. |
| `calculateTangents` | `true` | Tangent generation CPU cost. |
| `useSplineSize` | `true` | Multiplies sample size. |
| `useSplineColor` | `true` | Uses point/sample color. |
| `rotation` | `0` | Cross-section roll. |
| `flipFaces` | `false` | Reverses winding. |
| `doubleSided` | `false` | Doubles vertices/triangles. |
| `uvMode` | `Clip` | UV V mapping. |
| `uvScale` | `Vector2.one` | UV multiplier. |
| `uvOffset` | `Vector2.zero` | UV offset. |
| `uvRotation` | `0` | UV rotation. |
| `meshIndexFormat` | `UInt16` | 65535 vertex limit unless changed. |
| `colliderUpdateRate` | `0.2` | MeshCollider update throttle. |

Enums: `UVMode`: `Clip`, `UniformClip`, `Clamp`, `UniformClamp`; `NormalMethod`: `Recalculate`, `SplineNormals`.

Editor API: `Bake(makeStatic, lightmapUV)`, `Unbake`.

When not to use: high-frequency topology changes on low-end mobile without lowering sample count, sides, or rebuild frequency.

### Mesh Generator Derivatives

| Component | Defaults and purpose | Cost and notes |
|---|---|---|
| `TubeGenerator` | `sides = 12`, `roundCapLatitude = 6`, `capMode = None`, `revolve = 360`, `capUVScale = 1`, `uvTwist = 0`. Generates round/revolved tube. | Medium to high CPU/GPU; caps and high sides multiply vertices. |
| `SurfaceGenerator` | `expand = 0`, `extrude = 0`, side UV scale/offset/rotation, `extrudeFrom = 0`, `extrudeTo = 1`, `uniformUvs = false`. Generates filled/extruded surfaces. | Medium to high; triangulation/extrusion cost increases with samples. |
| `ComplexSurfaceGenerator` | `uvWrapMode = Clamp`, `subdivisions = 3`, `automaticNormals = true`, `separateMaterialIDs = false`, `otherComputers = []`. Multi-spline surface similar to NURBS. | High cost; depends on number of computers and subdivisions. |
| `SplineMesh` | Uses `SplineMeshChannel` definitions to extrude/place arbitrary meshes. | High potential cost; best baked for static tracks/roads. |
| `PathGenerator` | Generates path-like mesh strips. | Medium; width/sample count dependent. |
| `WaveformGenerator` | `axis = Y`, `symmetry = false`, `uvWrapMode = Clamp`, `slices = 1`. Turns sampled curve into waveform-like mesh. | Low to medium. |

### Collider Generators

| Component | Defaults | Purpose | Notes |
|---|---|---|---|
| `EdgeColliderGenerator` | `offset = 0`, `updateRate = 0.1` | Generates `EdgeCollider2D` points from spline. | CPU cost low; update throttled. |
| `PolygonColliderGenerator` | `Type`: `Path` or `Shape` | Generates 2D polygon/path collider. | Needs valid shape; more points cost more. |
| `BoxColliderGenerator` | `boxSize = Vector2.one`, `debugDraw = false`, `debugDrawColor = white`, colliders empty | Creates/updates box colliders along samples. | 3.0.6 feature; object count scales with sample/segment strategy. |
| `CapsuleColliderGenerator` | `radius = 1`, `height = 1`, `overlapCaps = true`, `direction = Z`, colliders empty | Creates/updates capsule colliders along samples. | 3.0.6 feature; overlap affects coverage and collider count. |

### Object Placement

`ObjectController`

Defaults: `objects = []`, `objectMethod = Instantiate`, `spawnMethod = Count`, `spawnCount = 0`, `retainPrefabInstancesInEditor = true`, `objectPositioning = Stretch`, `iteration = Ordered`, `randomSeed = 1`, min/max offset/rotation zero, min/max scale `Vector3.one`, `uniformScaleLerp = true`, `shellOffset = false`, `applyRotation = true`, `rotateByOffset = false`, `applyScale = false`, `evaluateOffset = 0`, `delayedSpawn = false`, `spawnDelay = 0.1`, custom rule refs null.

Enums: `SpawnMethod`: `Count`, `Points`; `ObjectMethod`: `Instantiate`, `GetChildren`; `Positioning`: `Stretch`, `Clip`; `Iteration`: `Ordered`, `Random`.

Important API: `Clear`, `GetAll`, `Spawn`.

Dependency: assigned prefab list or child objects, `SplineUser` sample cache, optional `ObjectControllerCustomRuleBase`.

Production guidance: use `GetChildren` or your own pooling for runtime-heavy placement. `Instantiate` is editor-friendly but allocates and causes object lifecycle churn.

### Custom Object Rules

`ObjectControllerCustomRuleBase` and derived classes under `ObjectController CustomRules/Classes`.

Purpose: calculate custom offset/rotation/scale for `ObjectController`.

Bundled assets: `Sine Rule.asset`, `Spiral Rule.asset`.

Use when random min/max ranges are insufficient. Do not allocate in rule evaluation for large object counts.

### Nodes

`Node`

Type: `MonoBehaviour`, `ExecuteInEditMode`, menu `Dreamteck/Splines/Node Connector`.

Defaults: `type = Smooth`, `transformSize = true`, `transformNormals = true`, `transformTangents = true`, connections empty.

`Node.Connection` exposes `spline`, `pointIndex`, `invertTangents = false`.

Important API: `GetPoint`, `SetPoint`, `ClearConnections`, `UpdateConnectedComputers`, `UpdatePoint`, `UpdatePoints`, `AddConnection`, `RemoveConnection`, `HasConnection`, `GetConnections`.

Side effects: moving node transform updates connected spline points. In `Smooth` mode, point data is propagated between connections.

### Triggers

`TriggerGroup`

Defaults: `enabled = true`, `name = ""`, `color = white`, `triggers = []`.

API: `Check`, `Reset`, `GetTriggers`, `AddTrigger`, `RemoveTrigger`.

`SplineTrigger`

Defaults: `name = "Trigger"`, `type = Double`, `workOnce = false`, `position = 0.5`, `enabled = true`, `color = white`, `onCross = TriggerEvent`.

Enums: `Type`: `Double`, `Forward`, `Backward`.

API: `AddListener(UnityAction<SplineUser>)`, `AddListener(UnityAction)`, `RemoveListener`, `RemoveAllListeners`, `Reset`, `Check`, `Invoke`.

Side effects: `Invoke` is disabled in editor non-play mode. `workOnce` requires `Reset` to fire again.

### `TransformModule`

Purpose: applies a `SplineSample` to `Transform`, `Rigidbody`, or `Rigidbody2D`.

Defaults: `offset = Vector2.zero`, `rotationOffset = Vector3.zero`, `baseScale = Vector3.one`, `is2D = false`, `velocityHandleMode = Zero`, apply position axes true, apply rotation axes true, apply scale axes false, retain local position/rotation false, `direction = Forward`.

Enum: `VelocityHandleMode`: `Zero`, `Preserve`, `Align`, `AlignRealistic`.

API: `ApplyTransform`, `ApplyRigidbody`, `ApplyRigidbody2D`.

Side effects: rigidbody velocity/angular velocity can be overwritten depending on velocity mode and applied axes. 3D velocity write is skipped when rigidbody is kinematic.

### Particles, Morphing, Length

| Component | Purpose | Defaults / API |
|---|---|---|
| `ParticleController` | Emits and/or moves particles along spline. | Enums: `EmitPoint`, `MotionType`, `Wrap`. Cost depends on particle count and per-particle sample evaluation. |
| `SplineMorph` | Stores point snapshots/channels and blends spline shape. | Enums: `CycleMode`, `UpdateMode`, channel `Interpolation`. API includes `SetCycle`, `SetWeight`, `CaptureSnapshot`, `Clear`, `GetSnapshot`, `SetSnapshot`, `AddChannel`, `RemoveChannel`, `UpdateMorph`. |
| `LengthCalculator` | Calculates length and invokes events around target lengths. | `lengthEvents = []`, `idealLength = 1`; `LengthEvent.enabled = true`, `targetLength = 0`, `type = Both`, `onChange = UnityEvent`. |

## Sample Modifiers

Base `SplineSampleModifier` defaults: `blend = 1`, `useClippedPercent = false`, no keys. Key ranges default to feather start/end `0`, center start `0.25`, center end `0.75`, `blend = 1`, custom interpolation curve.

| Modifier | Key defaults | Effect | Use |
|---|---|---|---|
| `OffsetModifier` | `offset = Vector2.zero` | Offsets sample along right/up. | Lane offsets, ribbons, formations. |
| `RotationModifier` | `useLookTarget = false`, `target = null`, `rotation = Vector3.zero` | Rotates sample orientation or looks at target. | Banking, aim-at behavior. |
| `ColorModifier` | `color = white`, `blendMode = Lerp` | Alters sample color. Modes: `Lerp`, `Multiply`, `Add`, `Subtract`. | Vertex color gradients. |
| `SizeModifier` | `size = 0` | Alters sample size. | Width/scale regions. |
| `FollowerSpeedModifier` | `speed = 0`, `mode = Add` | Alters follower speed. Modes: `Add`, `Multiply`. | Speed zones. |
| `MeshScaleModifier` | `scale = Vector3.one` | Alters mesh channel scale. | Mesh deformations along spline. |

Performance: modifiers are cheap per sample/key, but many keys across many users can become measurable. Use clipped percent carefully when clip ranges move.

## IO And Primitives

IO:

- `CSV`: columns `Position`, `Tangent`, `Tangent2`, `Normal`, `Size`, `Color`; can create spline/computer, flatten axes, write CSV.
- `SVG`: axes `X`, `Y`, `Z`; elements `All`, `Path`, `Polygon`, `Ellipse`, `Rectangle`, `Line`; creates splines/computers or writes SVG.
- `SplineParser`: shared parser base.

Primitives:

| Primitive | Defaults |
|---|---|
| `Capsule` | `radius = 1`, `height = 2` |
| `Ellipse` | `xRadius = 1`, `yRadius = 1` |
| `Line` | `mirror = true`, `length = 1`, `segments = 1` |
| `Ngon` | `radius = 1`, `sides = 3` |
| `Rectangle` | `size = Vector2.one` |
| `RoundedRectangle`, `Spiral`, `Star` | Editor primitives present; inspect their fields before custom tooling. |

## Editor Tools

| Tool/editor | Purpose |
|---|---|
| `SplineComputerEditor` and handles | Main inspector/scene editing for spline computers. |
| `SplineEditorWindow`, `SplineDrawer`, `DSSplineDrawer` | Shared editor UI/drawing. |
| `BakeTool`, `BakeMeshWindow` | Bake generated meshes and export OBJ. |
| `ImportTool` | Import CSV/SVG definitions. |
| `CatenaryTool` | Generate sagging cable curves. |
| `ObjectSpawnTool` | Editor placement workflow. |
| `LevelTerrainTool` | Terrain-leveling along splines. |
| `Explorer` | Spline exploration/editor helper. |
| Primitive editors | UI for capsule, ellipse, line, ngon, rectangle, rounded rectangle, spiral, star. |
| Welcome screen | Installs optional TextMeshPro and Playmaker integrations. |

## Utilities Reference

The `Dreamteck.Utilities` assembly is a runtime dependency of `Dreamteck.Splines`. These helpers are public and may be used by production code, but they are primarily package support utilities.

| Utility | Public API | Purpose / notes |
|---|---|---|
| `ArrayUtility` | extension helpers such as add/remove/shift methods | Array mutation used by spline internals; can allocate because arrays are resized. |
| `DMath` | `Sin`, `Cos`, `Tan`, `Pow`, `Log`, `Log10`, `Clamp01`, `Clamp`, `Lerp`, `InverseLerp`, `LerpVector3NonAlloc`, `LerpVector3`, `Round`, `RoundInt`, `Ceil`, `CeilInt`, `Floor`, `FloorInt`, `Move`, `Abs` | Double-precision math used by percent traversal. |
| `DuplicateUtility` | `DuplicateCurve`, `DuplicateGradient` | Deep-copy Unity curve/gradient data. |
| `LinearAlgebraUtility` | enum `Axis`; `ProjectOnLine`, `InverseLerp`, `DistanceOnSphere`, `FlattenVector` | Geometry helpers used by point flattening and import tools. |
| `MeshUtility` | `GeneratePlaneTriangles`, `CalculateTangents`, `MakeDoublesided`, `MakeDoublesidedHalf`, `TransformMesh`, `TransformVertices`, `TransformNormals`, `ToOBJString`, `Copy`, `Triangulate`, `FlipTriangles`, `FlipFaces`, `BreakMesh` | Mesh generation, export, and post-processing. High-cost methods should not run per frame unless profiled. |
| `Randomizer` | seeded random methods for floats, ints, vectors, sphere/circle points, `Reset` | Deterministic random generation. |
| `SceneUtility` | recursive child collection | Utility traversal; avoid in hot loops on large hierarchies. |
| `ScriptableObjectUtility` | `CreateAsset` | Editor/runtime guarded creation helper. |
| `TransformUtility` | matrix position/rotation/scale getters, child count, bounds merge, destroy children, parent check | Transform math and hierarchy helpers. |
| `TS_Bounds` | `CreateFromMinMax`, `Contains` | Serializable bounds wrapper. |
| `TS_Mesh` | `Clear`, `CreateFromMesh`, `Combine`, `AddMeshes`, `Copy`, `Absorb`, `WriteMesh` | Temporary mesh container used before writing Unity `Mesh`. |
| `TS_Transform` | `Update`, `SetTransform`, change checks, transform/inverse-transform point/direction | Thread-safe-ish transform snapshot wrapper. |
| `Utilities` | `SafeInvoke`, `HasCommandLineArgument` | Delegate and command line helpers. |
| `AsyncJobSystem` | `AsyncJobOperation`, `IJobData`, `JobData<T>` | Coroutine-like async job runner. |
| `Singleton<T>`, `PrivateSingleton<T>` | generic singleton MonoBehaviour base | Utility lifecycle pattern. |

Editor utility APIs include `DreamteckEditorGUI`, `EditorGUIEvents`, `FindDerivedClasses`, `ModuleInstaller`, `ScriptingDefineUtility`, `Toolbar`, and `WelcomeWindow`. They are editor-only and should not be referenced by runtime assemblies.

## Public API Inventory

Runtime public components and data types inspected:

```text
BlankUser
BoxColliderGenerator
CapsuleColliderGenerator
ComplexSurfaceGenerator
EdgeColliderGenerator
ISampleModifier
LengthCalculator
MeshGenerator
Node
ObjectBender
ObjectController
ObjectControllerCustomRuleBase
ObjectControllerSineRule
ObjectControllerSpiralRule
ParticleController
PathGenerator
PolygonColliderGenerator
SplineComputer
SplineFollower
SplineMesh
SplineMesh.Channel
SplineMeshChannel
SplineMorph
SplinePositioner
SplineProjector
SplineRenderer
SplineTracer
SplineUser
SurfaceGenerator
TubeGenerator
WaveformGenerator
Spline
SplinePoint
SplineSample
SampleCollection
ObjectSequence<T>
SplineTrigger
TriggerGroup
SplineThreading
SplineUtility
TransformModule
CSV
SVG
SplineParser
SplinePrimitive and primitive subclasses
```

Runtime public enums inspected:

```text
Spline.Direction
Spline.Type
SplinePoint.Type
SplineComputer.EditorUpdateMode
SplineComputer.Space
SplineComputer.EvaluateMode
SplineComputer.SampleMode
SplineComputer.UpdateMode
SplineUser.UpdateMethod
SplineTracer.PhysicsMode
SplineFollower.FollowMode
SplineFollower.Wrap
SplinePositioner.Mode
SplineProjector.Mode
MeshGenerator.UVMode
MeshGenerator.NormalMethod
TubeGenerator.CapMethod
WaveformGenerator.Axis
WaveformGenerator.Space
WaveformGenerator.UVWrapMode
ComplexSurfaceGenerator.UVWrapMode
ComplexSurfaceGenerator.SubdivisionMode
CapsuleColliderGenerator.CapsuleColliderZDirection
PolygonColliderGenerator.Type
ObjectController.SpawnMethod
ObjectController.ObjectMethod
ObjectController.Positioning
ObjectController.Iteration
ObjectBender.Axis
ObjectBender.NormalMode
ObjectBender.ForwardMode
ParticleController.EmitPoint
ParticleController.MotionType
ParticleController.Wrap
SplineMorph.CycleMode
SplineMorph.UpdateMode
SplineMorph.Channel.Interpolation
LengthCalculator.LengthEvent.Type
SplineTrigger.Type
TransformModule.VelocityHandleMode
SplineUtility.MergeSide
CSV.ColumnType
SVG.Axis
SVG.Element
ObjectSequence.Iteration
```

Editor public entry points inspected:

```text
SplineComputerEditor
SplineComputerEditorHandles
SplineEditor
SplineEditorBase
EditorModule
PointModule and point modules
SplineEditorGUI
SplineEditorHandles
SplineEditorWindow
SplineDrawer
DSSplineDrawer
BakeTool
CatenaryTool
Explorer
ImportExportTool
LevelTerrainTool
ObjectSpawnTool
SplineToolsWindow
UpdateTool
Component editors for every major runtime component
Primitive editors
Sample modifier editors
WelcomeScreen
```

## Shader And Material Reference

No `.shader`, `.shadergraph`, `.mat`, custom render queue, or shader keyword definitions were found inside the inspected `Assets/Plugins/Dreamteck` package. Mesh output uses assigned Unity materials. Visual impact is therefore driven by:

- Vertex color if the material reads vertex color.
- UVs generated by `MeshGenerator` and derivatives.
- Normals/tangents generated or recalculated.
- Submesh/material ID output from `SplineMeshChannel` and `ComplexSurfaceGenerator`.

Artifact cases:

- Wrong material pipeline: pink mesh in URP/HDRP.
- Tangents disabled on normal-mapped materials: broken normal maps.
- `flipFaces`/negative scale: invisible backfaces unless material is double-sided or `doubleSided = true`.
- UV mode mismatch: stretched or swimming textures.

Performance tier: shader cost is external; Dreamteck mesh generation cost is CPU/mesh upload, then normal material GPU cost.
