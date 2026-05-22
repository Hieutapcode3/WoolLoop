using UnityEngine;

namespace VTLTools
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField]
        private bool IsDontDestroyOnLoad;
        protected static bool INeedDontDestroy;
        protected static T instance;
        private static object _lock = new object();
        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    return instance;
                }
            }
        }

        protected virtual void Awake()
        {
            INeedDontDestroy = IsDontDestroyOnLoad;
            if (instance == null)
            {
                instance = this as T;
                if (INeedDontDestroy)
                {
                    DontDestroyOnLoad(this);
                }
            }
            else
            {
                if (instance != null)
                {
                    Destroy(gameObject);
                }
            }
        }
        protected virtual void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }

}
