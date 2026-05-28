using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public partial class YarnBoardEditorWindow : EditorWindow
{
    private const int MaxBoardSize = 16;
    private const float NarrowLayoutThreshold = 1080f;
    private const string UxmlPath = "Assets/WoolLoop/Scripts/CoreScript/YarnBoard/Editor/UXML/YarnBoardEditorWindow.uxml";
    private const string UssPath = "Assets/WoolLoop/Scripts/CoreScript/YarnBoard/Editor/USS/YarnBoardEditorWindow.uss";
    private const string DefaultLevelFolder = "Assets/WoolLoop/Scripts/CoreScript/YarnBoard/Editor/Levels";

    private enum EditorToolMode
    {
        Select,
        Paint,
        Erase,
        YarnBall
    }

    [Serializable]
    public class YarnBoardLevelJson
    {
        public string levelId;
        public Vector2Int size = new Vector2Int(8, 8);
        public bool[] tileData = new bool[64];
        public List<WoolBallData> yarnBalls = new List<WoolBallData>();
        public List<WoolBallData> blockData = new List<WoolBallData>();
        public GlobalYarnBoardSetting boardSetting = new GlobalYarnBoardSetting
        {
            centerPos = Vector3.zero,
            cellSpacing = 1f,
            cellSize = 1f
        };

        public List<WoolBallData> Balls
        {
            get
            {
                if (yarnBalls == null)
                    yarnBalls = new List<WoolBallData>();
                if ((yarnBalls.Count == 0) && blockData != null && blockData.Count > 0)
                    yarnBalls = blockData;
                blockData = yarnBalls;
                return yarnBalls;
            }
        }
    }

    private struct LevelListItem
    {
        public string Name;
        public string Path;
    }

    private readonly List<LevelListItem> _levels = new List<LevelListItem>();
    private readonly List<LevelListItem> _filteredLevels = new List<LevelListItem>();
    private readonly List<string> _validationErrors = new List<string>();
    private readonly Dictionary<Vector2Int, VisualElement> _cellViews = new Dictionary<Vector2Int, VisualElement>();
    private readonly HashSet<Vector2Int> _errorCells = new HashSet<Vector2Int>();

    private YarnBoardLevelJson _currentLevel;
    private string _currentJsonPath;
    private Vector2Int _selectedCell = new Vector2Int(-1, -1);
    private WoolBallData _selectedYarnBall;
    private EditorToolMode _currentTool = EditorToolMode.Select;
    private ColorsParamSO _colorsParam;
    private bool _isDirty;
    private bool _isDragging;
    private string _searchText = string.Empty;
    private bool _isNarrowLayout;
    private bool _pendingDragRefresh;

    private VisualElement _rootContainer;
    private TextField _levelSearch;
    private ListView _levelList;
    private Button _newLevelButton;
    private Button _duplicateLevelButton;
    private Button _deleteLevelButton;
    private Button _selectTool;
    private Button _paintTool;
    private Button _eraseTool;
    private Button _yarnTool;
    private Button _loadButton;
    private Button _saveButton;
    private Button _saveAsButton;
    private IntegerField _rowsField;
    private IntegerField _columnsField;
    private VisualElement _boardPreviewRoot;
    private VisualElement _inspectorContent;
    private VisualElement _validationPanel;
    private Label _validationStatus;
    private Label _cellSummary;
    private Label _yarnSummary;
    private Label _hoverCellLabel;

    [MenuItem("Tools/Wool Loop/Yarn Board Level Editor")]
    public static void ShowWindow()
    {
        YarnBoardEditorWindow window = GetWindow<YarnBoardEditorWindow>();
        window.titleContent = new GUIContent("Yarn Board");
        window.minSize = new Vector2(980, 560);
    }

    public void CreateGUI()
    {
        VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        StyleSheet style = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);

        rootVisualElement.Clear();
        if (tree != null)
            tree.CloneTree(rootVisualElement);
        else
            rootVisualElement.Add(new Label("Missing YarnBoardEditorWindow.uxml"));

        if (style != null)
            rootVisualElement.styleSheets.Add(style);

        CacheElements();
        BindResponsiveLayout();
        LoadFirstColorsParam();
        BindNavigation();
        BindWorkspace();
        RefreshLevelList();
        RefreshAll();
    }

    private void CacheElements()
    {
        _rootContainer = rootVisualElement.Q<VisualElement>(className: "yarn-editor-root");
        _levelSearch = rootVisualElement.Q<TextField>("levelSearch");
        _levelList = rootVisualElement.Q<ListView>("levelList");
        _newLevelButton = rootVisualElement.Q<Button>("newLevelButton");
        _duplicateLevelButton = rootVisualElement.Q<Button>("duplicateLevelButton");
        _deleteLevelButton = rootVisualElement.Q<Button>("deleteLevelButton");
        _selectTool = rootVisualElement.Q<Button>("selectTool");
        _paintTool = rootVisualElement.Q<Button>("paintTool");
        _eraseTool = rootVisualElement.Q<Button>("eraseTool");
        _yarnTool = rootVisualElement.Q<Button>("yarnTool");
        _loadButton = rootVisualElement.Q<Button>("loadButton");
        _saveButton = rootVisualElement.Q<Button>("saveButton");
        _saveAsButton = rootVisualElement.Q<Button>("saveAsButton");
        _rowsField = rootVisualElement.Q<IntegerField>("rowsField");
        _columnsField = rootVisualElement.Q<IntegerField>("columnsField");
        _boardPreviewRoot = rootVisualElement.Q<VisualElement>("boardPreviewRoot");
        _inspectorContent = rootVisualElement.Q<VisualElement>("inspectorContent");
        _validationPanel = rootVisualElement.Q<VisualElement>("validationPanel");
        _validationStatus = rootVisualElement.Q<Label>("validationStatus");
        _cellSummary = rootVisualElement.Q<Label>("cellSummary");
        _yarnSummary = rootVisualElement.Q<Label>("yarnSummary");
        _hoverCellLabel = rootVisualElement.Q<Label>("hoverCellLabel");
    }

    private void BindResponsiveLayout()
    {
        if (_rootContainer == null)
            return;

        rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
        UpdateResponsiveLayout(rootVisualElement.layout.width);
    }

    private void OnRootGeometryChanged(GeometryChangedEvent evt)
    {
        UpdateResponsiveLayout(evt.newRect.width);
    }

    private void UpdateResponsiveLayout(float width)
    {
        if (_rootContainer == null || width <= 0f)
            return;

        bool narrow = width < NarrowLayoutThreshold;
        if (narrow == _isNarrowLayout)
            return;

        _isNarrowLayout = narrow;
        _rootContainer.EnableInClassList("narrow", narrow);
    }

    private void RefreshAll()
    {
        EnsureCurrentLevelShape();
        ValidateCurrentLevel();
        RefreshToolbarState();
        RefreshBoardPreview();
        RefreshInspector();
        RefreshValidationPanel();
        RefreshStatus();
    }

    private void MarkDirty()
    {
        _isDirty = true;
        RefreshAll();
    }

    private void MarkDirtyForCell(Vector2Int cell)
    {
        _isDirty = true;

        if (_isDragging && (_currentTool == EditorToolMode.Paint || _currentTool == EditorToolMode.Erase))
        {
            UpdateCellVisual(cell);
            _pendingDragRefresh = true;
            RefreshStatus();
            return;
        }

        RefreshAll();
    }

    private void UpdateCellVisual(Vector2Int cell)
    {
        if (!_cellViews.TryGetValue(cell, out VisualElement cellView))
            return;

        cellView.Clear();
        cellView.EnableInClassList("active", IsActiveCell(cell));
        cellView.EnableInClassList("blocked", !IsActiveCell(cell));
        cellView.EnableInClassList("selected", cell == _selectedCell);
        cellView.EnableInClassList("error", _errorCells.Contains(cell));

        WoolBallData ball = FindBallAt(cell);
        if (ball != null)
        {
            VisualElement dot = new VisualElement();
            dot.AddToClassList("yarn-dot");
            if (ball.tileId != cell)
                dot.AddToClassList("child");
            dot.style.backgroundColor = GetColorForBall(ball.ColorId);
            cellView.Add(dot);
        }
    }

    private void EndDrag()
    {
        if (!_isDragging && !_pendingDragRefresh)
            return;

        _isDragging = false;
        if (_pendingDragRefresh)
        {
            _pendingDragRefresh = false;
            RefreshAll();
        }
    }

    private void LoadFirstColorsParam()
    {
        string[] guids = AssetDatabase.FindAssets("t:ColorsParamSO");
        if (guids.Length == 0)
            return;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        _colorsParam = AssetDatabase.LoadAssetAtPath<ColorsParamSO>(path);
    }

    private int ToIndex(Vector2Int cell)
    {
        return cell.y * _currentLevel.size.x + cell.x;
    }

    private bool IsInsideBoard(Vector2Int cell)
    {
        return _currentLevel != null &&
               cell.x >= 0 &&
               cell.y >= 0 &&
               cell.x < _currentLevel.size.x &&
               cell.y < _currentLevel.size.y;
    }

    private bool IsActiveCell(Vector2Int cell)
    {
        return IsInsideBoard(cell) &&
               _currentLevel.tileData != null &&
               ToIndex(cell) >= 0 &&
               ToIndex(cell) < _currentLevel.tileData.Length &&
               _currentLevel.tileData[ToIndex(cell)];
    }

    private WoolBallData FindBallAt(Vector2Int cell)
    {
        if (_currentLevel == null)
            return null;

        foreach (WoolBallData ball in _currentLevel.Balls)
        {
            if (ball == null)
                continue;
            if (ball.tileId == cell)
                return ball;
            if (ball.childrenTileIds != null && ball.childrenTileIds.Contains(cell))
                return ball;
        }

        return null;
    }

    private void EnsureCurrentLevelShape()
    {
        if (_currentLevel == null)
            return;

        int columns = Mathf.Clamp(_currentLevel.size.x, 1, MaxBoardSize);
        int rows = Mathf.Clamp(_currentLevel.size.y, 1, MaxBoardSize);
        _currentLevel.size = new Vector2Int(columns, rows);

        int expectedLength = columns * rows;
        if (_currentLevel.tileData == null)
            _currentLevel.tileData = new bool[expectedLength];
        if (_currentLevel.tileData.Length != expectedLength)
        {
            bool[] resized = new bool[expectedLength];
            Array.Copy(_currentLevel.tileData, resized, Mathf.Min(_currentLevel.tileData.Length, resized.Length));
            _currentLevel.tileData = resized;
        }

        if (_currentLevel.boardSetting == null)
            _currentLevel.boardSetting = new GlobalYarnBoardSetting();
        _currentLevel.blockData = _currentLevel.Balls;
    }
}
