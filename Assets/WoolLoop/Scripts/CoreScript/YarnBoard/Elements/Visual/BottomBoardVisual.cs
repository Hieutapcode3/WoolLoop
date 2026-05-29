using System.Collections.Generic;
using BoardSpline.Runtime;
using UnityEngine;

public class BottomBoardVisual : MonoBehaviour
{
    private const string CellsContainerName = "Cells";
    private const string ObstaclesContainerName = "Obstacles";
    private const string OuterWallName = "OuterWall";

    public void Render(LevelData level, BoardSplineDataAdapterInfo adapter)
    {
        ClearChildren();

        if (level == null)
            return;

        SpawnCells(level, adapter);
        SpawnObstacles(level, adapter);
        SpawnOuterWall(level, adapter);
    }

    private void SpawnCells(LevelData level, BoardSplineDataAdapterInfo adapter)
    {
        var container = CreateContainer(CellsContainerName);

        for (var y = 0; y < level.size.y; y++)
        {
            for (var x = 0; x < level.size.x; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!level.IsActive(cell)) continue;

                var instance = CreatePrefabOrEmpty(PrefabProfile.CellPrefab, $"Cell_{x}_{y}");
                instance.transform.SetParent(container, false);
                instance.transform.localPosition = adapter.IndexToWorld(cell);
            }
        }
    }

    private void SpawnObstacles(LevelData level, BoardSplineDataAdapterInfo adapter)
    {
        if (PrefabProfile.ObstaclePrefab == null)
            return;

        var container = CreateContainer(ObstaclesContainerName);

        for (var y = 0; y < level.size.y; y++)
        {
            for (var x = 0; x < level.size.x; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!YarnBoardLevelUtility.IsBlockedEmptyCell(level, cell)) continue;

                var instance = Object.Instantiate(PrefabProfile.ObstaclePrefab, container, false);
                instance.name = $"Obstacle_{x}_{y}";
                instance.transform.localPosition = adapter.IndexToWorld(cell);
            }
        }
    }

    private void SpawnOuterWall(LevelData level, BoardSplineDataAdapterInfo adapter)
    {
        if (PrefabProfile.OuterWallMesh == null)
            return;

        var settings = BoardSplineSettings.Default;
        var buildData = BoardSplineAnalyzer.Analyze(adapter);
        var outerRegion = YarnBoardLevelUtility.GetLargestRegion(buildData.BorderPointRegions);
        if (outerRegion == null || outerRegion.Count == 0)
            return;

        var borderPath = YarnBoardLevelUtility.CreateBorderPath(outerRegion, settings);
        if (borderPath.Length < 3)
            return;

        var wall = new GameObject(OuterWallName);
        wall.transform.SetParent(transform, false);
        wall.AddComponent<MeshFilter>();
        var renderer = wall.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = PrefabProfile.WallMaterial;

        var builder = wall.AddComponent<CustomFrameBuilder>();
        builder.CornerRadius = settings.borderCornerRadius;
        builder.CornerSegments = Mathf.Max(1, settings.borderSegmentCount);
        builder.SplineNormal = settings.splineNormal;
        builder.CustomMeshUseMapTestPreset = false;
        builder.UShapeCrossSection = PrefabProfile.OuterWallMesh;
        builder.CustomMeshRotation = Vector3.zero;
        builder.CustomMeshOffset = Vector3.zero;
        builder.CustomMeshScale = new Vector3(settings.borderWidth, settings.wallHeight, 1f) * 10f;
        builder.SetPath(borderPath, true);
        builder.Build();
    }

    private Transform CreateContainer(string containerName)
    {
        var container = new GameObject(containerName);
        container.transform.SetParent(transform, false);
        return container.transform;
    }

    private static GameObject CreatePrefabOrEmpty(GameObject prefab, string objectName)
    {
        var instance = prefab != null ? Object.Instantiate(prefab) : new GameObject(objectName);
        instance.name = objectName;
        return instance;
    }

    private void ClearChildren()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }
}
