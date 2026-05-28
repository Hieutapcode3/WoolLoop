using System.Collections.Generic;
using Dreamteck.Splines;
using NgoUyenNguyen.Line;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BoardSpline.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineComputer))]
    [RequireComponent(typeof(SplineMesh))]
    public sealed class CustomFrameBuilder : MonoBehaviour
    {
        private const string ChannelName = "Conveyor Frame";
        private const float Epsilon = 0.001f;
        private static readonly Vector3 DefaultMapTestMeshRotation = new Vector3(0f, 90f, 0f);

        [TitleGroup("Path")]
        [SerializeField] private Vector3[] centerPaths = new Vector3[0];

        [TitleGroup("Path")]
        [SerializeField] private bool editCenterPath;

        [TitleGroup("Path")]
        [SerializeField, Min(0f)] private float cornerRadius = 0.25f;

        [TitleGroup("Path")]
        [SerializeField, Min(1)] private int cornerSegments = 6;

        [TitleGroup("Path")]
        [SerializeField] private bool closed;

        [TitleGroup("Path")]
        [SerializeField] private Vector3 splineNormal = Vector3.up;

        [TitleGroup("Mesh")]
        [SerializeField] private Mesh uShapeCrossSection;

        [TitleGroup("Custom Mesh")]
        [SerializeField] private bool customMeshUseMapTestPreset = true;

        [TitleGroup("Custom Mesh")]
        [SerializeField] private Vector3 customMeshRotation = DefaultMapTestMeshRotation;

        [TitleGroup("Custom Mesh")]
        [SerializeField] private Vector3 customMeshOffset = Vector3.zero;

        [TitleGroup("Custom Mesh")]
        [SerializeField] private Vector3 customMeshScale = Vector3.one;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [SerializeField] private bool showRoundedPathGizmos = true;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showRoundedPathGizmos))]
        [SerializeField] private Color pathColor = new Color(0.15f, 0.85f, 1f, 0.9f);

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [SerializeField] private bool showCheckpointGizmo = true;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showCheckpointGizmo))]
        [SerializeField, Range(0f, 1f)] private float checkpointPercent = 0.5f;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showCheckpointGizmo))]
        [SerializeField] private Color checkpointColor = new Color(1f, 0.25f, 0.1f, 1f);

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showCheckpointGizmo))]
        [SerializeField, Min(0.01f)] private float checkpointRadius = 0.12f;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showRoundedPathGizmos))]
        [SerializeField] private bool showDebugLabels;

        public Vector3[] CenterPaths
        {
            get => centerPaths;
            set => centerPaths = value ?? new Vector3[0];
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

        public bool Closed
        {
            get => closed;
            set => closed = value;
        }

        public Mesh UShapeCrossSection
        {
            get => uShapeCrossSection;
            set => uShapeCrossSection = value;
        }

        public Vector3 SplineNormal
        {
            get => GetNormal();
            set => splineNormal = value == Vector3.zero ? Vector3.up : value.normalized;
        }

        public bool CustomMeshUseMapTestPreset
        {
            get => customMeshUseMapTestPreset;
            set => customMeshUseMapTestPreset = value;
        }

        public Vector3 CustomMeshRotation
        {
            get => GetCustomMeshRotation();
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

        private void Reset()
        {
            splineNormal = Vector3.up;
            customMeshUseMapTestPreset = true;
            customMeshRotation = DefaultMapTestMeshRotation;
            customMeshOffset = Vector3.zero;
            customMeshScale = Vector3.one;
        }

        private void OnValidate()
        {
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Max(1, cornerSegments);
            if (splineNormal == Vector3.zero) splineNormal = Vector3.up;
            if (customMeshScale == Vector3.zero) customMeshScale = Vector3.one;
            checkpointPercent = Mathf.Clamp01(checkpointPercent);
            checkpointRadius = Mathf.Max(0.01f, checkpointRadius);
        }

        public void SetPath(Vector3[] path, bool isClosed)
        {
            centerPaths = path ?? new Vector3[0];
            closed = isClosed;
        }

        public Vector3[] GetRoundedPath()
        {
            return CreateRoundedPath(centerPaths, cornerRadius, cornerSegments, closed);
        }

        public Vector3[] GetRoundedPathWorld(float yOffset = 0.5f)
        {
            var localPath = GetRoundedPath();
            var worldPath = new Vector3[localPath.Length];
            for (var i = 0; i < localPath.Length; i++)
                worldPath[i] = transform.TransformPoint(localPath[i]) + Vector3.up * yOffset;

            return worldPath;
        }

        public static float CalculateLength(IReadOnlyList<Vector3> path, bool closed)
        {
            var count = path?.Count ?? 0;
            if (count < 2) return 0f;

            var length = 0f;
            var segmentCount = closed ? count : count - 1;
            for (var i = 0; i < segmentCount; i++)
                length += Vector3.Distance(path[i], path[(i + 1) % count]);

            return length;
        }

        public static Vector3 SamplePathAtDistance(IReadOnlyList<Vector3> path, float distance, bool closed)
        {
            var count = path?.Count ?? 0;
            if (count == 0) return Vector3.zero;
            if (count == 1) return path[0];

            var totalLength = CalculateLength(path, closed);
            if (totalLength <= Epsilon) return path[0];

            distance = closed
                ? Mathf.Repeat(distance, totalLength)
                : Mathf.Clamp(distance, 0f, totalLength);

            var walked = 0f;
            var segmentCount = closed ? count : count - 1;
            for (var i = 0; i < segmentCount; i++)
            {
                var start = path[i];
                var end = path[(i + 1) % count];
                var segmentLength = Vector3.Distance(start, end);
                if (segmentLength <= Epsilon) continue;

                if (walked + segmentLength >= distance)
                {
                    var t = Mathf.Clamp01((distance - walked) / segmentLength);
                    return Vector3.Lerp(start, end, t);
                }

                walked += segmentLength;
            }

            return closed ? path[0] : path[count - 1];
        }

        public static Vector3 SamplePathAtPercent(IReadOnlyList<Vector3> path, float percent, bool closed)
        {
            var count = path?.Count ?? 0;
            if (count == 0) return Vector3.zero;
            if (count == 1) return path[0];

            percent = Mathf.Clamp01(percent);
            if (closed && percent >= 1f) return path[0];

            var totalLength = CalculateLength(path, closed);
            return SamplePathAtDistance(path, totalLength * percent, closed);
        }

        [Button("Build Conveyor Frame")]
        public bool Build()
        {
            return Build(GetComponent<SplineComputer>(), GetComponent<SplineMesh>());
        }

        public bool Build(SplineComputer splineComputer, SplineMesh splineMesh)
        {
            if (splineComputer == null || splineMesh == null) return false;

            var roundedPath = GetRoundedPath();
            if (!HasEnoughPoints(roundedPath, closed)) return false;

            var crossSection = GetCrossSectionMesh();
            if (crossSection == null) return false;

            var normals = CreatePointNormals(roundedPath, closed, GetNormal());
            var splinePoints = new SplinePoint[roundedPath.Length];
            for (var i = 0; i < roundedPath.Length; i++)
            {
                splinePoints[i] = new SplinePoint
                {
                    position = roundedPath[i],
                    normal = normals[i],
                    size = 1f,
                    color = Color.white
                };
            }

            splineComputer.type = Spline.Type.Linear;
            splineComputer.space = SplineComputer.Space.Local;
            splineComputer.SetPoints(splinePoints, SplineComputer.Space.Local);
            if (closed) splineComputer.Close();
            else splineComputer.Break();

            splineMesh.spline = splineComputer;
            ConfigureSplineMesh(
                splineMesh,
                crossSection,
                GetNormal(),
                GetSectionCount(roundedPath.Length, closed),
                GetCustomMeshRotation(),
                customMeshOffset,
                customMeshScale
            );
            splineMesh.RebuildImmediate();
            return true;
        }

        public static Vector3[] CreateRoundedPath(
            IReadOnlyList<Vector3> path,
            float radius,
            int segments,
            bool isClosed
        )
        {
            var simplified = SimplifyPath(path, isClosed);
            if (!HasEnoughPoints(simplified, isClosed)) return simplified;

            return RoundedCornerLine.AddRoundedCorners(
                simplified,
                Mathf.Max(0f, radius),
                Mathf.Max(1, segments),
                isClosed
            );
        }

        public static Vector3[] SimplifyPath(IReadOnlyList<Vector3> path, bool isClosed)
        {
            if (path == null || path.Count == 0) return new Vector3[0];

            var deduped = new List<Vector3>(path.Count);
            for (var i = 0; i < path.Count; i++)
            {
                var point = path[i];
                if (deduped.Count == 0 || Vector3.Distance(deduped[deduped.Count - 1], point) > Epsilon)
                    deduped.Add(point);
            }

            if (isClosed && deduped.Count > 1 && Vector3.Distance(deduped[0], deduped[deduped.Count - 1]) <= Epsilon)
                deduped.RemoveAt(deduped.Count - 1);

            if (deduped.Count < 3) return deduped.ToArray();

            var result = new List<Vector3>(deduped.Count);
            for (var i = 0; i < deduped.Count; i++)
            {
                if (!isClosed && (i == 0 || i == deduped.Count - 1))
                {
                    result.Add(deduped[i]);
                    continue;
                }

                var previous = deduped[(i + deduped.Count - 1) % deduped.Count];
                var current = deduped[i];
                var next = deduped[(i + 1) % deduped.Count];
                var incoming = current - previous;
                var outgoing = next - current;

                if (incoming.sqrMagnitude <= Epsilon * Epsilon || outgoing.sqrMagnitude <= Epsilon * Epsilon)
                    continue;

                if (IsCollinearSameDirection(incoming, outgoing))
                    continue;

                result.Add(current);
            }

            return HasEnoughPoints(result, isClosed) ? result.ToArray() : deduped.ToArray();
        }

        public static Vector3[] CreatePointNormals(
            IReadOnlyList<Vector3> path,
            bool isClosed,
            Vector3 preferredNormal
        )
        {
            var count = path?.Count ?? 0;
            if (count == 0) return new Vector3[0];

            preferredNormal = preferredNormal == Vector3.zero ? Vector3.up : preferredNormal.normalized;
            var normals = new Vector3[count];

            var tangents = new Vector3[count];
            for (var i = 0; i < count; i++)
                tangents[i] = GetPointTangent(path, i, isClosed);

            normals[0] = GetPerpendicularNormal(preferredNormal, tangents[0]);
            for (var i = 1; i < count; i++)
            {
                normals[i] = Vector3.ProjectOnPlane(normals[i - 1], tangents[i]);
                if (normals[i].sqrMagnitude <= Epsilon * Epsilon)
                    normals[i] = GetPerpendicularNormal(preferredNormal, tangents[i]);
                else
                    normals[i].Normalize();
            }

            return normals;
        }

        private static void ConfigureSplineMesh(
            SplineMesh splineMesh,
            Mesh crossSection,
            Vector3 normal,
            int sectionCount,
            Vector3 customRotation,
            Vector3 customOffset,
            Vector3 customScale
        )
        {
            for (var i = splineMesh.GetChannelCount() - 1; i >= 0; i--)
                splineMesh.RemoveChannel(i);

            var channel = splineMesh.AddChannel(crossSection, ChannelName);
            channel.type = SplineMesh.Channel.Type.Extrude;
            channel.count = Mathf.Max(1, sectionCount);
            channel.autoCount = false;
            channel.overrideNormal = false;
            channel.customNormal = normal;

            var definition = channel.GetMesh(0);
            definition.rotation = customRotation;
            definition.offset = customOffset;
            definition.scale = customScale == Vector3.zero ? Vector3.one : customScale;
        }

        private Mesh GetCrossSectionMesh()
        {
            return uShapeCrossSection;
        }

        private static Vector3 GetPointTangent(IReadOnlyList<Vector3> path, int index, bool isClosed)
        {
            var count = path.Count;
            Vector3 tangent;

            if (isClosed)
                tangent = path[(index + 1) % count] - path[(index + count - 1) % count];
            else if (index == 0)
                tangent = path[1] - path[0];
            else if (index == count - 1)
                tangent = path[index] - path[index - 1];
            else
                tangent = path[index + 1] - path[index - 1];

            if (tangent.sqrMagnitude > Epsilon * Epsilon)
                return tangent.normalized;

            return Vector3.forward;
        }

        private static Vector3 GetPerpendicularNormal(Vector3 preferredNormal, Vector3 tangent)
        {
            var normal = Vector3.ProjectOnPlane(preferredNormal, tangent);
            if (normal.sqrMagnitude > Epsilon * Epsilon)
                return normal.normalized;

            var fallback = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) < 0.9f ? Vector3.up : Vector3.forward;
            normal = Vector3.ProjectOnPlane(fallback, tangent);
            return normal.sqrMagnitude > Epsilon * Epsilon ? normal.normalized : Vector3.right;
        }

        private static bool HasEnoughPoints(IReadOnlyCollection<Vector3> points, bool isClosed)
        {
            var count = points?.Count ?? 0;
            return isClosed ? count >= 3 : count >= 2;
        }

        private static int GetSectionCount(int roundedPathLength, bool isClosed)
        {
            return isClosed ? Mathf.Max(1, roundedPathLength) : Mathf.Max(1, roundedPathLength - 1);
        }

        private static bool IsCollinearSameDirection(Vector3 incoming, Vector3 outgoing)
        {
            var magnitudeProduct = incoming.magnitude * outgoing.magnitude;
            return magnitudeProduct > Epsilon
                   && Vector3.Cross(incoming, outgoing).magnitude <= Epsilon * magnitudeProduct
                   && Vector3.Dot(incoming, outgoing) > 0f;
        }

        private Vector3 GetNormal()
        {
            return splineNormal == Vector3.zero ? Vector3.up : splineNormal.normalized;
        }

        private Vector3 GetCustomMeshRotation()
        {
            if (customMeshUseMapTestPreset)
                return DefaultMapTestMeshRotation;

            return customMeshRotation;
        }

        private void OnDrawGizmos()
        {
            if (!showRoundedPathGizmos && !showCheckpointGizmo)
                return;

            var worldPath = GetRoundedPathWorld();
            if (worldPath.Length == 0)
                return;
            var length = CalculateLength(worldPath, closed);
            if (showRoundedPathGizmos)
            {
                Gizmos.color = pathColor;
                DrawPath(worldPath, closed);
            }

            var checkpoint = Vector3.zero;
            if (showCheckpointGizmo)
            {
                checkpoint = SamplePathAtPercent(worldPath, checkpointPercent, closed);
                Gizmos.color = checkpointColor;
                Gizmos.DrawSphere(checkpoint, checkpointRadius);
                Gizmos.DrawWireSphere(checkpoint, checkpointRadius * 1.35f);
            }

#if UNITY_EDITOR
            if (showDebugLabels && showRoundedPathGizmos)
            {
                var labelPosition = showCheckpointGizmo ? checkpoint : worldPath[0];
                Handles.color = showCheckpointGizmo ? checkpointColor : pathColor;
                Handles.Label(
                    labelPosition + Vector3.up * (checkpointRadius * 2f),
                    $"L={length:0.##} | P={checkpointPercent:0.##}"
                );
            }
#endif
        }

        private static void DrawPath(IReadOnlyList<Vector3> path, bool isClosed)
        {
            var count = path?.Count ?? 0;
            if (count < 2) return;

            for (var i = 0; i < count - 1; i++)
                Gizmos.DrawLine(path[i], path[i + 1]);

            if (isClosed)
                Gizmos.DrawLine(path[count - 1], path[0]);
        }
    }
}
