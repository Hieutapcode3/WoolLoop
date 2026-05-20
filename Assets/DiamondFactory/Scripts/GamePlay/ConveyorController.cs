using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
[RequireComponent(typeof(LineRenderer))]
public partial class ConveyorController : MonoBehaviour
{
    [TitleGroup("Path")]
    [ListDrawerSettings(ShowFoldout = true, DraggableItems = false, IsReadOnly = true)]
    [SerializeField] private List<Transform> points = new();

    [TitleGroup("Spots")]
    [SerializeField, Range(2, 100)] public int spotCount = 8;
    public int SpotCount => spotCount;
    [SerializeField] private Vector3 spotPositionOffset = Vector3.zero;
    [SerializeField, Min(0f)] private float spotSpeedOrigin = 1.5f;
    [SerializeField, Min(0f)] private float currentSpotSpeed = 1.5f;
    [SerializeField] private bool isClosed = false;
    [SerializeField] private bool generateSpotTransforms = true;
    [SerializeField] private GameObject spotPrefab;

    [SerializeField, ReadOnly] private Transform spotsRoot;
    [SerializeField, ReadOnly] private List<Transform> listSpot = new();
    private readonly List<float> listSpotDistances = new();
    private readonly List<Vector3> slotPositionsCache = new();
    private LineRenderer line;

    private void Awake()
    {
        InitializeLineRenderer();
        RefreshPathVisual();
    }

    private void OnEnable()
    {
        SetConveyorSpotSpeed(spotSpeedOrigin);
        RefreshPathVisual();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            RefreshPathVisual();
    }
#endif

    [Button("Refresh Path", ButtonSizes.Large)]
    public void RefreshPathVisual()
    {
        InitializeLineRenderer();
        RefreshPoints();
        RebuildLine();
    }

    public void SetSpotCount(int count)
    {
        spotCount = Mathf.Clamp(count, 2, 64);
        RefreshPathVisual();
#if UNITY_EDITOR
        if (!Application.isPlaying)
            MarkEditorStateDirty();
#endif
    }

    public Transform GetSpotTransform(int index)
    {
        if (listSpot == null || index < 0 || index >= listSpot.Count)
            return null;
        return listSpot[index];
    }

    private void RefreshPoints()
    {
        points.Clear();

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Point"))
                points.Add(child);
        }

        points.Sort((a, b) => a.GetSiblingIndex().CompareTo(b.GetSiblingIndex()));
    }

    private void InitializeLineRenderer()
    {
        if (!TryGetComponent(out line))
            line = gameObject.AddComponent<LineRenderer>();
    }

    private void RebuildLine()
    {
        if (line == null)
            return;

        List<Vector3> path = GetAllPathPositions();
        line.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
            line.SetPosition(i, path[i]);

        RebuildSpots();
        EnsureSpotTransforms();
    }

#if UNITY_EDITOR
    private void MarkEditorStateDirty()
    {
        EditorUtility.SetDirty(this);

        if (spotsRoot != null)
            EditorUtility.SetDirty(spotsRoot.gameObject);

        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif

    private GameObject CreateSpotInstance(int index)
    {
        string spotName = $"Spot_{index:00}";
        if (spotPrefab != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(spotPrefab, spotsRoot) as GameObject;
                if (instance != null)
                    instance.name = spotName;
                return instance;
            }
#endif
            GameObject playInstance = Instantiate(spotPrefab, spotsRoot);
            playInstance.name = spotName;
            return playInstance;
        }

        GameObject go = new GameObject(spotName);
        go.transform.SetParent(spotsRoot, worldPositionStays: false);
        return go;
    }

    private void RebuildSpots()
    {
        listSpotDistances.Clear();
        int count = Mathf.Max(2, spotCount);
        List<Vector3> pathPositions = GetAllPathPositions();
        if (pathPositions.Count < 2)
            return;

        float[] cumulativeDistances = new float[pathPositions.Count];
        for (int i = 1; i < pathPositions.Count; i++)
            cumulativeDistances[i] = cumulativeDistances[i - 1] + Vector3.Distance(pathPositions[i - 1], pathPositions[i]);

        float totalLength = cumulativeDistances[^1];
        if (totalLength <= 0.00001f)
        {
            for (int i = 0; i < count; i++)
                listSpotDistances.Add(0f);
            return;
        }

        float step = totalLength / count;
        for (int i = 0; i < count; i++)
            listSpotDistances.Add(Mathf.Clamp(step * i, 0f, totalLength));
    }

    private void EnsureSpotTransforms()
    {
        if (!generateSpotTransforms)
            return;

        int count = Mathf.Max(2, spotCount);
        if (spotsRoot == null)
        {
            GameObject go = new GameObject("__GeneratedSpots");
            spotsRoot = go.transform;
            spotsRoot.SetParent(transform.parent, worldPositionStays: true);
        }

        listSpot.RemoveAll(t => t == null);
        while (listSpot.Count < count)
            listSpot.Add(CreateSpotInstance(listSpot.Count).transform);

        for (int i = listSpot.Count - 1; i >= count; i--)
        {
            Transform t = listSpot[i];
            listSpot.RemoveAt(i);
            if (t == null)
                continue;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.DestroyObjectImmediate(t.gameObject);
            else
                Destroy(t.gameObject);
#else
            Destroy(t.gameObject);
#endif
        }

        IReadOnlyList<Vector3> spots = GetSlotPositions();
        for (int i = 0; i < Mathf.Min(count, spots.Count); i++)
        {
            Transform t = listSpot[i];
            t.position = spots[i];
            if (!t.TryGetComponent(out ConveyorSpot conveyorSpot))
                conveyorSpot = t.gameObject.AddComponent<ConveyorSpot>();
            conveyorSpot.Setup(this, listSpotDistances[i]);
        }
    }

    public List<Vector3> GetAllPathPositions()
    {
        var orderedPoints = new List<Transform>();
        foreach (Transform point in points)
        {
            if (point != null)
                orderedPoints.Add(point);
        }

        if (orderedPoints.Count == 0)
            return new List<Vector3>();
        if (orderedPoints.Count == 1)
            return new List<Vector3> { orderedPoints[0].position };

        var result = new List<Vector3>(orderedPoints.Count * 2);
        result.Add(orderedPoints[0].position);

        int lastIndex = orderedPoints.Count - 1;
        for (int i = 1; i < lastIndex; i++)
        {
            Transform prev = orderedPoints[i - 1];
            Transform current = orderedPoints[i];
            Transform next = orderedPoints[i + 1];
            if (prev == null || current == null || next == null)
                continue;

            ConveyorPathPoint pathPoint = current.GetComponent<ConveyorPathPoint>();
            if (pathPoint != null && pathPoint.skipSegmentToNext)
                continue;

            if (pathPoint != null && pathPoint.smoothCorner)
            {
                AddSmoothCorner(result, prev.position, current.position, next.position, pathPoint.cornerRadius, pathPoint.cornerSegments);
                continue;
            }

            result.Add(current.position);
        }

        result.Add(orderedPoints[lastIndex].position);
        if (isClosed && result.Count > 1)
            result.Add(result[0]);
        return result;
    }

    private void AddSmoothCorner(List<Vector3> path, Vector3 prev, Vector3 corner, Vector3 next, float radius, int segments)
    {
        Vector3 inDir = (corner - prev).normalized;
        Vector3 outDir = (next - corner).normalized;
        if (inDir.sqrMagnitude <= 0.00001f || outDir.sqrMagnitude <= 0.00001f)
        {
            path.Add(corner);
            return;
        }

        float clampedRadius = Mathf.Max(0.01f, radius);
        float inLen = Vector3.Distance(prev, corner);
        float outLen = Vector3.Distance(corner, next);
        float maxReach = Mathf.Min(inLen, outLen) * 0.5f;
        float reach = Mathf.Min(clampedRadius, maxReach);
        if (reach <= 0.00001f)
        {
            path.Add(corner);
            return;
        }

        Vector3 start = corner - inDir * reach;
        Vector3 end = corner + outDir * reach;
        int segCount = Mathf.Clamp(segments, 2, 20);
        for (int i = 0; i <= segCount; i++)
        {
            float t = (float)i / segCount;
            Vector3 a = Vector3.Lerp(start, corner, t);
            Vector3 b = Vector3.Lerp(corner, end, t);
            path.Add(Vector3.Lerp(a, b, t));
        }
    }

    public IReadOnlyList<Vector3> GetSlotPositions()
    {
        int expectedCount = Mathf.Max(2, spotCount);
        if (listSpotDistances.Count != expectedCount)
            RebuildSpots();

        slotPositionsCache.Clear();
        foreach (float distance in listSpotDistances)
            slotPositionsCache.Add(SamplePathPositionAtDistance(distance) + spotPositionOffset);
        return slotPositionsCache;
    }

    public float GetConveyorSpotSpeed() => currentSpotSpeed;

    public void SetConveyorSpotSpeed(float newSpeed)
    {
        currentSpotSpeed = Mathf.Max(0.2f, newSpeed);
    }

    public void ResetSpotSpeedToOrigin() => SetConveyorSpotSpeed(spotSpeedOrigin);

    public bool UseSmoothLoopMoveToStart() => isClosed;
    public Vector3 GetSpotOffset() => spotPositionOffset;

    public IReadOnlyList<float> GetSpotDistances()
    {
        if (listSpotDistances.Count != Mathf.Max(2, spotCount))
            RebuildSpots();
        return listSpotDistances;
    }

    private Vector3 SamplePathPositionAtDistance(float targetDistance)
    {
        List<Vector3> pathPositions = GetAllPathPositions();
        if (pathPositions.Count == 0)
            return transform.position;
        if (pathPositions.Count == 1)
            return pathPositions[0];

        float[] cumulativeDistances = new float[pathPositions.Count];
        for (int i = 1; i < pathPositions.Count; i++)
            cumulativeDistances[i] = cumulativeDistances[i - 1] + Vector3.Distance(pathPositions[i - 1], pathPositions[i]);

        float totalLength = cumulativeDistances[^1];
        if (totalLength <= 0.00001f)
            return pathPositions[0];

        targetDistance = Mathf.Clamp(targetDistance, 0f, totalLength);
        int upper = 1;
        while (upper < pathPositions.Count - 1 && cumulativeDistances[upper] < targetDistance)
            upper++;

        int startIndex = Mathf.Clamp(upper - 1, 0, pathPositions.Count - 2);
        int endIndex = Mathf.Clamp(upper, 1, pathPositions.Count - 1);
        float startDist = cumulativeDistances[startIndex];
        float endDist = cumulativeDistances[endIndex];
        float segLen = endDist - startDist;
        float t = segLen <= 0.00001f ? 0f : (targetDistance - startDist) / segLen;
        return Vector3.Lerp(pathPositions[startIndex], pathPositions[endIndex], t);
    }
}