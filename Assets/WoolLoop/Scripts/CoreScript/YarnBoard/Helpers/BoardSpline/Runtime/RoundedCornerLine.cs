using System.Collections.Generic;
using UnityEngine;

namespace NgoUyenNguyen.Line
{
    [RequireComponent(typeof(LineRenderer))]
    public class RoundedCornerLine : MonoBehaviour
    {
        [Header("Input Points")]
        public Vector3[] controlPoints = new Vector3[0];

        [Header("Corner Settings")]
        [Range(0, 1)] public float cornerRadius = 0.5f;
        public int cornerSegments = 6;

        private LineRenderer line;







        void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        private void Update()
        {
            UpdateLine();
        }

        public void UpdateLine()
        {
            Vector3[] rounded = AddRoundedCorners(controlPoints, cornerRadius, cornerSegments, line.loop);

            line.positionCount = rounded.Length;
            line.SetPositions(rounded);
        }

        public static Vector3[] AddRoundedCorners(Vector3[] pts, float radius, int segments, bool loop = false)
        {
            if (pts.Length < 3) return pts;

            var result = new List<Vector3>();
            const float epsilon = 0.001f;

            if (!loop) TryAdd(pts[0]);

            var count = pts.Length;
            var max = loop ? count : count - 1;

            for (var i = (loop ? 0 : 1); i < max; i++)
            {
                var prev = pts[(i - 1 + count) % count];
                var current = pts[i];
                var next = pts[(i + 1) % count];

                var dir1 = (prev - current).normalized;
                var dir2 = (next - current).normalized;

                var angleDeg = Vector3.Angle(dir1, dir2);

                if (angleDeg is > 179.9f or < 0.1f)
                {
                    TryAdd(current);

                    continue;
                }

                var halfAngle = (angleDeg * Mathf.Deg2Rad) / 2f;

                var cutDist = radius / Mathf.Tan(halfAngle);
                var len1 = Vector3.Distance(prev, current);
                var len2 = Vector3.Distance(next, current);

                var dist1 = Mathf.Min(cutDist, len1 * 0.5f);
                var dist2 = Mathf.Min(cutDist, len2 * 0.5f);

                var p1 = current + dir1 * dist1;
                var p2 = current + dir2 * dist2;

                var bisector = (dir1 + dir2).normalized;
                var offset = radius / Mathf.Sin(halfAngle);
                var center = current + bisector * offset;

                var startDir = (p1 - center).normalized;
                var endDir = (p2 - center).normalized;
                var normal = Vector3.Cross(dir1, dir2).normalized;

                var sweepAngle = Vector3.SignedAngle(startDir, endDir, normal);

                TryAdd(p1);
                for (var j = 1; j < segments; j++)
                {
                    var t = j / (float)segments;
                    var rot = Quaternion.AngleAxis(sweepAngle * t, normal);
                    var arcPoint = center + rot * startDir * radius;
                    TryAdd(arcPoint);
                }

                TryAdd(p2);
            }

            if (!loop) TryAdd(pts[^1]);
            else if (result.Count > 0 && Vector3.Distance(result[^1], result[0]) < epsilon)
            {
                result.RemoveAt(result.Count - 1);
            }

            return result.ToArray();

            void TryAdd(Vector3 point)
            {
                if (result.Count == 0 || Vector3.Distance(result[^1], point) > epsilon)
                {
                    result.Add(point);
                }
            }
        }
    }
}
