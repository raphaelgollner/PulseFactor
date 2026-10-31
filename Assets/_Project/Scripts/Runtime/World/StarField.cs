using UnityEngine;

namespace PulseFactor
{
    // Fundo estrelado: pequenos quads numa esfera grande que acompanha a camera.
    public class StarField : MonoBehaviour
    {
        [SerializeField] int count = 4000;
        [SerializeField] float radius = 90000f;
        [SerializeField] int seed = 99;

        public Material Material;

        void Start()
        {
            var rng = new System.Random(seed);
            var vertices = new Vector3[count * 4];
            var colors = new Color[count * 4];
            var tris = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                var dir = new Vector3((float)Gauss(rng), (float)Gauss(rng), (float)Gauss(rng)).normalized;
                float size = radius * 0.0009f * (0.4f + (float)System.Math.Pow(rng.NextDouble(), 6) * 2.5f);
                var right = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.right : Vector3.up).normalized * size;
                var up = Vector3.Cross(right, dir).normalized * size;
                var c = dir * radius;
                float warm = (float)rng.NextDouble();
                var col = Color.Lerp(new Color(0.7f, 0.8f, 1f), new Color(1f, 0.85f, 0.7f), warm) * (0.5f + (float)rng.NextDouble());
                int v = i * 4;
                vertices[v] = c - right - up; vertices[v + 1] = c + right - up;
                vertices[v + 2] = c + right + up; vertices[v + 3] = c - right + up;
                for (int k = 0; k < 4; k++) colors[v + k] = col;
                int t = i * 6;
                tris[t] = v; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
                tris[t + 3] = v; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
            }
            var mesh = new Mesh { name = "Stars", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = vertices; mesh.colors = colors; mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static double Gauss(System.Random r) =>
            System.Math.Sqrt(-2 * System.Math.Log(1 - r.NextDouble())) * System.Math.Cos(2 * System.Math.PI * r.NextDouble());

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.position = cam.transform.position;
        }
    }
}
