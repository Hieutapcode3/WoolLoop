using System.Linq;
using BoardSpline.Runtime;
using Dreamteck.Splines;
using NUnit.Framework;
using UnityEngine;

public sealed class YarnConveyorEditorTests
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
    public void LevelJson_RoundTrip_PreservesYarnConveyor()
    {
        var level = new YarnBoardEditorWindow.YarnBoardLevelJson
        {
            levelId = "Level_Test",
            yarnConveyor = new YarnConveyorData
            {
                presetId = YarnConveyorEditorUtility.NoPresetId,
                loop = false,
                exits =
                {
                    new YarnConveyorExitData { percent = 0.35f, length = 1.25f },
                    new YarnConveyorExitData { percent = 0.7f, length = 0.5f }
                },
                cornerRadius = 0.5f,
                cornerSegments = 8,
                controlPoints =
                {
                    Vector3.zero,
                    Vector3.forward,
                    Vector3.forward + Vector3.right
                }
            }
        };

        var json = JsonUtility.ToJson(level, true);
        var restored = JsonUtility.FromJson<YarnBoardEditorWindow.YarnBoardLevelJson>(json);

        Assert.That(restored.yarnConveyor, Is.Not.Null);
        Assert.That(restored.yarnConveyor.presetId, Is.EqualTo(YarnConveyorEditorUtility.NoPresetId));
        Assert.That(restored.yarnConveyor.controlPoints.Count, Is.EqualTo(3));
        Assert.That(restored.yarnConveyor.exits.Count, Is.EqualTo(2));
        Assert.That(restored.yarnConveyor.exits[0].percent, Is.EqualTo(0.35f).Within(0.0001f));
        Assert.That(restored.yarnConveyor.exits[0].length, Is.EqualTo(1.25f).Within(0.0001f));
        Assert.That(restored.yarnConveyor.exits[1].percent, Is.EqualTo(0.7f).Within(0.0001f));
        Assert.That(restored.yarnConveyor.exits[1].length, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(restored.yarnConveyor.cornerRadius, Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(restored.yarnConveyor.cornerSegments, Is.EqualTo(8));
    }

    [Test]
    public void PresetChoices_DoNotExposeRemovedBuiltInPresets()
    {
        var choices = YarnConveyorEditorUtility.GetPresetChoices();

        Assert.That(choices, Has.Member(YarnConveyorEditorUtility.NoPresetId));
        Assert.That(choices, Has.No.Member("Circle Loop"));
        Assert.That(choices, Has.No.Member("Rounded Rectangle"));
        Assert.That(choices, Has.No.Member("S Curve"));
        Assert.That(choices, Has.No.Member("U Shape"));
        Assert.That(choices, Has.No.Member("Custom"));
    }

    [Test]
    public void NoPreset_DoesNotOverwriteExistingPath()
    {
        var data = new YarnConveyorData
        {
            controlPoints =
            {
                Vector3.zero,
                Vector3.forward
            }
        };

        YarnConveyorEditorUtility.ApplyPreset(data, YarnConveyorEditorUtility.NoPresetId);

        Assert.That(data.controlPoints.Count, Is.EqualTo(2));
        Assert.That(data.controlPoints[0], Is.EqualTo(Vector3.zero));
        Assert.That(data.controlPoints[1], Is.EqualTo(Vector3.forward));
    }

    [Test]
    public void UserPreset_SaveApplyAndDelete_RoundTripsPath()
    {
        var presetName = "TestPreset_" + System.Guid.NewGuid().ToString("N");
        var source = new YarnConveyorData
        {
            loop = true,
            exits =
            {
                new YarnConveyorExitData { percent = 0.25f, length = 1.5f },
                new YarnConveyorExitData { percent = 0.75f, length = 0.75f }
            },
            cornerRadius = 0.75f,
            cornerSegments = 5,
            controlPoints =
            {
                Vector3.zero,
                Vector3.forward,
                Vector3.forward + Vector3.right
            }
        };

        Assert.That(YarnConveyorEditorUtility.SaveCurrentAsPreset(presetName, source), Is.True);

        var restored = new YarnConveyorData();
        YarnConveyorEditorUtility.ApplyPreset(restored, YarnConveyorEditorUtility.UserPresetPrefix + presetName);

        Assert.That(restored.loop, Is.True);
        Assert.That(restored.exits.Count, Is.EqualTo(2));
        Assert.That(restored.exits[0].percent, Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(restored.exits[0].length, Is.EqualTo(1.5f).Within(0.0001f));
        Assert.That(restored.exits[1].percent, Is.EqualTo(0.75f).Within(0.0001f));
        Assert.That(restored.exits[1].length, Is.EqualTo(0.75f).Within(0.0001f));
        Assert.That(restored.cornerRadius, Is.EqualTo(0.75f).Within(0.0001f));
        Assert.That(restored.cornerSegments, Is.EqualTo(5));
        Assert.That(restored.controlPoints.SequenceEqual(source.controlPoints), Is.True);

        Assert.That(YarnConveyorEditorUtility.DeleteSavedPreset(YarnConveyorEditorUtility.UserPresetPrefix + presetName), Is.True);
    }

    [Test]
    public void Normalize_MigratesLegacySingleExitToExitList()
    {
        var data = new YarnConveyorData
        {
            hasExit = true,
            exitPercent = 0.4f
        };

        YarnConveyorEditorUtility.Normalize(data);

        Assert.That(data.exits.Count, Is.EqualTo(1));
        Assert.That(data.exits[0].percent, Is.EqualTo(0.4f).Within(0.0001f));
        Assert.That(data.exits[0].length, Is.EqualTo(0.5f).Within(0.0001f));
    }

    [Test]
    public void Validate_ReportsPathAndCornerErrorsWithoutBlockingEmptyDraftPath()
    {
        var empty = new YarnConveyorData();
        var emptyResult = YarnConveyorEditorUtility.Validate(empty, null);
        Assert.That(emptyResult.Errors, Is.Empty);
        Assert.That(emptyResult.Warnings, Has.Member("Yarn conveyor path is empty."));

        var invalid = new YarnConveyorData
        {
            loop = true,
            cornerRadius = -1f,
            cornerSegments = 0,
            exits =
            {
                new YarnConveyorExitData { percent = 0.5f, length = -1f }
            },
            controlPoints =
            {
                Vector3.zero,
                Vector3.forward
            }
        };

        var result = YarnConveyorEditorUtility.Validate(invalid, null);
        Assert.That(result.Errors, Has.Member("Loop yarn conveyor paths need at least 3 control points."));
        Assert.That(result.Errors, Has.Member("Yarn conveyor corner radius cannot be negative."));
        Assert.That(result.Errors, Has.Member("Yarn conveyor corner segments must be at least 1."));
        Assert.That(result.Errors, Has.Member("Yarn conveyor exit 1 length cannot be negative."));
    }

    [Test]
    public void ApplyToBuilder_ConfiguresCustomFrameBuilderAndBuilds()
    {
        var builder = CreateBuilder();
        builder.UShapeCrossSection = CreateSourceMesh();

        var data = new YarnConveyorData
        {
            loop = true,
            cornerRadius = 0.25f,
            cornerSegments = 4,
            controlPoints =
            {
                Vector3.zero,
                Vector3.forward,
                Vector3.forward + Vector3.right,
                Vector3.right
            }
        };

        Assert.That(YarnConveyorEditorUtility.ApplyToBuilder(data, builder), Is.True);
        Assert.That(builder.Closed, Is.True);
        Assert.That(builder.CornerRadius, Is.EqualTo(0.25f).Within(0.0001f));
        Assert.That(builder.CornerSegments, Is.EqualTo(4));
        Assert.That(builder.CenterPaths.SequenceEqual(data.controlPoints), Is.True);
        Assert.That(testObject.GetComponent<SplineComputer>().isClosed, Is.True);
        Assert.That(testObject.GetComponent<SplineMesh>().GetChannelCount(), Is.EqualTo(1));
    }

    private CustomFrameBuilder CreateBuilder()
    {
        testObject = new GameObject("Yarn Conveyor Editor Test");
        return testObject.AddComponent<CustomFrameBuilder>();
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
        testMesh.RecalculateNormals();
        testMesh.RecalculateBounds();
        return testMesh;
    }
}
