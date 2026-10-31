using UnityEngine;

namespace PulseFactor
{
    // Voo estilo Everspace: a nave persegue a mira, solta o acelerador e desliza
    // desacelerando devagar. Carga reduz so a velocidade maxima.
    public class FlightModel
    {
        readonly ShipClassDef def;

        public FlightModel(ShipClassDef def) { this.def = def; }

        public bool CruiseActive { get; private set; }
        public float CruiseTarget { get; private set; }

        // Fracao da velocidade maxima permitida em re.
        const float ReverseFraction = 0.25f;

        public float EffectiveMaxSpeed(float loadFraction) =>
            def.maxSpeed * (1f - def.fullCargoSpeedPenalty * Mathf.Clamp01(loadFraction));

        public Vector3 StepVelocity(Vector3 velocity, Quaternion rotation, FlightInput input, float loadFraction, float dt)
        {
            float maxSpeed = EffectiveMaxSpeed(loadFraction);
            float accel = def.acceleration;
            float release = accel * def.releaseDecelFactor;

            // Velocidade no referencial da nave.
            Vector3 local = Quaternion.Inverse(rotation) * velocity;

            if (input.cruiseToggle)
            {
                CruiseActive = !CruiseActive;
                if (CruiseActive) CruiseTarget = Mathf.Clamp(local.z, 0f, maxSpeed);
            }

            if (CruiseActive)
            {
                CruiseTarget = Mathf.Clamp(CruiseTarget + input.throttle * def.cruiseAdjustRate * dt, 0f, maxSpeed);
                local.z = Mathf.MoveTowards(local.z, CruiseTarget, accel * dt);
            }
            else if (Mathf.Abs(input.throttle) > 0.001f)
            {
                local.z += input.throttle * accel * dt;
                local.z = Mathf.Clamp(local.z, -maxSpeed * ReverseFraction, maxSpeed);
            }
            else
            {
                local.z = Mathf.MoveTowards(local.z, 0f, release * dt);
            }

            local.x = Axis(local.x, input.strafe.x, accel, release, dt);
            local.y = Axis(local.y, input.strafe.y, accel, release, dt);

            Vector3 result = rotation * local;
            return Vector3.ClampMagnitude(result, maxSpeed);
        }

        static float Axis(float speed, float input, float accel, float release, float dt) =>
            Mathf.Abs(input) > 0.001f
                ? speed + input * accel * dt
                : Mathf.MoveTowards(speed, 0f, release * dt);

        public Quaternion StepRotation(Quaternion rotation, FlightInput input, float dt)
        {
            float maxStep = def.turnRateDeg * dt;
            if (input.aimDirection.sqrMagnitude > 1e-6f)
            {
                var target = Quaternion.LookRotation(input.aimDirection, rotation * Vector3.up);
                rotation = Quaternion.RotateTowards(rotation, target, maxStep);
            }
            if (Mathf.Abs(input.roll) > 0.001f)
                rotation *= Quaternion.Euler(0f, 0f, -input.roll * maxStep);
            return rotation;
        }
    }
}
