using System.Collections.Generic;
using UnityEngine;

namespace BoardSpline.Runtime
{
    public static class BoardSplineAnalyzer
    {
        private static readonly Vector2Int[] CardinalIndices =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.right,
            Vector2Int.left,
        };

        public static BoardSplineBuildData Analyze(IBoardSplineDataAdapter adapter)
        {
            return new BoardSplineBuildData(
                GetBorderPointRegions(adapter),
                GetInnerEmptyPositions(adapter)
            );
        }

        public static List<List<BoardSplineBorderPoint>> GetBorderPointRegions(IBoardSplineDataAdapter adapter)
        {
            var result = new List<List<BoardSplineBorderPoint>>();
            var edges = GetBorderEdges(adapter);
            var visited = new HashSet<int>();

            while (visited.Count < edges.Count)
            {
                var startIndex = GetFirstUnvisited(edges.Count, visited);
                if (startIndex < 0) break;

                var regionEdges = new List<BoardSplineEdge>();
                var currentIndex = startIndex;

                while (currentIndex >= 0 && visited.Add(currentIndex))
                {
                    var current = edges[currentIndex];
                    regionEdges.Add(current);
                    currentIndex = FindNextEdgeIndex(edges, visited, current, adapter);
                }

                if (regionEdges.Count == 0) continue;
                result.Add(ToBorderPoints(regionEdges));
            }

            return result;
        }

        public static List<Vector3> GetInnerEmptyPositions(IBoardSplineDataAdapter adapter)
        {
            var result = new List<Vector3>();
            var emptyRegions = GetEmptyIndexRegions(adapter);

            foreach (var region in emptyRegions)
            {
                if (!IsInnerRegion(adapter.Size, region)) continue;

                foreach (var index in region)
                {
                    result.Add(adapter.IndexToWorld(index));
                }
            }

            return result;
        }

        private static List<BoardSplineEdge> GetBorderEdges(IBoardSplineDataAdapter adapter)
        {
            var edges = new List<BoardSplineEdge>();
            var right = adapter.Right.normalized;
            var forward = adapter.Forward.normalized;
            var halfSize = adapter.CellSize * 0.5f;

            for (var y = 0; y < adapter.Size.y; y++)
            {
                for (var x = 0; x < adapter.Size.x; x++)
                {
                    var index = new Vector2Int(x, y);
                    if (!adapter.HasTile(index)) continue;

                    var center = adapter.IndexToWorld(index);
                    AddEdgeIfEmpty(adapter, edges, index, Vector2Int.up,
                        center + halfSize * (forward - right),
                        center + halfSize * (forward + right),
                        forward);
                    AddEdgeIfEmpty(adapter, edges, index, Vector2Int.down,
                        center + halfSize * (-forward + right),
                        center + halfSize * (-forward - right),
                        -forward);
                    AddEdgeIfEmpty(adapter, edges, index, Vector2Int.right,
                        center + halfSize * (forward + right),
                        center + halfSize * (-forward + right),
                        right);
                    AddEdgeIfEmpty(adapter, edges, index, Vector2Int.left,
                        center + halfSize * (-forward - right),
                        center + halfSize * (forward - right),
                        -right);
                }
            }

            return edges;
        }

        private static void AddEdgeIfEmpty(
            IBoardSplineDataAdapter adapter,
            List<BoardSplineEdge> edges,
            Vector2Int index,
            Vector2Int neighborOffset,
            Vector3 start,
            Vector3 end,
            Vector3 emptyDirection
        )
        {
            if (adapter.HasTile(index + neighborOffset)) return;

            edges.Add(new BoardSplineEdge(
                new BoardSplineBorderPoint(start, emptyDirection),
                new BoardSplineBorderPoint(end, emptyDirection)
            ));
        }

        private static List<HashSet<Vector2Int>> GetEmptyIndexRegions(IBoardSplineDataAdapter adapter)
        {
            var result = new List<HashSet<Vector2Int>>();
            var visited = new HashSet<Vector2Int>();

            for (var y = 0; y < adapter.Size.y; y++)
            {
                for (var x = 0; x < adapter.Size.x; x++)
                {
                    var start = new Vector2Int(x, y);
                    if (adapter.HasTile(start) || !visited.Add(start)) continue;

                    var region = new HashSet<Vector2Int>();
                    var queue = new Queue<Vector2Int>();
                    queue.Enqueue(start);

                    while (queue.Count > 0)
                    {
                        var current = queue.Dequeue();
                        region.Add(current);

                        foreach (var offset in CardinalIndices)
                        {
                            var neighbor = current + offset;
                            if (!IsInBounds(adapter.Size, neighbor)) continue;
                            if (adapter.HasTile(neighbor) || !visited.Add(neighbor)) continue;

                            queue.Enqueue(neighbor);
                        }
                    }

                    result.Add(region);
                }
            }

            return result;
        }

        private static bool IsInnerRegion(Vector2Int size, HashSet<Vector2Int> region)
        {
            foreach (var index in region)
            {
                if (index.x == 0 || index.y == 0 || index.x == size.x - 1 || index.y == size.y - 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<BoardSplineBorderPoint> ToBorderPoints(List<BoardSplineEdge> regionEdges)
        {
            var points = new List<BoardSplineBorderPoint>(regionEdges.Count);
            for (var i = 0; i < regionEdges.Count; i++)
            {
                var current = regionEdges[i];
                var previous = regionEdges[(i + regionEdges.Count - 1) % regionEdges.Count];
                var point = current.Start;

                if (!Approximately(previous.End.EmptyDirection, current.Start.EmptyDirection))
                {
                    point.EmptyDirection += previous.End.EmptyDirection;
                }

                points.Add(point);
            }

            return points;
        }

        private static int FindNextEdgeIndex(
            List<BoardSplineEdge> edges,
            HashSet<int> visited,
            BoardSplineEdge current,
            IBoardSplineDataAdapter adapter
        )
        {
            var candidates = new List<int>();
            for (var i = 0; i < edges.Count; i++)
            {
                if (visited.Contains(i)) continue;
                if (Approximately(edges[i].Start.Position, current.End.Position))
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0) return -1;
            if (candidates.Count == 1) return candidates[0];

            return SelectRightHandCandidate(edges, candidates, current, adapter);
        }

        private static int SelectRightHandCandidate(
            List<BoardSplineEdge> edges,
            List<int> candidates,
            BoardSplineEdge current,
            IBoardSplineDataAdapter adapter
        )
        {
            var currentDirection = (current.End.Position - current.Start.Position).normalized;
            var normal = Vector3.Cross(adapter.Forward, adapter.Right).normalized;
            if (normal == Vector3.zero) normal = Vector3.up;

            var bestIndex = candidates[0];
            var bestScore = float.PositiveInfinity;
            foreach (var candidateIndex in candidates)
            {
                var candidate = edges[candidateIndex];
                var candidateDirection = (candidate.End.Position - candidate.Start.Position).normalized;
                var signedAngle = Vector3.SignedAngle(currentDirection, candidateDirection, normal);
                var score = GetTurnScore(signedAngle);

                if (score >= bestScore) continue;
                bestScore = score;
                bestIndex = candidateIndex;
            }

            return bestIndex;
        }

        private static float GetTurnScore(float signedAngle)
        {
            var angle = Mathf.Repeat(signedAngle + 360f, 360f);
            if (angle > 180f) angle -= 360f;

            if (Mathf.Approximately(angle, -90f)) return 0f;
            if (Mathf.Approximately(angle, 0f)) return 1f;
            if (Mathf.Approximately(angle, 90f)) return 2f;
            return 3f + Mathf.Abs(angle);
        }

        private static int GetFirstUnvisited(int count, HashSet<int> visited)
        {
            for (var i = 0; i < count; i++)
            {
                if (!visited.Contains(i)) return i;
            }

            return -1;
        }

        private static bool IsInBounds(Vector2Int size, Vector2Int index) =>
            index.x >= 0 && index.y >= 0 && index.x < size.x && index.y < size.y;

        private static bool Approximately(Vector3 a, Vector3 b) =>
            Mathf.Approximately(a.x, b.x)
            && Mathf.Approximately(a.y, b.y)
            && Mathf.Approximately(a.z, b.z);
    }
}
