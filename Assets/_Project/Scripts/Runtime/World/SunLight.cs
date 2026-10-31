using UnityEngine;

namespace PulseFactor
{
    // Luz direcional que sempre vem do sol do setor em direcao a camera.
    [RequireComponent(typeof(CelestialBody))]
    public class SunLight : MonoBehaviour
    {
        Light sun;
        CelestialBody body;

        void Awake()
        {
            body = GetComponent<CelestialBody>();
            sun = new GameObject("SunLight").AddComponent<Light>();
            sun.transform.SetParent(transform, false);
            sun.type = LightType.Directional;
            sun.intensity = 1.6f;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            var origin = FloatingOrigin.Instance;
            if (!cam || !origin) return;
            var toCam = origin.ToWorld(cam.transform.position) - body.Position;
            sun.transform.rotation = Quaternion.LookRotation((Vector3)(Unity.Mathematics.float3)Unity.Mathematics.math.normalize(toCam));
        }
    }
}
