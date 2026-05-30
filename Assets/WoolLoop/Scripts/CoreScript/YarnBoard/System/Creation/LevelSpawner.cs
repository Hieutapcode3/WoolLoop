using System;
using System.Threading;
using Common;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class LevelSpawner : MonoBehaviour
{
    [SerializeField, Min(1)] private int levelIndex = 1;
    [SerializeField] private bool autoSpawnOnStart = true;
    [SerializeField] private bool clearBeforeSpawn = true;
    [SerializeField] private Transform spawnRoot;
    [SerializeField] private ConveyorEntrance conveyorEntrance;
    [SerializeField] private string resourceFolder = "Levels";

    private readonly BottomBoardFactory bottomBoardFactory = new();
    private readonly WoolBallFactory woolBallFactory = new();
    private GameObject currentLevelRoot;
    private YarnBoardRuntimeState runtimeState;
    private CancellationTokenSource spawnCancellation;

    private void Start()
    {
        if (autoSpawnOnStart)
        {
            SpawnLevelAsync().Forget();
        }
    }

    private void OnDestroy()
    {
        spawnCancellation?.Cancel();
        spawnCancellation?.Dispose();
        spawnCancellation = null;
    }

    public UniTask SpawnLevelAsync(int? overrideIndex = null, CancellationToken token = default)
    {
        return SpawnLevelInternalAsync(overrideIndex ?? levelIndex, token);
    }

    public void ClearLevel()
    {
        if (currentLevelRoot == null)
        {
            runtimeState = null;
            return;
        }

        var pendingCleanup = currentLevelRoot.GetComponentsInChildren<IPendingCleanup>(true);
        for (var i = 0; i < pendingCleanup.Length; i++)
            pendingCleanup[i]?.CleanupForLevelUnload();

        if (Application.isPlaying)
            Destroy(currentLevelRoot);
        else
            DestroyImmediate(currentLevelRoot);

        currentLevelRoot = null;
        runtimeState = null;
    }

    private async UniTask SpawnLevelInternalAsync(int targetIndex, CancellationToken externalToken)
    {
        spawnCancellation?.Cancel();
        spawnCancellation?.Dispose();
        spawnCancellation = CancellationTokenSource.CreateLinkedTokenSource(externalToken, this.GetCancellationTokenOnDestroy());
        var token = spawnCancellation.Token;

        if (clearBeforeSpawn)
            ClearLevel();

        var level = LoadLevel(targetIndex);
        if (level == null)
            return;

        ValidateLevelShape(level);

        var parent = spawnRoot != null ? spawnRoot : transform;
        currentLevelRoot = new GameObject(string.IsNullOrEmpty(level.levelId) ? $"Level_{targetIndex:000}" : level.levelId);
        currentLevelRoot.transform.SetParent(parent, false);

        var adapter = YarnBoardLevelUtility.CreateAdapter(level);
        runtimeState = new YarnBoardRuntimeState(level, adapter);

        token.ThrowIfCancellationRequested();
        await bottomBoardFactory.Create(new BottomBoardCreateParameters(level, adapter, currentLevelRoot.transform), token);

        if (level.yarnBalls == null) return;

        for (var i = 0; i < level.yarnBalls.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            var ballData = level.yarnBalls[i];
            if (ballData == null) continue;

            await woolBallFactory.Create(new WoolBallCreateParameters(
                ballData,
                adapter,
                currentLevelRoot.transform,
                runtimeState,
                conveyorEntrance
            ), token);
        }
    }

    private LevelData LoadLevel(int targetIndex)
    {
        var folder = string.IsNullOrWhiteSpace(resourceFolder) ? "Levels" : resourceFolder.Trim('/');
        var resourcePath = $"{folder}/Level_{Mathf.Max(1, targetIndex):000}";
        var levelAsset = Resources.Load<TextAsset>(resourcePath);
        if (levelAsset == null)
        {
            Debug.LogError($"Cannot load YarnBoard level at Resources/{resourcePath}.json", this);
            return null;
        }

        var level = JsonUtility.FromJson<LevelData>(levelAsset.text);
        if (level == null)
        {
            Debug.LogError($"Invalid YarnBoard level JSON at Resources/{resourcePath}.json", this);
            return null;
        }

        if (string.IsNullOrEmpty(level.levelId))
            level.levelId = $"Level_{Mathf.Max(1, targetIndex):000}";

        return level;
    }

    private static void ValidateLevelShape(LevelData level)
    {
        if (level.size.x <= 0 || level.size.y <= 0)
            throw new InvalidOperationException($"Invalid level size for {level.levelId}.");

        var expectedLength = level.size.x * level.size.y;
        if (level.tileData == null || level.tileData.Length != expectedLength)
            throw new InvalidOperationException($"Invalid tileData length for {level.levelId}. Expected {expectedLength}.");
    }
}
