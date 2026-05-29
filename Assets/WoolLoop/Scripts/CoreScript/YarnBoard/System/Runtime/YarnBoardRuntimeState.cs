using System.Collections.Generic;
using BoardSpline.Runtime;
using UnityEngine;

public sealed class YarnBoardRuntimeState
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private readonly Dictionary<Vector2Int, WoolBall> occupiedTiles = new();
    private readonly HashSet<Vector2Int> reservedTiles = new();
    private readonly Dictionary<WoolBall, HashSet<Vector2Int>> reservationsByBall = new();

    public YarnBoardRuntimeState(LevelData level, BoardSplineDataAdapterInfo adapter)
    {
        Level = level;
        Adapter = adapter;
    }

    public LevelData Level { get; }
    public BoardSplineDataAdapterInfo Adapter { get; }

    public void Register(WoolBall ball)
    {
        if (ball == null)
            return;

        SetOccupancy(ball, ball.GetTiles());
    }

    public void Unregister(WoolBall ball)
    {
        ReleaseOccupancy(ball);
        ReleaseReservations(ball);
    }

    public void ReleaseOccupancy(WoolBall ball)
    {
        if (ball == null)
            return;

        var toRemove = new List<Vector2Int>();
        foreach (var pair in occupiedTiles)
        {
            if (pair.Value == ball)
                toRemove.Add(pair.Key);
        }

        for (var i = 0; i < toRemove.Count; i++)
            occupiedTiles.Remove(toRemove[i]);
    }

    public void SetOccupancy(WoolBall ball, IReadOnlyList<Vector2Int> tiles)
    {
        ReleaseOccupancy(ball);
        if (ball == null || tiles == null)
            return;

        for (var i = 0; i < tiles.Count; i++)
            occupiedTiles[tiles[i]] = ball;
    }

    public bool TryReserve(WoolBall ball, Vector2Int tile)
    {
        if (ball == null || IsReservedByOther(ball, tile))
            return false;

        reservedTiles.Add(tile);
        if (!reservationsByBall.TryGetValue(ball, out var reservations))
        {
            reservations = new HashSet<Vector2Int>();
            reservationsByBall.Add(ball, reservations);
        }

        reservations.Add(tile);
        return true;
    }

    public void ReleaseReservation(WoolBall ball, Vector2Int tile)
    {
        if (ball == null)
            return;

        if (!reservationsByBall.TryGetValue(ball, out var reservations))
            return;

        if (!reservations.Remove(tile))
            return;

        reservedTiles.Remove(tile);
        if (reservations.Count == 0)
            reservationsByBall.Remove(ball);
    }

    public void ReleaseReservations(WoolBall ball)
    {
        if (ball == null || !reservationsByBall.TryGetValue(ball, out var reservations))
            return;

        foreach (var tile in reservations)
            reservedTiles.Remove(tile);

        reservationsByBall.Remove(ball);
    }

    public bool IsWalkableFor(WoolBall ball, Vector2Int tile)
    {
        if (Level == null || !Level.IsActive(tile))
            return false;

        if (IsReservedByOther(ball, tile))
            return false;

        return !occupiedTiles.TryGetValue(tile, out var occupant) || occupant == null || occupant == ball;
    }

    public bool IsExit(Vector2Int tile)
    {
        if (Level == null || !Level.IsActive(tile))
            return false;

        for (var i = 0; i < Directions.Length; i++)
        {
            var neighbor = tile + Directions[i];
            if (!Level.IsInside(neighbor) || !Level.IsActive(neighbor))
                return true;
        }

        return false;
    }

    public bool TryFindPath(WoolBall ball, Vector2Int start, Vector2Int target, out List<Vector2Int> path)
    {
        path = null;
        if (!IsWalkableFor(ball, start) || !IsWalkableFor(ball, target))
            return false;

        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var previous = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == target)
            {
                path = ReconstructPath(start, target, previous);
                return true;
            }

            for (var i = 0; i < Directions.Length; i++)
            {
                var next = current + Directions[i];
                if (visited.Contains(next) || !IsWalkableFor(ball, next))
                    continue;

                visited.Add(next);
                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        return false;
    }

    public List<Vector2Int> GetExitTiles()
    {
        var exits = new List<Vector2Int>();
        if (Level == null)
            return exits;

        for (var y = 0; y < Level.size.y; y++)
        {
            for (var x = 0; x < Level.size.x; x++)
            {
                var tile = new Vector2Int(x, y);
                if (IsExit(tile))
                    exits.Add(tile);
            }
        }

        return exits;
    }

    private bool IsReservedByOther(WoolBall ball, Vector2Int tile)
    {
        if (!reservedTiles.Contains(tile))
            return false;

        return !reservationsByBall.TryGetValue(ball, out var reservations) || !reservations.Contains(tile);
    }

    private static List<Vector2Int> ReconstructPath(
        Vector2Int start,
        Vector2Int target,
        Dictionary<Vector2Int, Vector2Int> previous
    )
    {
        var path = new List<Vector2Int>();
        var current = target;
        path.Add(current);

        while (current != start)
        {
            current = previous[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
