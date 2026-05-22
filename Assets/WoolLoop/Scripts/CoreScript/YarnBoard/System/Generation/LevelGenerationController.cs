using Dreamteck.Splines;
using UnityEngine;

public sealed class LevelGenerationController : MonoBehaviour
{
    [SerializeField] private TextAsset levelJson;
    [SerializeField] private Transform cellRoot;
    [SerializeField] private Transform frameRoot;
    [SerializeField] private Material frameMaterial;
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private float frameTubeSize = 0.12f;
    [SerializeField] private int frameTubeSides = 8;
    [SerializeField] private int frameSampleRate = 4;

    public LevelData CurrentLevel { get; private set; }
    public SplineComputer PrimaryFrameSpline { get; private set; }

    private ILevelDataLoader _loader;
    private ICellSpawner _cellSpawner;
    private IFrameSplineBuilder _frameSplineBuilder;

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
        _frameSplineBuilder ??= new FrameSplineBuilder(frameMaterial, frameTubeSize, frameTubeSides, frameSampleRate);
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
