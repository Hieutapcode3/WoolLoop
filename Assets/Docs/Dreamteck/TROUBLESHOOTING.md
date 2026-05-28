# Dreamteck Splines Troubleshooting

## SYMPTOM
Spline-dependent object does not update.

CAUSE
`SplineUser.spline` is null, `autoUpdate = false`, component is baked, or `SplineComputer.updateMode = None`.

DEBUG METHOD
Check `spline`, `sampleCount`, `subscriberCount`, and whether the component is a `MeshGenerator` with `baked = true`.

FIX
Assign the `SplineComputer`, enable `autoUpdate`, call `RebuildImmediate`, or unbake mesh generators.

PREVENTION
For runtime API changes, call `computer.RebuildImmediate()` after mutating points when an immediate result is required.

## SYMPTOM
Follower moves at uneven visual speed.

CAUSE
Spline sample density is too low, `followMode = Time`, offset is applied without `preserveUniformSpeedWithOffset`, or curve is tight.

DEBUG METHOD
Inspect `sampleRate`, `sampleMode`, `followMode`, motion offset, and speed modifiers.

FIX
Use `followMode = Uniform`, increase `sampleRate`, use `sampleMode = Uniform`, or enable `preserveUniformSpeedWithOffset` when motion offset matters.

PREVENTION
Profile representative splines and standardize sample-rate tiers.

## SYMPTOM
Trigger does not fire.

CAUSE
`useTriggers = false`, wrong `triggerGroup`, trigger disabled, `workOnce` already consumed, or API call did not check triggers.

DEBUG METHOD
Check `SplineComputer.triggerGroups`, trigger `position`, `enabled`, `type`, and tracer call path.

FIX
Enable `useTriggers`, select correct group, call `ResetTriggers`, or call `SetPercent(percent, checkTriggers: true)`.

PREVENTION
Use `SplineFollower.Move` for motion that should invoke triggers.

## SYMPTOM
Generated mesh is invisible.

CAUSE
No samples, no material, wrong material pipeline, flipped winding, zero size, or backface culling.

DEBUG METHOD
Check `sampleCount > 1`, `MeshFilter.sharedMesh.vertexCount`, material assignment, `flipFaces`, `doubleSided`, and spline point sizes.

FIX
Assign compatible material, rebuild spline, disable `flipFaces`, enable `doubleSided` only if needed, ensure size is non-zero.

PREVENTION
Use a validation prefab with known material and point values.

## SYMPTOM
Pink mesh in URP/HDRP.

CAUSE
Assigned material shader is not compatible with the active render pipeline.

DEBUG METHOD
Inspect `MeshRenderer.sharedMaterials`.

FIX
Use URP/HDRP-compatible material. Dreamteck does not ship pipeline-specific shaders in the inspected package.

PREVENTION
Keep pipeline-specific materials outside the package and assign them in prefabs.

## SYMPTOM
SRP issue or shader keyword explosion after adding spline meshes.

CAUSE
The inspected Dreamteck package does not define custom shaders or keywords. Keyword growth comes from assigned project materials, imported example materials, Shader Graph variants, or URP/HDRP material features on the generated `MeshRenderer`.

DEBUG METHOD
Inspect materials on `MeshRenderer.sharedMaterials`, shader variant logs, URP/HDRP asset settings, and material keywords. Confirm there are no `.shader` or `.mat` assets under `Assets/Plugins/Dreamteck` in the current project state.

FIX
Use simpler pipeline-compatible materials for generated meshes, reduce material feature variants, share materials across generated objects, and avoid per-object unique material instances.

PREVENTION
Treat Dreamteck mesh generators as geometry producers. Manage shader keywords in the project material library, not in the spline package.

## SYMPTOM
Warning about vertex count limit.

CAUSE
Generated mesh exceeds 65535 vertices while `meshIndexFormat = UInt16`.

DEBUG METHOD
Check sample count, tube sides, caps, double-sided, spline mesh channel count.

FIX
Set `meshIndexFormat = UnityEngine.Rendering.IndexFormat.UInt32`, reduce sample rate/sides, or split mesh.

PREVENTION
Use vertex budget rules per platform.

## SYMPTOM
Performance spikes during play.

CAUSE
Mesh rebuilds, runtime instantiation, projection/raycast per frame, collider updates, or high sample counts.

DEBUG METHOD
Profile `Build`, `PostBuild`, mesh upload, `ObjectController.Spawn`, `SplineComputer.Project`, physics calls.

FIX
Bake static generators, pool objects, throttle projection, lower sample rate, disable tangents/colliders when unused.

PREVENTION
Use cost tiers from `PERFORMANCE_GUIDE.md`.

## SYMPTOM
GC allocations around object distribution.

CAUSE
`ObjectController` instantiate/remove, array resizing, coroutine delayed spawn, or API overloads returning new arrays/samples.

DEBUG METHOD
Profiler GC Alloc column; inspect spawn count changes and use of allocating `Evaluate` overloads.

FIX
Use `GetChildren` with preallocated children, reuse arrays and `SplineSample` refs, avoid per-frame spawn count changes.

PREVENTION
Pool runtime objects and use ref APIs in hot loops.

## SYMPTOM
Generated collider lags behind mesh.

CAUSE
Collider update is throttled by `colliderUpdateRate` or generator-specific `updateRate`.

DEBUG METHOD
Check `MeshGenerator.colliderUpdateRate`, `EdgeColliderGenerator.updateRate`, and current rebuild frequency.

FIX
Lower update rate for responsive colliders or call update manually after final rebuild.

PREVENTION
Separate visual mesh and physics update budgets.

## SYMPTOM
Node-connected splines change unexpectedly.

CAUSE
`Node.type = Smooth` propagates point data; `transformNormals`, `transformTangents`, or `transformSize` are enabled.

DEBUG METHOD
Inspect `Node.GetConnections()` and node transform flags.

FIX
Use `Node.type = Free` or disable propagation flags.

PREVENTION
Document ownership of shared junctions for level designers.

## SYMPTOM
Rigidbody follower fights physics.

CAUSE
`TransformModule` applies position/rotation and may rewrite velocity/angular velocity based on axes and `velocityHandleMode`.

DEBUG METHOD
Check `SplineTracer.physicsMode`, rigidbody kinematic state, apply axes, and velocity mode.

FIX
Use `Rigidbody`/`Rigidbody2D` mode intentionally, set velocity handling to `Preserve` or `Align` as needed, or use transform mode for non-physical followers.

PREVENTION
Do not mix external physics forces with full spline transform authority unless designed.

## SYMPTOM
Build compile errors after optional integrations.

CAUSE
TextMeshPro/Playmaker support installed without required package/assembly or define mismatch.

DEBUG METHOD
Check `DREAMTECK_SPLINES_TMPRO`, asmdef references, and package availability.

FIX
Install required package or uninstall integration from Dreamteck welcome screen.

PREVENTION
Keep optional integrations version-controlled and documented.

## SYMPTOM
Mobile frame drops.

CAUSE
High mesh vertex counts, frequent dynamic mesh upload, particle/path projection, or too many colliders.

DEBUG METHOD
Profile CPU mesh build, render thread, physics, GPU vertices.

FIX
Reduce sample rate, tube sides, caps, double-sided meshes, collider frequency, and projection frequency. Bake static meshes.

PREVENTION
Use mobile recipes and platform-specific prefabs.
