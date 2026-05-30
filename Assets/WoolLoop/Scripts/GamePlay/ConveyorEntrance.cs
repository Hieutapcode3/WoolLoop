using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ConveyorEntrance : MonoBehaviour
{
    [TitleGroup("References")]
    [SerializeField, Required] private ConveyorController conveyorController;
    [SerializeField, Required] private Transform waitPoint;
    [SerializeField, Required, AssetsOnly] private YarnItem yarnItemPrefab;

    [TitleGroup("Dispatch")]
    [Tooltip("0 = chỉ đúng 1 spot gần waitPoint nhất.")]
    [SerializeField, Min(0)] private int spotSearchSlotDistance = 0;
    [SerializeField, Min(0.05f)] private float moveToEntranceDuration = 0.25f;

    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private WoolBall activeDispatchingWoolBall;

    public bool IsEntranceBusy =>
        activeDispatchingWoolBall != null;

    public Vector3 WaitPosition => waitPoint != null ? waitPoint.position : transform.position;
    public bool CanAcceptWoolBallClick => !IsEntranceBusy;

    private void FixedUpdate()
    {
        ProcessActiveDispatchingBall();
    }

    public void RequestDispatch(WoolBall woolBall)
    {
        if (woolBall == null || !woolBall.HasYarnRemaining || !CanAcceptWoolBallClick)
            return;

        if (conveyorController == null)
            conveyorController = FindFirstObjectByType<ConveyorController>();

        woolBall.MoveIntoConveyorEntrance(this, WaitPosition, moveToEntranceDuration);
    }

    public void OnWoolBallArrived(WoolBall woolBall)
    {
        if (woolBall == null || yarnItemPrefab == null || waitPoint == null)
            return;

        if (activeDispatchingWoolBall != null && activeDispatchingWoolBall != woolBall)
            return;

        activeDispatchingWoolBall = woolBall;
        woolBall.BeginDispatchAtWait(WaitPosition);
    }

    public void ReleaseActiveDispatchingBall(WoolBall woolBall)
    {
        if (activeDispatchingWoolBall == woolBall)
            activeDispatchingWoolBall = null;
    }

    private void ProcessActiveDispatchingBall()
    {
        if (activeDispatchingWoolBall == null)
            return;

        if (!activeDispatchingWoolBall.HasYarnRemaining)
        {
            FinishDispatchingBall(activeDispatchingWoolBall);
            return;
        }

        TrySpawnYarnDirectToConveyor(activeDispatchingWoolBall);
    }

    private void FinishDispatchingBall(WoolBall woolBall)
    {
        ReleaseActiveDispatchingBall(woolBall);
        woolBall.Complete();
    }

    private void TrySpawnYarnDirectToConveyor(WoolBall woolBall)
    {
        if (woolBall == null || !woolBall.HasYarnRemaining || yarnItemPrefab == null || conveyorController == null)
            return;

        if (!conveyorController.TryFindNearestEmptySpotBySlot(WaitPosition, spotSearchSlotDistance, out ConveyorSpot spot))
            return;

        YarnItem item;
        if (!yarnItemPrefab.Use<YarnItem>(spot.transform, Vector3.zero, Quaternion.identity, out item))
            return;

        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
        item.Initialize(woolBall.WoolColorType);

        if (!spot.TryAttachYarnItem(item))
        {
            Destroy(item.gameObject);
            return;
        }

        woolBall.ConsumeOneYarnUnit();
    }

#if UNITY_EDITOR
    [TitleGroup("Gizmos")]
    [SerializeField] private bool showSearchAreaGizmos = true;
    [SerializeField] private float spotGizmoRadius = 0.3f;
    [SerializeField] private Color waitPointColor = Color.yellow;
    [SerializeField] private Color nearestSpotColor = Color.cyan;
    [SerializeField] private Color inRangeSpotColor = new(0.2f, 1f, 0.3f, 0.85f);
    [SerializeField] private Color occupiedInRangeColor = new(1f, 0.45f, 0.1f, 0.85f);
    [SerializeField] private Color outOfRangeSpotColor = new(1f, 1f, 1f, 0.2f);

    private void OnDrawGizmos()
    {
        if (!showSearchAreaGizmos)
            return;
        DrawSearchAreaGizmos(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showSearchAreaGizmos)
            return;
        DrawSearchAreaGizmos(true);
    }

    private void DrawSearchAreaGizmos(bool drawLabels)
    {
        Vector3 wait = WaitPosition;
        Gizmos.color = waitPointColor;
        Gizmos.DrawWireSphere(wait, spotGizmoRadius * 0.65f);
        Gizmos.DrawSphere(wait, spotGizmoRadius * 0.2f);

        ConveyorController controller = ResolveConveyorController();
        if (controller == null)
            return;

        int spotCount = controller.GetSpotTransformCount();
        if (spotCount == 0)
            return;

        int entranceIndex = controller.GetNearestSpotIndex(wait);
        if (entranceIndex < 0)
            return;

        if (!controller.TryGetSpotAtIndex(entranceIndex, out Transform nearestTransform, out _))
            return;

        Gizmos.color = nearestSpotColor;
        Gizmos.DrawLine(wait, nearestTransform.position);
        Gizmos.DrawWireSphere(nearestTransform.position, spotGizmoRadius * 1.1f);

        int searchRange = Mathf.Max(0, spotSearchSlotDistance);
        for (int i = 0; i < spotCount; i++)
        {
            if (!controller.TryGetSpotAtIndex(i, out Transform spotTransform, out ConveyorSpot conveyorSpot))
                continue;

            int slotOffset = controller.GetSlotOffsetBetween(entranceIndex, i);
            bool inRange = slotOffset <= searchRange;
            bool occupied = conveyorSpot != null && conveyorSpot.IsOccupied;

            Gizmos.color = inRange
                ? (occupied ? occupiedInRangeColor : inRangeSpotColor)
                : outOfRangeSpotColor;
            Gizmos.DrawWireSphere(spotTransform.position, inRange ? spotGizmoRadius : spotGizmoRadius * 0.55f);

            if (drawLabels && inRange)
            {
                Handles.color = Gizmos.color;
                Handles.Label(spotTransform.position + Vector3.up * 0.35f, $"#{i} (+{slotOffset})");
            }
        }

        if (drawLabels)
        {
            Handles.color = waitPointColor;
            string state = IsEntranceBusy ? "Busy" : "Ready";
            Handles.Label(wait + Vector3.up * 0.4f, $"WaitPoint ({state})");
        }
    }

    private ConveyorController ResolveConveyorController()
    {
        if (conveyorController != null)
            return conveyorController;
        return FindFirstObjectByType<ConveyorController>();
    }
#endif
}
