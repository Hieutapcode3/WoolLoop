using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UIElements;
using DG.Tweening;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class BobbinsBox : MonoBehaviour
{
    [TitleGroup("References")]
    [SerializeField] private Transform boxSpotParent;
    [SerializeField] private GameObject boxSpotPrefab;
    [TitleGroup("Layout")]
    [SerializeField] private Vector2 spotSpacing = new Vector2(0.15f, 0.15f);
    [SerializeField, Min(1)] private int itemColumns = 2;
    [ShowInInspector, ReadOnly] private GameObject boxVisual;
    [ShowInInspector, ReadOnly] private GameObject boxLid;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private WoolColorType currentColorType;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private BobbinsBoxSize boxSize;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private int spotCount;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private Renderer[] targetRenderers;
    private Transform[] builtSpots;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private bool isOpen;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private bool isBottomLine;
    [TitleGroup("Runtime"), ShowInInspector, ReadOnly]
    private int filledSpotCount;
    private string colorPropertyName = "_BaseColor";
    private string secondaryColorPropertyName = "_Color";
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock propertyBlock;
    private bool isBuildingSpots;
    public WoolColorType CurrentColorType => currentColorType;
    public bool IsOpen => isOpen;
    public bool IsBottomLine => isBottomLine;
    public int FilledSpotCount => filledSpotCount;

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

    public void InitBobbinsBox(WoolColorType colorType, BobbinsBoxSize size = BobbinsBoxSize.Size_8, bool bottomLine = false)
    {
        currentColorType = colorType;
        boxSize = size;
        isBottomLine = bottomLine;
        BuildSpotsForSize();
        ApplyColorToMaterials();
        ApplyModelBySize();
        SetOpenState(bottomLine);
        filledSpotCount = 0;
        RefreshFilledSpotVisuals();
    }

    public void ApplyEditModePreview(WoolColorType colorType, BobbinsBoxSize size = BobbinsBoxSize.Size_8, bool bottomLine = false)
    {
        currentColorType = colorType;
        boxSize = size;
        isBottomLine = bottomLine;
        ApplyColorToMaterials();
        ApplyModelBySize();
        SetOpenState(bottomLine);
        filledSpotCount = 0;
        RefreshFilledSpotVisuals();
    }

    private void BuildSpotsForSize()
    {
        if (isBuildingSpots)
            return;

        isBuildingSpots = true;
        try
        {
            EnsureSpotRoot();
            if (boxSpotParent == null)
                return;

            spotCount = (int)boxSize;
            int columns = Mathf.Max(1, itemColumns);
            int rows = Mathf.Max(1, Mathf.CeilToInt((float)spotCount / columns));

            if (builtSpots != null && builtSpots.Length == spotCount)
            {
                RefreshSpotPositions();
                SetVisibleForSpot();
                return;
            }

            if (!Application.isPlaying)
            {
                RefreshSpotPositionsPreview(spotCount, columns);
                return;
            }

            float spacingX = Mathf.Max(0f, spotSpacing.x);
            float spacingZ = Mathf.Max(0f, spotSpacing.y);
            float width = (columns - 1) * spacingX;
            float depth = (rows - 1) * spacingZ;

            ClearBuiltSpots();
            builtSpots = new Transform[spotCount];

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
                spot.localScale = Vector3.one;
                spot.name = $"Spot_{i:00}";
                builtSpots[i] = spot;
            }
            SetVisibleForSpot();
        }
        finally
        {
            isBuildingSpots = false;
        }
    }
    void SetVisibleForSpot()
    {
        if (builtSpots == null)
            return;

        foreach (Transform spot in builtSpots)
        {
            if (spot == null)
                continue;
            spot.gameObject.SetActive(isBottomLine);
        }
    }

    public bool TryFillNextSpot(YarnItem yarnItem)
    {
        if (yarnItem == null || !isBottomLine)
            return false;

        if (builtSpots == null || builtSpots.Length == 0)
            BuildSpotsForSize();

        if (builtSpots == null || builtSpots.Length == 0)
            return false;

        if (filledSpotCount >= builtSpots.Length)
            return false;

        Transform spot = builtSpots[filledSpotCount];
        if (spot == null)
            return false;

        if (spot.TryGetComponent(out ConveyorSpot conveyorSpot))
        {
            if (conveyorSpot.IsOccupied)
                return false;

            if (!conveyorSpot.TryAttachYarnItem(yarnItem))
                return false;
        }
        else
        {
            yarnItem.transform.SetParent(spot, worldPositionStays: false);
            yarnItem.transform.localPosition = Vector3.zero;
            yarnItem.transform.localRotation = Quaternion.identity;
            yarnItem.AttachToSpot(spot.GetComponentInParent<ConveyorSpot>());
        }

        filledSpotCount++;
        RefreshFilledSpotVisuals();
        return true;
    }

    private void RefreshFilledSpotVisuals()
    {
        if (builtSpots == null)
            return;

        for (int i = 0; i < builtSpots.Length; i++)
        {
            Transform spot = builtSpots[i];
            if (spot == null)
                continue;

            bool filled = i < filledSpotCount;
            if (spot.childCount > 0)
                spot.GetChild(0).gameObject.SetActive(true);
            if (!isBottomLine)
                spot.gameObject.SetActive(false);
        }
    }
    private void ApplyModelBySize()
    {
        if (boxLid == null)
            boxLid = FindObjectByKeyword("lid");
        if (boxVisual == null)
            boxVisual = FindObjectByKeyword("visual");
        if (boxLid != null)
            boxLid.SetActive(!isBottomLine);
    }

    private GameObject FindObjectByKeyword(string keyword = "")
    {
        var root = transform;
        if (root == null)
            return null;

        keyword = keyword?.ToLowerInvariant() ?? string.Empty;
        Transform[] children = root.GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child == root) continue;
            if (child.name.ToLowerInvariant().Contains(keyword))
                return child.gameObject;
        }

        return null;
    }

    private void SetOpenState(bool open)
    {
        isOpen = open;
        if (boxVisual == null)
            boxVisual = FindObjectByKeyword("visual");

        if (boxVisual == null)
            return;

        if (Application.isPlaying)
            boxVisual.transform.DOScaleY(isOpen ? 2.1f : 0.5f, 0.5f).SetEase(Ease.InOutSine);
        else boxVisual.transform.localScale = new Vector3(1f, isOpen ? 2.1f : 0.5f, 1f);
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

    private void RefreshSpotPositionsPreview(int count, int columns)
    {
        if (boxSpotParent == null)
            return;

        float spacingX = Mathf.Max(0f, spotSpacing.x);
        float spacingZ = Mathf.Max(0f, spotSpacing.y);
        int rows = Mathf.Max(1, Mathf.CeilToInt((float)count / columns));
        float width = (columns - 1) * spacingX;
        float depth = (rows - 1) * spacingZ;

        int childCount = boxSpotParent.childCount;
        int limit = Mathf.Min(count, childCount);

        for (int i = 0; i < limit; i++)
        {
            Transform spot = boxSpotParent.GetChild(i);
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
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(boxSpotPrefab, boxSpotParent);
            return instance != null ? instance.transform : null;
        }
#endif

        GameObject runtimeInstance = Instantiate(boxSpotPrefab, boxSpotParent, false);
        return runtimeInstance.transform;
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

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(spot.gameObject);
            else
#endif
                Destroy(spot.gameObject);
        }
    }

    private void ApplyColorToMaterials()
    {
        targetRenderers ??= GetFilteredRenderers();
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

    private Renderer[] GetFilteredRenderers()
    {
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
        if (boxSpotParent == null)
            return allRenderers;

        int count = 0;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer renderer = allRenderers[i];
            if (renderer == null)
                continue;

            if (renderer.transform == boxSpotParent || renderer.transform.IsChildOf(boxSpotParent))
                continue;

            allRenderers[count++] = renderer;
        }

        if (count == allRenderers.Length)
            return allRenderers;

        Renderer[] filtered = new Renderer[count];
        Array.Copy(allRenderers, filtered, count);
        return filtered;
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
