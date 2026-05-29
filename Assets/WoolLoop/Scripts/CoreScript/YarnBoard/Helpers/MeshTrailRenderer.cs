using UnityEngine;

public static class MeshTrailRenderer
{
    public static Mesh CreateTrailMesh(int segments)
    {
        Mesh mesh = new Mesh();
        int vertexCount = (segments + 1) * 2;
        int triCount = segments * 6;

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        int[] triangles = new int[triCount];

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            vertices[i * 2] = new Vector3(t, 0, 0);
            vertices[i * 2 + 1] = new Vector3(t, 1, 0);

            uv[i * 2] = new Vector2(t, 0);
            uv[i * 2 + 1] = new Vector2(t, 1);

            if (i < segments)
            {
                triangles[i * 6] = i * 2;
                triangles[i * 6 + 1] = i * 2 + 1;
                triangles[i * 6 + 2] = (i + 1) * 2;
                triangles[i * 6 + 3] = i * 2 + 1;
                triangles[i * 6 + 4] = (i + 1) * 2 + 1;
                triangles[i * 6 + 5] = (i + 1) * 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        return mesh;
    }
}
