

using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RoundedCornerLine : MonoBehaviour
{
    [Header("Input Points")]
    public Vector3[] controlPoints = new Vector3[0];

    [Header("Corner Settings")]
    [Range(0f, 1f)]
    public float cornerRadius = 0.5f;

    public int cornerSegments = 6;

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        UpdateLine();
    }

    public void UpdateLine()
    {
        Vector3[] array = AddRoundedCorners(controlPoints, cornerRadius, cornerSegments, line.loop);
        line.positionCount = array.Length;
        line.SetPositions(array);
    }

    public static Vector3[] AddRoundedCorners(Vector3[] pts, float radius, int segments, bool loop = false)
    {
        if (pts.Length < 3)
        {
            return pts;
        }

        List<Vector3> result = new List<Vector3>();
        if (!loop)
        {
            TryAdd(pts[0]);
        }

        int num = pts.Length;
        int num2 = (loop ? num : (num - 1));
        for (int i = ((!loop) ? 1 : 0); i < num2; i++)
        {
            Vector3 vector = pts[(i - 1 + num) % num];
            Vector3 vector2 = pts[i];
            Vector3 vector3 = pts[(i + 1) % num];
            Vector3 normalized = (vector - vector2).normalized;
            Vector3 normalized2 = (vector3 - vector2).normalized;
            float num3 = Vector3.Angle(normalized, normalized2);
            if (num3 > 179.9f || num3 < 0.1f)
            {
                TryAdd(vector2);
                continue;
            }

            float f = num3 * (MathF.PI / 180f) / 2f;
            float a = radius / Mathf.Tan(f);
            float num4 = Vector3.Distance(vector, vector2);
            float num5 = Vector3.Distance(vector3, vector2);
            float num6 = Mathf.Min(a, num4 * 0.5f);
            float num7 = Mathf.Min(a, num5 * 0.5f);
            Vector3 vector4 = vector2 + normalized * num6;
            Vector3 vector5 = vector2 + normalized2 * num7;
            Vector3 normalized3 = (normalized + normalized2).normalized;
            float num8 = radius / Mathf.Sin(f);
            Vector3 vector6 = vector2 + normalized3 * num8;
            Vector3 normalized4 = (vector4 - vector6).normalized;
            Vector3 normalized5 = (vector5 - vector6).normalized;
            Vector3 normalized6 = Vector3.Cross(normalized, normalized2).normalized;
            float num9 = Vector3.SignedAngle(normalized4, normalized5, normalized6);
            TryAdd(vector4);
            for (int j = 1; j < segments; j++)
            {
                float num10 = (float)j / (float)segments;
                Quaternion quaternion = Quaternion.AngleAxis(num9 * num10, normalized6);
                TryAdd(vector6 + quaternion * normalized4 * radius);
            }

            TryAdd(vector5);
        }

        if (!loop)
        {
            TryAdd(pts[^1]);
        }
        else if (result.Count > 0)
        {
            List<Vector3> list = result;
            if (Vector3.Distance(list[list.Count - 1], result[0]) < 0.001f)
            {
                result.RemoveAt(result.Count - 1);
            }
        }

        return result.ToArray();
        void TryAdd(Vector3 point)
        {
            if (result.Count != 0)
            {
                List<Vector3> list2 = result;
                if (!(Vector3.Distance(list2[list2.Count - 1], point) > 0.001f))
                {
                    return;
                }
            }

            result.Add(point);
        }
    }
}
