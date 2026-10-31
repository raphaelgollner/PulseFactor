using UnityEngine;

namespace PulseFactor
{
    // Seleciona o alvo mais proximo da direcao da mira (tecla T).
    public class TargetSelector : MonoBehaviour
    {
        [SerializeField] float maxAngle = 25f;
        [SerializeField] double maxAsteroidDistance = 10000;

        public PlayerShipInput Input;
        ITargetable current;

        // Alvos de setores descarregados, asteroides esgotados ou docas destruidas somem sozinhos.
        public ITargetable Current
        {
            get
            {
                if (current is Object o && !o) current = null;
                else if (current is DockTarget d && !d.IsAlive) current = null;
                return current;
            }
        }

        public event System.Action<ITargetable> Changed;

        void Start()
        {
            if (Input) Input.OnSelectTarget += SelectNearestToAim;
        }

        public void Select(ITargetable t)
        {
            current = t;
            Changed?.Invoke(t);
        }

        public void SelectNearestToAim()
        {
            var cam = Camera.main;
            if (!cam) return;
            var origin = FloatingOrigin.Instance;
            var camWorld = origin ? origin.ToWorld(cam.transform.position) : default;
            Vector3 aim = cam.transform.forward;
            if (Input) aim = Input.AimDirection;

            ITargetable best = null;
            float bestAngle = maxAngle;
            foreach (var t in TargetRegistry.All)
            {
                if (ReferenceEquals(t, GetComponent<ITargetable>())) continue;
                var delta = t.Position - camWorld;
                double dist = Unity.Mathematics.math.length(delta);
                if (t.Kind == TargetKind.Asteroid && dist > maxAsteroidDistance) continue;
                if (dist < 1) continue;
                var dir = (Vector3)(Unity.Mathematics.float3)(delta / dist);
                float angle = Vector3.Angle(aim, dir);
                if (angle < bestAngle) { bestAngle = angle; best = t; }
            }
            Select(best);
            GameMessages.Post(best != null ? "msg.target_selected" : "msg.no_target", best != null ? Loc.T("target." + best.DisplayId) : "");
        }
    }
}
