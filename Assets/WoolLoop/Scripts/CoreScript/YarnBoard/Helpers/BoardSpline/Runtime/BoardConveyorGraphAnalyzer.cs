using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BoardSpline.Runtime
{
    public static class BoardConveyorGraphAnalyzer
    {
        private struct EdgeRecord
        {
            public readonly int Index;
            public readonly int A;
            public readonly int B;

            public EdgeRecord(int index, int a, int b)
            {
                Index = index;
                A = a;
                B = b;
            }

            public int Other(int nodeId) => nodeId == A ? B : A;
        }

        public static List<BoardConveyorPath> Analyze(IBoardConveyorGraphData graphData)
        {
            var result = new List<BoardConveyorPath>();
            if (graphData?.Nodes == null || graphData.Edges == null) return result;

            var nodes = BuildNodeLookup(graphData.Nodes);
            var edges = BuildEdges(graphData.Edges, nodes);
            var adjacency = BuildAdjacency(edges);
            var visited = new HashSet<int>();

            foreach (var nodeId in GetBoundaryNodes(adjacency))
            {
                foreach (var edge in GetSortedEdges(adjacency, nodeId))
                {
                    if (visited.Contains(edge.Index)) continue;
                    result.Add(TraceFromBoundary(nodeId, edge, nodes, adjacency, visited));
                }
            }

            foreach (var edge in edges.OrderBy(edge => edge.Index))
            {
                if (visited.Contains(edge.Index)) continue;
                result.Add(TraceCycle(edge, nodes, adjacency, visited));
            }

            return result
                .Where(path => path.EdgeIndices.Count > 0 && path.CenterPaths.Count >= (path.Closed ? 3 : 2))
                .ToList();
        }

        private static Dictionary<int, BoardConveyorNode> BuildNodeLookup(IReadOnlyList<BoardConveyorNode> nodes)
        {
            var result = new Dictionary<int, BoardConveyorNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                if (!result.ContainsKey(nodes[i].id))
                    result.Add(nodes[i].id, nodes[i]);
            }

            return result;
        }

        private static List<EdgeRecord> BuildEdges(
            IReadOnlyList<BoardConveyorEdge> sourceEdges,
            IReadOnlyDictionary<int, BoardConveyorNode> nodes
        )
        {
            var result = new List<EdgeRecord>(sourceEdges.Count);
            for (var i = 0; i < sourceEdges.Count; i++)
            {
                var edge = sourceEdges[i];
                if (edge.fromId == edge.toId) continue;
                if (!nodes.ContainsKey(edge.fromId) || !nodes.ContainsKey(edge.toId)) continue;

                result.Add(new EdgeRecord(i, edge.fromId, edge.toId));
            }

            return result;
        }

        private static Dictionary<int, List<EdgeRecord>> BuildAdjacency(IReadOnlyList<EdgeRecord> edges)
        {
            var result = new Dictionary<int, List<EdgeRecord>>();
            for (var i = 0; i < edges.Count; i++)
            {
                Add(edges[i].A, edges[i]);
                Add(edges[i].B, edges[i]);
            }

            foreach (var pair in result)
                pair.Value.Sort(CompareEdgesFromNode(pair.Key));

            return result;

            void Add(int nodeId, EdgeRecord edge)
            {
                if (!result.TryGetValue(nodeId, out var nodeEdges))
                {
                    nodeEdges = new List<EdgeRecord>();
                    result.Add(nodeId, nodeEdges);
                }

                nodeEdges.Add(edge);
            }
        }

        private static IEnumerable<int> GetBoundaryNodes(Dictionary<int, List<EdgeRecord>> adjacency)
        {
            return adjacency
                .Where(pair => pair.Value.Count != 2)
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Key);
        }

        private static BoardConveyorPath TraceFromBoundary(
            int startNodeId,
            EdgeRecord startEdge,
            IReadOnlyDictionary<int, BoardConveyorNode> nodes,
            IReadOnlyDictionary<int, List<EdgeRecord>> adjacency,
            HashSet<int> visited
        )
        {
            var nodeIds = new List<int> { startNodeId };
            var edgeIndices = new List<int>();
            var currentNodeId = startNodeId;
            var currentEdge = startEdge;
            var closed = false;

            while (visited.Add(currentEdge.Index))
            {
                edgeIndices.Add(currentEdge.Index);
                var nextNodeId = currentEdge.Other(currentNodeId);
                nodeIds.Add(nextNodeId);

                if (nextNodeId == startNodeId && nodeIds.Count > 2)
                {
                    closed = true;
                    break;
                }

                if (IsBoundary(adjacency, nextNodeId))
                    break;

                if (!TryGetNextUnvisitedEdge(adjacency, nextNodeId, currentEdge.Index, visited, out var nextEdge))
                    break;

                currentNodeId = nextNodeId;
                currentEdge = nextEdge;
            }

            return CreatePath(nodeIds, edgeIndices, closed, nodes);
        }

        private static BoardConveyorPath TraceCycle(
            EdgeRecord seedEdge,
            IReadOnlyDictionary<int, BoardConveyorNode> nodes,
            IReadOnlyDictionary<int, List<EdgeRecord>> adjacency,
            HashSet<int> visited
        )
        {
            var componentEdges = GetUnvisitedComponent(seedEdge, adjacency, visited);
            var startNodeId = componentEdges
                .SelectMany(edge => new[] { edge.A, edge.B })
                .Distinct()
                .OrderBy(id => id)
                .First();
            var startEdge = GetSortedEdges(adjacency, startNodeId)
                .First(edge => componentEdges.Any(componentEdge => componentEdge.Index == edge.Index));

            var nodeIds = new List<int> { startNodeId };
            var edgeIndices = new List<int>();
            var currentNodeId = startNodeId;
            var currentEdge = startEdge;

            while (visited.Add(currentEdge.Index))
            {
                edgeIndices.Add(currentEdge.Index);
                var nextNodeId = currentEdge.Other(currentNodeId);
                nodeIds.Add(nextNodeId);

                if (nextNodeId == startNodeId)
                    break;

                if (!TryGetNextUnvisitedEdge(adjacency, nextNodeId, currentEdge.Index, visited, out var nextEdge))
                    break;

                currentNodeId = nextNodeId;
                currentEdge = nextEdge;
            }

            return CreatePath(nodeIds, edgeIndices, true, nodes);
        }

        private static List<EdgeRecord> GetUnvisitedComponent(
            EdgeRecord seedEdge,
            IReadOnlyDictionary<int, List<EdgeRecord>> adjacency,
            ISet<int> visited
        )
        {
            var result = new List<EdgeRecord>();
            var seenEdges = new HashSet<int>();
            var queue = new Queue<EdgeRecord>();
            queue.Enqueue(seedEdge);

            while (queue.Count > 0)
            {
                var edge = queue.Dequeue();
                if (visited.Contains(edge.Index) || !seenEdges.Add(edge.Index)) continue;

                result.Add(edge);
                EnqueueNeighbors(edge.A);
                EnqueueNeighbors(edge.B);
            }

            return result;

            void EnqueueNeighbors(int nodeId)
            {
                if (!adjacency.TryGetValue(nodeId, out var edges)) return;
                for (var i = 0; i < edges.Count; i++)
                {
                    if (!visited.Contains(edges[i].Index) && !seenEdges.Contains(edges[i].Index))
                        queue.Enqueue(edges[i]);
                }
            }
        }

        private static BoardConveyorPath CreatePath(
            List<int> nodeIds,
            List<int> edgeIndices,
            bool closed,
            IReadOnlyDictionary<int, BoardConveyorNode> nodes
        )
        {
            if (closed && nodeIds.Count > 1 && nodeIds[0] == nodeIds[nodeIds.Count - 1])
                nodeIds.RemoveAt(nodeIds.Count - 1);

            var positions = new List<Vector3>(nodeIds.Count);
            for (var i = 0; i < nodeIds.Count; i++)
            {
                if (nodes.TryGetValue(nodeIds[i], out var node))
                    positions.Add(node.position);
            }

            return new BoardConveyorPath(nodeIds, positions, edgeIndices, closed);
        }

        private static bool IsBoundary(IReadOnlyDictionary<int, List<EdgeRecord>> adjacency, int nodeId)
        {
            return !adjacency.TryGetValue(nodeId, out var edges) || edges.Count != 2;
        }

        private static IReadOnlyList<EdgeRecord> GetSortedEdges(
            IReadOnlyDictionary<int, List<EdgeRecord>> adjacency,
            int nodeId
        )
        {
            return adjacency.TryGetValue(nodeId, out var edges) ? edges : new List<EdgeRecord>();
        }

        private static bool TryGetNextUnvisitedEdge(
            IReadOnlyDictionary<int, List<EdgeRecord>> adjacency,
            int nodeId,
            int previousEdgeIndex,
            ISet<int> visited,
            out EdgeRecord result
        )
        {
            result = default;
            if (!adjacency.TryGetValue(nodeId, out var edges)) return false;

            for (var i = 0; i < edges.Count; i++)
            {
                if (edges[i].Index == previousEdgeIndex || visited.Contains(edges[i].Index)) continue;

                result = edges[i];
                return true;
            }

            return false;
        }

        private static System.Comparison<EdgeRecord> CompareEdgesFromNode(int nodeId)
        {
            return (a, b) =>
            {
                var neighborCompare = a.Other(nodeId).CompareTo(b.Other(nodeId));
                return neighborCompare != 0 ? neighborCompare : a.Index.CompareTo(b.Index);
            };
        }
    }
}
