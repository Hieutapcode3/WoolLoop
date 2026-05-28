using UnityEngine;
using UnityEngine.UIElements;

public partial class YarnBoardEditorWindow
{
    private void BindWorkspace()
    {
        _selectTool.clicked += () => SetTool(EditorToolMode.Select);
        _paintTool.clicked += () => SetTool(EditorToolMode.Paint);
        _eraseTool.clicked += () => SetTool(EditorToolMode.Erase);
        _yarnTool.clicked += () => SetTool(EditorToolMode.YarnBall);
        _loadButton.clicked += LoadJsonWithPicker;
        _saveButton.clicked += SaveCurrentLevel;
        _saveAsButton.clicked += SaveCurrentLevelAs;

        _rowsField.RegisterValueChangedCallback(evt => ResizeBoard(_currentLevel != null ? _currentLevel.size.x : 1, evt.newValue));
        _columnsField.RegisterValueChangedCallback(evt => ResizeBoard(evt.newValue, _currentLevel != null ? _currentLevel.size.y : 1));
        rootVisualElement.RegisterCallback<PointerUpEvent>(_ => EndDrag());
        rootVisualElement.RegisterCallback<PointerLeaveEvent>(_ => EndDrag());
    }

    private void SetTool(EditorToolMode tool)
    {
        _currentTool = tool;
        RefreshToolbarState();
    }

    private void RefreshToolbarState()
    {
        bool hasLevel = _currentLevel != null;
        _rowsField.SetEnabled(hasLevel);
        _columnsField.SetEnabled(hasLevel);
        _selectTool.SetEnabled(hasLevel);
        _paintTool.SetEnabled(hasLevel);
        _eraseTool.SetEnabled(hasLevel);
        _yarnTool.SetEnabled(hasLevel);
        _saveButton.SetEnabled(hasLevel && _validationErrors.Count == 0);
        _saveAsButton.SetEnabled(hasLevel && _validationErrors.Count == 0);
        _duplicateLevelButton.SetEnabled(hasLevel);
        _deleteLevelButton.SetEnabled(hasLevel);

        _selectTool.EnableInClassList("selected", _currentTool == EditorToolMode.Select);
        _paintTool.EnableInClassList("selected", _currentTool == EditorToolMode.Paint);
        _eraseTool.EnableInClassList("selected", _currentTool == EditorToolMode.Erase);
        _yarnTool.EnableInClassList("selected", _currentTool == EditorToolMode.YarnBall);

        if (_currentLevel != null)
        {
            _rowsField.SetValueWithoutNotify(_currentLevel.size.y);
            _columnsField.SetValueWithoutNotify(_currentLevel.size.x);
        }
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
