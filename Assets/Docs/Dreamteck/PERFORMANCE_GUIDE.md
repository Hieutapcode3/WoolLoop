# Dreamteck Splines Performance Guide

## Cost Model

Dreamteck Splines cost is mostly:

1. CPU sampling and cache rebuild in `SplineComputer`.
2. CPU user build in `SplineUser.Build`.
3. Main-thread object/mesh/collider writes in `PostBuild`.
4. Mesh upload and rendering through Unity.
5. Physics calls for raycast/collider/rigidbody systems.

The package is classic MonoBehaviour/GameObject code. It is not ECS/DOTS or Unity Job System based. Internal `SplineThreading` can run calculations on a worker thread, but Unity object access must remain on the main thread.

## Low Cost

| System | Why cheap | Notes |
|---|---|---|
| `SplineComputer` with static points | Cache reused | Rebuild only on point/transform changes. |
| `SplineUser.Evaluate` on cached samples | Uses sample collection | Use `ref SplineSample` overloads. |
| `SplineFollower` transform mode | Applies transform once per frame | Disable triggers/nodes if not needed. |
| `LengthCalculator` on simple splines | Length over sample cache | Event checks are cheap. |
| `EdgeColliderGenerator` with low samples | Simple 2D point list | Update is throttled. |

Recommended for mobile: followers, low-rate spline computers, baked meshes, child-pool object placement.

## Medium Cost

| System | Cost source | Tuning |
|---|---|---|
| `SplineComputer.sampleMode = Uniform` | Length-based resampling | Use only where uniform spacing is visible/needed. |
| `SplineProjector` cached mode | Closest sample search | Reduce frequency for many objects. |
| `TubeGenerator` low/medium sides | Mesh build and upload | Lower `sides`, `sampleRate`, disable caps/tangents. |
| `ObjectController.GetChildren` | Per-object transform writes | Keep count stable and pooled. |
| `ParticleController` | Per-particle sample work | Limit particles and update mode. |
| Triggers/nodes | Range scans | Keep groups small. |

## High Cost

| System | Cost source | Mitigation |
|---|---|---|
| `SplineComputer.Project` accurate calls | Recursive closest-point search | Cache result, use less often, use `SplineProjector.Mode.Cached`. |
| `Raycast`/`RaycastAll` along spline | Many physics queries | Lower resolution, restrict layer mask, avoid per-frame broad use. |
| `SplineMesh` | Arbitrary mesh deformation, submeshes | Bake static output; reduce channels and source mesh vertices. |
| `ComplexSurfaceGenerator` | Multiple splines times subdivisions | Keep subdivisions low; bake. |
| `SurfaceGenerator` extrusion | Triangulation/extrusion and wall mesh | Avoid runtime rebuilds. |
| `ObjectController.Instantiate` | Object allocation/lifecycle | Pool or use `GetChildren`. |
| `doubleSided` mesh generation | Doubles vertices/triangles | Prefer double-sided shader/material if cheaper on target. |
| MeshCollider updates | Physics mesh recook | Raise `colliderUpdateRate` or use simple colliders. |

## CPU Cost

CPU increases with:

- `sampleRate`
- control point count
- `sampleMode = Uniform` or `Optimized`
- active `SplineUser` subscriber count
- mesh vertex/triangle count
- collider count/update frequency
- projection/raycast frequency
- trigger/node checks

Rules:

- Static spline mesh: bake it.
- Runtime visual mesh: minimize topology changes.
- Runtime followers: prefer cached travel/evaluate over projection.
- Many moving objects: avoid per-object `Project` every frame.

## GPU Cost

Dreamteck does not define custom shaders in the inspected package. GPU cost comes from generated mesh size and material choice.

GPU increases with:

- high `sampleRate`
- high tube sides/caps
- `doubleSided = true`
- multiple submeshes/materials
- expensive user material/shader

For mobile:

- Tube sides `6..8`
- No round caps unless visible
- Avoid double-sided geometry
- Disable tangents if material does not use normal maps
- Bake static meshes and combine where appropriate

## Memory Impact

Memory comes from:

- `SplinePoint[]`, `SplineSample[]`, optimized index arrays.
- Generated `TS_Mesh` arrays and Unity `Mesh`.
- Mesh collider cooked data.
- ObjectController spawned object references and instantiated GameObjects.
- Morph snapshots storing arrays of points.

Allocation risks:

- Array-returning APIs and array resizing.
- `ObjectController` spawn/remove.
- New `SplineSample` from allocating overloads.
- Mesh refresh/recreation.

Use reusable arrays and `ref` overloads in hot paths.

## Batching And Instancing

Generated meshes are normal Unity meshes. Batching/instancing depends on:

- assigned material
- static/dynamic batching settings
- mesh uniqueness
- renderer flags

Dynamic mesh generators create unique meshes, so GPU instancing is generally not useful for the generated mesh itself. Static baked outputs can participate in static batching when marked static.

`ObjectController` spawned prefabs can use instancing if their renderers/materials support it.

## Threading

`SplineComputer.multithreaded` and `SplineUser.multithreaded` use internal threading. Calculation code can run off-main-thread; Unity API mutation must happen in `PostBuild`.

Safe custom user pattern:

- `Build`: calculate arrays, numbers, pure math.
- `PostBuild`: assign meshes, transforms, colliders, materials, events.

Unsafe:

- Calling `GetComponent`, `Instantiate`, `Destroy`, `Mesh` writes, `Transform` mutation inside threaded `Build`.

## Scalability Strategy

Low tier:

- `sampleRate <= 8`
- followers only or baked mesh
- no runtime projection/raycast
- no mesh colliders
- no round caps/double-sided

Medium tier:

- `sampleRate 10..16`
- some mesh generators
- cached projection at reduced frequency
- pooled object placement
- simple colliders

High tier:

- `sampleRate 24+`
- `SplineMesh`, `ComplexSurfaceGenerator`, extrusion, high tube sides
- runtime mesh/collider rebuilds
- many triggers/nodes
- accurate projection/raycast

Use high tier for editor tools, cinematic paths, or static baked content, not for uncontrolled runtime rebuilds on low-end devices.
