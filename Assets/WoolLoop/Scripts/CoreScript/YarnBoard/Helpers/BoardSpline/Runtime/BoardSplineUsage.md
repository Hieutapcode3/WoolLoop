# Board Spline Runtime

Copy this folder to another Unity project that has Dreamteck Splines installed.

Use `BoardSplineVisual` on a GameObject with `SplineComputer`, then call:

```csharp
var adapter = new BoardSplineDataAdapterInfo
{
    size = new Vector2Int(width, height),
    tileData = tiles,
    cellSize = 1f,
    origin = Vector3.zero,
    right = Vector3.right,
    forward = Vector3.forward,
    centerBoardOnOrigin = true,
};

boardSplineVisual.Apply(adapter);
```

`tileData` is row-major: `tileData[y * width + x]`.

For custom board data, implement `IBoardSplineDataAdapter` and pass it to `Apply`.
