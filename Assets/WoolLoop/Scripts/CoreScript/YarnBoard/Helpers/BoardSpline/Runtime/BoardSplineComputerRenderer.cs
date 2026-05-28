using System.Collections.Generic;
using Dreamteck.Splines;
using NgoUyenNguyen.Line;
using UnityEngine;

namespace BoardSpline.Runtime
{
    public static class BoardSplineComputerRenderer
    {
        public static void Render(
            GameObject target,
            BoardSplineBuildData buildData,
            BoardSplineSettings settings
        )
        {
            if (target == null || buildData == null || buildData.BorderPointRegions.Count == 0) return;

            var outerRegion = GetLargestRegion(buildData.BorderPointRegions);
            if (outerRegion == null || outerRegion.Count == 0) return;

            var splineComputer = target.GetOrAddComponent<SplineComputer>();
            var splineMesh = target.GetComponent<SplineMesh>();
            Render(target, splineComputer, splineMesh, outerRegion, settings);
        }

        public static void Render(
            GameObject target,
            SplineComputer splineComputer,
            SplineMesh splineMesh,
            IReadOnlyList<BoardSplineBorderPoint> borderPoints,
            BoardSplineSettings settings
        )
        {
            if (target == null || splineComputer == null || borderPoints == null || borderPoints.Count == 0) return;

            var controlPoints = CreateControlPoints(borderPoints, settings);
            if (controlPoints.Length == 0) return;

            splineComputer.type = Spline.Type.Linear;
            splineComputer.SetPoints(controlPoints);
            splineComputer.Close();

            if (splineMesh != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(splineMesh);
                else
                    Object.DestroyImmediate(splineMesh);
            }

            var meshFilter = target.GetOrAddComponent<MeshFilter>();
            meshFilter.sharedMesh = CreateContinuousWallMesh(controlPoints, settings);
        }

        public static SplinePoint[] CreateControlPoints(
            IReadOnlyList<BoardSplineBorderPoint> borderPoints,
            BoardSplineSettings settings
        )
        {
            if (borderPoints == null || borderPoints.Count == 0) return new SplinePoint[0];

            var rawPoints = new Vector3[borderPoints.Count];
            var borderOffset = GetNormal(settings) * settings.wallHeight * 0.5f;

            for (var i = 0; i < borderPoints.Count; i++)
            {
                rawPoints[i] = borderPoints[i].Position
                               + borderOffset
                               + borderPoints[i].EmptyDirection * settings.borderPadding;
            }

            rawPoints = RemoveCollinearPoints(rawPoints);

            var roundedPoints = RoundedCornerLine.AddRoundedCorners(
                rawPoints,
                settings.borderCornerRadius,
                Mathf.Max(1, settings.borderSegmentCount),
                true
            );

            var controlPoints = new SplinePoint[roundedPoints.Length];
            for (var i = 0; i < roundedPoints.Length; i++)
            {
                controlPoints[i] = new SplinePoint
                {
                    position = roundedPoints[i],
                    normal = GetNormal(settings),
                    size = settings.borderWidth,
                };
            }

            return controlPoints;
        }

        private static Mesh CreateContinuousWallMesh(IReadOnlyList<SplinePoint> controlPoints, BoardSplineSettings settings)
        {
            var pointCount = controlPoints?.Count ?? 0;
            if (pointCount < 3) return null;

            var normal = GetNormal(settings);
            var width = Mathf.Max(0.001f, settings.borderWidth);
            var halfWidth = width * 0.5f;
            var halfHeight = Mathf.Max(0.001f, settings.wallHeight) * 0.5f;
            var polygonNormal = CalculatePolygonNormal(controlPoints);
            var outwardSign = Vector3.Dot(polygonNormal, normal) >= 0f ? 1f : -1f;

            var vertices = new Vector3[pointCount * 4];
            var uvs = new Vector2[vertices.Length];
            var outwards = new Vector3[pointCount];
            var cumulativeDistance = 0f;

            for (var i = 0; i < pointCount; i++)
            {
                var previous = controlPoints[(i + pointCount - 1) % pointCount].position;
                var current = controlPoints[i].position;
                var next = controlPoints[(i + 1) % pointCount].position;

                if (i > 0)
                    cumulativeDistance += Vector3.Distance(controlPoints[i - 1].position, current);

                var tangent = Vector3.ProjectOnPlane(next - previous, normal).normalized;
                if (tangent == Vector3.zero)
                    tangent = Vector3.ProjectOnPlane(next - current, normal).normalized;
                if (tangent == Vector3.zero)
                    tangent = Vector3.forward;

                var outward = Vector3.Cross(normal, tangent).normalized * outwardSign;
                outwards[i] = outward;
                var outer = current + outward * halfWidth;
                var inner = current - outward * halfWidth;
                var topOffset = normal * halfHeight;
                var bottomOffset = -normal * halfHeight;
                var vertexIndex = i * 4;

                vertices[vertexIndex] = outer + topOffset;
                vertices[vertexIndex + 1] = inner + topOffset;
                vertices[vertexIndex + 2] = outer + bottomOffset;
                vertices[vertexIndex + 3] = inner + bottomOffset;

                var u = cumulativeDistance / width;
                uvs[vertexIndex] = new Vector2(u, 1f);
                uvs[vertexIndex + 1] = new Vector2(u, 0f);
                uvs[vertexIndex + 2] = new Vector2(u, 1f);
                uvs[vertexIndex + 3] = new Vector2(u, 0f);
            }

            var triangles = new List<int>(pointCount * 24);
            for (var i = 0; i < pointCount; i++)
            {
                var next = (i + 1) % pointCount;
                var currentIndex = i * 4;
                var nextIndex = next * 4;
                var sideOutward = (outwards[i] + outwards[next]).normalized;
                if (sideOutward == Vector3.zero)
                    sideOutward = outwards[i];

                AddQuadFacing(vertices, triangles, currentIndex, nextIndex, nextIndex + 1, currentIndex + 1, normal);
                AddQuadFacing(vertices, triangles, currentIndex + 2, currentIndex + 3, nextIndex + 3, nextIndex + 2, -normal);
                AddQuadFacing(vertices, triangles, currentIndex, currentIndex + 2, nextIndex + 2, nextIndex, sideOutward);
                AddQuadFacing(vertices, triangles, currentIndex + 1, nextIndex + 1, nextIndex + 3, currentIndex + 3, -sideOutward);
            }

            var mesh = new Mesh
            {
                name = "Board Continuous Wall"
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 CalculatePolygonNormal(IReadOnlyList<SplinePoint> points)
        {
            var normal = Vector3.zero;
            for (var i = 0; i < points.Count; i++)
            {
                var current = points[i].position;
                var next = points[(i + 1) % points.Count].position;
                normal.x += (current.y - next.y) * (current.z + next.z);
                normal.y += (current.z - next.z) * (current.x + next.x);
                normal.z += (current.x - next.x) * (current.y + next.y);
            }

            return normal == Vector3.zero ? Vector3.up : normal.normalized;
        }

        private static void AddQuadFacing(
            IReadOnlyList<Vector3> vertices,
            List<int> triangles,
            int a,
            int b,
            int c,
            int d,
            Vector3 desiredNormal
        )
        {
            var faceNormal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            if (Vector3.Dot(faceNormal, desiredNormal) >= 0f)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(d);
                return;
            }

            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
            triangles.Add(a);
            triangles.Add(d);
            triangles.Add(c);
        }

        private static Vector3[] RemoveCollinearPoints(Vector3[] points, float epsilon = 0.001f)
        {
            if (points == null || points.Length < 3) return points;

            var result = new List<Vector3>(points.Length);
            for (var i = 0; i < points.Length; i++)
            {
                var previous = points[(i + points.Length - 1) % points.Length];
                var current = points[i];
                var next = points[(i + 1) % points.Length];

                var incoming = current - previous;
                var outgoing = next - current;
                var incomingSqrMagnitude = incoming.sqrMagnitude;
                var outgoingSqrMagnitude = outgoing.sqrMagnitude;

                if (incomingSqrMagnitude <= epsilon * epsilon || outgoingSqrMagnitude <= epsilon * epsilon)
                    continue;

                if (IsCollinearSameDirection(incoming, outgoing, epsilon))
                    continue;

                result.Add(current);
            }

            return result.Count >= 3 ? result.ToArray() : points;
        }

        private static bool IsCollinearSameDirection(Vector3 incoming, Vector3 outgoing, float epsilon)
        {
            var cross = Vector3.Cross(incoming, outgoing);
            var magnitudeProduct = incoming.magnitude * outgoing.magnitude;
            return cross.magnitude <= epsilon * magnitudeProduct
                   && Vector3.Dot(incoming, outgoing) > 0f;
        }

        private static IReadOnlyList<BoardSplineBorderPoint> GetLargestRegion(
            IReadOnlyList<List<BoardSplineBorderPoint>> regions
        )
        {
            List<BoardSplineBorderPoint> result = null;
            var largestCount = -1;
            for (var i = 0; i < regions.Count; i++)
            {
                if (regions[i] == null || regions[i].Count <= largestCount) continue;

                largestCount = regions[i].Count;
                result = regions[i];
            }

            return result;
        }

        private static Vector3 GetNormal(BoardSplineSettings settings) =>
            settings.splineNormal == Vector3.zero ? Vector3.up : settings.splineNormal.normalized;
    }
}
