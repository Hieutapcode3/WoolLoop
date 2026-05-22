using UnityEngine;
using System.Collections.Generic;

namespace VTLTools
{
    public static class TransformExtensions
    {
        public static void DestroyAllChildren(this Transform transform, bool immediate = false)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (immediate)
                    Object.DestroyImmediate(transform.GetChild(i).gameObject);
                else
                    Object.Destroy(transform.GetChild(i).gameObject);
            }
        }

        public static void RecycleAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                ObjectPool.Recycle(transform.GetChild(i).gameObject);
            }
        }

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        public static List<T> GetAllChildrenComponent<T>(this Transform _parent)
        {
            List<T> _l = new();
            foreach (Transform _child in _parent.GetComponentsInChildren<Transform>(true))
            {
                if (_child.GetComponent<T>() != null)
                    _l.Add(_child.GetComponent<T>());
            }
            return _l;
        }

        public static List<T> GetAllChildsComponentByName<T>(this Transform _parent, string _name)
        {
            List<T> _l = new();
            foreach (Transform _child in _parent.GetComponentsInChildren<Transform>(true))
            {
                if (_child.name.Contains(_name) && _child.GetComponent<T>() != null)
                    _l.Add(_child.GetComponent<T>());
            }
            return _l;
        }
    }
}