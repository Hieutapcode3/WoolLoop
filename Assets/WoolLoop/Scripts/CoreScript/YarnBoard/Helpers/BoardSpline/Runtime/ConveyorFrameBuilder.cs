using System.Collections.Generic;
using Dreamteck.Splines;
using NgoUyenNguyen.Line;
using Sirenix.OdinInspector;
using UnityEngine;

namespace BoardSpline.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineComputer))]
    [RequireComponent(typeof(SplineMesh))]
    public sealed class ConveyorFrameBuilder : MonoBehaviour
    {
        private const string ChannelName = "Conveyor Frame";
        private const float Epsilon = 0.001f;
        private const string DefaultCrossSectionName = "Cube";
        private static readonly Vector3 DefaultMapTestMeshRotation = new Vector3(0f, 90f, 0f);

#if UNITY_EDITOR
        private const string DefaultCrossSectionPath = "Assets/WoolLoop/Models/map_test.fbx";
#endif

        [TitleGroup("Input")]
        [SerializeField] private Vector3[] centerPaths = new Vector3[0];

        [TitleGroup("Input")]
        [SerializeField, Min(0f)] private float cornerRadius = 0.25f;

        [TitleGroup("Input")]
        [SerializeField, Min(1)] private int cornerSegments = 6;

        [TitleGroup("Input")]
        [SerializeField] private bool closed;

        [TitleGroup("Input")]
        [SerializeField] private Vector3 splineNormal = Vector3.up;

        [TitleGroup("Mesh Settings")]
        [SerializeField] private Mesh uShapeCrossSection;

        [TitleGroup("Mesh Settings")]
        [SerializeField] private bool customMeshUseMapTestPreset = true;

        [TitleGroup("Mesh Settings")]
        [SerializeField] private Vector3 customMeshRotation = DefaultMapTestMeshRotation;

        [TitleGroup("Mesh Settings")]
        [SerializeField] private Vector3 customMeshOffset = Vector3.zero;

        [TitleGroup("Mesh Settings")]
        [SerializeField] private Vector3 customMeshScale = Vector3.one;

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
            TryAssignDefaultCrossSection();
        }

        private void OnValidate()
        {
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Max(1, cornerSegments);
            if (splineNormal == Vector3.zero) splineNormal = Vector3.up;
            if (customMeshScale == Vector3.zero) customMeshScale = Vector3.one;
            TryAssignDefaultCrossSection();
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
            splineComputer.SetPoints(splinePoints);
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
            if (customMeshUseMapTestPreset && IsDefaultMapTestCrossSection(uShapeCrossSection))
                return DefaultMapTestMeshRotation;

            return customMeshRotation;
        }

        private static bool IsDefaultMapTestCrossSection(Mesh mesh)
        {
            if (mesh == null || mesh.name != DefaultCrossSectionName) return false;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.GetAssetPath(mesh) == DefaultCrossSectionPath;
#else
            return true;
#endif
        }

        private void TryAssignDefaultCrossSection()
        {
#if UNITY_EDITOR
            if (uShapeCrossSection != null) return;

            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(DefaultCrossSectionPath);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Mesh mesh && mesh.name == DefaultCrossSectionName)
                {
                    uShapeCrossSection = mesh;
                    return;
                }
            }
#endif
        }
    }
}
