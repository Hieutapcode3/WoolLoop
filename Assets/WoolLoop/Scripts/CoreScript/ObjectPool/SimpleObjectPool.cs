using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public sealed class SimpleObjectPool : MonoBehaviour, IObjectPool
{
    [SerializeField] private Transform poolRoot;

    private readonly Dictionary<int, Stack<GameObject>> _stacks = new();
    private readonly Dictionary<int, Object> _prefabById = new();
    private readonly Dictionary<int, int> _poolIdByPrefabInstanceId = new();
    private int _nextPoolId = 1;

    private void Awake()
    {
        if (poolRoot == null)
            poolRoot = transform;

        this.Init();
    }

    public void CreatPool(Object prefab, uint cap = 1)
    {
        if (prefab == null) return;

        int poolId = GetOrCreatePoolId(prefab);
        if (!_stacks.TryGetValue(poolId, out var stack))
        {
            stack = new Stack<GameObject>((int)cap);
            _stacks[poolId] = stack;
        }

        for (int i = stack.Count; i < cap; i++)
        {
            var go = CreateInstance(prefab);
            PrepareForPool(poolId, go);
            stack.Push(go);
        }
    }

    public bool Use(Object prefab, out GameObject go)
    {
        go = null;
        if (prefab == null) return false;

        int poolId = GetOrCreatePoolId(prefab);
        if (!_stacks.TryGetValue(poolId, out var stack))
            stack = _stacks[poolId] = new Stack<GameObject>(8);

        go = stack.Count > 0 ? stack.Pop() : CreateInstance(prefab);
        EnsurePoolComponent(poolId, go);

        go.SetActive(true);
        return true;
    }

    public bool Use(Object prefab, float lifeTime, out GameObject go)
    {
        if (!Use(prefab, out go))
            return false;

        if (lifeTime > 0f)
            go.InitRecycleHandle(lifeTime);

        return true;
    }

    public void Recycle(int poolID, GameObject go)
    {
        if (go == null) return;

        if (!_stacks.TryGetValue(poolID, out var stack))
            stack = _stacks[poolID] = new Stack<GameObject>(8);

        go.SetActive(false);
        if (poolRoot != null)
            go.transform.SetParent(poolRoot, false);

        stack.Push(go);
    }

    private int GetOrCreatePoolId(Object prefab)
    {
        int prefabInstanceId = prefab.GetInstanceID();
        if (_poolIdByPrefabInstanceId.TryGetValue(prefabInstanceId, out int existing))
            return existing;

        int poolId = _nextPoolId++;
        _poolIdByPrefabInstanceId[prefabInstanceId] = poolId;
        _prefabById[poolId] = prefab;
        return poolId;
    }

    private GameObject CreateInstance(Object prefab)
    {
        if (prefab is GameObject prefabGo)
            return Instantiate(prefabGo);

        if (prefab is Component prefabComp)
            return Instantiate(prefabComp.gameObject);

        return null;
    }

    private static void PrepareForPool(int poolId, GameObject go)
    {
        if (go == null) return;
        EnsurePoolComponent(poolId, go);
        go.SetActive(false);
    }

    private static void EnsurePoolComponent(int poolId, GameObject go)
    {
        var pooled = go.GetOrAddComponent<ObjectInPool>();
        pooled.InitPool(poolId, ObjectPoolExt_GetPoolUnsafe());
    }

    private static IObjectPool ObjectPoolExt_GetPoolUnsafe()
    {
        // ObjectInPool needs a pool handle; we use the registered global pool.
        // The extension stores it internally, so we pass the current SimpleObjectPool via a static lookup.
        // If not initialized yet, InitPool will still be called again on Use().
        return FindAnyObjectByType<SimpleObjectPool>();
    }
}

