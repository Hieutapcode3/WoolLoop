using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public partial class YarnBoardEditorWindow
{
    private void RefreshInspector()
    {
        _inspectorContent.Clear();

        if (_currentLevel == null)
        {
            AddEmptyState("Select or create a level");
            return;
        }

        if (_selectedYarnBall != null)
        {
            DrawYarnBallInspector();
            return;
        }

        if (IsInsideBoard(_selectedCell))
        {
            DrawCellInspector();
            return;
        }

        DrawBoardInspector();
    }

    private void DrawBoardInspector()
    {
        AddSectionTitle("Board Settings");

        TextField levelId = new TextField("Level ID");
        levelId.value = _currentLevel.levelId;
        levelId.RegisterValueChangedCallback(evt =>
        {
            _currentLevel.levelId = evt.newValue;
            _isDirty = true;
            RefreshStatus();
        });
        _inspectorContent.Add(levelId);

        ObjectField colorProfile = new ObjectField("Color Profile");
        colorProfile.objectType = typeof(ColorProfile);
        colorProfile.value = _colorProfile;
        colorProfile.RegisterValueChangedCallback(evt =>
        {
            _colorProfile = evt.newValue as ColorProfile;
            RefreshAll();
        });
        _inspectorContent.Add(colorProfile);

        Vector3Field center = new Vector3Field("Center");
        center.value = _currentLevel.boardSetting.centerPos;
        center.RegisterValueChangedCallback(evt =>
        {
            _currentLevel.boardSetting.centerPos = evt.newValue;
            MarkDirty();
        });
        _inspectorContent.Add(center);

        FloatField spacing = new FloatField("Cell Spacing");
        spacing.value = _currentLevel.boardSetting.cellSpacing;
        spacing.RegisterValueChangedCallback(evt =>
        {
            _currentLevel.boardSetting.cellSpacing = Mathf.Max(0f, evt.newValue);
            MarkDirty();
        });
        _inspectorContent.Add(spacing);

        FloatField size = new FloatField("Cell Size");
        size.value = _currentLevel.boardSetting.cellSize;
        size.RegisterValueChangedCallback(evt =>
        {
            _currentLevel.boardSetting.cellSize = Mathf.Max(0f, evt.newValue);
            MarkDirty();
        });
        _inspectorContent.Add(size);

        Button clear = new Button(() =>
        {
            for (int i = 0; i < _currentLevel.tileData.Length; i++)
                _currentLevel.tileData[i] = false;
            _currentLevel.Balls.Clear();
            MarkDirty();
        })
        {
            text = "Clear Board"
        };
        clear.AddToClassList("button");
        clear.AddToClassList("secondary");
        _inspectorContent.Add(clear);
    }

    private void DrawCellInspector()
    {
        AddSectionTitle($"Cell ({_selectedCell.x}, {_selectedCell.y})");

        Toggle active = new Toggle("Active");
        active.value = IsActiveCell(_selectedCell);
        active.RegisterValueChangedCallback(evt => SetCellActive(_selectedCell, evt.newValue));
        _inspectorContent.Add(active);

        WoolBallData ball = FindBallAt(_selectedCell);
        if (ball == null && IsActiveCell(_selectedCell))
        {
            Button addBall = new Button(() =>
            {
                AddOrMoveYarnBall(_selectedCell);
            })
            {
                text = "Add Yarn Ball"
            };
            addBall.AddToClassList("button");
            addBall.AddToClassList("primary");
            _inspectorContent.Add(addBall);
        }
        else if (ball != null)
        {
            Button selectBall = new Button(() =>
            {
                _selectedYarnBall = ball;
                RefreshAll();
            })
            {
                text = "Edit Yarn Ball"
            };
            selectBall.AddToClassList("button");
            selectBall.AddToClassList("secondary");
            _inspectorContent.Add(selectBall);
        }
    }

    private void DrawYarnBallInspector()
    {
        AddSectionTitle("Yarn Ball");

        IntegerField ballId = new IntegerField("Ball ID");
        ballId.value = _selectedYarnBall.BallId;
        ballId.RegisterValueChangedCallback(evt =>
        {
            _selectedYarnBall.BallId = evt.newValue;
            MarkDirty();
        });
        _inspectorContent.Add(ballId);

        Vector2IntField mainTile = new Vector2IntField("Main Tile");
        mainTile.value = _selectedYarnBall.tileId;
        mainTile.RegisterValueChangedCallback(evt =>
        {
            _selectedYarnBall.tileId = evt.newValue;
            _selectedCell = evt.newValue;
            MarkDirty();
        });
        _inspectorContent.Add(mainTile);

        IntegerField colorId = new IntegerField("Color ID");
        colorId.value = _selectedYarnBall.ColorId;
        colorId.RegisterValueChangedCallback(evt =>
        {
            _selectedYarnBall.ColorId = evt.newValue;
            MarkDirty();
        });
        _inspectorContent.Add(colorId);

        DrawColorPalette();

        bool hasChildren = _selectedYarnBall.childrenTileIds != null && _selectedYarnBall.childrenTileIds.Count > 0;
        bool canUseSelectedCell = IsInsideBoard(_selectedCell) &&
                                  _selectedCell != _selectedYarnBall.tileId &&
                                  IsActiveCell(_selectedCell);
        bool isSelectedChild = hasChildren && _selectedYarnBall.childrenTileIds.Contains(_selectedCell);

        Button toggleSelectedChild = new Button(() =>
        {
            if (_selectedYarnBall.childrenTileIds == null)
                _selectedYarnBall.childrenTileIds = new System.Collections.Generic.List<Vector2Int>();

            if (_selectedYarnBall.childrenTileIds.Contains(_selectedCell))
                _selectedYarnBall.childrenTileIds.Remove(_selectedCell);
            else
                _selectedYarnBall.childrenTileIds.Add(_selectedCell);

            MarkDirty();
        })
        {
            text = isSelectedChild ? "Remove Selected Child" : "Add Selected Cell As Child"
        };
        toggleSelectedChild.SetEnabled(canUseSelectedCell);
        toggleSelectedChild.AddToClassList("button");
        toggleSelectedChild.AddToClassList("secondary");
        _inspectorContent.Add(toggleSelectedChild);

        Button newBall = new Button(() =>
        {
            _selectedYarnBall = null;
            _selectedCell = new Vector2Int(-1, -1);
            SetTool(EditorToolMode.YarnBall);
            RefreshAll();
        })
        {
            text = "New Yarn Ball"
        };
        newBall.AddToClassList("button");
        newBall.AddToClassList("primary");
        _inspectorContent.Add(newBall);

        if (_selectedYarnBall.childrenTileIds != null)
        {
            for (int i = 0; i < _selectedYarnBall.childrenTileIds.Count; i++)
            {
                int index = i;
                VisualElement row = new VisualElement();
                row.AddToClassList("child-row");

                VisualElement header = new VisualElement();
                header.AddToClassList("child-header");

                Label childLabel = new Label($"Child {i + 1}");
                childLabel.AddToClassList("child-label");
                header.Add(childLabel);

                Button removeChild = new Button(() =>
                {
                    _selectedYarnBall.childrenTileIds.RemoveAt(index);
                    MarkDirty();
                })
                {
                    text = "X"
                };
                removeChild.AddToClassList("button");
                removeChild.AddToClassList("icon-button");
                header.Add(removeChild);
                row.Add(header);

                VisualElement inputRow = new VisualElement();
                inputRow.AddToClassList("child-input-row");

                IntegerField childX = new IntegerField("X");
                childX.AddToClassList("child-input");
                childX.value = _selectedYarnBall.childrenTileIds[i].x;
                childX.RegisterValueChangedCallback(evt =>
                {
                    Vector2Int current = _selectedYarnBall.childrenTileIds[index];
                    _selectedYarnBall.childrenTileIds[index] = new Vector2Int(evt.newValue, current.y);
                    MarkDirty();
                });
                inputRow.Add(childX);

                IntegerField childY = new IntegerField("Y");
                childY.AddToClassList("child-input");
                childY.value = _selectedYarnBall.childrenTileIds[i].y;
                childY.RegisterValueChangedCallback(evt =>
                {
                    Vector2Int current = _selectedYarnBall.childrenTileIds[index];
                    _selectedYarnBall.childrenTileIds[index] = new Vector2Int(current.x, evt.newValue);
                    MarkDirty();
                });
                inputRow.Add(childY);
                row.Add(inputRow);

                _inspectorContent.Add(row);
            }
        }

        Button remove = new Button(() =>
        {
            _currentLevel.Balls.Remove(_selectedYarnBall);
            _selectedYarnBall = null;
            MarkDirty();
        })
        {
            text = "Remove Yarn Ball"
        };
        remove.AddToClassList("button");
        remove.AddToClassList("secondary");
        remove.AddToClassList("danger");
        _inspectorContent.Add(remove);
    }

    private void DrawColorPalette()
    {
        VisualElement row = new VisualElement();
        row.AddToClassList("color-row");
        _inspectorContent.Add(row);

        int count = GetColorCount();
        for (int i = 0; i < count; i++)
        {
            int colorIndex = i;
            VisualElement swatch = new VisualElement();
            swatch.AddToClassList("color-swatch");
            swatch.EnableInClassList("selected", _selectedYarnBall.ColorId == colorIndex);
            swatch.style.backgroundColor = GetColorForBall(colorIndex);
            swatch.RegisterCallback<PointerDownEvent>(_ =>
            {
                _selectedYarnBall.ColorId = colorIndex;
                MarkDirty();
            });
            row.Add(swatch);
        }
    }

    private void AddSectionTitle(string text)
    {
        Label label = new Label(text);
        label.AddToClassList("section-title");
        _inspectorContent.Add(label);
    }

    private void AddEmptyState(string text)
    {
        Label label = new Label(text);
        label.AddToClassList("empty-state");
        _inspectorContent.Add(label);
    }
}
