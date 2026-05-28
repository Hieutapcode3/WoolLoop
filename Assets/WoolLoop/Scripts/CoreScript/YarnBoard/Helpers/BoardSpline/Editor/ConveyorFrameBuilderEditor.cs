using BoardSpline.Runtime;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace BoardSpline.Editor
{
    [CustomEditor(typeof(ConveyorFrameBuilder))]
    public sealed class ConveyorFrameBuilderEditor : OdinEditor
    {
        private SerializedProperty centerPaths;
        private SerializedProperty editCenterPath;

        private void OnEnable()
        {
            centerPaths = serializedObject.FindProperty("centerPaths");
            editCenterPath = serializedObject.FindProperty("editCenterPath");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        private void OnSceneGUI()
        {
            if (editCenterPath == null || centerPaths == null) return;

            serializedObject.Update();
            if (!editCenterPath.boolValue) return;

            var builder = (ConveyorFrameBuilder)target;
            var transform = builder.transform;

            Handles.color = new Color(0.9f, 0.9f, 0.2f, 0.9f);
            for (var i = 0; i < centerPaths.arraySize; i++)
            {
                var element = centerPaths.GetArrayElementAtIndex(i);
                var localPoint = element.vector3Value;
                var worldPoint = transform.TransformPoint(localPoint);

                EditorGUI.BeginChangeCheck();
                var newWorldPoint = Handles.PositionHandle(worldPoint, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(builder, "Move Center Path Point");
                    var newLocalPoint = transform.InverseTransformPoint(newWorldPoint);
                    element.vector3Value = newLocalPoint;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(builder);
                }

                Handles.Label(worldPoint, i.ToString());
            }
        }
    }
}
