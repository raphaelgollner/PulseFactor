using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Desenha um corpo celeste distante a no maximo ProxyDistance da camera,
    // reduzido na mesma proporcao (mesmo tamanho angular).
    [RequireComponent(typeof(CelestialBody))]
    public class ScaledProxy : MonoBehaviour
    {
        public const float ProxyDistance = 40000f;

        public Camera Camera;
        public Material Material;

        public Transform Visual { get; private set; }
        public float DrawnRadius { get; private set; }

        CelestialBody body;

        void Awake()
        {
            body = GetComponent<CelestialBody>();
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(sphere.GetComponent<Collider>());
            sphere.name = "Visual";
            sphere.transform.SetParent(transform, false);
            if (Material) sphere.GetComponent<Renderer>().sharedMaterial = Material;
            Visual = sphere.transform;
        }

        public void SetMaterial(Material m)
        {
            Material = m;
            if (Visual) Visual.GetComponent<Renderer>().sharedMaterial = m;
        }

        void LateUpdate() => UpdateProxy();

        public void UpdateProxy()
        {
            var cam = Camera ? Camera : Camera.main;
            if (!cam) return;
            var origin = FloatingOrigin.Instance ? FloatingOrigin.Instance.Origin : default;
            var camWorld = WorldPosition.FromLocal(cam.transform.position, origin);

            double3 delta = body.Position - camWorld;
            double dist = math.length(delta);
            if (dist > ProxyDistance)
            {
                double f = ProxyDistance / dist;
                Visual.position = cam.transform.position + (Vector3)(float3)(delta * f);
                DrawnRadius = (float)(body.Radius * f);
            }
            else
            {
                Visual.position = body.Position.ToLocal(origin);
                DrawnRadius = (float)body.Radius;
            }
            Visual.localScale = Vector3.one * (DrawnRadius * 2f);
        }
    }
}
