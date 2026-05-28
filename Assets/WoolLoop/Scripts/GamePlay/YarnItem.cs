using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class YarnItem : MonoBehaviour
{
    [TitleGroup("Color")]
    [SerializeField] private WoolColorType colorType = WoolColorType.Red;
    [SerializeField] private Renderer targetRenderer;

    [TitleGroup("Curve Bones")]
    [SerializeField] private Transform boneUp;
    [SerializeField] private Transform boneDown;
    [SerializeField, Min(0.01f)] private float boneUpScaleMultiplier = 1.25f;
    [SerializeField, Min(0.01f)] private float boneDownScaleMultiplier = 0.75f;
    [SerializeField, Range(0f, 180f)] private float curveAngleThreshold = 5f;
    [SerializeField, Min(0.01f)] private float curveSampleDistance = 0.4f;
    [SerializeField, Min(0.01f)] private float boneScaleDuration = 0.12f;

    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private ConveyorSpot parentSpot;

    private MaterialPropertyBlock propertyBlock;
    private Vector3 boneUpOriginalScale = Vector3.one;
    private Vector3 boneDownOriginalScale = Vector3.one;
    private bool boneUpScaleCached;
    private bool boneDownScaleCached;
    private bool wasOnCurve;
    private Tween boneUpScaleTween;
    private Tween boneDownScaleTween;

    public WoolColorType ColorType => colorType;

    public void AttachToSpot(ConveyorSpot spot)
    {
        parentSpot = spot;
        CacheBoneOriginalScales();
    }
    public void Initialize(WoolColorType type, ColorsParamSO colorsParam)
    {
        colorType = type;
        if (colorsParam != null)
            SetDisplayColor(colorsParam.GetColor(colorType));
        CacheBoneOriginalScales();
    }
    public void SetColorType(WoolColorType type) => colorType = type;
    public void SetDisplayColor(Color color)
    {
        if (targetRenderer == null && !TryGetComponent(out targetRenderer))
            return;

        propertyBlock ??= new MaterialPropertyBlock();
        YarnBall.ApplyColorWithPropertyBlock(targetRenderer, color, propertyBlock);
    }
    private void LateUpdate()
    {
        ApplyBoneScalingFromParentSpot();
    }

    private void OnDisable()
    {
        boneUpScaleTween?.Kill();
        boneUpScaleTween = null;
        boneDownScaleTween?.Kill();
        boneDownScaleTween = null;
    }
    private void OnDestroy()
    {
        if (parentSpot != null)
            parentSpot.ReleaseYarnItem(this);
        ConveyorEntrance entrance = FindFirstObjectByType<ConveyorEntrance>();
    }
    private void CacheBoneOriginalScales()
    {
        if (boneUp != null && !boneUpScaleCached)
        {
            boneUpOriginalScale = boneUp.localScale;
            boneUpScaleCached = true;
        }

        if (boneDown != null && !boneDownScaleCached)
        {
            boneDownOriginalScale = boneDown.localScale;
            boneDownScaleCached = true;
        }
    }

    private void ApplyBoneScalingFromParentSpot()
    {
        if (parentSpot == null)
            parentSpot = GetComponentInParent<ConveyorSpot>();

        if (parentSpot == null)
            return;

        if (!boneUpScaleCached && !boneDownScaleCached)
            CacheBoneOriginalScales();

        bool isOnCurve = parentSpot.IsOnCurve(curveAngleThreshold, curveSampleDistance);
        if (isOnCurve == wasOnCurve)
            return;

        wasOnCurve = isOnCurve;

        if (boneUp != null && boneUpScaleCached)
        {
            Vector3 target = isOnCurve
                ? new Vector3(boneUpOriginalScale.x * boneUpScaleMultiplier, boneUpOriginalScale.y, boneUpOriginalScale.z)
                : boneUpOriginalScale;
            ApplyBoneScale(boneUp, target, ref boneUpScaleTween);
        }

        if (boneDown != null && boneDownScaleCached)
        {
            Vector3 target = isOnCurve
                ? new Vector3(boneDownOriginalScale.x * boneDownScaleMultiplier, boneDownOriginalScale.y, boneDownOriginalScale.z)
                : boneDownOriginalScale;
            ApplyBoneScale(boneDown, target, ref boneDownScaleTween);
        }
    }

    private void ApplyBoneScale(Transform bone, Vector3 targetScale, ref Tween slot)
    {
        if (bone == null)
            return;

        slot?.Kill();
        slot = bone.DOScale(targetScale, boneScaleDuration).SetEase(Ease.OutQuad);
    }
}
