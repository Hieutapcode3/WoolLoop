using BoardSpline.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class YarnBoardSplineWallEditor
{
    private const string LevelPath = "Assets/Resources/Levels/Level_001.json";
    private const string WallObjectName = "Level_001_DreamteckSplineWall";
    private const string ObstacleContainerName = "Blocked Obstacles";

    [MenuItem("GameEditor/YarnBoard/Render Level 001 Wall")]
    public static void RenderLevel001Wall()
    {
        var levelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(LevelPath);
        if (levelAsset == null)
        {
            Debug.LogError($"Cannot load level JSON at {LevelPath}");
            return;
        }

        var level = JsonUtility.FromJson<LevelData>(levelAsset.text);
        if (level == null || level.size.x <= 0 || level.size.y <= 0 || level.tileData == null)
        {
            Debug.LogError($"Invalid level data in {LevelPath}");
            return;
        }

        var existing = GameObject.Find(WallObjectName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        var wall = new GameObject(WallObjectName);
        Undo.RegisterCreatedObjectUndo(wall, "Render Yarn Board Spline Wall");

        wall.AddComponent<MeshFilter>();
        var renderer = wall.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = PrefabProfile.WallMaterial;

        var adapter = YarnBoardLevelUtility.CreateAdapter(level);

        var settings = BoardSplineSettings.Default;
        var buildData = BoardSplineAnalyzer.Analyze(adapter);

        var outerRegion = YarnBoardLevelUtility.GetLargestRegion(buildData.BorderPointRegions);
        if (outerRegion == null || outerRegion.Count == 0)
        {
            Debug.LogWarning("No border points found for the wall.");
            return;
        }

        var borderPath = YarnBoardLevelUtility.CreateBorderPath(outerRegion, settings);
        if (borderPath.Length < 3)
        {
            Debug.LogWarning("Not enough border points to build the wall spline.");
            return;
        }

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

        SpawnBlockedObstacles(level, adapter, wall.transform);

        Selection.activeGameObject = wall;
        EditorSceneManager.MarkSceneDirty(wall.scene);
        Debug.Log($"Rendered Dreamteck spline wall for {level.levelId} from {LevelPath}");
    }

    private static void SpawnBlockedObstacles(LevelData level, BoardSplineDataAdapterInfo adapter, Transform parent)
    {
        if (PrefabProfile.ObstaclePrefab == null) return;

        var existing = parent.Find(ObstacleContainerName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var container = new GameObject(ObstacleContainerName);
        Undo.RegisterCreatedObjectUndo(container, "Spawn Yarn Board Obstacles");
        container.transform.SetParent(parent, false);

        for (var y = 0; y < level.size.y; y++)
        {
            for (var x = 0; x < level.size.x; x++)
            {
                var cell = new Vector2Int(x, y);
                if (!YarnBoardLevelUtility.IsBlockedEmptyCell(level, cell)) continue;

                var instance = PrefabUtility.InstantiatePrefab(PrefabProfile.ObstaclePrefab) as GameObject;
                if (instance == null) continue;

                Undo.RegisterCreatedObjectUndo(instance, "Spawn Yarn Board Obstacles");
                instance.transform.SetParent(container.transform, true);
                instance.transform.position = adapter.IndexToWorld(cell);
            }
        }
    }

}
