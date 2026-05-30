using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class YarnBoardEditorWindow
{
    private void ValidateCurrentLevel()
    {
        _validationErrors.Clear();
        _conveyorWarnings.Clear();
        _errorCells.Clear();

        if (_currentLevel == null)
            return;

        if (_currentLevel.size.x < 1 || _currentLevel.size.y < 1 || _currentLevel.size.x > MaxBoardSize || _currentLevel.size.y > MaxBoardSize)
            _validationErrors.Add($"Board size must be between 1x1 and {MaxBoardSize}x{MaxBoardSize}.");

        int expectedTiles = _currentLevel.size.x * _currentLevel.size.y;
        if (_currentLevel.tileData == null || _currentLevel.tileData.Length != expectedTiles)
            _validationErrors.Add("Tile data does not match board size.");

        ValidateTargetExitCell();

        int colorCount = GetColorCount();
        Dictionary<Vector2Int, WoolBallData> occupiedCells = new Dictionary<Vector2Int, WoolBallData>();

        foreach (WoolBallData ball in _currentLevel.Balls)
        {
            if (ball == null)
                continue;

            if (colorCount > 0 && (ball.ColorId < 0 || ball.ColorId >= colorCount))
                _validationErrors.Add($"Ball {ball.BallId} has invalid color {ball.ColorId}.");

            ValidateBallCell(ball, ball.tileId, occupiedCells, true);

            if (ball.childrenTileIds == null)
                ball.childrenTileIds = new List<Vector2Int>();

            foreach (Vector2Int child in ball.childrenTileIds)
                ValidateBallCell(ball, child, occupiedCells, false);
        }

        ValidateYarnConveyor();
    }

    private void ValidateYarnConveyor()
    {
        YarnConveyorValidationResult result = YarnConveyorEditorUtility.Validate(
            _currentLevel.yarnConveyor,
            GetSelectedConveyorBuilder()
        );

        _validationErrors.AddRange(result.Errors);
        if (_currentTab == LevelEditTab.YarnConveyor)
            _conveyorWarnings.AddRange(result.Warnings);
    }

    private void ValidateTargetExitCell()
    {
        if (_currentLevel == null || !_currentLevel.hasTargetExitTileId)
            return;

        Vector2Int target = _currentLevel.targetExitTileId;
        if (!IsInsideBoard(target))
        {
            _validationErrors.Add($"Target exit tile is outside board at ({target.x}, {target.y}).");
            _errorCells.Add(target);
            return;
        }

        if (!IsActiveCell(target))
        {
            _validationErrors.Add($"Target exit tile is inactive at ({target.x}, {target.y}).");
            _errorCells.Add(target);
        }
    }

    private void ValidateBallCell(WoolBallData ball, Vector2Int cell, Dictionary<Vector2Int, WoolBallData> occupiedCells, bool isMain)
    {
        string label = isMain ? "main tile" : "child tile";
        if (!IsInsideBoard(cell))
        {
            _validationErrors.Add($"Ball {ball.BallId} {label} is outside board at ({cell.x}, {cell.y}).");
            _errorCells.Add(cell);
            return;
        }

        if (!IsActiveCell(cell))
        {
            _validationErrors.Add($"Ball {ball.BallId} {label} is on inactive cell ({cell.x}, {cell.y}).");
            _errorCells.Add(cell);
        }

        if (occupiedCells.TryGetValue(cell, out WoolBallData other))
        {
            string target = other == ball ? "itself" : $"ball {other.BallId}";
            _validationErrors.Add($"Ball {ball.BallId} overlaps {target} at ({cell.x}, {cell.y}).");
            _errorCells.Add(cell);
        }
        else if (!occupiedCells.ContainsKey(cell))
        {
            occupiedCells.Add(cell, ball);
        }
    }

    private void RefreshValidationPanel()
    {
        _validationPanel.Clear();
        if (_validationErrors.Count == 0 && _conveyorWarnings.Count == 0)
            return;

        foreach (string error in _validationErrors.Take(6))
        {
            Label label = new Label(error);
            label.AddToClassList("validation-item");
            _validationPanel.Add(label);
        }

        foreach (string warning in _conveyorWarnings.Take(Mathf.Max(0, 6 - _validationErrors.Count)))
        {
            Label label = new Label(warning);
            label.AddToClassList("validation-item");
            label.AddToClassList("subtle");
            _validationPanel.Add(label);
        }

        int hiddenCount = Mathf.Max(0, _validationErrors.Count + _conveyorWarnings.Count - 6);
        if (hiddenCount > 0)
        {
            Label label = new Label($"+ {hiddenCount} more");
            label.AddToClassList("validation-item");
            _validationPanel.Add(label);
        }
    }

    private void RefreshStatus()
    {
        if (_currentLevel == null)
        {
            _validationStatus.text = "No level selected";
            _cellSummary.text = string.Empty;
            _yarnSummary.text = string.Empty;
            _hoverCellLabel.text = string.Empty;
            return;
        }

        if (_validationErrors.Count > 0)
            _validationStatus.text = $"{_validationErrors.Count} error(s)";
        else if (_conveyorWarnings.Count > 0)
            _validationStatus.text = _isDirty ? $"Unsaved changes, {_conveyorWarnings.Count} warning(s)" : $"{_conveyorWarnings.Count} warning(s)";
        else
            _validationStatus.text = _isDirty ? "Unsaved changes" : "Valid";
        _validationStatus.EnableInClassList("ok", _validationErrors.Count == 0);
        _validationStatus.EnableInClassList("error", _validationErrors.Count > 0);

        int activeCells = _currentLevel.tileData == null ? 0 : _currentLevel.tileData.Count(active => active);
        _cellSummary.text = $"{activeCells}/{_currentLevel.size.x * _currentLevel.size.y} cells";
        if (_currentTab == LevelEditTab.YarnConveyor)
        {
            int pointCount = _currentLevel.yarnConveyor?.controlPoints?.Count ?? 0;
            _yarnSummary.text = $"{pointCount} conveyor points";
        }
        else
        {
            _yarnSummary.text = $"{_currentLevel.Balls.Count} yarn balls";
        }
    }
}
