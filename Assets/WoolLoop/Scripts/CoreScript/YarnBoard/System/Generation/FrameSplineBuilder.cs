using System.Collections.Generic;
using Dreamteck.Splines;
using UnityEngine;

public sealed class FrameSplineBuilder : IFrameSplineBuilder
{
    private readonly Material _frameMaterial;
    private readonly float _tubeSize;
    private readonly int _tubeSides;
    private readonly int _sampleRate;

    public FrameSplineBuilder(Material frameMaterial, float tubeSize = 0.12f, int tubeSides = 8, int sampleRate = 4)
    {
        _frameMaterial = frameMaterial;
        _tubeSize = Mathf.Max(0.01f, tubeSize);
        _tubeSides = Mathf.Max(3, tubeSides);
        _sampleRate = Mathf.Max(2, sampleRate);
    }

    public SplineComputer BuildFrame(LevelData data, Transform parent)
    {
        if (data == null || parent == null)
            return null;

        List<List<Vector2Int>> loops = BuildBoundaryLoops(data);
        SplineComputer first = null;

        for (int i = 0; i < loops.Count; i++)
        {
            if (loops[i].Count < 3)
                continue;

            SplineComputer computer = CreateSpline(data, parent, loops[i], i);
            if (first == null)
                first = computer;
        }

        return first;
    }

    private SplineComputer CreateSpline(LevelData data, Transform parent, List<Vector2Int> corners, int index)
    {
        GameObject frame = new GameObject($"FrameSpline_{index:00}");
        frame.transform.SetParent(parent, false);

        SplineComputer computer = frame.AddComponent<SplineComputer>();
        computer.type = Spline.Type.Linear;
        computer.sampleRate = _sampleRate;

        SplinePoint[] points = new SplinePoint[corners.Count];
        for (int i = 0; i < corners.Count; i++)
        {
            Vector3 position = GridCoordinateUtility.CornerToWorld(corners[i], data);
            points[i] = new SplinePoint(position)
            {
                normal = Vector3.up,
                size = 1f,
                color = Color.white
            };
        }

        computer.SetPoints(points);
        computer.Close();
        computer.RebuildImmediate();

        TubeGenerator tube = frame.AddComponent<TubeGenerator>();
        tube.spline = computer;
        tube.size = _tubeSize;
        tube.sides = _tubeSides;
        tube.capMode = TubeGenerator.CapMethod.None;
        tube.autoUpdate = false;

        MeshRenderer renderer = frame.GetComponent<MeshRenderer>();
        if (renderer != null && _frameMaterial != null)
            renderer.sharedMaterial = _frameMaterial;

        tube.RebuildImmediate();
        return computer;
    }

    private List<List<Vector2Int>> BuildBoundaryLoops(LevelData data)
    {
        List<GridEdge> edges = new List<GridEdge>();

        for (int y = 0; y < data.size.y; y++)
        {
            for (int x = 0; x < data.size.x; x++)
            {
                Vector2Int tile = new Vector2Int(x, y);
                if (data.IsActive(tile))
                    continue;

                AddEdgeIfBoundary(data, tile, new Vector2Int(x, y), new Vector2Int(x + 1, y), Vector2Int.down, edges);
                AddEdgeIfBoundary(data, tile, new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1), Vector2Int.right, edges);
                AddEdgeIfBoundary(data, tile, new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1), Vector2Int.up, edges);
                AddEdgeIfBoundary(data, tile, new Vector2Int(x, y + 1), new Vector2Int(x, y), Vector2Int.left, edges);
            }
        }

        return TraceLoops(edges);
    }

    private static void AddEdgeIfBoundary(LevelData data, Vector2Int tile, Vector2Int from, Vector2Int to, Vector2Int neighborDirection, List<GridEdge> edges)
    {
        Vector2Int neighbor = tile + neighborDirection;
        bool neighborIsFalse = data.IsInside(neighbor) && !data.IsActive(neighbor);
        if (!neighborIsFalse)
            edges.Add(new GridEdge(from, to));
    }

    private static List<List<Vector2Int>> TraceLoops(List<GridEdge> edges)
    {
        List<List<Vector2Int>> loops = new List<List<Vector2Int>>();
        Dictionary<Vector2Int, List<GridEdge>> outgoing = new Dictionary<Vector2Int, List<GridEdge>>();

        foreach (GridEdge edge in edges)
        {
            if (!outgoing.TryGetValue(edge.From, out List<GridEdge> bucket))
            {
                bucket = new List<GridEdge>();
                outgoing.Add(edge.From, bucket);
            }
            bucket.Add(edge);
        }

        while (edges.Count > 0)
        {
            GridEdge current = edges[edges.Count - 1];
            RemoveEdge(edges, outgoing, current);

            List<Vector2Int> loop = new List<Vector2Int> { current.From };
            Vector2Int cursor = current.To;

            int guard = 0;
            while (cursor != loop[0] && guard++ < 4096)
            {
                loop.Add(cursor);
                if (!outgoing.TryGetValue(cursor, out List<GridEdge> bucket) || bucket.Count == 0)
                    break;

                current = bucket[0];
                cursor = current.To;
                RemoveEdge(edges, outgoing, current);
            }

            if (cursor == loop[0] && loop.Count >= 3)
                loops.Add(loop);
        }

        return loops;
    }

    private static void RemoveEdge(List<GridEdge> edges, Dictionary<Vector2Int, List<GridEdge>> outgoing, GridEdge edge)
    {
        edges.Remove(edge);
        if (outgoing.TryGetValue(edge.From, out List<GridEdge> bucket))
            bucket.Remove(edge);
    }

    private readonly struct GridEdge
    {
        public readonly Vector2Int From;
        public readonly Vector2Int To;

        public GridEdge(Vector2Int from, Vector2Int to)
        {
            From = from;
            To = to;
        }
    }
}
