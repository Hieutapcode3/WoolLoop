using UnityEngine;

namespace BoardSpline.Runtime
{
    public static class GameObjectComponentExtensions
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent<T>(out var component)
                ? component
                : gameObject.AddComponent<T>();
        }
    }
}
