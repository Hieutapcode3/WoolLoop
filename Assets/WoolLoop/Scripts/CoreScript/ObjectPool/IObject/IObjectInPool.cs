public interface IObjectInPool
{
    int PoolID { get; }
    void InitPool(int poolID, IObjectPool poolHandle);
    void Recycle();
}

