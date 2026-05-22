using UnityEngine;
using UnityEngine.UIElements;

public partial class YarnBoardEditorWindow
{
    private void RefreshBoardPreview()
    {
        _boardPreviewRoot.Clear();
        _cellViews.Clear();

        if (_currentLevel == null)
        {
            _boardPreviewRoot.Add(new Label("Select or create a level") { name = "emptyWorkspaceLabel" });
            _boardPreviewRoot.Q<Label>("emptyWorkspaceLabel").AddToClassList("empty-state");
            return;
        }

        VisualElement grid = new VisualElement();
        grid.AddToClassList("board-grid");
        _boardPreviewRoot.Add(grid);

        for (int y = _currentLevel.size.y - 1; y >= 0; y--)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("board-row");
            grid.Add(row);

            for (int x = 0; x < _currentLevel.size.x; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                VisualElement cellView = CreateCellView(cell);
                _cellViews[cell] = cellView;
                row.Add(cellView);
            }
        }
    }

    private VisualElement CreateCellView(Vector2Int cell)
    {
        VisualElement cellView = new VisualElement();
        cellView.AddToClassList("board-cell");
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

        cellView.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 0)
                return;
            _isDragging = true;
            HandleCellAction(cell);
            evt.StopPropagation();
        });
        cellView.RegisterCallback<PointerEnterEvent>(_ =>
        {
            _hoverCellLabel.text = $"Cell ({cell.x}, {cell.y})";
            cellView.AddToClassList("hovered");
            if (_isDragging && (_currentTool == EditorToolMode.Paint || _currentTool == EditorToolMode.Erase))
                HandleCellAction(cell);
        });
        cellView.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            cellView.RemoveFromClassList("hovered");
        });

        return cellView;
    }

    private Color GetColorForBall(int colorId)
    {
        if (_colorProfile != null && _colorProfile.colorPalette != null)
        {
            for (int i = 0; i < _colorProfile.colorPalette.Length; i++)
            {
                if (_colorProfile.colorPalette[i].colorID == colorId || i == colorId)
                    return _colorProfile.colorPalette[i].color;
            }
        }

        return ColorProfile.GetColor(colorId);
    }

    private int GetColorCount()
    {
        if (_colorProfile != null && _colorProfile.colorPalette != null)
            return _colorProfile.colorPalette.Length;
        return ColorProfile.Count();
    }
}
