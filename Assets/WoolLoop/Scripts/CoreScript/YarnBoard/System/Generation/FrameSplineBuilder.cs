using System;
using System.Collections.Generic;
using Dreamteck.Splines;
using UnityEngine;

public sealed class FrameSplineBuilder : IFrameSplineBuilder
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.up,
        Vector2Int.left,
        Vector2Int.down
    };

    private readonly Material _frameMaterial;
    private readonly Mesh _frameMesh;
    private readonly float _wallWidth;
    private readonly int _tubeSides;
    private readonly int _sampleRate;
    private readonly float _cornerRoundness;
    private readonly int _cornerSegments;

    private Mesh _generatedFrameMesh;
    private float _generatedFrameMeshLength;

    public FrameSplineBuilder(Material frameMaterial, float wallWidth = 1f, int tubeSides = 8, int sampleRate = 4, float cornerRoundness = 0.35f, Mesh frameMesh = null, int cornerSegments = 6)
    {
        _frameMaterial = frameMaterial;
        _frameMesh = frameMesh;
        _wallWidth = Mathf.Max(0.01f, wallWidth);
        _tubeSides = Mathf.Max(3, tubeSides);
        _sampleRate = Mathf.Max(2, sampleRate);
        _cornerRoundness = Mathf.Max(0f, cornerRoundness);
        _cornerSegments = Mathf.Max(1, cornerSegments);
    }

    public SplineComputer BuildFrame(LevelData data, Transform parent)
    {
        if (data == null || parent == null)
            return null;

        List<CenterPath> paths = BuildCenterPaths(data);
        SplineComputer first = null;

        for (int i = 0; i < paths.Count; i++)
        {
            SplinePoint[] controlPoints = BuildControlPoints(data, paths[i]);
            if (controlPoints.Length < (paths[i].IsClosed ? 3 : 2))
                continue;

            SplineComputer computer = CreateSpline(data, parent, controlPoints, paths[i].IsClosed, i);
            if (first == null)
                first = computer;
        }

        return first;
    }

    private SplineComputer CreateSpline(LevelData data, Transform parent, SplinePoint[] controlPoints, bool isClosed, int index)
    {
        GameObject frame = new GameObject($"FrameSpline_{index:00}");
        frame.transform.SetParent(parent, false);

        SplineComputer computer = frame.AddComponent<SplineComputer>();
        computer.type = Spline.Type.Linear;
        computer.sampleRate = _sampleRate;
        computer.SetPoints(controlPoints);
        if (isClosed)
            computer.Close();
        computer.RebuildImmediate();

        SplineMesh splineMesh = frame.AddComponent<SplineMesh>();
        splineMesh.spline = computer;
        splineMesh.size = 1f;
        splineMesh.autoUpdate = false;
        splineMesh.normalMethod = MeshGenerator.NormalMethod.SplineNormals;

        SplineMesh.Channel channel = splineMesh.GetChannelCount() > 0
            ? splineMesh.GetChannel(0)
            : splineMesh.AddChannel("Frame Mesh");
        channel.type = SplineMesh.Channel.Type.Extrude;
        channel.autoCount = true;
        channel.overrideNormal = true;
        channel.customNormal = Vector3.up;
        Mesh mesh = GetFrameMesh(data);
        ApplyWallWidthScale(channel, mesh);
        channel.AddMesh(mesh);

        MeshRenderer renderer = frame.GetComponent<MeshRenderer>();
        if (renderer != null && _frameMaterial != null)
            renderer.sharedMaterial = _frameMaterial;

        splineMesh.RebuildImmediate();
        return computer;
    }

    private SplinePoint[] BuildControlPoints(LevelData data, CenterPath path)
    {
        List<Vector3> positions = BuildRoundedPositions(data, path);
        SplinePoint[] points = new SplinePoint[positions.Count];

        for (int i = 0; i < positions.Count; i++)
        {
            points[i] = new SplinePoint(positions[i])
            {
                normal = Vector3.up,
                size = 1f,
                color = Color.white
            };
        }

        return points;
    }

    private List<Vector3> BuildRoundedPositions(LevelData data, CenterPath path)
    {
        List<Vector2Int> tiles = path.Tiles;
        Vector3[] source = new Vector3[tiles.Count];

        for (int i = 0; i < tiles.Count; i++)
            source[i] = GridCoordinateUtility.TileToWorld(tiles[i], data);

        return new List<Vector3>(AddRoundedCorners(source, _cornerRoundness, _cornerSegments, path.IsClosed));
    }

    private static Vector3[] AddRoundedCorners(Vector3[] points, float radius, int segments, bool loop)
    {
        if (points == null || points.Length < 3 || radius <= 0f)
            return points ?? Array.Empty<Vector3>();

        List<Vector3> result = new List<Vector3>();
        int count = points.Length;
        int start = loop ? 0 : 1;
        int end = loop ? count : count - 1;

        if (!loop)
            TryAdd(result, points[0]);

        for (int i = start; i < end; i++)
        {
            Vector3 previous = points[(i - 1 + count) % count];
            Vector3 current = points[i];
            Vector3 next = points[(i + 1) % count];

            Vector3 toPrevious = previous - current;
            Vector3 toNext = next - current;
            float previousLength = toPrevious.magnitude;
            float nextLength = toNext.magnitude;
            if (previousLength <= Mathf.Epsilon || nextLength <= Mathf.Epsilon)
            {
                TryAdd(result, current);
                continue;
            }

            toPrevious /= previousLength;
            toNext /= nextLength;

            float angle = Vector3.Angle(toPrevious, toNext);
            if (angle > 179.9f || angle < 0.1f)
            {
                TryAdd(result, current);
                continue;
            }

            float halfAngleRadians = angle * Mathf.Deg2Rad * 0.5f;
            float tanHalf = Mathf.Tan(halfAngleRadians);
            float sinHalf = Mathf.Sin(halfAngleRadians);
            if (Mathf.Abs(tanHalf) <= Mathf.Epsilon || Mathf.Abs(sinHalf) <= Mathf.Epsilon)
            {
                TryAdd(result, current);
                continue;
            }

            float maxRadius = Mathf.Min(previousLength * 0.5f * tanHalf, nextLength * 0.5f * tanHalf);
            float effectiveRadius = Mathf.Min(radius, maxRadius);
            if (effectiveRadius <= Mathf.Epsilon)
            {
                TryAdd(result, current);
                continue;
            }

            float tangentDistance = effectiveRadius / tanHalf;
            Vector3 entryPoint = current + toPrevious * tangentDistance;
            Vector3 exitPoint = current + toNext * tangentDistance;
            Vector3 centerDirection = (toPrevious + toNext).normalized;
            Vector3 center = current + centerDirection * (effectiveRadius / sinHalf);
            Vector3 entryRadius = (entryPoint - center).normalized;
            Vector3 exitRadius = (exitPoint - center).normalized;
            Vector3 rotationAxis = Vector3.Cross(toPrevious, toNext).normalized;
            if (rotationAxis.sqrMagnitude <= Mathf.Epsilon)
            {
                TryAdd(result, current);
                continue;
            }

            float arcAngle = Vector3.SignedAngle(entryRadius, exitRadius, rotationAxis);
            TryAdd(result, entryPoint);
            for (int segment = 1; segment < segments; segment++)
            {
                float percent = (float)segment / segments;
                Quaternion rotation = Quaternion.AngleAxis(arcAngle * percent, rotationAxis);
                TryAdd(result, center + rotation * entryRadius * effectiveRadius);
            }

            TryAdd(result, exitPoint);
        }

        if (!loop)
        {
            TryAdd(result, points[count - 1]);
        }
        else if (result.Count > 1 && Vector3.Distance(result[result.Count - 1], result[0]) < 0.001f)
        {
            result.RemoveAt(result.Count - 1);
        }

        return result.ToArray();
    }

    private static void TryAdd(List<Vector3> points, Vector3 point)
    {
        if (points.Count > 0 && Vector3.Distance(points[points.Count - 1], point) <= 0.001f)
            return;

        points.Add(point);
    }

    private static List<CenterPath> BuildCenterPaths(LevelData data)
    {
        List<Vector2Int> wallTiles = new List<Vector2Int>();
        HashSet<Vector2Int> wallSet = new HashSet<Vector2Int>();

        for (int y = 0; y < data.size.y; y++)
        {
            for (int x = 0; x < data.size.x; x++)
            {
                Vector2Int tile = new Vector2Int(x, y);
                if (data.IsActive(tile))
                    continue;

                wallTiles.Add(tile);
                wallSet.Add(tile);
            }
        }

        List<CenterPath> paths = new List<CenterPath>();
        HashSet<GridEdge> visitedEdges = new HashSet<GridEdge>();

        foreach (Vector2Int tile in wallTiles)
        {
            if (GetDegree(tile, wallSet) == 2)
                continue;

            foreach (Vector2Int neighbor in GetNeighbors(tile, wallSet))
            {
                GridEdge edge = new GridEdge(tile, neighbor);
                if (visitedEdges.Contains(edge))
                    continue;

                paths.Add(TraceOpenPath(tile, neighbor, wallSet, visitedEdges));
            }
        }

        foreach (Vector2Int tile in wallTiles)
        {
            foreach (Vector2Int neighbor in GetNeighbors(tile, wallSet))
            {
                GridEdge edge = new GridEdge(tile, neighbor);
                if (visitedEdges.Contains(edge))
                    continue;

                paths.Add(TraceClosedPath(tile, neighbor, wallSet, visitedEdges));
            }
        }

        return paths;
    }

    private static CenterPath TraceOpenPath(Vector2Int start, Vector2Int firstNeighbor, HashSet<Vector2Int> wallSet, HashSet<GridEdge> visitedEdges)
    {
        List<Vector2Int> tiles = new List<Vector2Int> { start };
        Vector2Int previous = start;
        Vector2Int current = firstNeighbor;
        int guard = wallSet.Count * Directions.Length + 1;

        while (guard-- > 0)
        {
            visitedEdges.Add(new GridEdge(previous, current));
            tiles.Add(current);

            if (GetDegree(current, wallSet) != 2)
                break;

            Vector2Int next = GetNextPathTile(current, previous, wallSet);
            GridEdge nextEdge = new GridEdge(current, next);
            if (next == current || visitedEdges.Contains(nextEdge))
                break;

            previous = current;
            current = next;
        }

        return new CenterPath(tiles, false);
    }

    private static CenterPath TraceClosedPath(Vector2Int start, Vector2Int firstNeighbor, HashSet<Vector2Int> wallSet, HashSet<GridEdge> visitedEdges)
    {
        List<Vector2Int> tiles = new List<Vector2Int> { start };
        Vector2Int previous = start;
        Vector2Int current = firstNeighbor;
        bool isClosed = false;
        int guard = wallSet.Count * Directions.Length + 1;

        while (guard-- > 0)
        {
            visitedEdges.Add(new GridEdge(previous, current));
            if (current == start)
            {
                isClosed = true;
                break;
            }

            tiles.Add(current);
            Vector2Int next = GetNextPathTile(current, previous, wallSet);
            if (next == current)
                break;

            if (next == start)
            {
                visitedEdges.Add(new GridEdge(current, start));
                isClosed = true;
                break;
            }

            GridEdge nextEdge = new GridEdge(current, next);
            if (visitedEdges.Contains(nextEdge))
                break;

            previous = current;
            current = next;
        }

        return new CenterPath(tiles, isClosed);
    }

    private static Vector2Int GetNextPathTile(Vector2Int tile, Vector2Int previous, HashSet<Vector2Int> wallSet)
    {
        foreach (Vector2Int neighbor in GetNeighbors(tile, wallSet))
        {
            if (neighbor != previous)
                return neighbor;
        }

        return tile;
    }

    private static int GetDegree(Vector2Int tile, HashSet<Vector2Int> wallSet)
    {
        int degree = 0;
        foreach (Vector2Int direction in Directions)
        {
            if (wallSet.Contains(tile + direction))
                degree++;
        }

        return degree;
    }

    private static List<Vector2Int> GetNeighbors(Vector2Int tile, HashSet<Vector2Int> wallSet)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>(Directions.Length);
        foreach (Vector2Int direction in Directions)
        {
            Vector2Int neighbor = tile + direction;
            if (wallSet.Contains(neighbor))
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    private Mesh GetFrameMesh(LevelData data)
    {
        if (_frameMesh != null)
            return _frameMesh;

        float length = GridCoordinateUtility.GetCellPitch(data);
        if (_generatedFrameMesh == null || !Mathf.Approximately(_generatedFrameMeshLength, length))
        {
            _generatedFrameMesh = CreateDefaultFrameMesh(length);
            _generatedFrameMeshLength = length;
        }

        return _generatedFrameMesh;
    }

    private Mesh CreateDefaultFrameMesh(float length)
    {
        int sides = Mathf.Max(3, _tubeSides);
        float radius = _wallWidth * 0.5f;
        Vector3[] vertices = new Vector3[sides * 2];
        Vector3[] normals = new Vector3[sides * 2];
        Vector2[] uvs = new Vector2[sides * 2];
        int[] triangles = new int[sides * 6];

        for (int i = 0; i < sides; i++)
        {
            float percent = (float)i / sides;
            // float angle = percent * Mathf.PI * 2f;
            float angle = (i * Mathf.PI * 2f / _tubeSides) + (Mathf.PI / 4f);
            Vector3 radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

            vertices[i] = radial * radius;
            vertices[i + sides] = radial * radius + Vector3.forward * length;
            normals[i] = radial;
            normals[i + sides] = radial;
            uvs[i] = new Vector2(percent, 0f);
            uvs[i + sides] = new Vector2(percent, 1f);
        }

        int triangleIndex = 0;
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;

            triangles[triangleIndex++] = i;
            triangles[triangleIndex++] = i + sides;
            triangles[triangleIndex++] = next;

            triangles[triangleIndex++] = next;
            triangles[triangleIndex++] = i + sides;
            triangles[triangleIndex++] = next + sides;
        }

        Mesh mesh = new Mesh
        {
            name = "Generated Frame Mesh",
            vertices = vertices,
            normals = normals,
            uv = uvs,
            triangles = triangles
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ApplyWallWidthScale(SplineMesh.Channel channel, Mesh mesh)
    {
        if (_frameMesh == null || mesh == null)
            return;

        float meshWidth = mesh.bounds.size.x;
        float scale = meshWidth > Mathf.Epsilon ? _wallWidth / meshWidth : 1f;
        Vector3 channelScale = new Vector3(scale, 1f, 1f);
        channel.minScale = channelScale;
        channel.maxScale = channelScale;
    }

    private readonly struct CenterPath
    {
        public readonly List<Vector2Int> Tiles;
        public readonly bool IsClosed;

        public CenterPath(List<Vector2Int> tiles, bool isClosed)
        {
            Tiles = tiles;
            IsClosed = isClosed;
        }
    }

    private readonly struct GridEdge : IEquatable<GridEdge>
    {
        private readonly Vector2Int _a;
        private readonly Vector2Int _b;

        public GridEdge(Vector2Int from, Vector2Int to)
        {
            if (Compare(from, to) <= 0)
            {
                _a = from;
                _b = to;
            }
            else
            {
                _a = to;
                _b = from;
            }
        }

        public bool Equals(GridEdge other)
        {
            return _a == other._a && _b == other._b;
        }

        public override bool Equals(object obj)
        {
            return obj is GridEdge other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _a.x;
                hash = hash * 31 + _a.y;
                hash = hash * 31 + _b.x;
                hash = hash * 31 + _b.y;
                return hash;
            }
        }

        private static int Compare(Vector2Int left, Vector2Int right)
        {
            int xCompare = left.x.CompareTo(right.x);
            return xCompare != 0 ? xCompare : left.y.CompareTo(right.y);
        }
    }
}
