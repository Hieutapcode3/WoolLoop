using BoardSpline.Runtime;
using Dreamteck.Splines;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class YarnBoardSplineWallEditor
{
    private const string LevelPath = "Assets/WoolLoop/Scripts/CoreScript/YarnBoard/Editor/Levels/Level_001.json";
    private const string WallObjectName = "Level_001_DreamteckSplineWall";

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

        var splineComputer = wall.AddComponent<SplineComputer>();
        var splineMesh = wall.AddComponent<SplineMesh>();
        var renderer = wall.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = PrefabProfile.WallMaterial;
        }

        var boardSetting = level.boardSetting ?? new GlobalYarnBoardSetting();
        var cellSize = boardSetting.cellSpacing > 0f ? boardSetting.cellSpacing : boardSetting.cellSize;
        if (cellSize <= 0f) cellSize = 1f;

        var adapter = new BoardSplineDataAdapterInfo
        {
            size = level.size,
            tileData = level.tileData,
            cellSize = cellSize,
            origin = boardSetting.centerPos,
            right = Vector3.right,
            forward = Vector3.forward,
            centerBoardOnOrigin = true,
        };

        var settings = BoardSplineSettings.Default;
        var buildData = BoardSplineAnalyzer.Analyze(adapter);
        BoardSplineComputerRenderer.Render(wall, buildData, settings);

        Selection.activeGameObject = wall;
        EditorSceneManager.MarkSceneDirty(wall.scene);
        Debug.Log($"Rendered Dreamteck spline wall for {level.levelId} from {LevelPath}");
    }
}
