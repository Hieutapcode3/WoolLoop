namespace WoolGame
{
    public interface IEntityDomain
    {
        int EntityId { get; }
        void OnCreated();
        void Tick(float deltaTime);
        void Dispose();
    }
}
