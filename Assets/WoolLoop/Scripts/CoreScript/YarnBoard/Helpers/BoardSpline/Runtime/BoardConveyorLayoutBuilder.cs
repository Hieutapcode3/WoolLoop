using System.Collections.Generic;
using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BoardSpline.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BoardConveyorLayoutBuilder : MonoBehaviour
    {
        private const string GeneratedChildPrefix = "__ConveyorLane_";

        public enum InputMode
        {
            DebugFallback
        }

        [TitleGroup("Input")]
        [SerializeField] private InputMode inputMode = InputMode.DebugFallback;

        [TitleGroup("Input")]
        [SerializeField, Min(0f)] private float cornerRadius = 0.25f;

        [TitleGroup("Input")]
        [SerializeField, Min(1)] private int cornerSegments = 6;

        [TitleGroup("Input")]
        [SerializeField] private Vector3 splineNormal = Vector3.up;

        [FoldoutGroup("Graph Debug", Expanded = false)]
        [ShowIf(nameof(IsDebugFallbackMode))]
        [SerializeField] private SerializedBoardConveyorGraphData debugGraph = new SerializedBoardConveyorGraphData();

        [FoldoutGroup("Graph Debug", Expanded = false)]
        [ReadOnly, SerializeField] private int lastPathCount;

        [FoldoutGroup("Graph Debug", Expanded = false)]
        [ReadOnly, SerializeField] private int lastOpenPathCount;

        [FoldoutGroup("Graph Debug", Expanded = false)]
        [ReadOnly, SerializeField] private int lastClosedLoopCount;

        [TitleGroup("Mesh Settings")]
        [SerializeField] private Mesh uShapeCrossSection;

        [TitleGroup("Mesh Settings")]
        [SerializeField] private bool customMeshUseMapTestPreset = true;

        [TitleGroup("Mesh Settings")]
        [SerializeField] private Vector3 customMeshRotation = new Vector3(0f, 90f, 0f);

        [TitleGroup("Mesh Settings")]
        [SerializeField] private Vector3 customMeshOffset = Vector3.zero;

        [TitleGroup("Mesh Settings")]
        [SerializeField] private Vector3 customMeshScale = Vector3.one;

        public SerializedBoardConveyorGraphData DebugGraph => debugGraph;

        public Mesh UShapeCrossSection
        {
            get => uShapeCrossSection;
            set => uShapeCrossSection = value;
        }

        public float CornerRadius
        {
            get => cornerRadius;
            set => cornerRadius = Mathf.Max(0f, value);
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set => cornerSegments = Mathf.Max(1, value);
        }

        public Vector3 CustomMeshRotation
        {
            get => customMeshRotation;
            set => customMeshRotation = value;
        }

        public Vector3 CustomMeshOffset
        {
            get => customMeshOffset;
            set => customMeshOffset = value;
        }

        public Vector3 CustomMeshScale
        {
            get => customMeshScale;
            set => customMeshScale = value == Vector3.zero ? Vector3.one : value;
        }

        private bool IsDebugFallbackMode => inputMode == InputMode.DebugFallback;

        private void OnValidate()
        {
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Max(1, cornerSegments);
            if (splineNormal == Vector3.zero) splineNormal = Vector3.up;
            if (customMeshScale == Vector3.zero) customMeshScale = Vector3.one;
            TryAssignDefaultCrossSection();
        }

        [TitleGroup("Build Actions")]
        [Button("Analyze")]
        public List<BoardConveyorPath> AnalyzeDebugGraph()
        {
            var paths = BoardConveyorGraphAnalyzer.Analyze(GetCurrentGraphData());
            UpdateDebugCounts(paths);
            return paths;
        }

        [TitleGroup("Build Actions")]
        [Button("Build")]
        public void Build()
        {
            BuildConveyors(GetCurrentGraphData(), false);
        }

        [TitleGroup("Build Actions")]
        [Button("Rebuild")]
        public void Rebuild()
        {
            BuildConveyors(GetCurrentGraphData(), true);
        }

        [TitleGroup("Build Actions")]
        [Button("Clear")]
        public void Clear()
        {
            ClearGeneratedChildren();
            UpdateDebugCounts(new List<BoardConveyorPath>());
        }

        public List<ConveyorFrameBuilder> BuildConveyors(IBoardConveyorGraphData graphData, bool clearExisting)
        {
            var paths = BoardConveyorGraphAnalyzer.Analyze(graphData);
            UpdateDebugCounts(paths);

            if (clearExisting)
                ClearGeneratedChildren();

            var children = GetGeneratedChildren();
            var builders = new List<ConveyorFrameBuilder>(paths.Count);
            for (var i = 0; i < paths.Count; i++)
            {
                var builder = GetOrCreateBuilder(children, i);
                ConfigureBuilder(builder, paths[i], i);
                builders.Add(builder);
            }

            for (var i = children.Count - 1; i >= paths.Count; i--)
                DestroyGeneratedChild(children[i].gameObject);

            return builders;
        }

        private IBoardConveyorGraphData GetCurrentGraphData()
        {
            return debugGraph;
        }

        private ConveyorFrameBuilder GetOrCreateBuilder(IReadOnlyList<ConveyorFrameBuilder> existing, int index)
        {
            if (index < existing.Count && existing[index] != null)
                return existing[index];

            var child = new GameObject($"{GeneratedChildPrefix}{index:00}");
            child.transform.SetParent(transform, false);
            return child.AddComponent<ConveyorFrameBuilder>();
        }

        private void ConfigureBuilder(ConveyorFrameBuilder builder, BoardConveyorPath path, int index)
        {
            builder.gameObject.name = $"{GeneratedChildPrefix}{index:00}";
            builder.transform.localPosition = Vector3.zero;
            builder.transform.localRotation = Quaternion.identity;
            builder.transform.localScale = Vector3.one;

            builder.CornerRadius = cornerRadius;
            builder.CornerSegments = cornerSegments;
            builder.SplineNormal = splineNormal;
            builder.UShapeCrossSection = uShapeCrossSection;
            builder.CustomMeshUseMapTestPreset = customMeshUseMapTestPreset;
            builder.CustomMeshRotation = customMeshRotation;
            builder.CustomMeshOffset = customMeshOffset;
            builder.CustomMeshScale = customMeshScale;
            builder.SetPath(path.CenterPaths.ToArray(), path.Closed);
            builder.Build();
        }

        private List<ConveyorFrameBuilder> GetGeneratedChildren()
        {
            var result = new List<ConveyorFrameBuilder>();
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (!child.name.StartsWith(GeneratedChildPrefix)) continue;
                if (child.TryGetComponent(out ConveyorFrameBuilder builder))
                    result.Add(builder);
            }

            result.Sort((a, b) => a.name.CompareTo(b.name));
            return result;
        }

        private void ClearGeneratedChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name.StartsWith(GeneratedChildPrefix))
                    DestroyGeneratedChild(child.gameObject);
            }
        }

        private static void DestroyGeneratedChild(GameObject child)
        {
            if (child == null) return;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        private void UpdateDebugCounts(IReadOnlyList<BoardConveyorPath> paths)
        {
            lastPathCount = paths?.Count ?? 0;
            lastOpenPathCount = 0;
            lastClosedLoopCount = 0;
            if (paths == null) return;

            for (var i = 0; i < paths.Count; i++)
            {
                if (paths[i].Closed) lastClosedLoopCount++;
                else lastOpenPathCount++;
            }
        }

        private void TryAssignDefaultCrossSection()
        {
#if UNITY_EDITOR
            if (uShapeCrossSection != null) return;

            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/WoolLoop/Models/map_test.fbx");
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Mesh mesh && mesh.name == "Cube")
                {
                    uShapeCrossSection = mesh;
                    return;
                }
            }
#endif
        }
    }
}
