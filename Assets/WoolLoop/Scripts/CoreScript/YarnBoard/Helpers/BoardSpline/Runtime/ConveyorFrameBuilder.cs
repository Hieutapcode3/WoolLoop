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
        public enum OrientationMode
        {
            Stable3D,
            FixedNormal
        }

        public enum CrossSectionSource
        {
            GeneratedUShape,
            CustomMesh
        }

        private const string ChannelName = "Conveyor Frame";
        private const float Epsilon = 0.001f;
        private const string DefaultCrossSectionName = "Cube";
        private static readonly Vector3 DefaultMapTestMeshRotation = new Vector3(0f, 90f, 0f);

#if UNITY_EDITOR
        private const string DefaultCrossSectionPath = "Assets/WoolLoop/Models/map_test.fbx";
#endif

        [SerializeField] private Vector3[] centerPaths = new Vector3[0];
        [SerializeField, Min(0f)] private float cornerRadius = 0.25f;
        [SerializeField, Min(1)] private int cornerSegments = 6;
        [SerializeField] private bool closed;
        [SerializeField] private OrientationMode orientationMode = OrientationMode.Stable3D;
        [SerializeField] private CrossSectionSource crossSectionSource = CrossSectionSource.GeneratedUShape;
        [SerializeField] private Mesh uShapeCrossSection;
        [SerializeField] private Vector3 splineNormal = Vector3.up;
        [SerializeField, Min(0.001f)] private float generatedWidth = 0.6f;
        [SerializeField, Min(0.001f)] private float generatedHeight = 0.35f;
        [SerializeField, Min(0.001f)] private float generatedWallThickness = 0.08f;
        [SerializeField, Min(0.001f)] private float generatedSectionLength = 0.2f;
        [SerializeField] private bool customMeshUseMapTestPreset = true;
        [SerializeField] private Vector3 customMeshRotation = DefaultMapTestMeshRotation;
        [SerializeField] private Vector3 customMeshOffset = Vector3.zero;
        [SerializeField] private Vector3 customMeshScale = Vector3.one;

        private Mesh generatedCrossSectionMesh;

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

        public OrientationMode Orientation
        {
            get => orientationMode;
            set => orientationMode = value;
        }

        public CrossSectionSource SectionSource
        {
            get => crossSectionSource;
            set => crossSectionSource = value;
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

        public float GeneratedWidth
        {
            get => generatedWidth;
            set => generatedWidth = Mathf.Max(0.001f, value);
        }

        public float GeneratedHeight
        {
            get => generatedHeight;
            set => generatedHeight = Mathf.Max(0.001f, value);
        }

        public float GeneratedWallThickness
        {
            get => generatedWallThickness;
            set => generatedWallThickness = Mathf.Max(0.001f, value);
        }

        public float GeneratedSectionLength
        {
            get => generatedSectionLength;
            set => generatedSectionLength = Mathf.Max(0.001f, value);
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
            orientationMode = OrientationMode.Stable3D;
            crossSectionSource = CrossSectionSource.GeneratedUShape;
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
            generatedWidth = Mathf.Max(0.001f, generatedWidth);
            generatedHeight = Mathf.Max(0.001f, generatedHeight);
            generatedWallThickness = Mathf.Max(0.001f, generatedWallThickness);
            generatedSectionLength = Mathf.Max(0.001f, generatedSectionLength);
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

            var normals = CreatePointNormals(roundedPath, closed, GetNormal(), orientationMode);
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
                orientationMode,
                crossSectionSource == CrossSectionSource.CustomMesh,
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

        public static Mesh CreateUShapeCrossSectionMesh(float width, float height, float wallThickness, float sectionLength)
        {
            width = Mathf.Max(0.001f, width);
            height = Mathf.Max(0.001f, height);
            wallThickness = Mathf.Clamp(wallThickness, 0.001f, Mathf.Min(width, height) * 0.49f);
            sectionLength = Mathf.Max(0.001f, sectionLength);

            var halfWidth = width * 0.5f;
            var halfLength = sectionLength * 0.5f;
            var outerMinX = -halfWidth;
            var outerMaxX = halfWidth;
            var innerMinX = -halfWidth + wallThickness;
            var innerMaxX = halfWidth - wallThickness;
            var bottomY = 0f;
            var innerBottomY = wallThickness;
            var topY = height;

            var mesh = new Mesh { name = "Generated Conveyor U-Shape" };
            var vertices = new List<Vector3>(48);
            var triangles = new List<int>(72);

            AddBox(vertices, triangles, outerMinX, innerMinX, bottomY, topY, -halfLength, halfLength);
            AddBox(vertices, triangles, innerMaxX, outerMaxX, bottomY, topY, -halfLength, halfLength);
            AddBox(vertices, triangles, innerMinX, innerMaxX, bottomY, innerBottomY, -halfLength, halfLength);

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Vector3[] CreatePointNormals(
            IReadOnlyList<Vector3> path,
            bool isClosed,
            Vector3 preferredNormal,
            OrientationMode mode
        )
        {
            var count = path?.Count ?? 0;
            if (count == 0) return new Vector3[0];

            preferredNormal = preferredNormal == Vector3.zero ? Vector3.up : preferredNormal.normalized;
            var normals = new Vector3[count];
            if (mode == OrientationMode.FixedNormal)
            {
                for (var i = 0; i < count; i++)
                    normals[i] = preferredNormal;
                return normals;
            }

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
            OrientationMode mode,
            bool applyCustomMeshTransform,
            Vector3 customRotation,
            Vector3 customOffset,
            Vector3 customScale
        )
        {
            for (var i = splineMesh.GetChannelCount() - 1; i >= 0; i--)
                splineMesh.RemoveChannel(i);

            var channel = splineMesh.AddChannel(crossSection, ChannelName);
            channel.type = SplineMesh.Channel.Type.Extrude;
            channel.count = 1;
            channel.autoCount = false;
            channel.overrideNormal = mode == OrientationMode.FixedNormal;
            channel.customNormal = normal;

            if (!applyCustomMeshTransform) return;

            var definition = channel.GetMesh(0);
            definition.rotation = customRotation;
            definition.offset = customOffset;
            definition.scale = customScale == Vector3.zero ? Vector3.one : customScale;
        }

        private Mesh GetCrossSectionMesh()
        {
            if (crossSectionSource == CrossSectionSource.CustomMesh && uShapeCrossSection != null)
                return uShapeCrossSection;

            if (generatedCrossSectionMesh != null)
                DestroyGeneratedCrossSectionMesh();

            generatedCrossSectionMesh = CreateUShapeCrossSectionMesh(
                generatedWidth,
                generatedHeight,
                generatedWallThickness,
                generatedSectionLength
            );
            return generatedCrossSectionMesh;
        }

        private void DestroyGeneratedCrossSectionMesh()
        {
            if (generatedCrossSectionMesh == null) return;

            if (Application.isPlaying)
                Destroy(generatedCrossSectionMesh);
            else
                DestroyImmediate(generatedCrossSectionMesh);

            generatedCrossSectionMesh = null;
        }

        private static void AddBox(
            ICollection<Vector3> vertices,
            ICollection<int> triangles,
            float minX,
            float maxX,
            float minY,
            float maxY,
            float minZ,
            float maxZ
        )
        {
            var start = vertices.Count;
            var boxVertices = new[]
            {
                new Vector3(minX, minY, minZ),
                new Vector3(maxX, minY, minZ),
                new Vector3(maxX, maxY, minZ),
                new Vector3(minX, maxY, minZ),
                new Vector3(minX, minY, maxZ),
                new Vector3(maxX, minY, maxZ),
                new Vector3(maxX, maxY, maxZ),
                new Vector3(minX, maxY, maxZ)
            };

            foreach (var vertex in boxVertices)
                vertices.Add(vertex);

            AddQuad(triangles, start, 0, 2, 1, 3);
            AddQuad(triangles, start, 4, 5, 6, 7);
            AddQuad(triangles, start, 0, 1, 5, 4);
            AddQuad(triangles, start, 1, 2, 6, 5);
            AddQuad(triangles, start, 2, 3, 7, 6);
            AddQuad(triangles, start, 3, 0, 4, 7);
        }

        private static void AddQuad(ICollection<int> triangles, int start, int a, int b, int c, int d)
        {
            triangles.Add(start + a);
            triangles.Add(start + b);
            triangles.Add(start + c);
            triangles.Add(start + a);
            triangles.Add(start + d);
            triangles.Add(start + b);
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
