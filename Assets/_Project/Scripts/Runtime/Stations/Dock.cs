using UnityEngine;

namespace PulseFactor
{
    // Plataforma de pouso. Numero holografico sempre visivel; seta azul so quando a doca e a sua.
    public class Dock : MonoBehaviour
    {
        public int index;
        public DockSize size;
        public float padSize = 44f;
        public Transform ApproachPoint;
        public Transform LandingPoint;
        public GameObject Marker; // seta azul translucida

        public int Number => index + 1;

        public void SetHighlighted(bool on)
        {
            if (Marker) Marker.SetActive(on);
        }
    }

    // Alvo de navegacao que aponta para uma doca (usado pelo HUD depois da autorizacao).
    public class DockTarget : ITargetable
    {
        readonly Dock dock;

        public DockTarget(Dock dock) { this.dock = dock; }

        public bool IsAlive => dock && dock.LandingPoint;

        public WorldPosition Position
        {
            get
            {
                var o = FloatingOrigin.Instance;
                return o && IsAlive ? o.ToWorld(dock.LandingPoint.position) : default;
            }
        }

        public string DisplayId => "dock_" + dock.Number;
        public TargetKind Kind => TargetKind.Station;
    }

    // Faz o objeto sempre olhar para a camera (numeros holograficos).
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
        }
    }

    // Seta flutuando sobre a doca, subindo e descendo.
    public class Bob : MonoBehaviour
    {
        public float amplitude = 6f;
        public float speed = 1.6f;
        Vector3 basePos;

        void Awake() => basePos = transform.localPosition;
        void Update() => transform.localPosition = basePos + Vector3.up * (Mathf.Sin(Time.time * speed) * amplitude);
    }
}
