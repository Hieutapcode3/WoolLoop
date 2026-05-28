using System.Collections.Generic;
using Dreamteck.Splines;
using NgoUyenNguyen.Line;
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

#if UNITY_EDITOR
        private const string DefaultCrossSectionPath = "Assets/WoolLoop/Models/map_test.fbx";
        private const string DefaultCrossSectionName = "Cube";
#endif

        [SerializeField] private Vector3[] centerPaths = new Vector3[0];
        [SerializeField, Min(0f)] private float cornerRadius = 0.25f;
        [SerializeField, Min(1)] private int cornerSegments = 6;
        [SerializeField] private bool closed;
        [SerializeField] private Mesh uShapeCrossSection;
        [SerializeField] private Vector3 splineNormal = Vector3.up;

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

        private void Reset()
        {
            splineNormal = Vector3.up;
            TryAssignDefaultCrossSection();
        }

        private void OnValidate()
        {
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Max(1, cornerSegments);
            if (splineNormal == Vector3.zero) splineNormal = Vector3.up;
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

        public bool Build()
        {
            return Build(GetComponent<SplineComputer>(), GetComponent<SplineMesh>());
        }

        public bool Build(SplineComputer splineComputer, SplineMesh splineMesh)
        {
            if (splineComputer == null || splineMesh == null) return false;
            if (uShapeCrossSection == null) return false;

            var roundedPath = GetRoundedPath();
            if (!HasEnoughPoints(roundedPath, closed)) return false;

            var normal = GetNormal();
            var splinePoints = new SplinePoint[roundedPath.Length];
            for (var i = 0; i < roundedPath.Length; i++)
            {
                splinePoints[i] = new SplinePoint
                {
                    position = roundedPath[i],
                    normal = normal,
                    size = 1f,
                    color = Color.white
                };
            }

            splineComputer.type = Spline.Type.Linear;
            splineComputer.SetPoints(splinePoints);
            if (closed) splineComputer.Close();
            else splineComputer.Break();

            splineMesh.spline = splineComputer;
            ConfigureSplineMesh(splineMesh, uShapeCrossSection, normal);
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

        private static void ConfigureSplineMesh(SplineMesh splineMesh, Mesh crossSection, Vector3 normal)
        {
            for (var i = splineMesh.GetChannelCount() - 1; i >= 0; i--)
                splineMesh.RemoveChannel(i);

            var channel = splineMesh.AddChannel(crossSection, ChannelName);
            channel.type = SplineMesh.Channel.Type.Extrude;
            channel.count = 1;
            channel.autoCount = false;
            channel.overrideNormal = true;
            channel.customNormal = normal;
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
