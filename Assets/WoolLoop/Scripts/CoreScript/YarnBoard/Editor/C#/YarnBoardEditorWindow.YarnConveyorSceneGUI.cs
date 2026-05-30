using BoardSpline.Runtime;
using UnityEditor;
using UnityEngine;

public partial class YarnBoardEditorWindow
{
    private void OnConveyorSceneGUI(SceneView sceneView)
    {
        if (_currentTab != LevelEditTab.YarnConveyor || _currentLevel?.yarnConveyor == null)
            return;

        var data = _currentLevel.yarnConveyor;
        if (data.controlPoints == null)
            return;

        var builderTransform = GetSelectedConveyorBuilder()?.transform;
        Handles.color = new Color(0.15f, 0.85f, 1f, 0.9f);
        DrawConveyorPathHandles(data, builderTransform);
        DrawConveyorPointHandles(data, builderTransform);
        DrawConveyorExitHandle(data, builderTransform);

        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && evt.button == 0 && _currentConveyorTool == ConveyorToolMode.AddPoint && !evt.alt)
        {
            AddConveyorPointFromMouse(evt.mousePosition, builderTransform);
            evt.Use();
        }
    }

    private void DrawConveyorPathHandles(YarnConveyorData data, Transform builderTransform)
    {
        for (var i = 0; i < data.controlPoints.Count - 1; i++)
            Handles.DrawLine(
                LocalToConveyorWorld(data.controlPoints[i], builderTransform),
                LocalToConveyorWorld(data.controlPoints[i + 1], builderTransform)
            );

        if (data.loop && data.controlPoints.Count > 2)
        {
            Handles.DrawLine(
                LocalToConveyorWorld(data.controlPoints[data.controlPoints.Count - 1], builderTransform),
                LocalToConveyorWorld(data.controlPoints[0], builderTransform)
            );
        }
    }

    private void DrawConveyorPointHandles(YarnConveyorData data, Transform builderTransform)
    {
        for (var i = 0; i < data.controlPoints.Count; i++)
        {
            var index = i;
            var localPoint = data.controlPoints[index];
            var worldPoint = LocalToConveyorWorld(localPoint, builderTransform);
            var handleSize = HandleUtility.GetHandleSize(worldPoint) * 0.08f;
            Handles.color = index == _selectedConveyorPoint ? Color.white : new Color(0.15f, 0.85f, 1f, 0.9f);

            if (Handles.Button(worldPoint, Quaternion.identity, handleSize, handleSize * 1.4f, Handles.SphereHandleCap))
            {
                _selectedConveyorPoint = index;
                if (_currentConveyorTool == ConveyorToolMode.DeletePoint)
                    DeleteSelectedConveyorPoint();
                else
                    RefreshAll();
            }

            Handles.Label(worldPoint + Vector3.up * handleSize * 2f, index.ToString());

            if ((_currentConveyorTool == ConveyorToolMode.MovePoint || _currentConveyorTool == ConveyorToolMode.SelectPoint) &&
                index == _selectedConveyorPoint)
            {
                EditorGUI.BeginChangeCheck();
                var handleRotation = builderTransform != null ? builderTransform.rotation : Quaternion.identity;
                var moved = Handles.PositionHandle(worldPoint, handleRotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Move Yarn Conveyor Point");
                    data.controlPoints[index] = SnapConveyorPoint(ConveyorWorldToLocal(moved, builderTransform));
                    MarkDirty();
                }
            }
        }
    }

    private void DrawConveyorExitHandle(YarnConveyorData data, Transform builderTransform)
    {
        if (data.exits == null || data.exits.Count == 0 || data.controlPoints.Count == 0)
            return;

        for (var i = 0; i < data.exits.Count; i++)
        {
            YarnConveyorExitData exit = data.exits[i];
            if (exit == null)
                continue;

            var localStart = YarnConveyorEditorUtility.SampleExitPosition(data, exit);
            var localEnd = YarnConveyorEditorUtility.SampleExitEndPosition(data, exit);
            var worldStart = LocalToConveyorWorld(localStart, builderTransform);
            var worldEnd = LocalToConveyorWorld(localEnd, builderTransform);
            var radius = HandleUtility.GetHandleSize(worldStart) * 0.12f;

            Handles.color = new Color(1f, 0.25f, 0.1f, 1f);
            Handles.SphereHandleCap(0, worldStart, Quaternion.identity, radius, EventType.Repaint);
            Handles.DrawAAPolyLine(4f, worldStart, worldEnd);
            Handles.color = new Color(1f, 0.85f, 0.15f, 1f);
            Handles.SphereHandleCap(0, worldEnd, Quaternion.identity, radius * 0.65f, EventType.Repaint);
            Handles.Label(worldStart + Vector3.up * radius * 2f, $"Exit {i + 1}  P {exit.percent:0.##}  L {exit.length:0.##}");
        }
    }

    private void AddConveyorPointFromMouse(Vector2 mousePosition, Transform builderTransform)
    {
        var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        var planeNormal = builderTransform != null ? builderTransform.TransformDirection(Vector3.up) : Vector3.up;
        var planePoint = builderTransform != null ? builderTransform.position : Vector3.zero;
        var plane = new Plane(planeNormal, planePoint);
        if (!plane.Raycast(ray, out var distance))
            return;

        var worldPoint = ray.GetPoint(distance);
        var data = _currentLevel.yarnConveyor;
        Undo.RecordObject(this, "Add Yarn Conveyor Point");
        data.controlPoints.Add(SnapConveyorPoint(ConveyorWorldToLocal(worldPoint, builderTransform)));
        _selectedConveyorPoint = data.controlPoints.Count - 1;
        MarkDirty();
    }

    private static Vector3 LocalToConveyorWorld(Vector3 localPoint, Transform builderTransform)
    {
        return builderTransform != null ? builderTransform.TransformPoint(localPoint) : localPoint;
    }

    private static Vector3 ConveyorWorldToLocal(Vector3 worldPoint, Transform builderTransform)
    {
        return builderTransform != null ? builderTransform.InverseTransformPoint(worldPoint) : worldPoint;
    }
}
