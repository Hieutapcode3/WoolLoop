using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class YarnBall : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    [TitleGroup("Yarn")]
    [SerializeField] private WoolColorType woolColorType = WoolColorType.Red;
    [SerializeField, Min(1)][HideInInspector] private int yarnUnitCount = 10;
    [TitleGroup("Visual")]
    [SerializeField][HideInInspector] private Renderer[] rollRenderers;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private bool isMovingToEntrance;
    [ShowInInspector, ReadOnly]
    private bool isDispatchingAtWait;

    private MaterialPropertyBlock propertyBlock;
    private Tween moveTween;

    public WoolColorType WoolColorType => woolColorType;
    public int YarnUnitCount => yarnUnitCount;
    public bool HasYarnRemaining => yarnUnitCount > 0;
    public bool IsMovingToEntrance => isMovingToEntrance;
    public bool IsDispatchingAtWait => isDispatchingAtWait;

    private void Awake()
    {
        EnsureRollRenderers();
        ApplyColorToRoll();
    }


    private void OnMouseDown()
    {
        HandleClick();
    }

    private void HandleClick()
    {
        Debug.Log($"YarnBall clicked. Color: {woolColorType}, Remaining Units: {yarnUnitCount}");
    }

    public void MoveToEntrance(Vector3 waitPosition, float duration)
    {
        if (!HasYarnRemaining)
            return;

        moveTween?.Kill();
        isMovingToEntrance = true;

        moveTween = transform
            .DOMove(waitPosition, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                isMovingToEntrance = false;
            });
    }

    public void BeginDispatchAtWait(Vector3 waitPosition)
    {
        isDispatchingAtWait = true;
        isMovingToEntrance = false;
        moveTween?.Kill();
        transform.position = waitPosition;
    }

    public void ConsumeOneYarnUnit()
    {
        yarnUnitCount = Mathf.Max(0, yarnUnitCount - 1);
    }

    public void CompleteAndDestroy()
    {
        isMovingToEntrance = false;
        isDispatchingAtWait = false;
        moveTween?.Kill();
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        yarnUnitCount = Mathf.Max(1, yarnUnitCount);
        ApplyColorToRoll();
    }
#endif

    private void EnsureRollRenderers()
    {
        if (rollRenderers != null && rollRenderers.Length > 0)
            return;
        rollRenderers = GetComponents<Renderer>();
    }

    private void ApplyColorToRoll()
    {
        EnsureRollRenderers();
        Color color = ColorsParamSO.GetColor(woolColorType);
        propertyBlock ??= new MaterialPropertyBlock();
        foreach (Renderer renderer in rollRenderers)
            ApplyColorWithPropertyBlock(renderer, color, propertyBlock);
    }

    public static void ApplyColorWithPropertyBlock(Renderer renderer, Color color, MaterialPropertyBlock block)
    {
        if (renderer == null || block == null)
            return;

        int materialCount = renderer.sharedMaterials.Length;
        if (materialCount <= 0)
        {
            renderer.GetPropertyBlock(block);
            SetColorProperties(block, color);
            renderer.SetPropertyBlock(block);
            return;
        }

        for (int i = 0; i < materialCount; i++)
        {
            renderer.GetPropertyBlock(block, i);
            SetColorProperties(block, color);
            renderer.SetPropertyBlock(block, i);
        }
    }

    private static void SetColorProperties(MaterialPropertyBlock block, Color color)
    {
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
    }
}
