using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ConveyorSpot : MonoBehaviour
{
    [TitleGroup("References")]
    [SerializeField] private ConveyorController controller;

    [TitleGroup("Movement")]
    [SerializeField] private bool rotateAlongPath = true;
    [SerializeField, Min(0.01f)] private float lookAheadDistance = 0.15f;
    [SerializeField] private Vector3 upAxis = Vector3.up;

    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private int spotIndex;

    [ShowInInspector, ReadOnly]
    private bool isOccupied;

    private float distanceAlongPath;
    private float baseDistanceAlongPath;
    private readonly List<Vector3> cachedPath = new();
    private readonly List<float> cachedCumDist = new();
    private float cachedTotalLength;
    private bool cachedSmoothLoopMode;
    private YarnItem occupyingYarnItem;

    public bool IsOccupied
    {
        get
        {
            if (occupyingYarnItem != null)
                return true;

            occupyingYarnItem = GetComponentInChildren<YarnItem>();
            return occupyingYarnItem != null;
        }
    }

    public float PathDistance => distanceAlongPath;

    public void Setup(ConveyorController conveyorController, float startDistance, int index)
    {
        controller = conveyorController;
        spotIndex = index;
        baseDistanceAlongPath = Mathf.Max(0f, startDistance);
        distanceAlongPath = baseDistanceAlongPath;
        RebuildCache();
        ApplyPosition();
    }

    public float GetDistanceAlongPath() => distanceAlongPath;

    public void SetDistanceAlongPath(float newDistance)
    {
        baseDistanceAlongPath = Mathf.Max(0f, newDistance);
        distanceAlongPath = baseDistanceAlongPath;
        ApplyPosition();
    }

    public void ForceCacheRebuild()
    {
        RebuildCache();
        ApplyPosition();
    }

    public bool TryAttachYarnItem(YarnItem item)
    {
        if (item == null || occupyingYarnItem != null)
            return false;

        if (GetComponentInChildren<YarnItem>() != null && GetComponentInChildren<YarnItem>() != item)
            return false;

        occupyingYarnItem = item;
        item.AttachToSpot(this);
        item.transform.SetParent(transform, worldPositionStays: false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        return true;
    }

    public void ReleaseYarnItem(YarnItem item)
    {
        if (occupyingYarnItem != item)
            return;

        occupyingYarnItem = null;
    }

    public bool IsOnCurve(float angleThreshold, float sampleDistance)
    {
        if (cachedPath.Count < 3 || cachedTotalLength <= 0.00001f)
            return false;

        float span = Mathf.Max(0.05f, sampleDistance);
        float s = distanceAlongPath;

        if (GetTurnAngleAtDistance(s, span) > angleThreshold)
            return true;

        float wideSpan = Mathf.Min(span * 3f, cachedTotalLength * 0.25f);
        return GetTurnAngleAtDistance(s, wideSpan) > angleThreshold;
    }

    private float GetTurnAngleAtDistance(float pathDistance, float halfWindow)
    {
        float dPrev = GetOffsetPathDistance(pathDistance - halfWindow);
        float dNext = GetOffsetPathDistance(pathDistance + halfWindow);
        Vector3 pPrev = SampleAtDistance(dPrev);
        Vector3 pCur = SampleAtDistance(pathDistance);
        Vector3 pNext = SampleAtDistance(dNext);

        Vector3 inDir = pCur - pPrev;
        Vector3 outDir = pNext - pCur;
        if (inDir.sqrMagnitude <= 0.000001f || outDir.sqrMagnitude <= 0.000001f)
            return 0f;

        return Vector3.Angle(inDir, outDir);
    }

    private void FixedUpdate()
    {
        if (controller == null)
            return;

        bool useSmoothLoop = controller.UseSmoothLoopMoveToStart();
        if (cachedPath.Count < 2 || cachedTotalLength <= 0.00001f || cachedSmoothLoopMode != useSmoothLoop)
            RebuildCache();

        if (cachedPath.Count < 2 || cachedTotalLength <= 0.00001f)
            return;

        float speed = controller.GetConveyorSpotSpeed();
        distanceAlongPath += speed * Time.fixedDeltaTime;

        if (!useSmoothLoop && distanceAlongPath >= cachedTotalLength)
            distanceAlongPath = 0f;

        if (useSmoothLoop && cachedTotalLength > 0.00001f)
            distanceAlongPath = Mathf.Repeat(distanceAlongPath, cachedTotalLength);

        ApplyPosition();
        isOccupied = IsOccupied;
    }

    private void RebuildCache()
    {
        cachedPath.Clear();
        cachedCumDist.Clear();
        cachedTotalLength = 0f;

        if (controller == null)
            return;

        cachedSmoothLoopMode = controller.UseSmoothLoopMoveToStart();
        List<Vector3> path = controller.GetAllPathPositions();
        if (path == null || path.Count < 2)
            return;

        cachedPath.AddRange(path);
        cachedCumDist.Capacity = cachedPath.Count;
        cachedCumDist.Add(0f);
        for (int i = 1; i < cachedPath.Count; i++)
        {
            float d = Vector3.Distance(cachedPath[i - 1], cachedPath[i]);
            cachedTotalLength += d;
            cachedCumDist.Add(cachedTotalLength);
        }
    }

    private void ApplyPosition()
    {
        if (cachedPath.Count < 2 || cachedTotalLength <= 0.00001f)
            return;

        Vector3 posOnPath = SampleAtDistance(distanceAlongPath);
        transform.position = posOnPath + controller.GetSpotOffset();
        ApplyRotation(posOnPath);
    }

    private void ApplyRotation(Vector3 currentPathPosition)
    {
        if (!rotateAlongPath)
            return;

        float nextDistance = distanceAlongPath + Mathf.Max(0.01f, lookAheadDistance);
        if (controller != null && controller.UseSmoothLoopMoveToStart())
            nextDistance = Mathf.Repeat(nextDistance, cachedTotalLength);
        else
            nextDistance = Mathf.Clamp(nextDistance, 0f, cachedTotalLength);

        Vector3 nextPathPosition = SampleAtDistance(nextDistance);
        Vector3 moveDirection = nextPathPosition - currentPathPosition;
        if (moveDirection.sqrMagnitude <= 0.000001f)
            return;

        Vector3 up = upAxis.sqrMagnitude <= 0.000001f ? Vector3.up : upAxis.normalized;
        transform.rotation = Quaternion.LookRotation(moveDirection.normalized, up);
    }

    private Vector3 SampleAtDistance(float distance)
    {
        distance = controller != null && controller.UseSmoothLoopMoveToStart()
            ? Mathf.Repeat(distance, cachedTotalLength)
            : Mathf.Clamp(distance, 0f, cachedTotalLength);

        int upper = 1;
        while (upper < cachedCumDist.Count - 1 && cachedCumDist[upper] < distance)
            upper++;

        int startIndex = Mathf.Clamp(upper - 1, 0, cachedPath.Count - 2);
        int endIndex = Mathf.Clamp(upper, 1, cachedPath.Count - 1);

        float startDist = cachedCumDist[startIndex];
        float endDist = cachedCumDist[endIndex];
        float segLen = endDist - startDist;
        float t = segLen <= 0.00001f ? 0f : (distance - startDist) / segLen;

        return Vector3.Lerp(cachedPath[startIndex], cachedPath[endIndex], t);
    }

    private float GetOffsetPathDistance(float distance)
    {
        if (controller != null && controller.UseSmoothLoopMoveToStart())
            return Mathf.Repeat(distance, cachedTotalLength);
        return Mathf.Clamp(distance, 0f, cachedTotalLength);
    }
}
