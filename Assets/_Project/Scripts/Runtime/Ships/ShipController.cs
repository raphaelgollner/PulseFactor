using System.Collections.Generic;
using UnityEngine;

namespace PulseFactor
{
    [RequireComponent(typeof(Rigidbody))]
    public class ShipController : MonoBehaviour
    {
        public static readonly List<ShipController> All = new();

        public ShipClassDef ClassDef { get; private set; }
        public CargoHold Cargo { get; private set; }
        public FlightModel Flight { get; private set; }

        // Escrito por PlayerShipInput (jogador) ou por uma IA.
        public FlightInput Input;
        public bool ControlsLocked;

        // Em dobra a translacao e do WarpController; a nave so gira.
        public bool TranslationLocked;

        // Piloto automatico (ex.: evasao de planeta) sobrepoe a mira e o acelerador do jogador.
        public Vector3? AimOverride;
        public float? ThrottleOverride;

        // Referencial em movimento (ex.: rotacao de estacao), escrito a cada passo de fisica e consumido aqui.
        [System.NonSerialized] public Vector3 FrameVelocity;
        [System.NonSerialized] public Quaternion FrameRotationStep = Quaternion.identity;

        Rigidbody body;
        Vector3 appliedFrameVelocity;

        // Velocidade propria da nave (relativa ao referencial em que ela esta).
        public Vector3 Velocity => body ? body.linearVelocity - appliedFrameVelocity : Vector3.zero;

        void Awake() => SetupBody();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void SetupBody()
        {
            if (body) return;
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        public void Initialize(ShipClassDef def)
        {
            SetupBody();
            ClassDef = def;
            Cargo = new CargoHold(def.cargoCapacity);
            Flight = new FlightModel(def);
        }

        void FixedUpdate()
        {
            var frameVel = FrameVelocity;
            var frameRot = FrameRotationStep;
            FrameVelocity = Vector3.zero;
            FrameRotationStep = Quaternion.identity;

            if (Flight == null || body.isKinematic) return;
            if (ControlsLocked)
            {
                Input.cruiseToggle = false;
                return;
            }

            float dt = Time.fixedDeltaTime;
            if (AimOverride.HasValue) Input.aimDirection = AimOverride.Value;
            if (ThrottleOverride.HasValue) Input.throttle = ThrottleOverride.Value;

            // Velocidade relativa (inclui o efeito de colisoes), girada junto com o referencial.
            var relative = frameRot * (body.linearVelocity - appliedFrameVelocity);
            relative = TranslationLocked ? Vector3.zero
                : Flight.StepVelocity(relative, body.rotation, Input, Cargo.LoadFraction, dt);
            appliedFrameVelocity = TranslationLocked ? Vector3.zero : frameVel;
            body.linearVelocity = relative + appliedFrameVelocity;

            body.angularVelocity = Vector3.zero; // rotacao controlada pelo FlightModel
            body.MoveRotation(frameRot * Flight.StepRotation(body.rotation, Input, dt));
            Input.cruiseToggle = false; // pulso consumido
        }

        public void Stop()
        {
            appliedFrameVelocity = Vector3.zero;
            if (body && !body.isKinematic) body.linearVelocity = Vector3.zero;
        }

        public void SetVelocity(Vector3 v)
        {
            appliedFrameVelocity = Vector3.zero;
            if (body && !body.isKinematic) body.linearVelocity = v;
        }
    }
}
