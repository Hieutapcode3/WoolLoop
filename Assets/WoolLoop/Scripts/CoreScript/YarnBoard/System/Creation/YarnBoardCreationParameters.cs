using BoardSpline.Runtime;
using Common;
using UnityEngine;

public sealed class BottomBoardCreateParameters : ICreateParameters
{
    public BottomBoardCreateParameters(LevelData level, BoardSplineDataAdapterInfo adapter, Transform parent)
    {
        Level = level;
        Adapter = adapter;
        Parent = parent;
    }

    public LevelData Level { get; }
    public BoardSplineDataAdapterInfo Adapter { get; }
    public Transform Parent { get; }
}

public sealed class WoolBallCreateParameters : ICreateParameters
{
    public WoolBallCreateParameters(WoolBallData data, BoardSplineDataAdapterInfo adapter, Transform parent)
    {
        Data = data;
        Adapter = adapter;
        Parent = parent;
    }

    public WoolBallData Data { get; }
    public BoardSplineDataAdapterInfo Adapter { get; }
    public Transform Parent { get; }
}
