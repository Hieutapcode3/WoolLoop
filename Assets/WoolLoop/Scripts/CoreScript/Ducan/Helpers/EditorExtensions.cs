#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Common.Helper
{
    public static class EditorExtensions
    {
        public readonly static string projectFolderPath = Application.dataPath.Replace("/Assets", "/");

        public static IEnumerable<SerializedProperty> GetChildren(this SerializedProperty property)
        {
            property = property.Copy();
            var nextElement = property.Copy();
            bool hasNextElement = nextElement.NextVisible(false);
            if (!hasNextElement)
            {
                nextElement = null;
            }

            property.NextVisible(true);
            while (true)
            {
                if ((SerializedProperty.EqualContents(property, nextElement)))
                {
                    yield break;
                }

                yield return property;

                bool hasNext = property.NextVisible(false);
                if (!hasNext)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Get asset in project
        /// </summary>
        public static T GetAssetByName<T>(string name = "") where T : Object
        {
            string[] assets = AssetDatabase.FindAssets((string.IsNullOrEmpty(name) ? "" : name + " ") + "t:" + typeof(T).Name);
            if (assets.Length > 0)
            {
                string assetPath;
                for (int i = 0; i < assets.Length; i++)
                {
                    assetPath = AssetDatabase.GUIDToAssetPath(assets[i]);
                    if (System.IO.Path.GetFileNameWithoutExtension(assetPath) == name)
                    {
                        return (T)AssetDatabase.LoadAssetAtPath(assetPath, typeof(T));
                    }
                }
            }

            return null;
        }
    }
}
#endif