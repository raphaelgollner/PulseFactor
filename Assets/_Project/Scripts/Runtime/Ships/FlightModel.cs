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

        // Velocidade angular local (graus/s): x = pitch, y = yaw, z = roll.
        public Vector3 AngularVelocity { get; private set; }

        // Giro proporcional ao erro da mira (satura em steerAngle) com aceleracao angular: a nave tem peso.
        public Quaternion StepRotation(Quaternion rotation, FlightInput input, float dt)
        {
            float rate = def.turnRateDeg;
            Vector2 steer = Vector2.zero;
            if (input.aimDirection.sqrMagnitude > 1e-6f)
            {
                var local = Quaternion.Inverse(rotation) * input.aimDirection.normalized;
                float yawErr = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                float pitchErr = -Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;
                if (local.z < 0f && Mathf.Abs(local.x) < 1e-3f) yawErr = 180f; // mira exatamente atras
                steer = new Vector2(
                    Mathf.Clamp(yawErr / def.steerAngleDeg, -1f, 1f),
                    Mathf.Clamp(pitchErr / def.steerAngleDeg, -1f, 1f));
            }

            var target = new Vector3(steer.y * rate, steer.x * rate, -Mathf.Clamp(input.roll, -1f, 1f) * rate);
            float accel = rate * def.angularResponse * dt;
            var w = AngularVelocity;
            w = new Vector3(
                Mathf.MoveTowards(w.x, target.x, accel),
                Mathf.MoveTowards(w.y, target.y, accel),
                Mathf.MoveTowards(w.z, target.z, accel));
            AngularVelocity = w;
            return rotation * Quaternion.Euler(w * dt);
        }
    }
}
