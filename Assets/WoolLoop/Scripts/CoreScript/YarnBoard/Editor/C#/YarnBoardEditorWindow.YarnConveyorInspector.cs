using System.Collections.Generic;
using BoardSpline.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public partial class YarnBoardEditorWindow
{
    private void DrawConveyorInspector()
    {
        AddSectionTitle("Yarn Conveyor");

        if (_currentLevel == null)
        {
            AddEmptyState("Select or create a level");
            return;
        }

        var data = _currentLevel.yarnConveyor;

        AddSectionTitle("Preset Library");
        List<string> presetChoices = YarnConveyorEditorUtility.GetPresetChoices();
        if (!presetChoices.Contains(_activeConveyorPreset))
            _activeConveyorPreset = YarnConveyorEditorUtility.CustomPresetId;

        PopupField<string> preset = new PopupField<string>(
            "Preset",
            presetChoices,
            _activeConveyorPreset
        );
        preset.RegisterValueChangedCallback(evt => _activeConveyorPreset = evt.newValue);
        _inspectorContent.Add(preset);

        VisualElement presetActions = new VisualElement();
        presetActions.AddToClassList("inspector-action-row");

        Button savePreset = new Button(SaveCurrentConveyorPreset) { text = "Save to New Preset" };
        savePreset.SetEnabled(data.controlPoints != null && data.controlPoints.Count > 0);
        savePreset.AddToClassList("button");
        savePreset.AddToClassList("secondary");
        presetActions.Add(savePreset);

        Button replacePreset = new Button(ReplaceSelectedConveyorPreset) { text = "Replace Selected" };
        replacePreset.SetEnabled(
            YarnConveyorEditorUtility.IsUserPreset(_activeConveyorPreset) &&
            data.controlPoints != null &&
            data.controlPoints.Count > 0
        );
        replacePreset.AddToClassList("button");
        replacePreset.AddToClassList("secondary");
        presetActions.Add(replacePreset);

        Button duplicatePreset = new Button(DuplicateSelectedConveyorPreset) { text = "Duplicate Selected" };
        duplicatePreset.SetEnabled(YarnConveyorEditorUtility.IsUserPreset(_activeConveyorPreset));
        duplicatePreset.AddToClassList("button");
        duplicatePreset.AddToClassList("secondary");
        presetActions.Add(duplicatePreset);

        Button deletePreset = new Button(DeleteSelectedConveyorPreset) { text = "Delete User" };
        deletePreset.SetEnabled(YarnConveyorEditorUtility.IsUserPreset(_activeConveyorPreset));
        deletePreset.AddToClassList("button");
        deletePreset.AddToClassList("secondary");
        deletePreset.AddToClassList("danger");
        presetActions.Add(deletePreset);

        _inspectorContent.Add(presetActions);

        Button applyPreset = new Button(ApplyConveyorPreset) { text = "Apply Preset" };
        applyPreset.SetEnabled(YarnConveyorEditorUtility.IsUserPreset(_activeConveyorPreset));
        applyPreset.AddToClassList("button");
        applyPreset.AddToClassList("primary");
        _inspectorContent.Add(applyPreset);

        DrawConveyorLayoutEditControls(data);

        AddSectionTitle("Path Settings");

        Toggle snap = new Toggle("Snap To Grid");
        snap.value = _conveyorSnapToGrid;
        snap.RegisterValueChangedCallback(evt =>
        {
            _conveyorSnapToGrid = evt.newValue;
            RefreshAll();
        });
        _inspectorContent.Add(snap);

        FloatField snapSize = new FloatField("Snap Size");
        snapSize.value = _conveyorSnapSize;
        snapSize.SetEnabled(_conveyorSnapToGrid);
        snapSize.RegisterValueChangedCallback(evt =>
        {
            _conveyorSnapSize = Mathf.Max(0.0001f, evt.newValue);
            RefreshAll();
        });
        _inspectorContent.Add(snapSize);

        Toggle loop = new Toggle("Loop");
        loop.value = data.loop;
        loop.RegisterValueChangedCallback(evt =>
        {
            data.loop = evt.newValue;
            MarkDirty();
        });
        _inspectorContent.Add(loop);

        FloatField cornerRadius = new FloatField("Corner Radius");
        cornerRadius.value = data.cornerRadius;
        cornerRadius.RegisterValueChangedCallback(evt =>
        {
            data.cornerRadius = Mathf.Max(0f, evt.newValue);
            MarkDirty();
        });
        _inspectorContent.Add(cornerRadius);

        IntegerField cornerSegments = new IntegerField("Corner Segments");
        cornerSegments.value = data.cornerSegments;
        cornerSegments.RegisterValueChangedCallback(evt =>
        {
            data.cornerSegments = Mathf.Max(1, evt.newValue);
            MarkDirty();
        });
        _inspectorContent.Add(cornerSegments);

        DrawConveyorExitControls(data);

        ObjectField selectedBuilder = new ObjectField("Selected Builder");
        selectedBuilder.objectType = typeof(CustomFrameBuilder);
        selectedBuilder.value = GetSelectedConveyorBuilder();
        selectedBuilder.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue is CustomFrameBuilder builder)
                Selection.activeGameObject = builder.gameObject;
            else
                Selection.activeObject = null;
            RefreshAll();
        });
        _inspectorContent.Add(selectedBuilder);

        Label count = new Label($"Control Points: {data.controlPoints.Count}");
        count.AddToClassList("section-title");
        _inspectorContent.Add(count);

        DrawSelectedConveyorPointInspector();
    }

    private void DrawSelectedConveyorPointInspector()
    {
        var data = _currentLevel?.yarnConveyor;
        if (data?.controlPoints == null ||
            _selectedConveyorPoint < 0 ||
            _selectedConveyorPoint >= data.controlPoints.Count)
            return;

        AddSectionTitle($"Point {_selectedConveyorPoint}");

        DrawSelectedConveyorPointPositionFields(data);

        VisualElement row = new VisualElement();
        row.AddToClassList("child-input-row");

        Button up = new Button(() => MoveSelectedConveyorPoint(-1)) { text = "Move Up" };
        up.SetEnabled(_selectedConveyorPoint > 0);
        up.AddToClassList("button");
        up.AddToClassList("secondary");
        row.Add(up);

        Button down = new Button(() => MoveSelectedConveyorPoint(1)) { text = "Move Down" };
        down.SetEnabled(_selectedConveyorPoint < data.controlPoints.Count - 1);
        down.AddToClassList("button");
        down.AddToClassList("secondary");
        row.Add(down);

        _inspectorContent.Add(row);

        Button delete = new Button(DeleteSelectedConveyorPoint) { text = "Delete Point" };
        delete.AddToClassList("button");
        delete.AddToClassList("secondary");
        delete.AddToClassList("danger");
        _inspectorContent.Add(delete);
    }

    private void DrawConveyorExitControls(YarnConveyorData data)
    {
        AddSectionTitle("Exits");

        if (data.exits == null)
            data.exits = new List<YarnConveyorExitData>();

        VisualElement actionRow = new VisualElement();
        actionRow.AddToClassList("inspector-action-row");

        Button addExit = new Button(() => AddConveyorExit(0f)) { text = "+ Start" };
        addExit.tooltip = "Add exit at the beginning of the conveyor path.";
        addExit.AddToClassList("button");
        addExit.AddToClassList("primary");
        actionRow.Add(addExit);

        Button addAtPoint = new Button(AddConveyorExitAtSelectedPoint) { text = "+ Selected Point" };
        addAtPoint.tooltip = "Add exit at the currently selected control point.";
        addAtPoint.SetEnabled(TryGetSelectedConveyorPointPercent(out _));
        addAtPoint.AddToClassList("button");
        addAtPoint.AddToClassList("secondary");
        actionRow.Add(addAtPoint);

        _inspectorContent.Add(actionRow);

        if (data.exits.Count == 0)
        {
            Label empty = new Label("No exits");
            empty.AddToClassList("conveyor-empty");
            _inspectorContent.Add(empty);
            return;
        }

        for (var i = 0; i < data.exits.Count; i++)
        {
            var index = i;
            YarnConveyorExitData exit = data.exits[index];
            if (exit == null)
                continue;

            VisualElement exitBlock = new VisualElement();
            exitBlock.AddToClassList("conveyor-exit-card");

            VisualElement header = new VisualElement();
            header.AddToClassList("conveyor-exit-header");

            Label title = new Label($"Exit {index + 1}");
            title.AddToClassList("conveyor-exit-title");
            header.Add(title);

            Label summary = new Label($"{exit.percent * 100f:0.#}%  |  {exit.length:0.##}u");
            summary.AddToClassList("conveyor-exit-summary");
            header.Add(summary);

            Button duplicate = new Button(() => DuplicateConveyorExit(index)) { text = "Duplicate" };
            duplicate.AddToClassList("button");
            duplicate.AddToClassList("secondary");
            duplicate.AddToClassList("conveyor-exit-small-button");
            header.Add(duplicate);

            Button remove = new Button(() => RemoveConveyorExit(index)) { text = "X" };
            remove.tooltip = "Remove this exit.";
            remove.AddToClassList("button");
            remove.AddToClassList("icon-button");
            header.Add(remove);

            exitBlock.Add(header);

            VisualElement percentRow = new VisualElement();
            percentRow.AddToClassList("conveyor-exit-edit-row");
            Label percentLabel = new Label("Start");
            percentLabel.tooltip = "Position along the conveyor path. 0% is the first point, 100% is the end.";
            percentLabel.AddToClassList("conveyor-exit-field-label");
            percentRow.Add(percentLabel);

            Slider percent = new Slider(0f, 1f);
            percent.value = exit.percent;
            percent.AddToClassList("conveyor-exit-slider");
            percent.RegisterValueChangedCallback(evt =>
            {
                exit.percent = Mathf.Clamp01(evt.newValue);
                summary.text = $"{exit.percent * 100f:0.#}%  |  {exit.length:0.##}u";
                MarkConveyorDirtyLight();
            });
            percentRow.Add(percent);

            FloatField percentField = new FloatField("%");
            percentField.value = exit.percent * 100f;
            percentField.AddToClassList("conveyor-exit-percent-field");
            percentField.RegisterValueChangedCallback(evt =>
            {
                exit.percent = Mathf.Clamp01(evt.newValue / 100f);
                percent.SetValueWithoutNotify(exit.percent);
                percentField.SetValueWithoutNotify(exit.percent * 100f);
                summary.text = $"{exit.percent * 100f:0.#}%  |  {exit.length:0.##}u";
                MarkConveyorDirtyLight();
            });
            percentRow.Add(percentField);

            exitBlock.Add(percentRow);

            VisualElement lengthRow = new VisualElement();
            lengthRow.AddToClassList("conveyor-exit-edit-row");
            Label lengthLabel = new Label("Length");
            lengthLabel.tooltip = "World/local units of path length occupied by this exit.";
            lengthLabel.AddToClassList("conveyor-exit-field-label");
            lengthRow.Add(lengthLabel);

            FloatField length = new FloatField();
            length.value = exit.length;
            length.AddToClassList("conveyor-exit-length-field");
            length.RegisterValueChangedCallback(evt =>
            {
                exit.length = Mathf.Max(0f, evt.newValue);
                length.SetValueWithoutNotify(exit.length);
                summary.text = $"{exit.percent * 100f:0.#}%  |  {exit.length:0.##}u";
                MarkConveyorDirtyLight();
            });
            lengthRow.Add(length);

            AddExitLengthPresetButton(lengthRow, exit, length, summary, 0.5f);
            AddExitLengthPresetButton(lengthRow, exit, length, summary, 1f);
            AddExitLengthPresetButton(lengthRow, exit, length, summary, 2f);

            exitBlock.Add(lengthRow);

            Button moveToPoint = new Button(() => MoveConveyorExitToSelectedPoint(index)) { text = "Move To Selected Point" };
            moveToPoint.tooltip = "Move this exit start to the selected path control point.";
            moveToPoint.SetEnabled(TryGetSelectedConveyorPointPercent(out _));
            moveToPoint.AddToClassList("button");
            moveToPoint.AddToClassList("secondary");
            moveToPoint.AddToClassList("conveyor-exit-wide-button");
            exitBlock.Add(moveToPoint);

            _inspectorContent.Add(exitBlock);
        }
    }

    private void DrawConveyorLayoutEditControls(YarnConveyorData data)
    {
        AddSectionTitle("Layout Edit");

        Label offsetLabel = new Label("Offset");
        offsetLabel.AddToClassList("section-title");
        _inspectorContent.Add(offsetLabel);

        FloatField offsetX = CreateConveyorPointAxisField("X", _conveyorLayoutOffset.x);
        offsetX.RegisterValueChangedCallback(evt => _conveyorLayoutOffset.x = evt.newValue);
        _inspectorContent.Add(offsetX);

        FloatField offsetY = CreateConveyorPointAxisField("Y", _conveyorLayoutOffset.y);
        offsetY.RegisterValueChangedCallback(evt => _conveyorLayoutOffset.y = evt.newValue);
        _inspectorContent.Add(offsetY);

        FloatField offsetZ = CreateConveyorPointAxisField("Z", _conveyorLayoutOffset.z);
        offsetZ.RegisterValueChangedCallback(evt => _conveyorLayoutOffset.z = evt.newValue);
        _inspectorContent.Add(offsetZ);

        Label scaleLabel = new Label("Scale");
        scaleLabel.AddToClassList("section-title");
        _inspectorContent.Add(scaleLabel);

        FloatField scaleX = CreateConveyorPointAxisField("X", _conveyorLayoutScale.x);
        scaleX.RegisterValueChangedCallback(evt => _conveyorLayoutScale.x = evt.newValue);
        _inspectorContent.Add(scaleX);

        FloatField scaleY = CreateConveyorPointAxisField("Y", _conveyorLayoutScale.y);
        scaleY.RegisterValueChangedCallback(evt => _conveyorLayoutScale.y = evt.newValue);
        _inspectorContent.Add(scaleY);

        FloatField scaleZ = CreateConveyorPointAxisField("Z", _conveyorLayoutScale.z);
        scaleZ.RegisterValueChangedCallback(evt => _conveyorLayoutScale.z = evt.newValue);
        _inspectorContent.Add(scaleZ);

        VisualElement layoutActions = new VisualElement();
        layoutActions.AddToClassList("inspector-action-row");

        Button applyTransform = new Button(ApplyConveyorLayoutTransform) { text = "Apply Transform" };
        applyTransform.SetEnabled(data.controlPoints != null && data.controlPoints.Count > 0);
        applyTransform.AddToClassList("button");
        applyTransform.AddToClassList("primary");
        layoutActions.Add(applyTransform);

        Button resetTransform = new Button(ResetConveyorLayoutTransform) { text = "Reset" };
        resetTransform.AddToClassList("button");
        resetTransform.AddToClassList("secondary");
        layoutActions.Add(resetTransform);

        _inspectorContent.Add(layoutActions);

        Button clearLayout = new Button(ClearConveyorPath) { text = "Clear Current Layout" };
        clearLayout.SetEnabled(data.controlPoints != null && data.controlPoints.Count > 0);
        clearLayout.AddToClassList("button");
        clearLayout.AddToClassList("secondary");
        clearLayout.AddToClassList("danger");
        _inspectorContent.Add(clearLayout);
    }

    private void DrawSelectedConveyorPointPositionFields(YarnConveyorData data)
    {
        Vector3 point = data.controlPoints[_selectedConveyorPoint];

        Label positionLabel = new Label("Position");
        positionLabel.AddToClassList("section-title");
        _inspectorContent.Add(positionLabel);

        FloatField x = CreateConveyorPointAxisField("X", point.x);
        x.RegisterValueChangedCallback(evt =>
        {
            Vector3 current = data.controlPoints[_selectedConveyorPoint];
            data.controlPoints[_selectedConveyorPoint] = SnapConveyorPoint(new Vector3(evt.newValue, current.y, current.z));
            MarkDirty();
        });
        _inspectorContent.Add(x);

        FloatField y = CreateConveyorPointAxisField("Y", point.y);
        y.RegisterValueChangedCallback(evt =>
        {
            Vector3 current = data.controlPoints[_selectedConveyorPoint];
            data.controlPoints[_selectedConveyorPoint] = SnapConveyorPoint(new Vector3(current.x, evt.newValue, current.z));
            MarkDirty();
        });
        _inspectorContent.Add(y);

        FloatField z = CreateConveyorPointAxisField("Z", point.z);
        z.RegisterValueChangedCallback(evt =>
        {
            Vector3 current = data.controlPoints[_selectedConveyorPoint];
            data.controlPoints[_selectedConveyorPoint] = SnapConveyorPoint(new Vector3(current.x, current.y, evt.newValue));
            MarkDirty();
        });
        _inspectorContent.Add(z);
    }

    private static FloatField CreateConveyorPointAxisField(string axis, float value)
    {
        FloatField field = new FloatField(axis);
        field.value = value;
        field.AddToClassList("conveyor-axis-field");
        return field;
    }

    private void AddConveyorExit(float percent)
    {
        var data = _currentLevel?.yarnConveyor;
        if (data == null)
            return;

        if (data.exits == null)
            data.exits = new List<YarnConveyorExitData>();

        data.exits.Add(new YarnConveyorExitData
        {
            percent = Mathf.Clamp01(percent),
            length = 0.5f
        });
        MarkDirty();
    }

    private void AddConveyorExitAtSelectedPoint()
    {
        if (!TryGetSelectedConveyorPointPercent(out float percent))
            return;

        AddConveyorExit(percent);
    }

    private void RemoveConveyorExit(int index)
    {
        var data = _currentLevel?.yarnConveyor;
        if (data?.exits == null || index < 0 || index >= data.exits.Count)
            return;

        data.exits.RemoveAt(index);
        MarkDirty();
    }

    private void DuplicateConveyorExit(int index)
    {
        var data = _currentLevel?.yarnConveyor;
        if (data?.exits == null || index < 0 || index >= data.exits.Count || data.exits[index] == null)
            return;

        YarnConveyorExitData source = data.exits[index];
        data.exits.Insert(index + 1, new YarnConveyorExitData
        {
            percent = Mathf.Clamp01(source.percent),
            length = Mathf.Max(0f, source.length)
        });
        MarkDirty();
    }

    private void MoveConveyorExitToSelectedPoint(int index)
    {
        var data = _currentLevel?.yarnConveyor;
        if (data?.exits == null || index < 0 || index >= data.exits.Count || data.exits[index] == null)
            return;
        if (!TryGetSelectedConveyorPointPercent(out float percent))
            return;

        data.exits[index].percent = percent;
        MarkDirty();
    }

    private void AddExitLengthPresetButton(
        VisualElement row,
        YarnConveyorExitData exit,
        FloatField lengthField,
        Label summary,
        float length
    )
    {
        Button preset = new Button(() =>
        {
            exit.length = length;
            lengthField.SetValueWithoutNotify(exit.length);
            summary.text = $"{exit.percent * 100f:0.#}%  |  {exit.length:0.##}u";
            MarkConveyorDirtyLight();
        })
        {
            text = length.ToString("0.#")
        };
        preset.tooltip = $"Set exit length to {length:0.#}.";
        preset.AddToClassList("button");
        preset.AddToClassList("secondary");
        preset.AddToClassList("conveyor-exit-length-preset");
        row.Add(preset);
    }

    private void MarkConveyorDirtyLight()
    {
        _isDirty = true;
        ValidateCurrentLevel();
        RefreshValidationPanel();
        RefreshStatus();
        SceneView.RepaintAll();
    }

    private void SaveCurrentConveyorPreset()
    {
        if (_currentLevel?.yarnConveyor == null)
            return;

        string newPresetName = YarnConveyorEditorUtility.CreateUniquePresetName();
        if (!YarnConveyorEditorUtility.SaveCurrentAsPreset(newPresetName, _currentLevel.yarnConveyor))
        {
            EditorUtility.DisplayDialog(
                "Preset Not Saved",
                "Create at least one conveyor point before saving a preset.",
                "OK"
            );
            return;
        }

        _activeConveyorPreset = YarnConveyorEditorUtility.UserPresetPrefix + newPresetName;
        RefreshAll();
    }

    private void ReplaceSelectedConveyorPreset()
    {
        if (_currentLevel?.yarnConveyor == null)
            return;

        if (!YarnConveyorEditorUtility.SaveCurrentAsSelectedPreset(_activeConveyorPreset, _currentLevel.yarnConveyor))
        {
            EditorUtility.DisplayDialog(
                "Preset Not Replaced",
                "Select a user preset and create at least one conveyor point before replacing it.",
                "OK"
            );
            return;
        }

        RefreshAll();
    }

    private void DuplicateSelectedConveyorPreset()
    {
        string selectedName = YarnConveyorEditorUtility.GetDisplayUserPresetName(_activeConveyorPreset);
        string newPresetName = YarnConveyorEditorUtility.CreateUniquePresetName(selectedName);
        if (!YarnConveyorEditorUtility.DuplicateSavedPreset(_activeConveyorPreset, newPresetName))
        {
            EditorUtility.DisplayDialog(
                "Preset Not Duplicated",
                "Select a user preset before duplicating.",
                "OK"
            );
            return;
        }

        _activeConveyorPreset = YarnConveyorEditorUtility.UserPresetPrefix + newPresetName;
        ApplyConveyorPreset();
    }

    private void DeleteSelectedConveyorPreset()
    {
        if (!YarnConveyorEditorUtility.DeleteSavedPreset(_activeConveyorPreset))
            return;

        _activeConveyorPreset = YarnConveyorEditorUtility.NoPresetId;
        RefreshAll();
    }
}
