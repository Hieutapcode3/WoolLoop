using Dreamteck.Splines;
using UnityEngine;

public sealed class LevelGenerationController : MonoBehaviour
{
    [SerializeField] private TextAsset levelJson;
    [SerializeField] private Transform cellRoot;
    [SerializeField] private Transform frameRoot;
    [SerializeField] private Material frameMaterial;
    [SerializeField] private Mesh frameMesh;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField, Min(0f)] private float frameWallWidth = 0f;
    [SerializeField] private int frameTubeSides = 8;
    [SerializeField] private int frameSampleRate = 4;
    [SerializeField, Min(0f)] private float frameCornerRoundness = 0.35f;
    [SerializeField, Min(1)] private int frameCornerSegments = 6;

    public LevelData CurrentLevel { get; private set; }
    public SplineComputer PrimaryFrameSpline { get; private set; }
    public float ActiveFrameWallWidth { get; private set; }

    private ILevelDataLoader _loader;
    private ICellSpawner _cellSpawner;
    private IFrameSplineBuilder _frameSplineBuilder;
    private bool _hasRuntimePreviewFrameWallWidth;
    private float _runtimePreviewFrameWallWidth;

    private void Awake()
    {
        EnsureServices();
    }

    private void Start()
    {
        if (generateOnStart)
            Generate();
    }

    public void Generate()
    {
        EnsureServices();
        Clear();

        if (levelJson == null)
        {
            Debug.LogError("LevelGenerationController requires a level JSON TextAsset.");
            return;
        }

        CurrentLevel = ((LevelDataLoader)_loader).LoadFromJson(levelJson.text);
        if (CurrentLevel == null)
            return;

        Transform cells = EnsureRoot(ref cellRoot, "Cells");
        Transform frames = EnsureRoot(ref frameRoot, "Frame");

        _cellSpawner.SpawnCells(CurrentLevel, cells);
        RebuildFrameBuilder(CurrentLevel);
        PrimaryFrameSpline = _frameSplineBuilder.BuildFrame(CurrentLevel, frames);
    }

    public void PreviewFrameWallWidth(float wallWidth)
    {
        _runtimePreviewFrameWallWidth = Mathf.Max(0.01f, wallWidth);
        _hasRuntimePreviewFrameWallWidth = true;

        if (CurrentLevel == null)
        {
            Generate();
            return;
        }

        RebuildFrame();
    }

    [ContextMenu("Save Frame Wall Width To Global Setting")]
    public void SaveFrameWallWidthToGlobalSetting()
    {
        float wallWidth = ActiveFrameWallWidth > 0f
            ? ActiveFrameWallWidth
            : ResolveFrameWallWidth(CurrentLevel);

        FrameGenerationGlobalSettings.SaveWallWidth(wallWidth);
        frameWallWidth = wallWidth;
        _runtimePreviewFrameWallWidth = wallWidth;
        _hasRuntimePreviewFrameWallWidth = true;
    }

    [ContextMenu("Reset Saved Frame Wall Width")]
    public void ResetSavedFrameWallWidth()
    {
        FrameGenerationGlobalSettings.ClearWallWidth();
        _hasRuntimePreviewFrameWallWidth = false;
        frameWallWidth = 0f;
        RebuildFrame();
    }

    private void RebuildFrame()
    {
        if (CurrentLevel == null)
            return;

        Transform frames = EnsureRoot(ref frameRoot, "Frame");
        ClearChildren(frames);
        RebuildFrameBuilder(CurrentLevel);
        PrimaryFrameSpline = _frameSplineBuilder.BuildFrame(CurrentLevel, frames);
    }

    public void Clear()
    {
        ClearChildren(cellRoot);
        ClearChildren(frameRoot);
        CurrentLevel = null;
        PrimaryFrameSpline = null;
    }

    private void EnsureServices()
    {
        _loader ??= new LevelDataLoader();
        _cellSpawner ??= new CellSpawner(PrefabProfile.CellPrefab);
    }

    private void RebuildFrameBuilder(LevelData data)
    {
        ActiveFrameWallWidth = ResolveFrameWallWidth(data);
        _frameSplineBuilder = new FrameSplineBuilder(frameMaterial, ActiveFrameWallWidth, frameTubeSides, frameSampleRate, frameCornerRoundness, frameMesh, frameCornerSegments);
    }

    private float ResolveFrameWallWidth(LevelData data)
    {
        if (_hasRuntimePreviewFrameWallWidth)
            return _runtimePreviewFrameWallWidth;

        if (FrameGenerationGlobalSettings.TryGetWallWidth(out float savedWallWidth))
            return savedWallWidth;

        if (frameWallWidth > 0f)
            return frameWallWidth;

        return GridCoordinateUtility.GetCellPitch(data);
    }

    private Transform EnsureRoot(ref Transform root, string rootName)
    {
        if (root != null)
            return root;

        GameObject child = new GameObject(rootName);
        child.transform.SetParent(transform, false);
        root = child.transform;
        return root;
    }

    private static void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }
}

internal static class FrameGenerationGlobalSettings
{
    private const string WallWidthKey = "WoolLoop.FrameGeneration.WallWidth";

    public static bool TryGetWallWidth(out float wallWidth)
    {
        wallWidth = PlayerPrefs.GetFloat(WallWidthKey, 0f);
        return wallWidth > 0f;
    }

    public static void SaveWallWidth(float wallWidth)
    {
        PlayerPrefs.SetFloat(WallWidthKey, Mathf.Max(0.01f, wallWidth));
        PlayerPrefs.Save();
    }

    public static void ClearWallWidth()
    {
        PlayerPrefs.DeleteKey(WallWidthKey);
        PlayerPrefs.Save();
    }
}
