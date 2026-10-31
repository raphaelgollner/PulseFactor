using UnityEngine;

namespace PulseFactor
{
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public enum Mode { Cockpit, ThirdPerson, Showcase }

        public const string ExteriorCockpitLayer = "ShipExteriorCockpit";

        [SerializeField] float rotationFollow = 7f; // quanto maior, mais rapido a camera alcanca a rotacao da nave
        [SerializeField] float showcaseSpeed = 12f; // graus/s

        public Mode CurrentMode { get; private set; } = Mode.ThirdPerson;

        // Olhar livre (Z): yaw/pitch em graus somados a camera, em qualquer modo de voo.
        public PlayerShipInput LookSource;
        Quaternion Look => LookSource ? Quaternion.Euler(LookSource.LookAngles.y, LookSource.LookAngles.x, 0f) : Quaternion.identity;

        Camera cam;
        AssembledShip ship;
        Quaternion followRotation = Quaternion.identity;
        float shipSize = 10f;
        float showcaseAngle;
        int fullMask = ~0;

        void Awake() => Init();

        void Init()
        {
            if (cam) return;
            cam = GetComponent<Camera>();
            cam.farClipPlane = 100000f;
            fullMask = cam.cullingMask & ~(1 << ShipPreview.Layer); // palco do estaleiro
        }

        public void Attach(GameObject shipObject)
        {
            Init();
            ship = shipObject.GetComponent<AssembledShip>();
            shipSize = ship.LocalBounds().size.magnitude;
            SetMode(CurrentMode);
        }

        public void Toggle() => SetMode(CurrentMode == Mode.Cockpit ? Mode.ThirdPerson : Mode.Cockpit);

        public void SetMode(Mode mode)
        {
            Init();
            CurrentMode = mode;
            int layer = LayerMask.NameToLayer(ExteriorCockpitLayer);
            cam.cullingMask = mode == Mode.Cockpit && layer >= 0 ? fullMask & ~(1 << layer) : fullMask;
            cam.nearClipPlane = mode == Mode.Cockpit ? 0.05f : 0.3f;
            if (ship) followRotation = ship.transform.rotation;
            Snap();
        }

        void LateUpdate()
        {
            if (!ship) return;
            switch (CurrentMode)
            {
                case Mode.Cockpit:
                    Snap();
                    break;
                case Mode.ThirdPerson:
                    // Rotacao com leve atraso (peso); posicao presa a nave para nao tremer em alta velocidade.
                    float k = 1f - Mathf.Exp(-rotationFollow * Time.deltaTime);
                    followRotation = Quaternion.Slerp(followRotation, ship.transform.rotation, k);
                    var orbit = followRotation * Look;
                    transform.SetPositionAndRotation(ThirdPersonPosition(orbit), orbit);
                    break;
                case Mode.Showcase:
                    showcaseAngle += showcaseSpeed * Time.unscaledDeltaTime;
                    PlaceShowcase();
                    break;
            }
        }

        void Snap()
        {
            if (!ship) return;
            switch (CurrentMode)
            {
                case Mode.Cockpit:
                    var eye = ship.EyePoint ? ship.EyePoint : ship.transform;
                    transform.SetPositionAndRotation(eye.position, ship.transform.rotation * Look);
                    break;
                case Mode.ThirdPerson:
                    followRotation = ship.transform.rotation;
                    transform.SetPositionAndRotation(ThirdPersonPosition(followRotation), followRotation);
                    break;
                case Mode.Showcase:
                    PlaceShowcase();
                    break;
            }
        }

        Vector3 ThirdPersonPosition(Quaternion rotation)
        {
            float d = Mathf.Max(8f, shipSize * 1.2f);
            return ship.transform.position + rotation * new Vector3(0f, d * 0.28f, -d);
        }

        void PlaceShowcase()
        {
            float d = Mathf.Max(8f, shipSize * 1.4f);
            var t = ship.transform;
            var dir = Quaternion.AngleAxis(showcaseAngle, t.up) * (t.forward + t.up * 0.35f).normalized;
            transform.position = t.position + dir * d;
            transform.LookAt(t.position, t.up);
        }
    }
}
