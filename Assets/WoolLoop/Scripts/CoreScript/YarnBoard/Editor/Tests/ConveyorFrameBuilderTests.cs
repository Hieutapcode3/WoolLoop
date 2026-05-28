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
        Assert.That(splineComputer.GetPoints().All(point => point.normal == Vector3.up), Is.True);
        Assert.That(splineMesh.GetChannelCount(), Is.EqualTo(1));

        var channel = splineMesh.GetChannel(0);
        Assert.That(channel.type, Is.EqualTo(SplineMesh.Channel.Type.Extrude));
        Assert.That(channel.count, Is.EqualTo(1));
        Assert.That(channel.autoCount, Is.False);
        Assert.That(channel.overrideNormal, Is.True);
        Assert.That(channel.customNormal, Is.EqualTo(Vector3.up));
        Assert.That(channel.GetMesh(0).mesh, Is.SameAs(sourceMesh));
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
    public void MapTest_DefaultCrossSectionMesh_IsCube()
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
}
