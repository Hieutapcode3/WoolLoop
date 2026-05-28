using System.Collections.Generic;
using BoardSpline.Runtime;
using Dreamteck.Splines;
using NUnit.Framework;
using UnityEngine;

public sealed class BoardConveyorLayoutBuilderTests
{
    private GameObject testObject;
    private Mesh testMesh;

    [TearDown]
    public void TearDown()
    {
        if (testObject != null) Object.DestroyImmediate(testObject);
        if (testMesh != null) Object.DestroyImmediate(testMesh);
    }

    [Test]
    public void BuildConveyors_TJunction_GeneratedChildCountMatchesAnalyzedPaths()
    {
        var builder = CreateBuilder();
        var graph = Graph(
            Nodes(
                (0, Vector3.zero),
                (1, Vector3.left),
                (2, Vector3.right),
                (3, Vector3.forward)
            ),
            Edges((0, 1), (0, 2), (0, 3))
        );

        var conveyors = builder.BuildConveyors(graph, true);

        Assert.That(conveyors.Count, Is.EqualTo(3));
        Assert.That(testObject.transform.childCount, Is.EqualTo(3));
        for (var i = 0; i < conveyors.Count; i++)
        {
            Assert.That(conveyors[i].CenterPaths.Length, Is.EqualTo(2));
            Assert.That(conveyors[i].Closed, Is.False);
            Assert.That(conveyors[i].UShapeCrossSection, Is.SameAs(testMesh));
        }
    }

    [Test]
    public void BuildConveyors_CurvedCustomMeshPath_SetsChannelCountFromRoundedPath()
    {
        var builder = CreateBuilder();
        builder.CornerRadius = 0.25f;
        builder.CornerSegments = 4;
        var graph = Graph(
            Nodes(
                (0, Vector3.zero),
                (1, Vector3.forward),
                (2, Vector3.forward + Vector3.right)
            ),
            Edges((0, 1), (1, 2))
        );

        var conveyors = builder.BuildConveyors(graph, true);

        Assert.That(conveyors.Count, Is.EqualTo(1));
        var conveyor = conveyors[0];
        var expected = Mathf.Max(1, conveyor.GetRoundedPath().Length - 1);
        Assert.That(conveyor.GetComponent<SplineMesh>().GetChannel(0).count, Is.EqualTo(expected));
        Assert.That(expected, Is.GreaterThan(1));
    }

    [Test]
    public void BuildConveyors_ClosedLoop_UsesFullRoundedPathLengthForChannelCount()
    {
        var builder = CreateBuilder();
        builder.CornerRadius = 0.25f;
        builder.CornerSegments = 4;
        var graph = Graph(
            Nodes(
                (0, Vector3.zero),
                (1, Vector3.forward),
                (2, Vector3.forward + Vector3.right),
                (3, Vector3.right)
            ),
            Edges((0, 1), (1, 2), (2, 3), (3, 0))
        );

        var conveyors = builder.BuildConveyors(graph, true);

        Assert.That(conveyors.Count, Is.EqualTo(1));
        var conveyor = conveyors[0];
        Assert.That(conveyor.Closed, Is.True);
        var expected = Mathf.Max(1, conveyor.GetRoundedPath().Length);
        Assert.That(conveyor.GetComponent<SplineMesh>().GetChannel(0).count, Is.EqualTo(expected));
    }

    private BoardConveyorLayoutBuilder CreateBuilder()
    {
        testObject = new GameObject("Board Conveyor Layout Builder Test");
        testMesh = CreateSourceMesh();

        var builder = testObject.AddComponent<BoardConveyorLayoutBuilder>();
        builder.UShapeCrossSection = testMesh;
        builder.CustomMeshRotation = new Vector3(0f, 90f, 0f);
        builder.CustomMeshOffset = Vector3.zero;
        builder.CustomMeshScale = Vector3.one;
        return builder;
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

    private static IEnumerable<BoardConveyorNode> Nodes(params (int id, Vector3 position)[] nodes)
    {
        for (var i = 0; i < nodes.Length; i++)
            yield return new BoardConveyorNode(nodes[i].id, nodes[i].position);
    }

    private static IEnumerable<BoardConveyorEdge> Edges(params (int fromId, int toId)[] edges)
    {
        for (var i = 0; i < edges.Length; i++)
            yield return new BoardConveyorEdge(edges[i].fromId, edges[i].toId);
    }

    private Mesh CreateSourceMesh()
    {
        var mesh = new Mesh { name = "Test Conveyor Cross Section" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.1f, 0f),
            new Vector3(0.5f, -0.1f, 0f),
            new Vector3(0.5f, 0.1f, 0f),
            new Vector3(-0.5f, 0.1f, 0f),
            new Vector3(-0.5f, -0.1f, 1f),
            new Vector3(0.5f, -0.1f, 1f),
            new Vector3(0.5f, 0.1f, 1f),
            new Vector3(-0.5f, 0.1f, 1f)
        };
        mesh.triangles = new[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            2, 3, 7, 2, 7, 6,
            3, 0, 4, 3, 4, 7
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
