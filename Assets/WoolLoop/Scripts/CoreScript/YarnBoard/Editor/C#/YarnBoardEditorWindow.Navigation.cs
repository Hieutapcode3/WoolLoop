using System.IO;
using UnityEngine.UIElements;

public partial class YarnBoardEditorWindow
{
    private void BindNavigation()
    {
        _levelSearch.RegisterValueChangedCallback(evt =>
        {
            _searchText = evt.newValue ?? string.Empty;
            ApplyLevelFilter();
        });

        _levelList.itemsSource = _filteredLevels;
        _levelList.fixedItemHeight = 34;
        _levelList.makeItem = () =>
        {
            Label label = new Label();
            label.AddToClassList("level-item");
            return label;
        };
        _levelList.bindItem = (element, index) =>
        {
            Label label = (Label)element;
            LevelListItem item = _filteredLevels[index];
            label.text = item.Name;
            label.EnableInClassList("selected", item.Path == _currentJsonPath);
        };
        _levelList.selectionChanged += selection =>
        {
            foreach (object selected in selection)
            {
                if (selected is LevelListItem item)
                {
                    LoadLevelFromProjectPath(item.Path);
                    break;
                }
            }
        };

        _newLevelButton.clicked += CreateNewLevel;
        _duplicateLevelButton.clicked += DuplicateCurrentLevel;
        _deleteLevelButton.clicked += DeleteCurrentLevel;
    }

    private void RefreshLevelList()
    {
        _levels.Clear();
        EnsureDefaultLevelFolder();

        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:TextAsset", new[] { DefaultLevelFolder }))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetExtension(path).ToLowerInvariant() != ".json")
                continue;

            _levels.Add(new LevelListItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path
            });
        }

        _levels.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        ApplyLevelFilter();
    }

    private void ApplyLevelFilter()
    {
        _filteredLevels.Clear();
        foreach (LevelListItem item in _levels)
        {
            if (string.IsNullOrEmpty(_searchText) || item.Name.ToLowerInvariant().Contains(_searchText.ToLowerInvariant()))
                _filteredLevels.Add(item);
        }

        _levelList.Rebuild();
    }

    private void CreateNewLevel()
    {
        EnsureDefaultLevelFolder();
        string levelId = GetUniqueLevelId("Level_001");
        _currentLevel = CreateDefaultLevel(levelId);
        _currentJsonPath = $"{DefaultLevelFolder}/{levelId}.json";
        _selectedCell = new UnityEngine.Vector2Int(-1, -1);
        _selectedYarnBall = null;
        _isDirty = true;
        RefreshAll();
        RefreshLevelList();
    }

    private void DuplicateCurrentLevel()
    {
        if (_currentLevel == null)
            return;

        string sourceName = string.IsNullOrEmpty(_currentLevel.levelId) ? "Level" : _currentLevel.levelId;
        string levelId = GetUniqueLevelId(sourceName + "_Copy");
        string json = UnityEngine.JsonUtility.ToJson(_currentLevel, true);
        _currentLevel = UnityEngine.JsonUtility.FromJson<YarnBoardLevelJson>(json);
        _currentLevel.levelId = levelId;
        _currentJsonPath = $"{DefaultLevelFolder}/{levelId}.json";
        _isDirty = true;
        RefreshAll();
        RefreshLevelList();
    }

    private void DeleteCurrentLevel()
    {
        if (string.IsNullOrEmpty(_currentJsonPath) || !File.Exists(_currentJsonPath))
        {
            _currentLevel = null;
            _currentJsonPath = null;
            RefreshAll();
            return;
        }

        if (!UnityEditor.EditorUtility.DisplayDialog("Delete Level", $"Delete {Path.GetFileName(_currentJsonPath)}?", "Delete", "Cancel"))
            return;

        UnityEditor.AssetDatabase.DeleteAsset(_currentJsonPath);
        _currentLevel = null;
        _currentJsonPath = null;
        _selectedYarnBall = null;
        _selectedCell = new UnityEngine.Vector2Int(-1, -1);
        RefreshLevelList();
        RefreshAll();
    }
}
