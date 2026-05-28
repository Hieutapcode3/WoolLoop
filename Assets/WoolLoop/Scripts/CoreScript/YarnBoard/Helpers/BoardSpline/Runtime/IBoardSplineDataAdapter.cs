using UnityEngine;

namespace BoardSpline.Runtime
{
    public interface IBoardSplineDataAdapter
    {
        Vector2Int Size { get; }
        float CellSize { get; }
        Vector3 Right { get; }
        Vector3 Forward { get; }
        bool HasTile(Vector2Int index);
        Vector3 IndexToWorld(Vector2Int index);
    }
}
