using System.Collections.Generic;
using BoardSpline.Runtime;
using NUnit.Framework;
using UnityEngine;

public sealed class WoolBallMovementTests
{
    private readonly List<GameObject> createdObjects = new();

    [TearDown]
    public void TearDown()
    {
        for (var i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void MoveTo_SingleTileBall_ReachesReachableTarget()
    {
        var context = CreateContext(3, 1);
        var ball = CreateBall(context, 1, Tile(0, 0));

        var moved = ball.MoveTo(Tile(2, 0)).GetAwaiter().GetResult();

        Assert.That(moved, Is.True);
        Assert.That(ball.Data.tileId, Is.EqualTo(Tile(2, 0)));
    }

    [Test]
    public void MoveTo_BlockedPath_ReturnsFalseWithoutMutation()
    {
        var context = CreateContext(3, 1, Tile(0, 0), Tile(2, 0));
        var ball = CreateBall(context, 1, Tile(0, 0));

        var moved = ball.MoveTo(Tile(2, 0)).GetAwaiter().GetResult();

        Assert.That(moved, Is.False);
        Assert.That(ball.Data.tileId, Is.EqualTo(Tile(0, 0)));
        Assert.That(ball.Data.childrenTileIds, Is.Empty);
    }

    [Test]
    public void MoveTo_MultiTileBall_ChoosesShortestTailEndpoint()
    {
        var context = CreateContext(5, 1);
        var ball = CreateBall(context, 1, Tile(1, 0), Tile(2, 0), Tile(3, 0));

        var moved = ball.MoveTo(Tile(4, 0)).GetAwaiter().GetResult();

        Assert.That(moved, Is.True);
        AssertTiles(ball, Tile(2, 0), Tile(3, 0), Tile(4, 0));
    }

    [Test]
    public void MoveTo_EqualEndpointCosts_ChoosesRootEndpoint()
    {
        var context = CreateContext(3, 3);
        var ball = CreateBall(context, 1, Tile(0, 1), Tile(1, 1), Tile(2, 1));

        var moved = ball.MoveTo(Tile(1, 2)).GetAwaiter().GetResult();

        Assert.That(moved, Is.True);
        AssertTiles(ball, Tile(1, 2), Tile(0, 2), Tile(0, 1));
    }

    [Test]
    public void CanMoveTo_OtherWoolBallOccupancy_BlocksPath()
    {
        var context = CreateContext(3, 1);
        var ball = CreateBall(context, 1, Tile(0, 0));
        CreateBall(context, 2, Tile(1, 0));

        Assert.That(ball.CanMoveTo(Tile(2, 0)), Is.False);
    }

    [Test]
    public void CanMoveTo_OpenPathFromEndpoint_RemainsValidWhenOwnBodyExists()
    {
        var context = CreateContext(5, 1);
        var ball = CreateBall(context, 1, Tile(1, 0), Tile(2, 0), Tile(3, 0));

        Assert.That(ball.CanMoveTo(Tile(4, 0)), Is.True);
    }

    [Test]
    public void CanMoveTo_TargetInsideOwnBody_ReturnsFalse()
    {
        var context = CreateContext(5, 1);
        var ball = CreateBall(context, 1, Tile(1, 0), Tile(2, 0), Tile(3, 0));

        Assert.That(ball.CanMoveTo(Tile(2, 0)), Is.False);
    }

    [Test]
    public void CanMoveTo_ReservedTargetByOtherBall_ReturnsFalse()
    {
        var context = CreateContext(3, 2);
        var first = CreateBall(context, 1, Tile(0, 0));
        var second = CreateBall(context, 2, Tile(0, 1));

        Assert.That(context.State.TryReserve(first, Tile(2, 0)), Is.True);

        Assert.That(second.CanMoveTo(Tile(2, 0)), Is.False);
    }

    [Test]
    public void MoveTo_FromRootEndpoint_AppliesSnakeFollowOrder()
    {
        var context = CreateContext(5, 1);
        var ball = CreateBall(context, 1, Tile(3, 0), Tile(2, 0), Tile(1, 0));

        var moved = ball.MoveTo(Tile(4, 0)).GetAwaiter().GetResult();

        Assert.That(moved, Is.True);
        AssertTiles(ball, Tile(4, 0), Tile(3, 0), Tile(2, 0));
    }

    [Test]
    public void MoveTo_FromTailEndpoint_AppliesSnakeFollowOrder()
    {
        var context = CreateContext(5, 1);
        var ball = CreateBall(context, 1, Tile(1, 0), Tile(2, 0), Tile(3, 0));

        var moved = ball.MoveTo(Tile(4, 0)).GetAwaiter().GetResult();

        Assert.That(moved, Is.True);
        AssertTiles(ball, Tile(2, 0), Tile(3, 0), Tile(4, 0));
    }

    [Test]
    public void Complete_DisablesMovementAndReleasesOccupancy()
    {
        var context = CreateContext(3, 1);
        var completed = CreateBall(context, 1, Tile(0, 0));
        var other = CreateBall(context, 2, Tile(2, 0));

        completed.Complete();

        Assert.That(completed.IsCompleted, Is.True);
        Assert.That(completed.IsPendingCleanup, Is.True);
        Assert.That(completed.CanMoveTo(Tile(1, 0)), Is.False);
        Assert.That(other.CanMoveTo(Tile(0, 0)), Is.True);
        Assert.That(completed.GetComponents<BoxCollider>(), Has.All.Matches<BoxCollider>(collider => !collider.enabled));
        Assert.That(completed.Visual.VisiblePieceCount, Is.Zero);
    }

    [Test]
    public void OnCreated_AddsInteractionCollidersToWoolBallDomain()
    {
        var context = CreateContext(3, 1);
        var ball = CreateBall(context, 1, Tile(0, 0), Tile(1, 0), Tile(2, 0));

        var colliders = ball.GetComponents<BoxCollider>();

        Assert.That(colliders.Length, Is.EqualTo(3));
        Assert.That(colliders, Has.All.Matches<BoxCollider>(collider => collider.enabled));
    }

    [Test]
    public void OnCreated_DoesNotAddInteractionComponentsToVisualPieces()
    {
        var context = CreateContext(3, 1);
        var ball = CreateBall(context, 1, Tile(0, 0), Tile(1, 0));

        foreach (var pieceTransform in ball.Visual.PieceTransforms)
        {
            Assert.That(pieceTransform.GetComponent<BoxCollider>(), Is.Null);
            Assert.That(pieceTransform.GetComponent("WoolBallPieceInput"), Is.Null);
        }
    }

    [Test]
    public void MoveToTarget_NoConfiguredTarget_ReturnsFalseWithoutMutation()
    {
        var context = CreateContext(3, 1);
        var ball = CreateBall(context, 1, Tile(0, 0));

        var moved = ball.MoveToTarget().GetAwaiter().GetResult();

        Assert.That(moved, Is.False);
        Assert.That(ball.Data.tileId, Is.EqualTo(Tile(0, 0)));
    }

    [Test]
    public void MoveToTarget_ConfiguredReachableTarget_MovesToTarget()
    {
        var context = CreateContext(3, 1);
        var ball = CreateBall(context, 1, Tile(0, 0));
        context.Level.hasTargetExitTileId = true;
        context.Level.targetExitTileId = Tile(2, 0);

        var moved = ball.MoveToTarget().GetAwaiter().GetResult();

        Assert.That(moved, Is.True);
        Assert.That(ball.Data.tileId, Is.EqualTo(Tile(2, 0)));
    }

    [Test]
    public void MoveToTarget_Level001Ball5_DoesNotChooseTailThroughOwnBodyAfterBall3Completes()
    {
        var context = CreateContext(
            7,
            7,
            Tile(2, 1), Tile(3, 1), Tile(4, 1),
            Tile(1, 2), Tile(2, 2), Tile(3, 2), Tile(4, 2), Tile(5, 2),
            Tile(1, 3), Tile(2, 3), Tile(5, 3),
            Tile(1, 4), Tile(2, 4), Tile(3, 4), Tile(4, 4), Tile(5, 4),
            Tile(2, 5), Tile(3, 5), Tile(4, 5),
            Tile(3, 6)
        );
        context.Level.hasTargetExitTileId = true;
        context.Level.targetExitTileId = Tile(3, 6);

        var ball3 = CreateBall(context, 3, Tile(3, 2), Tile(4, 2), Tile(5, 2));
        var ball5 = CreateBall(context, 5, Tile(2, 1), Tile(2, 2), Tile(1, 2));
        CreateBall(context, 6, Tile(3, 4), Tile(2, 4), Tile(2, 5));
        ball3.Complete();

        var moved = ball5.MoveToTarget().GetAwaiter().GetResult();

        Assert.That(moved, Is.True);
        Assert.That(ball5.Data.tileId, Is.EqualTo(Tile(3, 6)));
        Assert.That(ball5.Data.childrenTileIds, Has.No.EqualTo(Tile(2, 2)));
    }

    [Test]
    public void DispatchUnitCount_UsesGroupTileCount()
    {
        var context = CreateContext(3, 1);
        var ball = CreateBall(context, 1, Tile(0, 0), Tile(1, 0), Tile(2, 0));

        Assert.That(ball.YarnUnitCount, Is.EqualTo(3));
        Assert.That(ball.HasYarnRemaining, Is.True);
    }

    [Test]
    public void ConsumeOneYarnUnit_HidesOneVisualPieceAndReachesZero()
    {
        var context = CreateContext(3, 1);
        var ball = CreateBall(context, 1, Tile(0, 0), Tile(1, 0), Tile(2, 0));
        ball.BeginDispatchAtWait(Vector3.zero);

        ball.ConsumeOneYarnUnit();
        ball.ConsumeOneYarnUnit();
        ball.ConsumeOneYarnUnit();

        Assert.That(ball.YarnUnitCount, Is.Zero);
        Assert.That(ball.HasYarnRemaining, Is.False);
        Assert.That(ball.Visual.VisiblePieceCount, Is.Zero);
    }

    [Test]
    public void ConsumeOneYarnUnit_WithMultipleUnitsPerTile_HidesPieceAfterTileUnitsConsumed()
    {
        var context = CreateContext(2, 1);
        var ball = CreateBall(context, 1, Tile(0, 0), Tile(1, 0));
        ball.YarnUnitsPerTile = 2;
        ball.BeginDispatchAtWait(Vector3.zero);

        Assert.That(ball.YarnUnitCount, Is.EqualTo(4));

        ball.ConsumeOneYarnUnit();

        Assert.That(ball.YarnUnitCount, Is.EqualTo(3));
        Assert.That(ball.Visual.VisiblePieceCount, Is.EqualTo(2));

        ball.ConsumeOneYarnUnit();

        Assert.That(ball.YarnUnitCount, Is.EqualTo(2));
        Assert.That(ball.Visual.VisiblePieceCount, Is.EqualTo(1));
    }

    private TestContext CreateContext(int width, int height, params Vector2Int[] activeTiles)
    {
        var level = new LevelData
        {
            levelId = "Test",
            size = new Vector2Int(width, height),
            tileData = new bool[width * height],
            yarnBalls = new List<WoolBallData>(),
            boardSetting = new GlobalYarnBoardSetting { cellSize = 1f, cellSpacing = 1f }
        };

        if (activeTiles == null || activeTiles.Length == 0)
        {
            for (var i = 0; i < level.tileData.Length; i++)
                level.tileData[i] = true;
        }
        else
        {
            for (var i = 0; i < activeTiles.Length; i++)
                level.tileData[activeTiles[i].y * width + activeTiles[i].x] = true;
        }

        var adapter = new BoardSplineDataAdapterInfo
        {
            size = level.size,
            tileData = level.tileData,
            cellSize = 1f,
            origin = Vector3.zero,
            right = Vector3.right,
            forward = Vector3.forward
        };

        return new TestContext(level, adapter, new YarnBoardRuntimeState(level, adapter));
    }

    private WoolBall CreateBall(TestContext context, int id, Vector2Int tile, params Vector2Int[] children)
    {
        var data = new WoolBallData
        {
            BallId = id,
            ColorId = 0,
            tileId = tile,
            childrenTileIds = new List<Vector2Int>(children ?? new Vector2Int[0])
        };
        context.Level.yarnBalls.Add(data);

        var gameObject = new GameObject($"WoolBall_{id}");
        createdObjects.Add(gameObject);

        var ball = gameObject.AddComponent<WoolBall>();
        ball.StepDuration = 0f;
        ball.OnCreated(new WoolBallCreateParameters(data, context.Adapter, null, context.State));
        return ball;
    }

    private static Vector2Int Tile(int x, int y)
    {
        return new Vector2Int(x, y);
    }

    private static void AssertTiles(WoolBall ball, params Vector2Int[] expected)
    {
        Assert.That(ball.Data.tileId, Is.EqualTo(expected[0]));
        Assert.That(ball.Data.childrenTileIds.Count, Is.EqualTo(expected.Length - 1));
        for (var i = 1; i < expected.Length; i++)
            Assert.That(ball.Data.childrenTileIds[i - 1], Is.EqualTo(expected[i]));
    }

    private sealed class TestContext
    {
        public TestContext(LevelData level, BoardSplineDataAdapterInfo adapter, YarnBoardRuntimeState state)
        {
            Level = level;
            Adapter = adapter;
            State = state;
        }

        public LevelData Level { get; }
        public BoardSplineDataAdapterInfo Adapter { get; }
        public YarnBoardRuntimeState State { get; }
    }
}
