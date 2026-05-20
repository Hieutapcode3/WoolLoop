using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class ConveyorPathPoint : MonoBehaviour
{
    [OnValueChanged(nameof(RefreshConveyorPath))]
    [Tooltip("If enabled, this point becomes a rounded corner instead of a sharp turn.")]
    public bool smoothCorner = false;

    [OnValueChanged(nameof(RefreshConveyorPath))]
    [Tooltip("If enabled, the path segment from this point to the next point is skipped visually and items jump to the next segment.")]
    public bool skipSegmentToNext = false;

    [OnValueChanged(nameof(RefreshConveyorPath))]
    [Min(0.01f)]
    [Tooltip("How far the curve starts before and ends after this point.")]
    public float cornerRadius = 0.35f;

    [OnValueChanged(nameof(RefreshConveyorPath))]
    [Range(2, 20)]
    [Tooltip("How many segments are used to draw this rounded corner.")]
    public int cornerSegments = 6;

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshConveyorPath();
    }
#endif

    private void RefreshConveyorPath()
    {
        var controller = GetComponentInParent<ConveyorController>();
        if (controller != null)
        {
            controller.RefreshPathVisual();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
#endif
    }
}
