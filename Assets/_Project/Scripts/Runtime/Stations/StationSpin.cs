using UnityEngine;

namespace PulseFactor
{
    // Rotacao lenta da estacao. Dentro da zona de influencia (8 km) as naves giram junto,
    // como numa orbita artificial: o referencial da estacao e somado ao movimento da nave.
    [DefaultExecutionOrder(-100)]
    public class StationSpin : MonoBehaviour
    {
        public const float InfluenceRadius = 8000f;

        public float degreesPerSecond = 0.15f; // ~1 volta a cada 40 min (playtest: 0,5 era perceptivel demais)

        public static bool InInfluence(float distance) => distance < InfluenceRadius;

        public static (Vector3 position, Quaternion rotation, Vector3 velocity) CoRotate(
            Vector3 center, Vector3 axis, float degrees, Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            var q = Quaternion.AngleAxis(degrees, axis);
            return (center + q * (position - center), q * rotation, q * velocity);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float deg = degreesPerSecond * dt;
            var axis = transform.up;
            var center = transform.position;
            transform.rotation = Quaternion.AngleAxis(deg, axis) * transform.rotation;

            foreach (var ship in ShipController.All)
            {
                var pos = ship.transform.position;
                if (!InInfluence(Vector3.Distance(pos, center))) continue;
                var r = CoRotate(center, axis, deg, pos, Quaternion.identity, Vector3.zero);
                ship.FrameVelocity = (r.position - pos) / dt;
                ship.FrameRotationStep = r.rotation;
            }
        }
    }
}
