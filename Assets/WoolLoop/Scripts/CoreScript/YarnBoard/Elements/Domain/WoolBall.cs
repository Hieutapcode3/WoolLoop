using System;
using System.Collections.Generic;
using Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using BoardSpline.Runtime;
using UnityEngine;

public class WoolBall : MonoBehaviour, IRuntimeCreatable
{
    private sealed class MovePlan
    {
        public Vector2Int Target;
        public bool FromRootEndpoint;
        public List<Vector2Int> Path;
        public List<List<Vector2Int>> Frames;
        public List<Vector2Int> FinalTiles;
    }

    private const float VisualHeightOffset = .3f;

    [SerializeField, Min(0f)] private float stepDuration = 0.12f;

    private YarnBoardRuntimeState runtimeState;
    private Tween activeTween;
    private bool isRegistered;
    private readonly List<BoxCollider> interactionColliders = new();

    public WoolBallData Data { get; private set; }
    public BoardSplineDataAdapterInfo Adapter { get; private set; }
    public WoolBallVisual Visual { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsCompleted { get; private set; }
    public float StepDuration
    {
        get => stepDuration;
        set => stepDuration = Mathf.Max(0f, value);
    }

    public event Action<WoolBall> OnCompleted;

    public void OnCreated(ICreateParameters parameters)
    {
        if (parameters is not WoolBallCreateParameters createParameters)
            throw new System.ArgumentException($"Expected {nameof(WoolBallCreateParameters)}.", nameof(parameters));

        Data = createParameters.Data;
        Adapter = createParameters.Adapter;
        runtimeState = createParameters.RuntimeState ?? new YarnBoardRuntimeState(null, Adapter);
        transform.localPosition = Adapter.IndexToWorld(Data.tileId);

        Visual = GetComponentInChildren<WoolBallVisual>(true);
        if (Visual == null)
            Visual = EnsureVisualChild().AddComponent<WoolBallVisual>();

        Visual.Render(Data, Adapter);
        RebuildInteractionColliders(CollectTiles(Data));
        runtimeState.Register(this);
        isRegistered = true;
    }

    private void OnMouseDown()
    {
        OnInteract();
    }

    private void OnDestroy()
    {
        CleanupForLevelUnload();
    }

    public IReadOnlyList<Vector2Int> GetTiles()
    {
        return CollectTiles(Data);
    }

    public bool CanMoveTo(Vector2Int target)
    {
        return TryCreateMovePlan(target, out _);
    }

    public bool CanMoveToTarget()
    {
        return runtimeState?.Level != null &&
               runtimeState.Level.hasTargetExitTileId &&
               CanMoveTo(runtimeState.Level.targetExitTileId);
    }

    public async UniTask<bool> MoveTo(Vector2Int target)
    {
        if (!TryBeginMove(target, out var plan))
            return false;

        try
        {
            await PlayMove(plan);
            CommitMove(plan.FinalTiles);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            runtimeState.ReleaseReservation(this, plan.Target);
            IsMoving = false;
            SetInteractionCollidersEnabled(!IsCompleted);
        }
    }

    public async UniTask<bool> MoveToNearestExit()
    {
        if (!TryCreateNearestExitMovePlan(out var plan))
            return false;

        if (!TryBeginMove(plan))
            return false;

        try
        {
            await PlayMove(plan);
            CommitMove(plan.FinalTiles);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            runtimeState.ReleaseReservation(this, plan.Target);
            IsMoving = false;
            SetInteractionCollidersEnabled(!IsCompleted);
        }
    }

    public UniTask<bool> MoveToTarget()
    {
        if (runtimeState?.Level == null || !runtimeState.Level.hasTargetExitTileId)
            return UniTask.FromResult(false);

        return MoveTo(runtimeState.Level.targetExitTileId);
    }

    public void Complete()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
        IsMoving = false;
        KillActiveTween();
        SetInteractionCollidersEnabled(false);
        runtimeState?.Unregister(this);
        isRegistered = false;
        OnCompleted?.Invoke(this);
    }

    public void CleanupForLevelUnload()
    {
        KillActiveTween();
        runtimeState?.ReleaseReservations(this);
        if (isRegistered)
        {
            runtimeState?.Unregister(this);
            isRegistered = false;
        }
    }

    public void OnInteract()
    {
        if (IsMoving || IsCompleted)
            return;

        MoveToTarget().Forget();
    }

    private bool TryBeginMove(Vector2Int target, out MovePlan plan)
    {
        plan = null;
        if (!TryCreateMovePlan(target, out var candidate))
            return false;

        if (!TryBeginMove(candidate))
            return false;

        plan = candidate;
        return true;
    }

    private bool TryBeginMove(MovePlan plan)
    {
        if (plan == null || IsMoving || IsCompleted)
            return false;

        if (!runtimeState.TryReserve(this, plan.Target))
            return false;

        IsMoving = true;
        SetInteractionCollidersEnabled(false);
        return true;
    }

    private bool TryCreateMovePlan(Vector2Int target, out MovePlan plan)
    {
        plan = null;
        if (Data == null || runtimeState == null || IsMoving || IsCompleted)
            return false;

        var tiles = CollectTiles(Data);
        if (tiles.Count == 0)
            return false;

        var root = tiles[0];
        var rootPathFound = runtimeState.TryFindPath(this, root, target, out var rootPath);
        var tailPathFound = false;
        List<Vector2Int> tailPath = null;

        if (tiles.Count > 1)
        {
            var tail = tiles[^1];
            tailPathFound = runtimeState.TryFindPath(this, tail, target, out tailPath);
        }

        if (!rootPathFound && !tailPathFound)
            return false;

        var useRoot = rootPathFound && (!tailPathFound || rootPath.Count <= tailPath.Count);
        var path = useRoot ? rootPath : tailPath;
        plan = CreateMovePlan(target, useRoot, path, tiles);
        return plan != null;
    }

    private bool TryCreateNearestExitMovePlan(out MovePlan plan)
    {
        plan = null;
        if (runtimeState == null)
            return false;

        var exits = runtimeState.GetExitTiles();
        for (var i = 0; i < exits.Count; i++)
        {
            if (!TryCreateMovePlan(exits[i], out var candidate))
                continue;

            if (plan == null || IsBetterPlan(candidate, plan))
                plan = candidate;
        }

        return plan != null;
    }

    private static bool IsBetterPlan(MovePlan candidate, MovePlan current)
    {
        var candidateCost = candidate.Path.Count;
        var currentCost = current.Path.Count;
        if (candidateCost != currentCost)
            return candidateCost < currentCost;

        if (candidate.FromRootEndpoint != current.FromRootEndpoint)
            return candidate.FromRootEndpoint;

        if (candidate.Target.y != current.Target.y)
            return candidate.Target.y < current.Target.y;

        return candidate.Target.x < current.Target.x;
    }

    private MovePlan CreateMovePlan(
        Vector2Int target,
        bool fromRootEndpoint,
        List<Vector2Int> path,
        List<Vector2Int> initialTiles
    )
    {
        if (path == null || path.Count == 0)
            return null;

        var frames = new List<List<Vector2Int>> { new(initialTiles) };
        var current = new List<Vector2Int>(initialTiles);

        for (var i = 1; i < path.Count; i++)
        {
            if (fromRootEndpoint)
            {
                current.Insert(0, path[i]);
                current.RemoveAt(current.Count - 1);
            }
            else
            {
                current.Add(path[i]);
                current.RemoveAt(0);
            }

            frames.Add(new List<Vector2Int>(current));
        }

        return new MovePlan
        {
            Target = target,
            FromRootEndpoint = fromRootEndpoint,
            Path = path,
            Frames = frames,
            FinalTiles = new List<Vector2Int>(current)
        };
    }

    private async UniTask PlayMove(MovePlan plan)
    {
        if (plan.Frames.Count <= 1 || stepDuration <= 0f)
            return;

        KillActiveTween();
        var rootWorld = Adapter.IndexToWorld(plan.Frames[0][0]);

        for (var frameIndex = 1; frameIndex < plan.Frames.Count; frameIndex++)
        {
            var sequence = Visual.CreateMoveTween(plan.Frames[frameIndex], Adapter, rootWorld, stepDuration);
            activeTween = sequence;
            await AwaitTween(sequence, this.GetCancellationTokenOnDestroy());
            activeTween = null;
        }
    }

    private void CommitMove(IReadOnlyList<Vector2Int> finalTiles)
    {
        if (Data == null || finalTiles == null || finalTiles.Count == 0)
            return;

        Data.tileId = finalTiles[0];
        Data.childrenTileIds ??= new List<Vector2Int>();
        Data.childrenTileIds.Clear();

        for (var i = 1; i < finalTiles.Count; i++)
            Data.childrenTileIds.Add(finalTiles[i]);

        transform.localPosition = Adapter.IndexToWorld(Data.tileId);
        Visual.Render(Data, Adapter);
        RebuildInteractionColliders(finalTiles);
        runtimeState.SetOccupancy(this, finalTiles);
    }

    private async UniTask AwaitTween(Tween tween, System.Threading.CancellationToken cancellationToken)
    {
        if (tween == null || !tween.active)
            return;

        var completion = new UniTaskCompletionSource();
        var settled = false;

        tween.OnComplete(() =>
        {
            settled = true;
            completion.TrySetResult();
        });
        tween.OnKill(() =>
        {
            if (!settled)
                completion.TrySetCanceled(cancellationToken);
        });

        using (cancellationToken.Register(() =>
        {
            if (tween.active)
                tween.Kill();
        }))
        {
            await completion.Task;
        }
    }

    private void KillActiveTween()
    {
        if (activeTween != null && activeTween.active)
            activeTween.Kill();

        activeTween = null;
    }

    private static List<Vector2Int> CollectTiles(WoolBallData data)
    {
        var tiles = new List<Vector2Int>();
        if (data == null)
            return tiles;

        tiles.Add(data.tileId);
        if (data.childrenTileIds == null)
            return tiles;

        for (var i = 0; i < data.childrenTileIds.Count; i++)
        {
            var child = data.childrenTileIds[i];
            if (!tiles.Contains(child))
                tiles.Add(child);
        }

        return tiles;
    }

    private void RebuildInteractionColliders(IReadOnlyList<Vector2Int> tiles)
    {
        if (tiles == null)
        {
            SetInteractionColliderCount(0);
            return;
        }

        SetInteractionColliderCount(tiles.Count);
        var rootWorld = Adapter.IndexToWorld(Data.tileId);
        var size = Vector3.one * Adapter.CellSize * 0.8f;

        for (var i = 0; i < tiles.Count; i++)
        {
            var collider = interactionColliders[i];
            collider.center = Adapter.IndexToWorld(tiles[i]) - rootWorld + Vector3.up * VisualHeightOffset;
            collider.size = size;
            collider.enabled = !IsMoving && !IsCompleted;
        }
    }

    private void SetInteractionColliderCount(int targetCount)
    {
        for (var i = interactionColliders.Count - 1; i >= targetCount; i--)
        {
            var collider = interactionColliders[i];
            interactionColliders.RemoveAt(i);
            if (collider == null)
                continue;

            if (Application.isPlaying)
                Destroy(collider);
            else
                DestroyImmediate(collider);
        }

        while (interactionColliders.Count < targetCount)
            interactionColliders.Add(gameObject.AddComponent<BoxCollider>());
    }

    private void SetInteractionCollidersEnabled(bool enabled)
    {
        for (var i = 0; i < interactionColliders.Count; i++)
        {
            if (interactionColliders[i] != null)
                interactionColliders[i].enabled = enabled;
        }
    }

    private GameObject EnsureVisualChild()
    {
        var visualTransform = transform.Find("Visual");
        if (visualTransform != null)
            return visualTransform.gameObject;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);
        return visual;
    }
}
