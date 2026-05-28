using System.Collections.Generic;
using System.Linq;
using BoardSpline.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class BoardConveyorGraphAnalyzerTests
{
    [Test]
    public void Analyze_OpenChain_ReturnsOneOpenPath()
    {
        var paths = BoardConveyorGraphAnalyzer.Analyze(Graph(
            Nodes(1, 2, 3),
            Edges((1, 2), (2, 3))
        ));

        Assert.That(paths.Count, Is.EqualTo(1));
        Assert.That(paths[0].Closed, Is.False);
        Assert.That(paths[0].NodeIds, Is.EqualTo(new[] { 1, 2, 3 }));
        Assert.That(paths[0].CenterPaths.Count, Is.EqualTo(3));
    }

    [Test]
    public void Analyze_TJunction_ReturnsLanesBoundedAtJunction()
    {
        var paths = BoardConveyorGraphAnalyzer.Analyze(Graph(
            Nodes(0, 1, 2, 3),
            Edges((0, 1), (0, 2), (0, 3))
        ));

        Assert.That(paths.Count, Is.EqualTo(3));
        Assert.That(paths.All(path => !path.Closed), Is.True);
        Assert.That(paths.All(path => path.NodeIds.Count == 2), Is.True);
        Assert.That(paths.All(path => path.NodeIds.Contains(0)), Is.True);
        AssertEdgesUsedOnce(paths, 3);
    }

    [Test]
    public void Analyze_SquareCycle_ReturnsOneClosedLoop()
    {
        var paths = BoardConveyorGraphAnalyzer.Analyze(Graph(
            Nodes(0, 1, 2, 3),
            Edges((0, 1), (1, 2), (2, 3), (3, 0))
        ));

        Assert.That(paths.Count, Is.EqualTo(1));
        Assert.That(paths[0].Closed, Is.True);
        Assert.That(paths[0].NodeIds.Count, Is.EqualTo(4));
        Assert.That(paths[0].NodeIds[0], Is.EqualTo(0));
        AssertEdgesUsedOnce(paths, 4);
    }

    [Test]
    public void Analyze_JunctionToSameJunctionLoop_ReturnsClosedLoopAndBranch()
    {
        var paths = BoardConveyorGraphAnalyzer.Analyze(Graph(
            Nodes(0, 1, 2, 3),
            Edges((0, 1), (1, 2), (2, 0), (0, 3))
        ));

        Assert.That(paths.Count, Is.EqualTo(2));
        Assert.That(paths.Count(path => path.Closed), Is.EqualTo(1));
        Assert.That(paths.Count(path => !path.Closed), Is.EqualTo(1));
        Assert.That(paths.Single(path => path.Closed).NodeIds, Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(paths.Single(path => !path.Closed).NodeIds, Is.EqualTo(new[] { 0, 3 }));
        AssertEdgesUsedOnce(paths, 4);
    }

    private static SerializedBoardConveyorGraphData Graph(
        IEnumerable<BoardConveyorNode> nodes,
        IEnumerable<BoardConveyorEdge> edges
    )
    {
        var graph = new SerializedBoardConveyorGraphData();
        graph.SetData(nodes, edges);
        return graph;
    }

    private static IEnumerable<BoardConveyorNode> Nodes(params int[] ids)
    {
        for (var i = 0; i < ids.Length; i++)
            yield return new BoardConveyorNode(ids[i], new Vector3(ids[i], 0f, 0f));
    }

    private static IEnumerable<BoardConveyorEdge> Edges(params (int fromId, int toId)[] edges)
    {
        for (var i = 0; i < edges.Length; i++)
            yield return new BoardConveyorEdge(edges[i].fromId, edges[i].toId);
    }

    private static void AssertEdgesUsedOnce(IReadOnlyList<BoardConveyorPath> paths, int expectedEdgeCount)
    {
        var edgeIndices = paths.SelectMany(path => path.EdgeIndices).ToArray();
        Assert.That(edgeIndices.Length, Is.EqualTo(expectedEdgeCount));
        Assert.That(edgeIndices.Distinct().Count(), Is.EqualTo(expectedEdgeCount));
    }
}
