using UnityEngine;

namespace PulseFactor
{
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public enum Mode { Cockpit, ThirdPerson, Showcase }

        public const string ExteriorCockpitLayer = "ShipExteriorCockpit";

        [SerializeField] float followSmoothTime = 0.1f;
        [SerializeField] float showcaseSpeed = 12f; // graus/s

        public Mode CurrentMode { get; private set; } = Mode.ThirdPerson;

        Camera cam;
        AssembledShip ship;
        Vector3 followVelocity;
        float showcaseAngle;
        int fullMask = ~0;

        void Awake() => Init();

        void Init()
        {
            if (cam) return;
            cam = GetComponent<Camera>();
            cam.farClipPlane = 100000f;
            fullMask = cam.cullingMask;
        }

        public void Attach(GameObject shipObject)
        {
            Init();
            ship = shipObject.GetComponent<AssembledShip>();
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
            followVelocity = Vector3.zero;
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
                    var target = ThirdPersonPosition();
                    transform.position = Vector3.SmoothDamp(transform.position, target, ref followVelocity, followSmoothTime);
                    transform.rotation = ship.transform.rotation;
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
                    transform.SetPositionAndRotation(eye.position, ship.transform.rotation);
                    break;
                case Mode.ThirdPerson:
                    transform.SetPositionAndRotation(ThirdPersonPosition(), ship.transform.rotation);
                    break;
                case Mode.Showcase:
                    PlaceShowcase();
                    break;
            }
        }

        float ShipSize() => ship.LocalBounds().size.magnitude;

        Vector3 ThirdPersonPosition()
        {
            float d = Mathf.Max(8f, ShipSize() * 1.2f);
            var t = ship.transform;
            return t.position - t.forward * d + t.up * (d * 0.28f);
        }

        void PlaceShowcase()
        {
            float d = Mathf.Max(8f, ShipSize() * 1.4f);
            var t = ship.transform;
            var dir = Quaternion.AngleAxis(showcaseAngle, t.up) * (t.forward + t.up * 0.35f).normalized;
            transform.position = t.position + dir * d;
            transform.LookAt(t.position, t.up);
        }
    }
}
