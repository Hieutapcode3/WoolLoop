using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ConveyorExit : MonoBehaviour
{
    [TitleGroup("References")]
    [SerializeField, Required] private ConveyorController conveyorController;

    [TitleGroup("Dispatch")]
    [Tooltip("0 = chỉ đúng 1 spot gần exit nhất.")]
    [SerializeField, Min(0)] private int spotSearchSlotDistance = 0;
    [SerializeField] private BobbinsConveyor nearestBobbinsConveyor;

    [TitleGroup("Gizmos")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color exitColor = Color.red;
    [SerializeField] private Color targetSpotColor = Color.magenta;
    [SerializeField] private float gizmoRadius = 0.3f;

    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private bool isBusy;

    public bool IsExitBusy => isBusy;
    public Vector3 ExitPosition => transform.position;

    void OnEnable()
    {
        FindNearestBobbinsConveyor();
    }

    private void FixedUpdate()
    {
        ProcessSpotsAtExit();
    }

    private void ProcessSpotsAtExit()
    {
        ConveyorController controller = ResolveConveyorController();
        if (controller == null)
            return;

        if (!TryFindNearestOccupiedSpot(controller, ExitPosition, spotSearchSlotDistance, out ConveyorSpot spot))
            return;

        isBusy = true;
        TryDestroyYarnItemOnSpot(spot);
        isBusy = false;
    }
    void FindNearestBobbinsConveyor()
    {
        if (nearestBobbinsConveyor != null)
            return;

        BobbinsConveyor[] conveyors = FindObjectsByType<BobbinsConveyor>(FindObjectsSortMode.None);
        if (conveyors == null || conveyors.Length == 0)
            return;

        BobbinsConveyor nearest = null;
        float nearestDistSqr = float.MaxValue;
        Vector3 exitPos = ExitPosition;

        foreach (var conveyor in conveyors)
        {
            float distSqr = (conveyor.transform.position - exitPos).sqrMagnitude;
            if (distSqr < nearestDistSqr)
            {
                nearestDistSqr = distSqr;
                nearest = conveyor;
            }
        }

        nearestBobbinsConveyor = nearest;
    }
    private bool TryFindNearestOccupiedSpot(ConveyorController controller, Vector3 worldPosition, int maxSlotDistance, out ConveyorSpot spot)
    {
        spot = null;

        int spotCount = controller.GetSpotTransformCount();
        if (spotCount == 0)
            return false;

        int exitSpotIndex = controller.GetNearestSpotIndex(worldPosition);
        if (exitSpotIndex < 0)
            return false;

        int searchRange = Mathf.Max(0, maxSlotDistance);
        int bestSlotOffset = int.MaxValue;

        for (int i = 0; i < spotCount; i++)
        {
            if (!controller.TryGetSpotAtIndex(i, out _, out ConveyorSpot conveyorSpot))
                continue;

            if (conveyorSpot == null || !conveyorSpot.IsOccupied)
                continue;

            int slotOffset = controller.GetSlotOffsetBetween(exitSpotIndex, i);
            if (slotOffset > searchRange || slotOffset >= bestSlotOffset)
                continue;

            bestSlotOffset = slotOffset;
            spot = conveyorSpot;
        }

        return spot != null;
    }

    private bool TryDestroyYarnItemOnSpot(ConveyorSpot spot)
    {
        if (spot == null)
            return false;

        YarnItem yarnItem = spot.GetComponentInChildren<YarnItem>();
        if (yarnItem == null)
            return false;

        spot.ReleaseYarnItem(yarnItem);

#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(yarnItem.gameObject);
        else
#endif
            Destroy(yarnItem.gameObject);

        return true;
    }

    private ConveyorController ResolveConveyorController()
    {
        if (conveyorController != null)
            return conveyorController;

        conveyorController = FindFirstObjectByType<ConveyorController>();
        return conveyorController;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            EditorUtility.SetDirty(this);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;
        DrawSearchAreaGizmos(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;
        DrawSearchAreaGizmos(true);
    }

    private void DrawSearchAreaGizmos(bool drawLabels)
    {
        Vector3 exit = ExitPosition;
        Gizmos.color = exitColor;
        Gizmos.DrawWireSphere(exit, gizmoRadius * 0.65f);
        Gizmos.DrawSphere(exit, gizmoRadius * 0.2f);

        ConveyorController controller = ResolveConveyorController();
        if (controller == null)
            return;

        int spotCount = controller.GetSpotTransformCount();
        if (spotCount == 0)
            return;

        int exitIndex = controller.GetNearestSpotIndex(exit);
        if (exitIndex < 0)
            return;

        if (!controller.TryGetSpotAtIndex(exitIndex, out Transform nearestTransform, out _))
            return;

        Gizmos.color = targetSpotColor;
        Gizmos.DrawLine(exit, nearestTransform.position);
        Gizmos.DrawWireSphere(nearestTransform.position, gizmoRadius * 1.1f);

        int searchRange = Mathf.Max(0, spotSearchSlotDistance);
        for (int i = 0; i < spotCount; i++)
        {
            if (!controller.TryGetSpotAtIndex(i, out Transform spotTransform, out ConveyorSpot conveyorSpot))
                continue;

            int slotOffset = controller.GetSlotOffsetBetween(exitIndex, i);
            bool inRange = slotOffset <= searchRange;
            bool occupied = conveyorSpot != null && conveyorSpot.IsOccupied;

            Gizmos.color = inRange
                ? (occupied ? new Color(1f, 0.45f, 0.1f, 0.85f) : new Color(0.2f, 1f, 0.3f, 0.85f))
                : new Color(1f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(spotTransform.position, inRange ? gizmoRadius : gizmoRadius * 0.55f);

            if (drawLabels && inRange)
            {
                Handles.color = Gizmos.color;
                Handles.Label(spotTransform.position + Vector3.up * 0.35f, $"#{i} (+{slotOffset})");
            }
        }

        if (drawLabels)
        {
            Handles.color = exitColor;
            string state = IsExitBusy ? "Busy" : "Ready";
            Handles.Label(exit + Vector3.up * 0.4f, $"Exit ({state})");
        }
    }
#endif
}
