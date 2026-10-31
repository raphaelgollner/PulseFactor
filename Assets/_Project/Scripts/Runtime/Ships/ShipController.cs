using UnityEngine;

namespace PulseFactor
{
    [RequireComponent(typeof(Rigidbody))]
    public class ShipController : MonoBehaviour
    {
        public ShipClassDef ClassDef { get; private set; }
        public CargoHold Cargo { get; private set; }
        public FlightModel Flight { get; private set; }

        // Escrito por PlayerShipInput (jogador) ou por uma IA.
        public FlightInput Input;
        public bool ControlsLocked;

        Rigidbody body;

        public Vector3 Velocity => body ? body.linearVelocity : Vector3.zero;

        void Awake() => SetupBody();

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
            if (Flight == null) return;
            if (ControlsLocked)
            {
                Input.cruiseToggle = false;
                return;
            }

            float dt = Time.fixedDeltaTime;
            body.linearVelocity = Flight.StepVelocity(body.linearVelocity, body.rotation, Input, Cargo.LoadFraction, dt);
            body.angularVelocity = Vector3.zero; // rotacao controlada pelo FlightModel
            body.MoveRotation(Flight.StepRotation(body.rotation, Input, dt));
            Input.cruiseToggle = false; // pulso consumido
        }

        public void Stop()
        {
            if (body) body.linearVelocity = Vector3.zero;
        }
    }
}
