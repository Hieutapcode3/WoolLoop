using UnityEngine;

public interface IObjectPool
{
    void CreatPool(Object prefab, uint cap = 1);
    bool Use(Object prefab, out GameObject go);
    bool Use(Object prefab, float lifeTime, out GameObject go);
    void Recycle(int poolID, GameObject go);
}

