using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ConveyorSpot : MonoBehaviour
{
    [SerializeField] private ConveyorController controller;
    [SerializeField] private bool rotateAlongPath = true;
    [SerializeField, Min(0.01f)] private float lookAheadDistance = 0.15f;
    [SerializeField] private Vector3 upAxis = Vector3.up;
    [SerializeField] private Transform boneUp;
    [SerializeField] private Transform boneDown;
    [SerializeField, Min(0.01f)] private float boneUpScaleAmount = 1.2f;
    [SerializeField, Min(0.01f)] private float boneDownScaleAmount = 0.8f;
    [SerializeField, Range(0f, 180f)] private float curveAngleThreshold = 12f;
    [SerializeField, Min(0.01f)] private float curveSampleDistance = 0.35f;
    [SerializeField, Min(0.01f)] private float boneScaleDuration = 0.2f;
    private float distanceAlongPath;
    private float baseDistanceAlongPath;
    private readonly List<Vector3> cachedPath = new();
    private readonly List<float> cachedCumDist = new();
    private float cachedTotalLength;
    private bool cachedSmoothLoopMode;
    private Vector3 originalScale;
    private Vector3 boneUpOriginalScale;
    private Vector3 boneDownOriginalScale;
    private bool boneOriginalScalesCached;
    private Tween boneUpScaleTween;
    private Tween boneDownScaleTween;

    public void Setup(ConveyorController conveyorController, float startDistance)
    {
        controller = conveyorController;
        baseDistanceAlongPath = Mathf.Max(0f, startDistance);
        distanceAlongPath = baseDistanceAlongPath;
        if (originalScale == Vector3.zero)
            originalScale = transform.localScale;
        CacheBoneOriginalScalesIfNeeded();
        RebuildCache();
        ApplyPosition();
    }

    public float GetDistanceAlongPath()
    {
        return distanceAlongPath;
    }

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
    }

    private void OnDisable()
    {
        boneUpScaleTween?.Kill();
        boneUpScaleTween = null;
        boneDownScaleTween?.Kill();
        boneDownScaleTween = null;
    }

    private void CacheBoneOriginalScalesIfNeeded()
    {
        if (boneOriginalScalesCached)
            return;
        if (boneUp != null)
            boneUpOriginalScale = boneUp.localScale;
        if (boneDown != null)
            boneDownOriginalScale = boneDown.localScale;
        if (boneUp != null && boneDown != null)
            boneOriginalScalesCached = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        boneOriginalScalesCached = false;
        CacheBoneOriginalScalesIfNeeded();
    }
#endif

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
        ApplyBoneScaling();
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

    private void ApplyBoneScaling()
    {
        CacheBoneOriginalScalesIfNeeded();
        if (boneUp == null || boneDown == null || cachedPath.Count < 3)
            return;

        float span = Mathf.Max(0.01f, curveSampleDistance);
        float s = distanceAlongPath;
        Vector3 tangentA = GetForwardTangentAt(s, span);
        Vector3 tangentB = GetForwardTangentAt(GetOffsetPathDistance(s + span), span);
        float angle = Vector3.Angle(tangentA, tangentB);
        bool isOnCurve = angle > curveAngleThreshold;

        if (isOnCurve)
        {
            ScaleBoneX(boneUp, boneUpScaleAmount, ref boneUpScaleTween);
            ScaleBoneX(boneDown, boneDownScaleAmount, ref boneDownScaleTween);
        }
        else
        {
            ScaleBoneX(boneUp, boneUpOriginalScale.x, ref boneUpScaleTween);
            ScaleBoneX(boneDown, boneDownOriginalScale.x, ref boneDownScaleTween);
        }
    }

    private float GetOffsetPathDistance(float distance)
    {
        if (controller != null && controller.UseSmoothLoopMoveToStart())
            return Mathf.Repeat(distance, cachedTotalLength);
        return Mathf.Clamp(distance, 0f, cachedTotalLength);
    }

    private Vector3 GetForwardTangentAt(float atDistance, float halfSpan)
    {
        float d0 = GetOffsetPathDistance(atDistance - halfSpan);
        float d1 = GetOffsetPathDistance(atDistance + halfSpan);
        Vector3 p0 = SampleAtDistance(d0);
        Vector3 p1 = SampleAtDistance(d1);
        Vector3 t = p1 - p0;
        return t.sqrMagnitude > 0.000001f ? t.normalized : transform.forward;
    }

    private void ScaleBoneX(Transform bone, float targetScaleX, ref Tween slot)
    {
        if (bone == null)
            return;

        if (Mathf.Approximately(bone.localScale.x, targetScaleX))
            return;

        slot?.Kill();
        slot = bone.DOScaleX(targetScaleX, boneScaleDuration);
    }
}