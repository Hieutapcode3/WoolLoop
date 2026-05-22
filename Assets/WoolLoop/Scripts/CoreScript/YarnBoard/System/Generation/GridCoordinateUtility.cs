using UnityEngine;

public static class GridCoordinateUtility
{
    public static int ToIndex(Vector2Int tile, int columns)
    {
        return tile.y * columns + tile.x;
    }

    public static Vector2Int ToTile(int index, int columns)
    {
        return new Vector2Int(index % columns, index / columns);
    }

    public static Vector3 TileToWorld(Vector2Int tile, LevelData data)
    {
        float pitch = GetCellPitch(data);
        Vector3 origin = GetBoardOrigin(data);
        return origin + new Vector3(tile.x * pitch, 0f, tile.y * pitch);
    }

    public static Vector3 CornerToWorld(Vector2Int corner, LevelData data)
    {
        float pitch = GetCellPitch(data);
        float halfCell = GetCellSize(data) * 0.5f;
        Vector3 origin = GetBoardOrigin(data);
        return origin + new Vector3(corner.x * pitch - halfCell, 0f, corner.y * pitch - halfCell);
    }

    public static float GetCellPitch(LevelData data)
    {
        if (data == null || data.boardSetting == null)
            return 1f;

        return Mathf.Max(data.boardSetting.cellSpacing, data.boardSetting.cellSize, 0.01f);
    }

    public static float GetCellSize(LevelData data)
    {
        if (data == null || data.boardSetting == null)
            return 1f;

        return Mathf.Max(data.boardSetting.cellSize, 0.01f);
    }

    private static Vector3 GetBoardOrigin(LevelData data)
    {
        float pitch = GetCellPitch(data);
        Vector3 center = data.boardSetting != null ? data.boardSetting.centerPos : Vector3.zero;
        Vector3 boardExtent = new Vector3((data.size.x - 1) * pitch, 0f, (data.size.y - 1) * pitch);
        return center - boardExtent * 0.5f;
    }
}
