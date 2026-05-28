using UnityEngine;

namespace BoardSpline.Runtime
{
    [System.Serializable]
    public struct BoardSplineDataAdapterInfo : IBoardSplineDataAdapter
    {
        public Vector2Int size;
        public bool[] tileData;
        public float cellSize;
        public Vector3 origin;
        public Vector3 right;
        public Vector3 forward;
        public bool centerBoardOnOrigin;

        public Vector2Int Size => size;
        public float CellSize => Mathf.Approximately(cellSize, 0f) ? 1f : Mathf.Abs(cellSize);
        public Vector3 Right => right == Vector3.zero ? Vector3.right : right.normalized;
        public Vector3 Forward => forward == Vector3.zero ? Vector3.forward : forward.normalized;

        public bool HasTile(Vector2Int index)
        {
            if (tileData == null || !IsInBounds(index)) return false;

            var dataIndex = index.y * size.x + index.x;
            return dataIndex >= 0 && dataIndex < tileData.Length && tileData[dataIndex];
        }

        public Vector3 IndexToWorld(Vector2Int index)
        {
            var start = origin;
            if (centerBoardOnOrigin)
            {
                start -= (Right * (size.x - 1) + Forward * (size.y - 1)) * CellSize * 0.5f;
            }

            return start + (Right * index.x + Forward * index.y) * CellSize;
        }

        private bool IsInBounds(Vector2Int index) =>
            index.x >= 0 && index.y >= 0 && index.x < size.x && index.y < size.y;
    }
}
