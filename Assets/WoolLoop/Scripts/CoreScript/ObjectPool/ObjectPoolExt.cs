using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


/// <summary>
/// Object pool extension
/// </summary>
public static class ObjectPoolExt
{
    private static List<IRecycleHandle> _recycleHandles = new List<IRecycleHandle>(32);

    private static IObjectPool _objectPool;

    public static void Init(this IObjectPool objectPool) => _objectPool = objectPool;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Recycle(this GameObject gameObject) => gameObject.GetComponent<IObjectInPool>()?.Recycle();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void InitRecycleHandle(this GameObject gameObject, float lifeTime)
    {
        _recycleHandles.Clear();
        gameObject.GetComponentsInChildren(false, _recycleHandles);
        foreach (var handle in _recycleHandles)
        {
            handle.SetRecycle(lifeTime);
        }

        if (_recycleHandles.Count == 0)
        {
            var recycleOnTime = gameObject.AddComponent<RecycleOnTime>();
            recycleOnTime.SetRecycle(lifeTime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CreatePool(this Object prefab, uint cap = 1) => _objectPool.CreatPool(prefab, cap);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Use(this Object prefab, out GameObject go) => _objectPool.Use(prefab, out go);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Use(this Object prefab, float lifeTime, out GameObject go) => _objectPool.Use(prefab, lifeTime, out go);

    public static bool Use<T>(this Object prefab, Transform parent, Vector3 position, Quaternion rotation, out T instance)
    {
        instance = default;
        if (false == prefab.Use(out var go))
            return false;

        instance = go.GetComponent<T>();

        if (instance == null)
            return false;

        var trs = go.transform;

        trs.SetParent(parent);
        // trs.localScale = Vector3.one;
        trs.SetPositionAndRotation(position, rotation);

        return true;
    }

    public static void Instantiate(this IObjectPool pool, GameObject prefab, Vector3 position, Quaternion rotation, out GameObject go)
    {
        pool.CreatPool(prefab);
        pool.Use(prefab, out go);

        go.transform.position = position;
        go.transform.rotation = rotation;
    }

    public static void Instantiate(this IObjectPool pool, GameObject prefab, Transform parent, out GameObject go)
    {
        pool.CreatPool(prefab);
        pool.Use(prefab, out go);

        go.transform.SetParent(parent);
        go.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        if (false == go.TryGetComponent(out T component))
        {
            component = go.AddComponent<T>();
        }

        return component;
    }
}

