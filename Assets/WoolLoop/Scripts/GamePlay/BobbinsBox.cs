using System;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BobbinsBox : MonoBehaviour
{
    [TitleGroup("References")]
    [SerializeField] private ColorsParamSO colorsParam;
    [SerializeField] private Transform boxSpotParent;
    [SerializeField] private GameObject boxSpotPrefab;

    [TitleGroup("Layout")]
    [SerializeField] private Vector2 spotSpacing = new Vector2(0.15f, 0.15f);
    [SerializeField, Min(1)] private int itemColumns = 2;
    [ShowInInspector, ReadOnly]
    private GameObject boxLid;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private WoolColorType currentColorType;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private BobbinsBoxSize boxSize;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private int spotCount;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private Renderer[] targetRenderers;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private Transform[] builtSpots;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private bool isOpen;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private bool isBottomLine;
    private string colorPropertyName = "_BaseColor";
    private string secondaryColorPropertyName = "_Color";
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock propertyBlock;

    public WoolColorType CurrentColorType => currentColorType;
    public bool IsOpen => isOpen;
    public bool IsBottomLine => isBottomLine;

    private void Awake()
    {
        ApplyColorToMaterials();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (PrefabUtility.IsPartOfPrefabAsset(this))
            return;

        if (!Application.isPlaying)
        {
            RefreshSpotPositions();
            ApplyColorToMaterials();
        }
    }
#endif

    public void InitBobbinsBox(WoolColorType colorType, BobbinsBoxSize size = BobbinsBoxSize.Size_8, bool open = false, bool bottomLine = false)
    {
        currentColorType = colorType;
        boxSize = size;
        isOpen = open;
        isBottomLine = bottomLine;
        ApplyColorToMaterials();
    }

    private void BuildSpotsForSize()
    {
        EnsureSpotRoot();
        if (boxSpotParent == null)
            return;

        spotCount = (int)boxSize;
        int columns = Mathf.Max(1, itemColumns);
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)spotCount / columns));

        if (Application.isPlaying)
            ClearBuiltSpots();

        builtSpots = new Transform[spotCount];

        float spacingX = Mathf.Max(0f, spotSpacing.x);
        float spacingZ = Mathf.Max(0f, spotSpacing.y);
        float width = (columns - 1) * spacingX;
        float depth = (rows - 1) * spacingZ;

        for (int i = 0; i < spotCount; i++)
        {
            Transform spot = InstantiateSpot();
            if (spot == null)
                continue;

            int col = i % columns;
            int row = i / columns;
            Vector3 localPos = new Vector3(
                -width * 0.5f + col * spacingX,
                0f,
                depth * 0.5f - row * spacingZ);

            if (spot.parent != boxSpotParent)
                spot.SetParent(boxSpotParent, false);

            spot.localPosition = localPos;
            // spot.localRotation = Quaternion.identity;
            spot.localScale = Vector3.one;
            spot.name = $"Spot_{i:00}";
            builtSpots[i] = spot;
        }
    }

    private void RefreshSpotPositions()
    {
        if (builtSpots == null || builtSpots.Length == 0)
            return;

        int columns = Mathf.Max(1, itemColumns);
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)builtSpots.Length / columns));

        float spacingX = Mathf.Max(0f, spotSpacing.x);
        float spacingZ = Mathf.Max(0f, spotSpacing.y);
        float width = (columns - 1) * spacingX;
        float depth = (rows - 1) * spacingZ;

        for (int i = 0; i < builtSpots.Length; i++)
        {
            Transform spot = builtSpots[i];
            if (spot == null)
                continue;

            int col = i % columns;
            int row = i / columns;
            spot.localPosition = new Vector3(
                -width * 0.5f + col * spacingX,
                0f,
                depth * 0.5f - row * spacingZ);
        }
    }

    private int ResolveSpotCountFromSize(BobbinsBoxSize size)
    {
        return size switch
        {
            BobbinsBoxSize.Size_2 => 2,
            BobbinsBoxSize.Size_4 => 4,
            BobbinsBoxSize.Size_6 => 6,
            BobbinsBoxSize.Size_8 => 8,
            BobbinsBoxSize.Size_10 => 10,
            _ => 8,
        };
    }

    private void EnsureSpotRoot()
    {
        if (boxSpotParent != null)
            return;

        boxSpotParent = transform;
    }

    private Transform InstantiateSpot()
    {
        if (boxSpotPrefab == null)
        {
            GameObject go = new GameObject("Spot");
            Transform spotTransform = go.transform;
            spotTransform.SetParent(boxSpotParent, false);
            return spotTransform;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject prefabInstance = PrefabUtility.InstantiatePrefab(boxSpotPrefab) as GameObject;
            if (prefabInstance == null)
                return null;

            prefabInstance.transform.SetParent(boxSpotParent, false);
            return prefabInstance.transform;
        }
#endif

        GameObject instance = Instantiate(boxSpotPrefab, boxSpotParent, false);
        return instance.transform;
    }
    private void ClearBuiltSpots()
    {
        if (builtSpots == null)
            return;

        for (int i = builtSpots.Length - 1; i >= 0; i--)
        {
            Transform spot = builtSpots[i];
            if (spot == null)
                continue;
            Destroy(spot.gameObject);
        }
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
        if (colorsParam != null)
            return colorsParam.GetColor(colorType);
        return Color.white;
    }
}
