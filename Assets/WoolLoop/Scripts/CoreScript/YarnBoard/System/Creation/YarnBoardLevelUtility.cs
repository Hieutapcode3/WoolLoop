using System.Collections.Generic;
using BoardSpline.Runtime;
using UnityEngine;

public static class YarnBoardLevelUtility
{
    public static BoardSplineDataAdapterInfo CreateAdapter(LevelData level)
    {
        var boardSetting = level?.boardSetting ?? new GlobalYarnBoardSetting();
        var cellSize = boardSetting.cellSpacing > 0f ? boardSetting.cellSpacing : boardSetting.cellSize;
        if (cellSize <= 0f) cellSize = 1f;

        return new BoardSplineDataAdapterInfo
        {
            size = level != null ? level.size : Vector2Int.zero,
            tileData = level != null ? level.tileData : null,
            cellSize = cellSize,
            origin = boardSetting.centerPos,
            right = Vector3.right,
            forward = Vector3.forward,
            centerBoardOnOrigin = true,
        };
    }

    public static bool IsBlockedEmptyCell(LevelData level, Vector2Int cell)
    {
        if (level == null || level.IsActive(cell)) return false;

        return HasActiveInDirection(level, cell, Vector2Int.up)
            && HasActiveInDirection(level, cell, Vector2Int.down)
            && HasActiveInDirection(level, cell, Vector2Int.left)
            && HasActiveInDirection(level, cell, Vector2Int.right);
    }

    public static List<BoardSplineBorderPoint> GetLargestRegion(
        IReadOnlyList<List<BoardSplineBorderPoint>> regions
    )
    {
        if (regions == null || regions.Count == 0) return null;

        List<BoardSplineBorderPoint> result = null;
        var largestCount = -1;
        for (var i = 0; i < regions.Count; i++)
        {
            if (regions[i] == null || regions[i].Count <= largestCount) continue;

            largestCount = regions[i].Count;
            result = regions[i];
        }

        return result;
    }

    public static Vector3[] CreateBorderPath(
        IReadOnlyList<BoardSplineBorderPoint> borderPoints,
        BoardSplineSettings settings
    )
    {
        if (borderPoints == null || borderPoints.Count == 0) return new Vector3[0];

        var borderOffset = GetNormal(settings) * settings.wallHeight * 0.5f;
        var rawPoints = new Vector3[borderPoints.Count];
        for (var i = 0; i < borderPoints.Count; i++)
        {
            rawPoints[i] = borderPoints[i].Position
                         + borderOffset
                         + borderPoints[i].EmptyDirection * settings.borderPadding;
        }

        return rawPoints;
    }

    private static bool HasActiveInDirection(LevelData level, Vector2Int start, Vector2Int dir)
    {
        var current = start + dir;

        while (level.IsInside(current))
        {
            if (level.IsActive(current))
                return true;

            current += dir;
        }

        return false;
    }

    private static Vector3 GetNormal(BoardSplineSettings settings) =>
        settings.splineNormal == Vector3.zero ? Vector3.up : settings.splineNormal.normalized;
}
