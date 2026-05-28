using System.Linq;
using BoardSpline.Runtime;
using Dreamteck.Splines;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ConveyorFrameBuilderTests
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
    public void Build_OpenPath_ConfiguresLinearUnlockedSplineAndExtrudeChannel()
    {
        var builder = CreateBuilder();
        var sourceMesh = CreateSourceMesh();
        builder.UShapeCrossSection = sourceMesh;
        builder.SetPath(
            new[]
            {
                Vector3.zero,
                Vector3.forward,
                Vector3.forward * 2f,
                Vector3.forward * 2f + Vector3.right
            },
            false
        );

        Assert.That(builder.Build(), Is.True);

        var splineComputer = testObject.GetComponent<SplineComputer>();
        var splineMesh = testObject.GetComponent<SplineMesh>();
        Assert.That(splineComputer.type, Is.EqualTo(Spline.Type.Linear));
        Assert.That(splineComputer.isClosed, Is.False);
        Assert.That(splineComputer.GetPoints().All(point => Vector3.Dot(point.normal, Vector3.up) > 0.99f), Is.True);
        Assert.That(splineMesh.GetChannelCount(), Is.EqualTo(1));

        var channel = splineMesh.GetChannel(0);
        Assert.That(channel.type, Is.EqualTo(SplineMesh.Channel.Type.Extrude));
        Assert.That(channel.count, Is.EqualTo(Mathf.Max(1, builder.GetRoundedPath().Length - 1)));
        Assert.That(channel.autoCount, Is.False);
        Assert.That(channel.overrideNormal, Is.False);
        Assert.That(channel.customNormal, Is.EqualTo(Vector3.up));
        var meshDefinition = channel.GetMesh(0);
        Assert.That(meshDefinition.mesh, Is.SameAs(sourceMesh));
        Assert.That(meshDefinition.rotation, Is.EqualTo(new Vector3(0f, 90f, 0f)));
        Assert.That(meshDefinition.offset, Is.EqualTo(Vector3.zero));
        Assert.That(meshDefinition.scale, Is.EqualTo(Vector3.one));
        Assert.That(testObject.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
    }

    [Test]
    public void Build_ClosedPath_ClosesSpline()
    {
        var builder = CreateBuilder();
        builder.UShapeCrossSection = CreateSourceMesh();
        builder.SetPath(
            new[]
            {
                Vector3.zero,
                Vector3.forward,
                Vector3.forward + Vector3.right,
                Vector3.right
            },
            true
        );

        Assert.That(builder.Build(), Is.True);
        Assert.That(testObject.GetComponent<SplineComputer>().isClosed, Is.True);
    }

    [Test]
    public void Build_Stable3DPath_KeepsNormalsPerpendicularToVerticalSegment()
    {
        var builder = CreateBuilder();
        builder.UShapeCrossSection = CreateSourceMesh();
        builder.SetPath(
            new[]
            {
                Vector3.zero,
                Vector3.forward * 5f,
                Vector3.forward * 5f + Vector3.right * 3f,
                Vector3.forward * 5f + Vector3.right * 3f + Vector3.up * 3f
            },
            false
        );
        builder.CornerRadius = 1f;
        builder.CornerSegments = 6;

        Assert.That(builder.Build(), Is.True);

        var points = testObject.GetComponent<SplineComputer>().GetPoints();
        Assert.That(points.Length, Is.EqualTo(16));
        for (var i = 0; i < points.Length; i++)
        {
            var tangent = GetPointTangent(points, i);
            Assert.That(Mathf.Abs(Vector3.Dot(points[i].normal.normalized, tangent)), Is.LessThan(0.001f));
        }

        var channel = testObject.GetComponent<SplineMesh>().GetChannel(0);
        Assert.That(channel.overrideNormal, Is.False);
        Assert.That(testObject.GetComponent<MeshFilter>().sharedMesh.vertexCount, Is.GreaterThan(0));
    }

    [Test]
    public void CreateRoundedPath_RemovesDuplicateAndCollinearPoints()
    {
        var rounded = ConveyorFrameBuilder.CreateRoundedPath(
            new[]
            {
                Vector3.zero,
                Vector3.forward,
                Vector3.forward,
                Vector3.forward * 2f,
                Vector3.forward * 2f + Vector3.right
            },
            0.25f,
            4,
            false
        );

        Assert.That(rounded.First(), Is.EqualTo(Vector3.zero));
        Assert.That(rounded.Last(), Is.EqualTo(Vector3.forward * 2f + Vector3.right));
        Assert.That(rounded.Length, Is.GreaterThan(3));
    }

    [Test]
    public void Build_MapTestCustomMeshPreset_AppliesAxisRemapRotation()
    {
        var builder = CreateBuilder();
        var mesh = AssetDatabase
            .LoadAllAssetsAtPath("Assets/WoolLoop/Models/map_test.fbx")
            .OfType<Mesh>()
            .FirstOrDefault(asset => asset.name == "Cube");
        Assert.That(mesh, Is.Not.Null);

        builder.CustomMeshUseMapTestPreset = true;
        builder.UShapeCrossSection = mesh;
        builder.SetPath(new[] { Vector3.zero, Vector3.forward }, false);

        Assert.That(builder.Build(), Is.True);

        var meshDefinition = testObject.GetComponent<SplineMesh>().GetChannel(0).GetMesh(0);
        Assert.That(meshDefinition.mesh, Is.SameAs(mesh));
        Assert.That(meshDefinition.rotation, Is.EqualTo(new Vector3(0f, 90f, 0f)));
        Assert.That(meshDefinition.offset, Is.EqualTo(Vector3.zero));
        Assert.That(meshDefinition.scale, Is.EqualTo(Vector3.one));
    }

    [Test]
    public void Build_CustomMeshCurvedOpenPath_UsesRoundedSegmentCount()
    {
        var builder = CreateBuilder();
        builder.UShapeCrossSection = CreateSourceMesh();
        builder.CornerRadius = 0.25f;
        builder.CornerSegments = 4;
        builder.SetPath(
            new[]
            {
                Vector3.zero,
                Vector3.forward,
                Vector3.forward + Vector3.right
            },
            false
        );

        Assert.That(builder.Build(), Is.True);

        var expected = Mathf.Max(1, builder.GetRoundedPath().Length - 1);
        Assert.That(testObject.GetComponent<SplineMesh>().GetChannel(0).count, Is.EqualTo(expected));
        Assert.That(expected, Is.GreaterThan(1));
    }

    [Test]
    public void Build_CustomMeshClosedLoop_UsesRoundedPathLength()
    {
        var builder = CreateBuilder();
        builder.UShapeCrossSection = CreateSourceMesh();
        builder.CornerRadius = 0.25f;
        builder.CornerSegments = 4;
        builder.SetPath(
            new[]
            {
                Vector3.zero,
                Vector3.forward,
                Vector3.forward + Vector3.right,
                Vector3.right
            },
            true
        );

        Assert.That(builder.Build(), Is.True);

        var expected = Mathf.Max(1, builder.GetRoundedPath().Length);
        Assert.That(testObject.GetComponent<SplineMesh>().GetChannel(0).count, Is.EqualTo(expected));
    }

    [Test]
    public void MapTest_LegacyDefaultCrossSectionMesh_IsCube()
    {
        var mesh = AssetDatabase
            .LoadAllAssetsAtPath("Assets/WoolLoop/Models/map_test.fbx")
            .OfType<Mesh>()
            .FirstOrDefault(asset => asset.name == "Cube");

        Assert.That(mesh, Is.Not.Null);
        Assert.That(mesh.vertexCount, Is.GreaterThan(0));
    }

    private ConveyorFrameBuilder CreateBuilder()
    {
        testObject = new GameObject("Conveyor Frame Builder Test");
        return testObject.AddComponent<ConveyorFrameBuilder>();
    }

    private Mesh CreateSourceMesh()
    {
        testMesh = new Mesh { name = "Test Conveyor Cross Section" };
        testMesh.vertices = new[]
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
        testMesh.triangles = new[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            2, 3, 7, 2, 7, 6,
            3, 0, 4, 3, 4, 7
        };
        testMesh.uv = new[]
        {
            Vector2.zero,
            Vector2.right,
            Vector2.one,
            Vector2.up,
            Vector2.zero,
            Vector2.right,
            Vector2.one,
            Vector2.up
        };
        testMesh.RecalculateNormals();
        testMesh.RecalculateBounds();
        return testMesh;
    }

    private static Vector3 GetPointTangent(SplinePoint[] points, int index)
    {
        Vector3 tangent;
        if (index == 0)
            tangent = points[1].position - points[0].position;
        else if (index == points.Length - 1)
            tangent = points[index].position - points[index - 1].position;
        else
            tangent = points[index + 1].position - points[index - 1].position;

        return tangent.normalized;
    }
}
