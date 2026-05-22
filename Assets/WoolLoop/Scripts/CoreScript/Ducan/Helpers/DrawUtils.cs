using UnityEngine;
using System.Runtime.CompilerServices;
namespace Common.Helper
{
    public static class DrawUtils
    {
        public static void DrawSegment(Vector3 start, Vector3 end, Color color, float duration = -1, bool depthTest = true)
        {
            float time = duration < 0 ? Time.deltaTime : duration;
            Debug.DrawLine(start, end, color, time, depthTest);
        }

        public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration = -1, bool depthTest = true)
        {
            int segments = 24;
            float step = 360f / segments;

            // Vòng ngang XY
            for (int i = 0; i < segments; i++)
            {
                Vector3 p1 = center + Quaternion.Euler(0, step * i, 0) * Vector3.forward * radius;
                Vector3 p2 = center + Quaternion.Euler(0, step * (i + 1), 0) * Vector3.forward * radius;
                Debug.DrawLine(p1, p2, color, duration, depthTest);
            }

            // Vòng dọc XZ
            for (int i = 0; i < segments; i++)
            {
                Vector3 p1 = center + Quaternion.Euler(step * i, 0, 0) * Vector3.up * radius;
                Vector3 p2 = center + Quaternion.Euler(step * (i + 1), 0, 0) * Vector3.up * radius;
                Debug.DrawLine(p1, p2, color, duration, depthTest);
            }

            // Vòng dọc YZ
            for (int i = 0; i < segments; i++)
            {
                Vector3 p1 = center + Quaternion.Euler(0, 0, step * i) * Vector3.up * radius;
                Vector3 p2 = center + Quaternion.Euler(0, 0, step * (i + 1)) * Vector3.up * radius;
                Debug.DrawLine(p1, p2, color, duration, depthTest);
            }
        }

        public static void DrawWireCube(Vector3 center, Vector3 size, Color color, float time = -1, bool depthTest = true)
        {
            float duration = time == -1 ? Time.deltaTime : time;
            Vector3 halfSize = size / 2;

            Vector3 p1 = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            Vector3 p2 = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            Vector3 p3 = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            Vector3 p4 = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);

            Vector3 p5 = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p6 = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            Vector3 p7 = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            Vector3 p8 = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            // Bottom face
            Debug.DrawLine(p1, p2, color, duration, depthTest);
            Debug.DrawLine(p2, p3, color, duration, depthTest);
            Debug.DrawLine(p3, p4, color, duration, depthTest);
            Debug.DrawLine(p4, p1, color, duration, depthTest);

            // Top face
            Debug.DrawLine(p5, p6, color, duration, depthTest);
            Debug.DrawLine(p6, p7, color, duration, depthTest);
            Debug.DrawLine(p7, p8, color, duration, depthTest);
            Debug.DrawLine(p8, p5, color, duration, depthTest);

            // Vertical edges
            Debug.DrawLine(p1, p5, color, duration, depthTest);
            Debug.DrawLine(p2, p6, color, duration, depthTest);
            Debug.DrawLine(p3, p7, color, duration, depthTest);
            Debug.DrawLine(p4, p8, color, duration, depthTest);
        }

    }

    //static clear unity console
    public static class UnityConsoleClearer
    {
        public static void Clear()
        {
#if UNITY_EDITOR
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            method.Invoke(null, null);
#endif
        }
    }
}