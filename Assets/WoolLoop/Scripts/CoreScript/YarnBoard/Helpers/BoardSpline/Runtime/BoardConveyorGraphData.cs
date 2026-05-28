using System.Collections.Generic;
using UnityEngine;

namespace BoardSpline.Runtime
{
    [System.Serializable]
    public struct BoardConveyorNode
    {
        public int id;
        public Vector3 position;

        public BoardConveyorNode(int id, Vector3 position)
        {
            this.id = id;
            this.position = position;
        }
    }

    [System.Serializable]
    public struct BoardConveyorEdge
    {
        public int fromId;
        public int toId;

        public BoardConveyorEdge(int fromId, int toId)
        {
            this.fromId = fromId;
            this.toId = toId;
        }
    }

    public interface IBoardConveyorGraphData
    {
        IReadOnlyList<BoardConveyorNode> Nodes { get; }
        IReadOnlyList<BoardConveyorEdge> Edges { get; }
    }

    [System.Serializable]
    public sealed class SerializedBoardConveyorGraphData : IBoardConveyorGraphData
    {
        [SerializeField] private List<BoardConveyorNode> nodes = new List<BoardConveyorNode>();
        [SerializeField] private List<BoardConveyorEdge> edges = new List<BoardConveyorEdge>();

        public IReadOnlyList<BoardConveyorNode> Nodes => nodes;
        public IReadOnlyList<BoardConveyorEdge> Edges => edges;

        public List<BoardConveyorNode> MutableNodes => nodes;
        public List<BoardConveyorEdge> MutableEdges => edges;

        public void SetData(IEnumerable<BoardConveyorNode> newNodes, IEnumerable<BoardConveyorEdge> newEdges)
        {
            nodes = newNodes != null ? new List<BoardConveyorNode>(newNodes) : new List<BoardConveyorNode>();
            edges = newEdges != null ? new List<BoardConveyorEdge>(newEdges) : new List<BoardConveyorEdge>();
        }
    }

    public sealed class BoardConveyorPath
    {
        public readonly List<int> NodeIds;
        public readonly List<Vector3> CenterPaths;
        public readonly List<int> EdgeIndices;
        public readonly bool Closed;

        public BoardConveyorPath(
            List<int> nodeIds,
            List<Vector3> centerPaths,
            List<int> edgeIndices,
            bool closed
        )
        {
            NodeIds = nodeIds ?? new List<int>();
            CenterPaths = centerPaths ?? new List<Vector3>();
            EdgeIndices = edgeIndices ?? new List<int>();
            Closed = closed;
        }
    }
}
