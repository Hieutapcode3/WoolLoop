# Dreamteck Splines API Usage Examples

## Minimal Spline Creation

```csharp
using UnityEngine;
using Dreamteck.Splines;

public class CreateSplineAtRuntime : MonoBehaviour
{
    private SplineComputer computer;

    private void Awake()
    {
        computer = gameObject.AddComponent<SplineComputer>();
        computer.space = SplineComputer.Space.World;
        computer.type = Spline.Type.CatmullRom;
        computer.sampleRate = 12;

        var points = new[]
        {
            new SplinePoint(new Vector3(0f, 0f, 0f)),
            new SplinePoint(new Vector3(2f, 0f, 3f)),
            new SplinePoint(new Vector3(6f, 0f, 4f))
        };

        computer.SetPoints(points);
        computer.RebuildImmediate();
    }
}
```

## GC-Safe Sample Evaluation

```csharp
using UnityEngine;
using Dreamteck.Splines;

public class SampleReader : MonoBehaviour
{
    public SplineComputer computer;
    private SplineSample sample;

    private void Update()
    {
        if (computer == null || computer.sampleCount == 0) return;

        double percent = Mathf.PingPong(Time.time * 0.1f, 1f);
        computer.Evaluate(percent, ref sample, SplineComputer.EvaluateMode.Cached);

        transform.position = sample.position;
        transform.rotation = sample.rotation;
    }
}
```

## Follower Setup From Code

```csharp
using UnityEngine;
using Dreamteck.Splines;

public class ConfigureFollower : MonoBehaviour
{
    public SplineComputer path;
    public SplineFollower follower;

    private void Awake()
    {
        follower.spline = path;
        follower.followMode = SplineFollower.FollowMode.Uniform;
        follower.wrapMode = SplineFollower.Wrap.Loop;
        follower.followSpeed = 3f;
        follower.follow = true;
        follower.motion.applyPosition = true;
        follower.motion.applyRotation = true;
    }
}
```

## Runtime Point Modification

```csharp
using UnityEngine;
using Dreamteck.Splines;

public class MoveSplinePoint : MonoBehaviour
{
    public SplineComputer computer;
    public int pointIndex = 1;
    public Transform target;

    private void LateUpdate()
    {
        if (computer == null || target == null) return;
        if (pointIndex < 0 || pointIndex >= computer.pointCount) return;

        computer.ResampleTransform();
        computer.SetPointPosition(pointIndex, target.position, SplineComputer.Space.World);
        computer.RebuildImmediate();
    }
}
```

## Trigger Listener

```csharp
using UnityEngine;
using Dreamteck.Splines;

public class AddSplineTrigger : MonoBehaviour
{
    public SplineComputer computer;
    public SplineFollower follower;

    private void Awake()
    {
        var trigger = computer.AddTrigger(0, 0.5, SplineTrigger.Type.Forward, "Midpoint", Color.yellow);
        trigger.workOnce = true;
        trigger.AddListener(OnCrossed);

        follower.useTriggers = true;
        follower.triggerGroup = 0;
    }

    private void OnCrossed(SplineUser user)
    {
        Debug.Log("Crossed midpoint: " + user.name);
    }
}
```

## Custom SplineUser

```csharp
using UnityEngine;
using Dreamteck.Splines;

public class SpawnMarkersUser : SplineUser
{
    public Transform[] markers;
    private SplineSample workSample;
    private Vector3[] positions;

    protected override void Build()
    {
        if (markers == null) return;
        if (positions == null || positions.Length != markers.Length)
        {
            positions = new Vector3[markers.Length];
        }

        for (int i = 0; i < markers.Length; i++)
        {
            double percent = markers.Length <= 1 ? 0.0 : (double)i / (markers.Length - 1);
            Evaluate(percent, ref workSample);
            positions[i] = workSample.position;
        }
    }

    protected override void PostBuild()
    {
        if (markers == null || positions == null) return;

        for (int i = 0; i < markers.Length; i++)
        {
            if (markers[i] != null) markers[i].position = positions[i];
        }
    }
}
```

## Mesh Generator Configuration

```csharp
using UnityEngine;
using Dreamteck.Splines;

public class ConfigureTube : MonoBehaviour
{
    public SplineComputer path;
    public Material material;

    private void Awake()
    {
        var tube = gameObject.AddComponent<TubeGenerator>();
        gameObject.AddComponent<MeshFilter>();
        var renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        tube.spline = path;
        tube.sides = 8;
        tube.capMode = TubeGenerator.CapMethod.None;
        tube.calculateTangents = false;
        tube.meshIndexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        tube.RebuildImmediate();
    }
}
```

## Node Connection

```csharp
using UnityEngine;
using Dreamteck.Splines;

public class ConnectSplineNode : MonoBehaviour
{
    public SplineComputer a;
    public SplineComputer b;

    private void Awake()
    {
        var nodeObject = new GameObject("Spline Node");
        var node = nodeObject.AddComponent<Node>();
        node.type = Node.Type.Smooth;

        nodeObject.transform.position = a.GetPointPosition(a.pointCount - 1);

        a.ConnectNode(node, a.pointCount - 1);
        b.ConnectNode(node, 0);
    }
}
```

## Production Notes

- Prefer `Evaluate(percent, ref sample)` over `Evaluate(percent)` in hot loops.
- Prefer `ObjectController.GetChildren` with pre-created children for runtime placement.
- Use `RebuildImmediate` for deterministic setup, but avoid calling it every frame on heavy mesh generators.
- Bake static `MeshGenerator` outputs.
- Keep Unity API writes in `PostBuild` when extending `SplineUser`.
