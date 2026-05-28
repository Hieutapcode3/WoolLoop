using Sirenix.OdinInspector;
using UnityEngine;

public class BobbinsBox : MonoBehaviour
{
    [TitleGroup("References")]

    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private WoolColorType currentColorType;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private Renderer[] targetRenderers;
    private string colorPropertyName = "_BaseColor";
    private string secondaryColorPropertyName = "_Color";
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock propertyBlock;

    public WoolColorType CurrentColorType => currentColorType;

    private void Awake()
    {
        ApplyColorToMaterials();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            ApplyColorToMaterials();
    }
#endif

    public void SetColorType(WoolColorType colorType)
    {
        currentColorType = colorType;
        ApplyColorToMaterials();
    }

    public void SetColorProperties(WoolColorType colorType)
    {
        SetColorType(colorType);
    }
    private void ApplyColorToMaterials()
    {
        targetRenderers ??= GetComponentsInChildren<Renderer>(true);

        propertyBlock ??= new MaterialPropertyBlock();

        Color color = ResolveColor(currentColorType);
        int propertyId = ResolvePropertyId();

        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(propertyId, color);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private int ResolvePropertyId()
    {
        if (!string.IsNullOrWhiteSpace(colorPropertyName))
            return Shader.PropertyToID(colorPropertyName);

        if (!string.IsNullOrWhiteSpace(secondaryColorPropertyName))
            return Shader.PropertyToID(secondaryColorPropertyName);

        return ColorId != 0 ? ColorId : BaseColorId;
    }

    private Color ResolveColor(WoolColorType colorType)
    {
        return ColorsParamSO.GetColor(colorType);
    }
}
