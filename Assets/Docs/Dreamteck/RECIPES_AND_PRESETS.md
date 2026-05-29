# Dreamteck Splines Recipes And Presets

These recipes are source-compatible setups for the inspected package. Values are starting points, not art direction.

## Mobile Optimized Follower Path

Goal: many simple followers on paths.

Required settings:

- `SplineComputer.sampleMode = Default`
- `SplineComputer.sampleRate = 6..10`
- `SplineComputer.multithreaded = false` for small counts, true only after profiling.
- `SplineFollower.followMode = Uniform`
- `SplineFollower.useTriggers = false` unless needed.

Recommended values:

- `SplineFollower.followSpeed`: gameplay value.
- `SplineUser.autoUpdate = true` only while spline changes; set false for static baked paths after setup.

Tradeoffs: less sampling accuracy on tight curves. Projection and raycast should not run every frame for crowds.

Expected result: stable low-CPU path following with no mesh rebuild cost.

## High Quality Tube/Rail

Goal: smooth visible tube mesh.

Required settings:

- Add `TubeGenerator` with `MeshFilter` and `MeshRenderer`.
- `SplineComputer.sampleRate = 16..32`
- `TubeGenerator.sides = 16..24`
- `TubeGenerator.capMode = Flat` or `Round` if ends are visible.
- `MeshGenerator.calculateTangents = true` for normal maps.
- `MeshGenerator.meshIndexFormat = UInt32` if vertex count can exceed 65535.

Tradeoffs: higher CPU mesh build and GPU vertex cost.

Expected result: smooth tube with correct UVs and normals.

## Low-End Tube

Goal: visible path mesh for low-end hardware.

Required settings:

- `sampleRate = 6..10`
- `TubeGenerator.sides = 6..8`
- `capMode = None`
- `calculateTangents = false` if material does not need normal maps.
- `doubleSided = false`
- `normalMethod = SplineNormals`

Tradeoffs: faceted silhouette and simpler lighting.

Expected result: cheap mesh with predictable vertex count.

## Stylized Ribbon

Goal: flat trail/road/stripe.

Required settings:

- Use `PathGenerator` or `SplineRenderer`.
- `MeshGenerator.useSplineColor = true`
- Add `ColorModifier` keys for vertex color if material supports vertex color.
- `MeshGenerator.uvMode = UniformClip` for distance-based texture tiling.

Tradeoffs: camera-facing `SplineRenderer` can change per camera; path mesh is more stable but less billboard-like.

Expected result: controlled strip with color/width variation from spline data.

## Road Or Track Mesh

Goal: place mesh segments along spline.

Required settings:

- Use `SplineMesh`.
- Configure one or more `SplineMeshChannel` entries.
- Keep `MeshGenerator.meshIndexFormat = UInt32` for long tracks.
- Bake once static: editor `Bake` with static/lightmap UVs if needed.

Recommended values:

- Use `UniformClip`/`UniformClamp` UV modes for consistent tiling.
- Avoid `doubleSided` unless the underside is visible.

Tradeoffs: flexible but can be high CPU during authoring; bake for runtime.

Expected result: repeated or extruded mesh following spline curvature.

## Object Scatter Along Path

Goal: place props along spline.

Required settings:

- Use `ObjectController`.
- `objectMethod = GetChildren` for production/pooling, `Instantiate` for editor setup.
- `spawnMethod = Count` for fixed count, `Points` for point-relative count.
- `iteration = Ordered` for predictable assets, `Random` for variation.
- Set `randomSeed` for repeatable results.

Recommended values:

- `applyRotation = true`
- `applyScale = false` unless spline size controls prop scale.
- `minObjectDistance`/`maxObjectDistance` for distance-based spacing when enabled.

Tradeoffs: instantiate mode allocates and changes hierarchy; child mode requires prepared pool.

Expected result: reusable prop distribution with deterministic randomization.

## Junction Network

Goal: connected spline graph.

Required settings:

- Create `Node` objects at junction points.
- Use `SplineComputer.ConnectNode(node, pointIndex)`.
- `Node.type = Smooth` for shared point shape; `Free` for independent tangents.
- Enable/disable `transformNormals`, `transformTangents`, `transformSize` depending on what should propagate.

Tradeoffs: node movement mutates connected splines. Use with clear ownership rules in level editing.

Expected result: moving node updates connected endpoints/points.

## Trigger Zones

Goal: fire events when follower crosses positions.

Required settings:

- Add trigger group to `SplineComputer`.
- Add `SplineTrigger` with `position` 0..1.
- Set `SplineTracer.useTriggers = true` and correct `triggerGroup`.
- Use `SplineFollower.Move` or `SetPercent(..., checkTriggers: true)`.

Recommended values:

- `type = Forward` for one-way gameplay events.
- `workOnce = true` for collectibles/checkpoints, then call `ResetTriggers` on restart.

Tradeoffs: trigger scanning costs scale with trigger count and active tracers.

Expected result: `onCross` receives the crossing `SplineUser`.

## Runtime Morph

Goal: animate one spline between shapes.

Required settings:

- Add `SplineMorph` to the spline object.
- Capture snapshots for each target shape.
- Drive channel weight or use cycle mode.
- Call `UpdateMorph` after programmatic weight changes.

Tradeoffs: all target snapshots need compatible point counts or expected interpolation behavior.

Expected result: `SplineComputer` points update and subscribers rebuild.
