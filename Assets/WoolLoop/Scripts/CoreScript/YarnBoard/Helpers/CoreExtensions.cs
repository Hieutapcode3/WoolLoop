using System.Collections.Generic;
using UnityEngine;

namespace Common.Helper
{
    public static class CoreExtensions
    {
        /// <summary>
        /// Get random item from array
        /// </summary>
        public static T GetRandomItem<T>(this T[] array)
        {
            return array[Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Check if array is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this T[] array)
        {
            return (array == null || array.Length == 0);
        }

        /// <summary>
        /// Check if list is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this List<T> list)
        {
            return (list == null || list.Count == 0);
        }

        public static void CacheBehaviours<T>(this Component owner, List<T> behaviours, ref bool usingBehaviour)
            where T : Behaviour
        {
            if (owner == null || behaviours == null) return;

            behaviours.Clear();
            owner.GetComponentsInChildren(true, behaviours);
            behaviours.TrySetBehavioursActive(ref usingBehaviour, false);
        }

        public static void TrySetBehavioursActive<T>(this IList<T> behaviours, ref bool usingBehaviour, bool isActive)
            where T : Behaviour
        {
            if (behaviours == null || usingBehaviour == isActive) return;

            for (var i = 0; i < behaviours.Count; i++)
            {
                if (behaviours[i] == null) continue;
                behaviours[i].enabled = isActive;
            }

            usingBehaviour = isActive;
        }
    }

}