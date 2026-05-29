# Dreamteck Splines Quick Start

## STEP 1

ACTION

Verify the package exists at `Assets/Plugins/Dreamteck/Splines` and `Assets/Plugins/Dreamteck/Utilities`. Let Unity compile `Dreamteck.Splines` and `Dreamteck.Utilities`.

EXPECTED RESULT

No compile errors. Add Component menu contains `Dreamteck/Splines/Spline Computer` and user components such as `Spline Follower`, `Tube Generator`, `Object Controller`.

## STEP 2

ACTION

Create an empty GameObject named `Path`. Add `SplineComputer`.

EXPECTED RESULT

The object now owns the spline data. Default important values are `space = Local`, `type = CatmullRom`, `sampleRate = 10`, `sampleMode = Default`, `updateMode = Update`, `multithreaded = false`.

## STEP 3

ACTION

In the `SplineComputer` inspector or scene handles, create at least two points. For a closed loop, use at least three points and call close from the editor/API.

EXPECTED RESULT

The spline draws in the Scene view. `pointCount > 0`, `sampleCount > 0` after rebuild.

## STEP 4

ACTION

Create a GameObject named `Follower`. Add `SplineFollower`. Assign the `Path` `SplineComputer` to `spline`.

EXPECTED RESULT

The follower subscribes to the computer. Defaults: `follow = true`, `followSpeed = 1`, `followMode = Uniform`, `wrapMode = Default`, `direction = Forward`.

## STEP 5

ACTION

Enter Play Mode.

EXPECTED RESULT

The follower moves along the spline in distance-uniform mode. It stops at the end unless `wrapMode` is changed to `Loop` or `PingPong`.

## STEP 6

ACTION

To render geometry, add `TubeGenerator` to a GameObject with `MeshFilter` and `MeshRenderer`, assign the same `SplineComputer`, and set a material compatible with the current render pipeline.

EXPECTED RESULT

A mesh tube is generated along the spline. Defaults include `sides = 12`, `capMode = None`, `revolve = 360`, `size = 1`, `useSplineSize = true`, `useSplineColor = true`.

## Minimal Working Example

```csharp
using UnityEngine;
using Dreamteck.Splines;

public class DreamteckMinimalPath : MonoBehaviour
{
    public SplineComputer computer;
    public SplineFollower follower;

    private void Awake()
    {
        if (computer == null) computer = gameObject.AddComponent<SplineComputer>();

        var points = new[]
        {
            new SplinePoint(new Vector3(0f, 0f, 0f)),
            new SplinePoint(new Vector3(0f, 0f, 5f)),
            new SplinePoint(new Vector3(3f, 0f, 8f))
        };

        computer.type = Spline.Type.CatmullRom;
        computer.sampleRate = 12;
        computer.SetPoints(points, SplineComputer.Space.World);
        computer.RebuildImmediate();

        if (follower != null)
        {
            follower.spline = computer;
            follower.follow = true;
            follower.followSpeed = 2f;
            follower.wrapMode = SplineFollower.Wrap.Loop;
        }
    }
}
```

## Common Mistakes

| Symptom | Likely mistake | Fix |
|---|---|---|
| Follower does not move | `spline` reference missing or `follow = false` | Assign `SplineComputer`; enable `follow`. |
| Object stays at origin | Spline has zero samples or no points | Add points and call `RebuildImmediate` after API changes. |
| Mesh invisible | No material or no generated samples | Assign a pipeline-compatible material; confirm `sampleCount > 1`. |
| Mesh vertex warning | Generated mesh exceeds 65535 vertices | Set `meshIndexFormat = UInt32` or lower sample/sides/count. |
| Runtime API point change appears one frame late | Computer queued rebuild | Call `ResampleTransform` before point changes if transform changed in same frame, then `RebuildImmediate`. |
| Trigger not firing | Tracer did not check triggers | Use `SplineFollower.Move`, or call `SetPercent(..., checkTriggers: true)`. |
