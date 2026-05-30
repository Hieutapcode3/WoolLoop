using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class YarnBoardEditorWindow
{
    private void BindWorkspace()
    {
        _yarnBoardTab.clicked += () => SetTab(LevelEditTab.YarnBoard);
        _yarnConveyorTab.clicked += () => SetTab(LevelEditTab.YarnConveyor);
        _bobbinsTab.clicked += () => SetTab(LevelEditTab.Bobbins);
        _selectTool.clicked += () => SetTool(EditorToolMode.Select);
        _paintTool.clicked += () => SetTool(EditorToolMode.Paint);
        _eraseTool.clicked += () => SetTool(EditorToolMode.Erase);
        _yarnTool.clicked += () => SetTool(EditorToolMode.YarnBall);
        _conveyorSelectPointTool.clicked += () => SetConveyorTool(ConveyorToolMode.SelectPoint);
        _conveyorAddPointTool.clicked += () => SetConveyorTool(ConveyorToolMode.AddPoint);
        _conveyorMovePointTool.clicked += () => SetConveyorTool(ConveyorToolMode.MovePoint);
        _conveyorDeletePointTool.clicked += () => SetConveyorTool(ConveyorToolMode.DeletePoint);
        _conveyorSnapButton.clicked += ToggleConveyorSnap;
        _conveyorLoopButton.clicked += ToggleConveyorLoop;
        _conveyorBuildPreviewButton.clicked += BuildConveyorPreview;
        _conveyorApplyBuilderButton.clicked += ApplyConveyorToSelectedBuilder;
        _conveyorClearPathButton.clicked += ClearConveyorPath;
        _loadButton.clicked += LoadJsonWithPicker;
        _saveButton.clicked += SaveCurrentLevel;
        _saveAsButton.clicked += SaveCurrentLevelAs;
        ConfigureToolbarIconButtons();

        _rowsField.RegisterValueChangedCallback(evt => ResizeBoard(_currentLevel != null ? _currentLevel.size.x : 1, evt.newValue));
        _columnsField.RegisterValueChangedCallback(evt => ResizeBoard(evt.newValue, _currentLevel != null ? _currentLevel.size.y : 1));
        rootVisualElement.RegisterCallback<PointerUpEvent>(_ => EndDrag());
        rootVisualElement.RegisterCallback<PointerLeaveEvent>(_ => EndDrag());
    }

    private void SetTab(LevelEditTab tab)
    {
        if (_currentTab == tab)
            return;

        _currentTab = tab;
        EndDrag();
        _selectedYarnBall = null;
        _selectedCell = new Vector2Int(-1, -1);
        _selectedConveyorPoint = -1;
        RefreshAll();
    }

    private void SetTool(EditorToolMode tool)
    {
        _currentTool = tool;
        RefreshToolbarState();
    }

    private void SetConveyorTool(ConveyorToolMode tool)
    {
        _currentConveyorTool = tool;
        RefreshToolbarState();
        SceneView.RepaintAll();
    }

    private void RefreshToolbarState()
    {
        bool hasLevel = _currentLevel != null;
        bool isBoard = _currentTab == LevelEditTab.YarnBoard;
        bool isConveyor = _currentTab == LevelEditTab.YarnConveyor;

        _yarnBoardTab.EnableInClassList("selected", isBoard);
        _yarnConveyorTab.EnableInClassList("selected", isConveyor);
        _bobbinsTab.EnableInClassList("selected", _currentTab == LevelEditTab.Bobbins);

        _boardSizeGroup.style.display = isBoard ? DisplayStyle.Flex : DisplayStyle.None;
        _boardToolGroup.style.display = isBoard ? DisplayStyle.Flex : DisplayStyle.None;
        _conveyorToolGroup.style.display = isConveyor ? DisplayStyle.Flex : DisplayStyle.None;
        _conveyorActionGroup.style.display = isConveyor ? DisplayStyle.Flex : DisplayStyle.None;

        _rowsField.SetEnabled(hasLevel && isBoard);
        _columnsField.SetEnabled(hasLevel && isBoard);
        _selectTool.SetEnabled(hasLevel && isBoard);
        _paintTool.SetEnabled(hasLevel && isBoard);
        _eraseTool.SetEnabled(hasLevel && isBoard);
        _yarnTool.SetEnabled(hasLevel && isBoard);
        _conveyorSelectPointTool.SetEnabled(hasLevel && isConveyor);
        _conveyorAddPointTool.SetEnabled(hasLevel && isConveyor);
        _conveyorMovePointTool.SetEnabled(hasLevel && isConveyor);
        _conveyorDeletePointTool.SetEnabled(hasLevel && isConveyor);
        _conveyorSnapButton.SetEnabled(hasLevel && isConveyor);
        _conveyorLoopButton.SetEnabled(hasLevel && isConveyor);
        _conveyorBuildPreviewButton.SetEnabled(hasLevel && isConveyor);
        _conveyorApplyBuilderButton.SetEnabled(hasLevel && isConveyor);
        _conveyorClearPathButton.SetEnabled(hasLevel && isConveyor);
        _saveButton.SetEnabled(hasLevel && _validationErrors.Count == 0);
        _saveAsButton.SetEnabled(hasLevel && _validationErrors.Count == 0);
        _duplicateLevelButton.SetEnabled(hasLevel);
        _deleteLevelButton.SetEnabled(hasLevel);

        _selectTool.EnableInClassList("selected", _currentTool == EditorToolMode.Select);
        _paintTool.EnableInClassList("selected", _currentTool == EditorToolMode.Paint);
        _eraseTool.EnableInClassList("selected", _currentTool == EditorToolMode.Erase);
        _yarnTool.EnableInClassList("selected", _currentTool == EditorToolMode.YarnBall);
        _conveyorSelectPointTool.EnableInClassList("selected", _currentConveyorTool == ConveyorToolMode.SelectPoint);
        _conveyorAddPointTool.EnableInClassList("selected", _currentConveyorTool == ConveyorToolMode.AddPoint);
        _conveyorMovePointTool.EnableInClassList("selected", _currentConveyorTool == ConveyorToolMode.MovePoint);
        _conveyorDeletePointTool.EnableInClassList("selected", _currentConveyorTool == ConveyorToolMode.DeletePoint);
        _conveyorSnapButton.EnableInClassList("selected", _conveyorSnapToGrid);
        _conveyorLoopButton.EnableInClassList("selected", false);

        if (_currentLevel != null)
        {
            _rowsField.SetValueWithoutNotify(_currentLevel.size.y);
            _columnsField.SetValueWithoutNotify(_currentLevel.size.x);
            bool loop = _currentLevel.yarnConveyor != null && _currentLevel.yarnConveyor.loop;
            _conveyorLoopButton.EnableInClassList("selected", loop);
            _conveyorLoopButton.tooltip = loop ? "Loop enabled" : "Loop disabled";
        }
    }

    private void ConfigureToolbarIconButtons()
    {
        ConfigureToolbarIconButton(_selectTool, "d_ViewToolOrbit", "Pick", "Pick cell", "info");
        ConfigureToolbarIconButton(_paintTool, "d_Toolbar Plus", "+", "Add tile", "add");
        ConfigureToolbarIconButton(_eraseTool, "d_Toolbar Minus", "-", "Remove tile", "danger");
        ConfigureToolbarIconButton(_yarnTool, "d_CreateAddNew", "+", "Add or edit yarn ball", "accent");

        ConfigureToolbarIconButton(_conveyorSelectPointTool, "d_ViewToolOrbit", "Pick", "Pick point", "info");
        ConfigureToolbarIconButton(_conveyorAddPointTool, "d_Toolbar Plus", "+", "Add point", "add");
        ConfigureToolbarIconButton(_conveyorMovePointTool, "d_MoveTool", "Move", "Move point", "accent");
        ConfigureToolbarIconButton(_conveyorDeletePointTool, "TreeEditor.Trash", "Del", "Delete point", "danger");

        ConfigureToolbarIconButton(_conveyorSnapButton, "d_Grid.BoxTool", "Snap", "Snap to grid", "info");
        ConfigureToolbarIconButton(_conveyorLoopButton, "d_Refresh", "Loop", "Toggle loop", "accent");
        ConfigureToolbarIconButton(_conveyorBuildPreviewButton, "d_PlayButton", "Preview", "Build preview", "add");
        ConfigureToolbarIconButton(_conveyorApplyBuilderButton, "d_FilterSelectedOnly", "Apply", "Apply to selected builder", "confirm");
        ConfigureToolbarIconButton(_conveyorClearPathButton, "d_TreeEditor.Trash", "Clear", "Clear current layout", "danger");

        ConfigureToolbarIconButton(_loadButton, "d_FolderOpened Icon", "Load", "Load JSON", "file");
        ConfigureToolbarIconButton(_saveAsButton, "d_SaveAs", "Save As", "Save JSON as", "file");
        ConfigureToolbarIconButton(_saveButton, "d_SaveActive", "Save", "Save JSON", "confirm");
    }

    private static void ConfigureToolbarIconButton(
        Button button,
        string unityIconName,
        string fallbackText,
        string tooltip,
        string tone
    )
    {
        if (button == null)
            return;

        GUIContent iconContent = EditorGUIUtility.IconContent(unityIconName);
        if (iconContent?.image is Texture2D iconTexture)
        {
            button.text = string.Empty;
            button.style.backgroundImage = iconTexture;
        }
        else
        {
            button.text = fallbackText;
        }

        button.tooltip = tooltip;
        button.AddToClassList("toolbar-icon-button");
        button.AddToClassList($"toolbar-icon-{tone}");
    }

    private void ResizeBoard(int columns, int rows)
    {
        if (_currentLevel == null)
            return;

        columns = Mathf.Clamp(columns, 1, MaxBoardSize);
        rows = Mathf.Clamp(rows, 1, MaxBoardSize);
        Vector2Int oldSize = _currentLevel.size;
        bool[] oldTiles = _currentLevel.tileData;
        bool[] resized = new bool[columns * rows];

        for (int y = 0; y < Mathf.Min(oldSize.y, rows); y++)
        {
            for (int x = 0; x < Mathf.Min(oldSize.x, columns); x++)
            {
                resized[y * columns + x] = oldTiles[y * oldSize.x + x];
            }
        }

        _currentLevel.size = new Vector2Int(columns, rows);
        _currentLevel.tileData = resized;
        _selectedCell = new Vector2Int(-1, -1);
        _selectedYarnBall = null;
        MarkDirty();
    }

    private void HandleCellAction(Vector2Int cell)
    {
        if (_currentLevel == null || !IsInsideBoard(cell))
            return;

        if (_currentTool == EditorToolMode.Paint)
        {
            _selectedCell = cell;
            _selectedYarnBall = FindBallAt(cell);
            SetCellActive(cell, true);
        }
        else if (_currentTool == EditorToolMode.Erase)
        {
            _selectedCell = cell;
            _selectedYarnBall = FindBallAt(cell);
            SetCellActive(cell, false);
        }
        else if (_currentTool == EditorToolMode.YarnBall)
        {
            AddOrMoveYarnBall(cell);
        }
        else
        {
            _selectedCell = cell;
            _selectedYarnBall = FindBallAt(cell);
            RefreshAll();
        }
    }

    private void SetCellActive(Vector2Int cell, bool active)
    {
        if (!IsInsideBoard(cell))
            return;

        _currentLevel.tileData[ToIndex(cell)] = active;
        if (!active)
            RemoveCellFromYarnBalls(cell);
        MarkDirtyForCell(cell);
    }

    private void AddOrMoveYarnBall(Vector2Int cell)
    {
        if (!IsActiveCell(cell))
            return;

        WoolBallData existing = FindBallAt(cell);
        if (existing != null)
        {
            _selectedYarnBall = existing;
            _selectedCell = cell;
            RefreshAll();
            return;
        }

        if (_selectedYarnBall != null)
        {
            if (_selectedYarnBall.childrenTileIds == null)
                _selectedYarnBall.childrenTileIds = new System.Collections.Generic.List<Vector2Int>();

            if (_selectedYarnBall.childrenTileIds.Contains(cell))
                _selectedYarnBall.childrenTileIds.Remove(cell);
            else if (_selectedYarnBall.tileId != cell)
                _selectedYarnBall.childrenTileIds.Add(cell);

            _selectedCell = cell;
            MarkDirty();
            return;
        }

        WoolBallData ball = new WoolBallData
        {
            BallId = GetNextBallId(),
            ColorId = 0,
            tileId = cell,
            childrenTileIds = new System.Collections.Generic.List<Vector2Int>()
        };
        _currentLevel.Balls.Add(ball);
        _selectedYarnBall = ball;
        _selectedCell = cell;
        MarkDirty();
    }

    private int GetNextBallId()
    {
        int id = 1;
        foreach (WoolBallData ball in _currentLevel.Balls)
            id = Mathf.Max(id, ball.BallId + 1);
        return id;
    }

    private void RemoveCellFromYarnBalls(Vector2Int cell)
    {
        for (int i = _currentLevel.Balls.Count - 1; i >= 0; i--)
        {
            WoolBallData ball = _currentLevel.Balls[i];
            if (ball.tileId == cell)
            {
                _currentLevel.Balls.RemoveAt(i);
                continue;
            }

            if (ball.childrenTileIds != null)
                ball.childrenTileIds.RemoveAll(child => child == cell);
        }
    }
}
