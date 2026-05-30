using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LevelData
{
    public string levelId;
    public Vector2Int size;
    public bool[] tileData;
    public List<WoolBallData> yarnBalls;
    public bool hasTargetExitTileId;
    public Vector2Int targetExitTileId;
    public GlobalYarnBoardSetting boardSetting;

    public bool IsValidTileIndex(int index)
    {
        return tileData != null && index >= 0 && index < tileData.Length;
    }

    public bool IsInside(Vector2Int tile)
    {
        return tile.x >= 0 && tile.y >= 0 && tile.x < size.x && tile.y < size.y;
    }

    public bool IsActive(Vector2Int tile)
    {
        if (!IsInside(tile)) return false;

        var index = tile.y * size.x + tile.x;
        return IsValidTileIndex(index) && tileData[index];
    }
}
