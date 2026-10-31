using UnityEngine;
using UnityEngine.Rendering;

namespace PulseFactor
{
    public static class TorusMesh
    {
        // Toro no plano XY (o eixo Z atravessa o anel).
        public static Mesh Build(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
        {
            int cols = majorSegments + 1, rows = minorSegments + 1;
            var vertices = new Vector3[cols * rows];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (int i = 0; i < cols; i++)
            {
                float u = i / (float)majorSegments * Mathf.PI * 2f;
                var center = new Vector3(Mathf.Cos(u), Mathf.Sin(u), 0f) * majorRadius;
                for (int j = 0; j < rows; j++)
                {
                    float v = j / (float)minorSegments * Mathf.PI * 2f;
                    var n = new Vector3(Mathf.Cos(u) * Mathf.Cos(v), Mathf.Sin(u) * Mathf.Cos(v), Mathf.Sin(v));
                    int k = i * rows + j;
                    vertices[k] = center + n * minorRadius;
                    normals[k] = n;
                    uvs[k] = new Vector2(i / (float)majorSegments, j / (float)minorSegments);
                }
            }

            var triangles = new int[majorSegments * minorSegments * 6];
            int t = 0;
            for (int i = 0; i < majorSegments; i++)
            for (int j = 0; j < minorSegments; j++)
            {
                int a = i * rows + j, b = (i + 1) * rows + j, c = a + 1, d = b + 1;
                triangles[t++] = a; triangles[t++] = c; triangles[t++] = b;
                triangles[t++] = b; triangles[t++] = c; triangles[t++] = d;
            }

            var mesh = new Mesh { name = "Torus", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
