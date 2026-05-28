# Dreamteck Splines Version Notes

## Tested Package

| Item | Value |
|---|---|
| Package path | `Assets/Plugins/Dreamteck` |
| Product | Dreamteck Splines |
| Tested package version | `3.0.6` from changelog |
| Unity project version | `6000.0.70f1` |
| Render pipeline in project | URP package `17.0.4`; Dreamteck package itself is pipeline-neutral |
| Documentation generated | Source inspection of runtime/editor files present in project |

## Version 3.0.6 Highlights

Source: `Assets/Plugins/Dreamteck/Splines/Editor/changelog.txt`.

Support:

- Official support for Unity 2023 and Unity 6000.

Features:

- Added `BoxColliderGenerator`.
- Added `CapsuleColliderGenerator`.
- Added `ComplexSurfaceGenerator`.
- Better readability for spline points.

API behavior:

- `SplineUser.Evaluate` returns samples modified by sample modifiers.
- `SplineUser.Project` operates on modified samples.

Fixes relevant to integration:

- Unity 2023/6 drag selection and editor handle fixes.
- Object Bender editor event leak fixed.
- `SplineTracer` transform module updated for Unity 2022+ physics; rigidbody velocity assignment happens only if rigidbody is not kinematic.
- Object Controller prefab serialization fixes from community contribution.
- Spline Mesh UV generation fix from community contribution.
- More accurate moved value in Spline User API from community contribution.

## Recent Migration Notes From 3.0.x Changelog

From 3.0.5:

- Fixed empty sample array error in `SplineUser`.
- Fixed `SplineFollower` speed/direction inconsistencies.
- Fixed PingPong looping behavior.
- Fixed `SplineFollower` and `SplinePositioner` `onNodes` event not triggering from `SetPercent` or `SetDistance`.
- Fixed incorrect `SplinePositioner.SetPercent`/`SetDistance` behavior when mode did not match.
- Fixed generated mesh bake edge cases.

From 3.0.4 and 3.0.3:

- Trigger positions preserved when spline points are added/removed in editor.
- Optimized sample mode fixes.
- `SplineRenderer` null reference fixes.
- Optimized Linear mode with two points fixed.

From 3.0.0:

- TextMeshPro support added through optional installer.
- CatmullRom parametrization added.
- Catenary tool added.
- Follower supports negative speed.
- Sample modifiers can use clipped percents.
- Surface Generator side UV rotation added.
- Spline Positioner follow target feature added.
- Runtime/editor performance improvements.
- Serialization and memory footprint improvements.

Breaking/API changes noted:

- `SplineUser.Evaluate` should be called with `ref` keyword for ref overloads.
- Trigger wrapper methods added to `SplineComputer`.
- `SplineTrigger.RemoveListener` and `RemoveAllListeners` added.
- `SplineTrigger.onUserCross` removed; `onCrossEvent`/`onCross` passes `SplineUser`.
- `SplineComputer.rebuildOnAwake` removed; runtime instantiated splines are handled automatically.
- Closing a spline bridges the first/last point instead of moving both points together.

## Deprecated Systems

The source includes obsolete overloads in `Spline`:

- `Evaluate(ref SplineSample sample, int pointIndex)` -> use `Evaluate(int pointIndex, ref SplineSample sample)`.
- `Evaluate(ref SplineSample sample, double percent)` -> use `Evaluate(double percent, ref SplineSample sample)`.
- `EvaluatePosition(ref Vector3 position, double percent)` -> use `EvaluatePosition(double percent, ref Vector3 position)`.

## Compatibility Notes

- Current project uses Unity 6000, which package version `3.0.6` explicitly supports.
- Package assembly definitions are auto-referenced and do not constrain platforms.
- Optional TextMeshPro integration requires TextMeshPro package/assembly availability and define `DREAMTECK_SPLINES_TMPRO`.
- Optional Playmaker actions require Playmaker.
- Render pipeline compatibility is material-driven; no bundled Dreamteck shader stack was found.

## Tracking Checklist For Future Updates

When upgrading Dreamteck:

1. Diff `Editor/changelog.txt`.
2. Check `Dreamteck.Splines.asmdef` references and optional integrations.
3. Search for new/removed public APIs in `Components` and `Core`.
4. Re-test `SplineComputer` point mutation, `SplineFollower`, mesh generators, collider generators, triggers, and ObjectController prefab behavior.
5. Re-run performance checks for dynamic mesh rebuilds and projection.
6. Update this docs folder with changed defaults and migration notes.
