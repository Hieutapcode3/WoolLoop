using System;
using UnityEngine;

/// <summary>
/// Object In Pool
/// </summary>
class ObjectInPool : MonoBehaviour, IObjectInPool
{
    public int PoolID { get; private set; }

    public bool inPoolStack;

    private IObjectPool _pool;
    private IOnRecycle[] _recycleCallbacks = Array.Empty<IOnRecycle>();

    public void InitPool(int poolID, IObjectPool poolHandle)
    {
        PoolID = poolID;
        _pool = poolHandle;

        _recycleCallbacks = GetComponents<IOnRecycle>() ?? Array.Empty<IOnRecycle>();
    }

    public void Recycle()
    {
        _pool?.Recycle(PoolID, gameObject);

        foreach (var cb in _recycleCallbacks)
        {
            cb?.OnRecycle();
        }
    }
}
