using BoardSpline.Runtime;
using Dreamteck.Splines;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class YarnBoardEditorWindow
{
    private void RefreshConveyorPreview()
    {
        _boardPreviewRoot.Clear();
        _cellViews.Clear();

        if (_currentLevel == null)
        {
            _boardPreviewRoot.Add(new Label("Select or create a level") { name = "emptyWorkspaceLabel" });
            _boardPreviewRoot.Q<Label>("emptyWorkspaceLabel").AddToClassList("empty-state");
            return;
        }

        var data = _currentLevel.yarnConveyor;
        VisualElement root = new VisualElement();
        root.AddToClassList("conveyor-preview");
        _boardPreviewRoot.Add(root);

        Label header = new Label($"{data.controlPoints.Count} control point(s) | {(data.loop ? "Loop" : "Open path")}");
        header.AddToClassList("section-title");
        root.Add(header);

        if (data.controlPoints.Count == 0)
        {
            Label empty = new Label("Create a preset or add points in the Scene View.");
            empty.AddToClassList("conveyor-empty");
            root.Add(empty);
            return;
        }

        for (var i = 0; i < data.controlPoints.Count; i++)
        {
            var index = i;
            var row = new VisualElement();
            row.AddToClassList("conveyor-point-row");
            row.EnableInClassList("selected", index == _selectedConveyorPoint);
            row.RegisterCallback<PointerDownEvent>(_ =>
            {
                _selectedConveyorPoint = index;
                RefreshAll();
            });

            Label indexLabel = new Label($"Point {i}");
            indexLabel.AddToClassList("conveyor-point-label");
            row.Add(indexLabel);

            Label valueLabel = new Label(FormatPoint(data.controlPoints[i]));
            valueLabel.AddToClassList("conveyor-point-value");
            row.Add(valueLabel);

            root.Add(row);
        }
    }

    private void ToggleConveyorLoop()
    {
        if (_currentLevel?.yarnConveyor == null)
            return;

        _currentLevel.yarnConveyor.loop = !_currentLevel.yarnConveyor.loop;
        MarkDirty();
    }

    private void ToggleConveyorSnap()
    {
        _conveyorSnapToGrid = !_conveyorSnapToGrid;
        RefreshAll();
    }

    private Vector3 SnapConveyorPoint(Vector3 point)
    {
        if (!_conveyorSnapToGrid)
            return point;

        float step = Mathf.Max(0.0001f, _conveyorSnapSize);
        return new Vector3(
            Mathf.Round(point.x / step) * step,
            Mathf.Round(point.y / step) * step,
            Mathf.Round(point.z / step) * step
        );
    }

    private void ClearConveyorPath()
    {
        if (_currentLevel?.yarnConveyor == null)
            return;

        _currentLevel.yarnConveyor.controlPoints.Clear();
        _currentLevel.yarnConveyor.exits?.Clear();
        _currentLevel.yarnConveyor.hasExit = false;
        _selectedConveyorPoint = -1;
        ClearSelectedConveyorBuilderPreview();
        MarkDirty();
    }

    private void ApplyConveyorLayoutTransform()
    {
        var data = _currentLevel?.yarnConveyor;
        if (data?.controlPoints == null || data.controlPoints.Count == 0)
            return;

        Vector3 pivot = CalculateConveyorLayoutPivot(data);
        for (var i = 0; i < data.controlPoints.Count; i++)
        {
            Vector3 relative = data.controlPoints[i] - pivot;
            Vector3 scaled = Vector3.Scale(relative, _conveyorLayoutScale);
            data.controlPoints[i] = SnapConveyorPoint(pivot + scaled + _conveyorLayoutOffset);
        }

        _selectedConveyorPoint = -1;
        MarkDirty();
    }

    private void ResetConveyorLayoutTransform()
    {
        _conveyorLayoutOffset = Vector3.zero;
        _conveyorLayoutScale = Vector3.one;
        RefreshAll();
    }

    private Vector3 CalculateConveyorLayoutPivot(YarnConveyorData data)
    {
        Bounds bounds = new Bounds(data.controlPoints[0], Vector3.zero);
        for (var i = 1; i < data.controlPoints.Count; i++)
            bounds.Encapsulate(data.controlPoints[i]);

        return bounds.center;
    }

    private void BuildConveyorPreview()
    {
        ApplyConveyorToSelectedBuilder();
    }

    private void ApplyConveyorToSelectedBuilder()
    {
        if (_currentLevel?.yarnConveyor == null)
            return;

        var builder = GetSelectedConveyorBuilder();
        if (builder == null)
        {
            EditorUtility.DisplayDialog(
                "Select CustomFrameBuilder",
                "Select a scene GameObject with CustomFrameBuilder before applying the Yarn Conveyor path.",
                "OK"
            );
            RefreshAll();
            return;
        }

        Undo.RecordObject(builder, "Apply Yarn Conveyor Path");
        if (!YarnConveyorEditorUtility.ApplyToBuilder(_currentLevel.yarnConveyor, builder))
        {
            EditorUtility.DisplayDialog(
                "Build Failed",
                "CustomFrameBuilder could not build the conveyor. Check the path and cross-section mesh.",
                "OK"
            );
        }

        EditorUtility.SetDirty(builder);
        SceneView.RepaintAll();
        RefreshAll();
    }

    private void ClearSelectedConveyorBuilderPreview()
    {
        var builder = GetSelectedConveyorBuilder();
        if (builder == null)
            return;

        Undo.RecordObject(builder, "Clear Yarn Conveyor Preview");
        builder.SetPath(new Vector3[0], false);
        EditorUtility.SetDirty(builder);

        var splineComputer = builder.GetComponent<SplineComputer>();
        if (splineComputer != null)
        {
            Undo.RecordObject(splineComputer, "Clear Yarn Conveyor Spline");
            splineComputer.SetPoints(new SplinePoint[0], SplineComputer.Space.Local);
            splineComputer.Break();
            EditorUtility.SetDirty(splineComputer);
        }

        var splineMesh = builder.GetComponent<SplineMesh>();
        if (splineMesh != null)
        {
            Undo.RecordObject(splineMesh, "Clear Yarn Conveyor Mesh");
            for (var i = splineMesh.GetChannelCount() - 1; i >= 0; i--)
                splineMesh.RemoveChannel(i);
            splineMesh.RebuildImmediate();
            EditorUtility.SetDirty(splineMesh);
        }

        var meshFilter = builder.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Undo.RecordObject(meshFilter, "Clear Yarn Conveyor Mesh Filter");
            meshFilter.sharedMesh = null;
            EditorUtility.SetDirty(meshFilter);
        }

        SceneView.RepaintAll();
    }

    private void ApplyConveyorPreset()
    {
        if (_currentLevel?.yarnConveyor == null)
            return;

        YarnConveyorEditorUtility.ApplyPreset(
            _currentLevel.yarnConveyor,
            _activeConveyorPreset
        );
        _selectedConveyorPoint = -1;
        MarkDirty();
    }

    private void MoveSelectedConveyorPoint(int direction)
    {
        if (_currentLevel?.yarnConveyor?.controlPoints == null)
            return;

        var points = _currentLevel.yarnConveyor.controlPoints;
        var newIndex = _selectedConveyorPoint + direction;
        if (_selectedConveyorPoint < 0 || _selectedConveyorPoint >= points.Count || newIndex < 0 || newIndex >= points.Count)
            return;

        var point = points[_selectedConveyorPoint];
        points[_selectedConveyorPoint] = points[newIndex];
        points[newIndex] = point;
        _selectedConveyorPoint = newIndex;
        MarkDirty();
    }

    private void DeleteSelectedConveyorPoint()
    {
        if (_currentLevel?.yarnConveyor?.controlPoints == null)
            return;

        var points = _currentLevel.yarnConveyor.controlPoints;
        if (_selectedConveyorPoint < 0 || _selectedConveyorPoint >= points.Count)
            return;

        points.RemoveAt(_selectedConveyorPoint);
        _selectedConveyorPoint = Mathf.Min(_selectedConveyorPoint, points.Count - 1);
        MarkDirty();
    }

    private bool TryGetSelectedConveyorPointPercent(out float percent)
    {
        percent = 0f;
        if (_currentLevel?.yarnConveyor?.controlPoints == null)
            return false;

        var data = _currentLevel.yarnConveyor;
        if (_selectedConveyorPoint < 0 || _selectedConveyorPoint >= data.controlPoints.Count)
            return false;

        var totalLength = CustomFrameBuilder.CalculateLength(data.controlPoints, data.loop);
        if (totalLength <= 0f)
            return false;

        var walked = 0f;
        for (var i = 0; i < _selectedConveyorPoint; i++)
            walked += Vector3.Distance(data.controlPoints[i], data.controlPoints[(i + 1) % data.controlPoints.Count]);

        percent = Mathf.Clamp01(walked / totalLength);
        return true;
    }

    private CustomFrameBuilder GetSelectedConveyorBuilder()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
            return null;

        return selected.GetComponent<CustomFrameBuilder>();
    }

    private static string FormatPoint(Vector3 point)
    {
        return $"({point.x:0.##}, {point.y:0.##}, {point.z:0.##})";
    }
}
