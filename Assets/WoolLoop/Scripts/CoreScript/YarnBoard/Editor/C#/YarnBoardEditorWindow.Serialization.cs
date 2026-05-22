using System.IO;
using UnityEditor;
using UnityEngine;

public partial class YarnBoardEditorWindow
{
    private YarnBoardLevelJson CreateDefaultLevel(string levelId)
    {
        YarnBoardLevelJson level = new YarnBoardLevelJson
        {
            levelId = levelId,
            size = new Vector2Int(8, 8),
            tileData = new bool[64],
            yarnBalls = new System.Collections.Generic.List<WoolBallData>(),
            boardSetting = new GlobalYarnBoardSetting
            {
                centerPos = Vector3.zero,
                cellSpacing = 1f,
                cellSize = 1f
            }
        };

        for (int i = 0; i < level.tileData.Length; i++)
            level.tileData[i] = true;

        level.blockData = level.yarnBalls;
        return level;
    }

    private void LoadJsonWithPicker()
    {
        string start = Path.GetFullPath(DefaultLevelFolder);
        string path = EditorUtility.OpenFilePanel("Load Yarn Board JSON", Directory.Exists(start) ? start : Application.dataPath, "json");
        if (string.IsNullOrEmpty(path))
            return;

        LoadLevelFromFullPath(path);
    }

    private void LoadLevelFromProjectPath(string projectPath)
    {
        LoadLevelFromFullPath(Path.GetFullPath(projectPath));
    }

    private void LoadLevelFromFullPath(string fullPath)
    {
        if (!File.Exists(fullPath))
            return;

        string json = File.ReadAllText(fullPath);
        YarnBoardLevelJson loaded = JsonUtility.FromJson<YarnBoardLevelJson>(json);
        if (loaded == null)
        {
            EditorUtility.DisplayDialog("Load Failed", "Selected JSON is not a Yarn Board level.", "OK");
            return;
        }

        _currentLevel = loaded;
        _currentJsonPath = ToProjectPath(fullPath);
        if (string.IsNullOrEmpty(_currentLevel.levelId))
            _currentLevel.levelId = Path.GetFileNameWithoutExtension(fullPath);
        _selectedCell = new Vector2Int(-1, -1);
        _selectedYarnBall = null;
        _isDirty = false;
        RefreshAll();
        RefreshLevelList();
    }

    private void SaveCurrentLevel()
    {
        if (_currentLevel == null)
            return;

        if (string.IsNullOrEmpty(_currentJsonPath))
        {
            SaveCurrentLevelAs();
            return;
        }

        SaveToProjectPath(_currentJsonPath);
    }

    private void SaveCurrentLevelAs()
    {
        if (_currentLevel == null)
            return;

        EnsureDefaultLevelFolder();
        string fileName = string.IsNullOrEmpty(_currentLevel.levelId) ? "YarnBoardLevel" : _currentLevel.levelId;
        string fullFolder = Path.GetFullPath(DefaultLevelFolder);
        string path = EditorUtility.SaveFilePanel("Save Yarn Board JSON", fullFolder, fileName, "json");
        if (string.IsNullOrEmpty(path))
            return;

        SaveToProjectPath(ToProjectPath(path));
    }

    private void SaveToProjectPath(string projectPath)
    {
        EnsureCurrentLevelShape();
        ValidateCurrentLevel();
        if (_validationErrors.Count > 0)
        {
            EditorUtility.DisplayDialog("Invalid Level", "Fix validation errors before saving.", "OK");
            RefreshAll();
            return;
        }

        _currentLevel.blockData = _currentLevel.Balls;
        string fullPath = Path.GetFullPath(projectPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, JsonUtility.ToJson(_currentLevel, true));
        _currentJsonPath = ToProjectPath(fullPath);
        _isDirty = false;
        AssetDatabase.Refresh();
        RefreshLevelList();
        RefreshAll();
    }

    private void EnsureDefaultLevelFolder()
    {
        if (AssetDatabase.IsValidFolder(DefaultLevelFolder))
            return;

        string[] parts = DefaultLevelFolder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private string GetUniqueLevelId(string baseName)
    {
        string clean = string.IsNullOrWhiteSpace(baseName) ? "Level" : baseName.Replace(" ", "_");
        string candidate = clean;
        int counter = 1;
        while (File.Exists($"{DefaultLevelFolder}/{candidate}.json"))
        {
            candidate = $"{clean}_{counter:00}";
            counter++;
        }

        return candidate;
    }

    private string ToProjectPath(string fullPath)
    {
        string normalized = fullPath.Replace('\\', '/');
        string dataPath = Application.dataPath.Replace('\\', '/');
        if (normalized.StartsWith(dataPath))
            return "Assets" + normalized.Substring(dataPath.Length);
        return normalized;
    }
}
