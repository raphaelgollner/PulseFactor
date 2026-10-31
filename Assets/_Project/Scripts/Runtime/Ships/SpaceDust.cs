using UnityEngine;

namespace PulseFactor
{
    // Poeira espacial que passa pela camera e estica com a velocidade (sensacao de movimento).
    // O sistema vive num ponto que segue a nave sem girar, e as particulas recebem -velocidade.
    public class SpaceDust : MonoBehaviour
    {
        [SerializeField] float radius = 140f;
        [SerializeField] float fullAlphaSpeed = 120f;
        [SerializeField] float warpVisualSpeed = 2500f;

        public ShipController Ship;
        public WarpController Warp;
        public Material Material;

        ParticleSystem ps;
        ParticleSystem.VelocityOverLifetimeModule velocity;
        ParticleSystem.MainModule main;

        void Start()
        {
            ps = gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            main = ps.main;
            main.loop = true;
            main.startLifetime = 2.5f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
            main.maxParticles = 900;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startColor = new Color(0.8f, 0.9f, 1f, 0f);

            var emission = ps.emission;
            emission.rateOverTime = 320f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;

            var r = GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.04f;
            r.lengthScale = 1f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (Material) r.sharedMaterial = Material;

            ps.Play();
        }

        void LateUpdate()
        {
            if (!Ship || ps == null) return;
            transform.SetPositionAndRotation(Ship.transform.position, Quaternion.identity);

            Vector3 v = Ship.Velocity;
            if (Warp && Warp.Drive.State == WarpState.Active) v = Ship.transform.forward * warpVisualSpeed;

            velocity.x = -v.x;
            velocity.y = -v.y;
            velocity.z = -v.z;

            // So aparece com a nave em movimento.
            float a = Mathf.Clamp01(v.magnitude / fullAlphaSpeed) * 0.8f;
            main.startColor = new Color(0.8f, 0.9f, 1f, a);
        }
    }
}
