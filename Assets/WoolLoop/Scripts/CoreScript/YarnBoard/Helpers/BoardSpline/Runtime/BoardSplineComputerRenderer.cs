using System.Collections.Generic;
using Dreamteck.Splines;
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
            var splineMesh = target.GetOrAddComponent<SplineMesh>();
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

            splineComputer.SetPoints(controlPoints);
            splineComputer.Close();

            if (splineMesh == null) return;

            splineMesh.spline = splineComputer;
            var channel = splineMesh.GetChannel(0);
            channel.count = Mathf.Max(0, controlPoints.Length - 1);
            channel.GetMesh(0).removeInnerFaces = settings.removeInnerFaces;
        }

        public static SplinePoint[] CreateControlPoints(
            IReadOnlyList<BoardSplineBorderPoint> borderPoints,
            BoardSplineSettings settings
        )
        {
            if (borderPoints == null || borderPoints.Count == 0) return new SplinePoint[0];

            var rawPoints = new Vector3[borderPoints.Count];
            var borderOffset = GetNormal(settings) * settings.borderWidth * 0.5f;

            for (var i = 0; i < borderPoints.Count; i++)
            {
                rawPoints[i] = borderPoints[i].Position
                               + borderOffset
                               + borderPoints[i].EmptyDirection * settings.borderPadding;
            }

            var roundedPoints = AddRoundedCorners(
                rawPoints,
                settings.borderCornerRadius,
                Mathf.Max(1, settings.borderSegmentCount)
            );

            var controlPoints = new SplinePoint[roundedPoints.Count];
            for (var i = 0; i < roundedPoints.Count; i++)
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

        private static List<Vector3> AddRoundedCorners(Vector3[] points, float radius, int segmentCount)
        {
            var result = new List<Vector3>();
            if (points == null || points.Length == 0) return result;
            if (points.Length < 3 || radius <= 0f)
            {
                result.AddRange(points);
                return result;
            }

            for (var i = 0; i < points.Length; i++)
            {
                var previous = points[(i + points.Length - 1) % points.Length];
                var current = points[i];
                var next = points[(i + 1) % points.Length];

                var toPrevious = previous - current;
                var toNext = next - current;
                var previousDistance = toPrevious.magnitude;
                var nextDistance = toNext.magnitude;

                if (previousDistance <= Mathf.Epsilon || nextDistance <= Mathf.Epsilon)
                {
                    result.Add(current);
                    continue;
                }

                var cornerDistance = Mathf.Min(radius, previousDistance * 0.5f, nextDistance * 0.5f);
                var start = current + toPrevious.normalized * cornerDistance;
                var end = current + toNext.normalized * cornerDistance;

                result.Add(start);
                for (var segment = 1; segment <= segmentCount; segment++)
                {
                    var t = segment / (float)(segmentCount + 1);
                    result.Add(QuadraticBezier(start, current, end, t));
                }
                result.Add(end);
            }

            return result;
        }

        private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * a
                   + 2f * oneMinusT * t * b
                   + t * t * c;
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
