# Dreamteck Splines Package Overview

Source inspected: `Assets/Plugins/Dreamteck/Splines`, `Assets/Plugins/Dreamteck/Utilities`, asmdefs, changelog, URL/readme, runtime/editor source, presets, icons, and packaged examples file.

## Package Summary

Dreamteck Splines is a runtime/editor spline framework for Unity. It provides spline authoring, cached sampling, spline-following motion, mesh generation, collider generation, object distribution, node junctions, triggers, import/export helpers, and editor tools for creating and maintaining spline assets.

This project contains Dreamteck Splines `3.0.6` according to `Assets/Plugins/Dreamteck/Splines/Editor/changelog.txt`. The active Unity editor version in `ProjectSettings/ProjectVersion.txt` is `6000.0.70f1`. The changelog states official support for Unity 2023 and Unity 6000 in version `3.0.6`.

## Render Pipeline Compatibility

The inspected package is render-pipeline neutral. It does not ship custom shader files or render pipeline assets inside `Assets/Plugins/Dreamteck`. Mesh components write standard Unity `Mesh`, `MeshFilter`, `MeshRenderer`, `MeshCollider`, `EdgeCollider2D`, `PolygonCollider2D`, `BoxCollider`, and `CapsuleCollider` data.

Pipeline notes:

| Pipeline | Status | Notes |
|---|---:|---|
| Built-in | Supported by standard mesh/material flow | Reset assigns Unity built-in `Default-Diffuse.mat` when no material exists. |
| URP | Supported | Current project has `com.unity.render-pipelines.universal` `17.0.4`; Dreamteck itself does not require URP APIs. |
| HDRP | Expected compatible for mesh output | Materials must be HDRP-compatible, but package source is not HDRP-specific. |

No Dreamteck shader properties, shader keywords, or custom render queues were found in the inspected package folder. Material behavior is defined by user-assigned materials or imported examples.

## Dependencies

Assembly definitions:

| Assembly | Depends on | Auto referenced |
|---|---|---:|
| `Dreamteck.Utilities` | none | true |
| `Dreamteck.Splines` | `Dreamteck.Utilities` | true |
| `Dreamteck.Utilities.Editor` | editor-only utility code | true |
| `Dreamteck.Splines.Editor` | editor-only spline tooling | true |

Runtime Unity module/API dependencies observed:

- `UnityEngine`
- Physics: `Rigidbody`, `Rigidbody2D`, `MeshCollider`, `BoxCollider`, `CapsuleCollider`, `EdgeCollider2D`, `PolygonCollider2D`, raycast APIs.
- Particle System: used by `ParticleController`.
- Mesh APIs: `Mesh`, `MeshFilter`, `MeshRenderer`, `IndexFormat`, tangent/normal utilities.
- `UnityEvent`: used by spline triggers and length events.

Optional integrations exposed through editor welcome installer:

- TextMeshPro support via define `DREAMTECK_SPLINES_TMPRO` and assembly link to `Unity.TextMeshPro`.
- Playmaker actions installer.

## Folder Structure

| Path | Purpose |
|---|---|
| `Splines/Core` | Serializable spline data, sampling, triggers, transform module, threading wrapper, IO, primitives, utility merge API. |
| `Splines/Components` | Runtime MonoBehaviours: `SplineComputer`, `SplineUser` derivatives, generators, followers, controllers, nodes. |
| `Splines/Components/Sample Modifiers` | Offset/rotation/color/size/speed/mesh-scale modifier systems. |
| `Splines/Components/ObjectController CustomRules` | Scriptable custom rule assets and runtime rule base/classes. |
| `Splines/Editor` | Inspectors, spline editor UI, scene handles, tools, bake/import windows, welcome screen, changelog. |
| `Splines/Presets` | `.jsp` preset data such as `heart.jsp`. |
| `Splines/Examples.unitypackage` | Packaged examples not unpacked in this project. |
| `Utilities` | Shared array/math/mesh/transform/singleton/async/editor utilities used by Splines. |

## Runtime And Editor Separation

Runtime code lives outside `Editor` folders and is compiled into `Dreamteck.Splines` and `Dreamteck.Utilities`.

Editor code lives under `Splines/Editor` and `Utilities/Editor`. It provides custom inspectors, handles, primitive editors, tools, module installers, bake/export helpers, and welcome UI. Editor code references `UnityEditor` and should not be used in player builds.

Important runtime entry points:

- `SplineComputer`: authoritative MonoBehaviour wrapper around `Spline`; owns points, sample cache, transform-space conversion, nodes, triggers, subscribers.
- `SplineUser`: base class for consumers; clips samples, applies sample modifiers, schedules rebuilds, supports multithreaded build calculations.
- `SplineTracer`: base class for transform motion along a spline.
- `MeshGenerator`: base class for generated mesh components.
- `SplineTrigger` and `TriggerGroup`: event crossing system.
- `Node`: shared junction/connection point between spline computers.

Important editor entry points:

- `SplineComputerEditor`, `SplineComputerEditorHandles`
- `SplineDrawer`, `DSSplineDrawer`, `SplineEditorWindow`
- `SplineToolsWindow` plus `BakeTool`, `ImportTool`, `CatenaryTool`, `ObjectSpawnTool`, `LevelTerrainTool`, `UpdateTool`
- Component editors such as `SplineFollowerEditor`, `SplineMeshEditor`, `MeshGenEditor`, `NodeEditor`

## Important Systems

| System | Core classes | Runtime role |
|---|---|---|
| Spline data and sampling | `Spline`, `SplinePoint`, `SplineSample`, `SampleCollection` | Converts control points into samples; supports CatmullRom, BSpline, Bezier, Linear. |
| Spline computer cache | `SplineComputer` | Caches raw/transformed samples, handles update mode, transform changes, subscriber rebuilds. |
| Users and modifiers | `SplineUser`, `ISampleModifier`, modifier classes | Consumes clipped/modified samples; rebuilds dependent output. |
| Motion | `SplineTracer`, `SplineFollower`, `SplinePositioner`, `SplineProjector`, `TransformModule` | Applies spline sample position/rotation/scale to transforms or rigidbodies. |
| Mesh generation | `MeshGenerator`, `TubeGenerator`, `SurfaceGenerator`, `SplineMesh`, `SplineRenderer`, `PathGenerator`, `WaveformGenerator`, `ComplexSurfaceGenerator` | Creates runtime mesh data from sampled splines. |
| Collider generation | `EdgeColliderGenerator`, `PolygonColliderGenerator`, `BoxColliderGenerator`, `CapsuleColliderGenerator`, mesh collider via `MeshGenerator` | Generates 2D/3D collider geometry along splines. |
| Object placement | `ObjectController`, custom rule classes | Instantiates/reuses children and positions them along splines. |
| Particles | `ParticleController` | Spawns/moves particles using spline samples. |
| Morphing | `SplineMorph` | Blends spline point snapshots over time or by weight. |
| Junctions | `Node`, `SplineComputer.NodeLink` | Keeps multiple spline endpoints/points connected. |
| IO | `CSV`, `SVG`, `SplineParser` | Imports/exports spline definitions. |

## Initialization Flow

1. A scene object owns `SplineComputer`.
2. `SplineComputer.Awake` samples its transform matrix and prepares caches.
3. `SplineComputer` runs according to `updateMode`: `Update`, `FixedUpdate`, `LateUpdate`, `AllUpdate`, or `None`.
4. On point/type/sample settings changes, it queues resampling and rebuild.
5. `SplineUser` derivatives subscribe to the computer and call `GetSamples`.
6. `SplineUser` applies clip range and sample modifiers before component-specific `Build`.
7. `Build` may run on a worker thread when `multithreaded` is true; `PostBuild` is main-thread only.
8. Mesh/collider/transform outputs are written after build.

## Compatibility

- Tested package version in this project: `3.0.6`.
- Unity project version: `6000.0.70f1`.
- Changelog support: Unity 2023 and Unity 6000.
- Uses classic GameObject/MonoBehaviour workflow, not DOTS/ECS.
- Multithreading is internal thread dispatch for calculations; Unity object writes remain main-thread only.
- Works with project materials, not a bundled shader stack.

## Known Limitations

- No custom shaders/material assets were found in `Assets/Plugins/Dreamteck`; shader-specific documentation is not applicable to the inspected package state.
- `Examples.unitypackage` is present but not unpacked, so demo scenes/materials/prefabs inside it were not inspected as project assets.
- Heavy mesh generation can exceed Unity 16-bit index limit unless `meshIndexFormat` is set to `UInt32`.
- Runtime object spawning can allocate and instantiate; prefer pooling or `GetChildren` for production use.
- `SplineUser.multithreaded` and `SplineComputer.multithreaded` can move calculations off the main thread, but custom `Build` overrides must avoid Unity API calls outside `PostBuild`.
- Physics application behavior depends on `Rigidbody`/`Rigidbody2D` mode; velocity assignment is skipped for kinematic 3D rigidbodies in newer Unity versions.
