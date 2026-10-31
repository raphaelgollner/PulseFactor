using UnityEngine;

namespace PulseFactor
{
    // Pedido de docagem (L), pouso automatico perto da doca e decolagem.
    [RequireComponent(typeof(ShipController))]
    public class DockingAutopilot : MonoBehaviour
    {
        public const string PlayerId = "player";

        public enum Phase { Free, Authorized, Landing, Docked, Undocking }

        [SerializeField] float autopilotRange = 250f;
        [SerializeField] float landingSeconds = 6f;
        [SerializeField] float undockSeconds = 4f;

        public PlayerShipInput Input;
        public WarpController Warp;
        public TargetSelector Targets;

        public Phase Current { get; private set; }
        public Station Station { get; private set; }
        public Dock Dock { get; private set; }

        public event System.Action Docked, Undocked;

        ShipController ship;
        Rigidbody body;
        float t;
        Vector3 fromPos;
        Quaternion fromRot;

        void Awake()
        {
            ship = GetComponent<ShipController>();
            body = GetComponent<Rigidbody>();
        }

        void Start()
        {
            if (Input) Input.OnRequestDock += RequestDock;
        }

        public Station NearestStation(out float distance)
        {
            Station best = null;
            distance = float.MaxValue;
            foreach (var t in TargetRegistry.All)
            {
                if (t is not Station s) continue;
                float d = s.DistanceTo(transform.position);
                if (d < distance) { distance = d; best = s; }
            }
            return best;
        }

        public void RequestDock()
        {
            if (Current != Phase.Free && Current != Phase.Authorized) return;
            if (Warp && Warp.Drive.State != WarpState.Idle) return;
            var s = NearestStation(out float dist);
            if (!s) { GameMessages.Post("msg.dock_no_station"); return; }

            if (dist > s.Docking.RequestRange)
            {
                // Fora da zona de influencia: aviso em destaque com a distancia atual.
                HudNotice.Show("notice.dock_too_far", 4f, HudModel.FormatDistance(s.Docking.RequestRange), HudModel.FormatDistance(dist));
                return;
            }
            var shipSize = ship.ClassDef ? ship.ClassDef.dockSize : DockSize.P;
            var r = s.Docking.Request(PlayerId, dist, shipSize);
            if (!r.granted)
            {
                GameMessages.Post(r.reason switch
                {
                    DockDenyReason.OutOfRange => "msg.dock_out_of_range",
                    DockDenyReason.NoFreeDock => "msg.dock_no_free",
                    _ => "msg.dock_denied",
                });
                return;
            }
            SetDock(s, s.Docks[r.dockIndex]);
            Current = Phase.Authorized;
            GameMessages.Post("msg.dock_granted", Dock.Number, Dock.size);
            if (Targets) Targets.Select(new DockTarget(Dock));
        }

        void SetDock(Station s, Dock d)
        {
            if (Dock) Dock.SetHighlighted(false);
            Station = s;
            Dock = d;
            if (Dock) Dock.SetHighlighted(true);
        }

        void Update()
        {
            switch (Current)
            {
                case Phase.Authorized:
                    float dist = Station.DistanceTo(transform.position);
                    Station.Docking.Tick(PlayerId, dist);
                    if (!Station.Docking.AssignedDock(PlayerId).HasValue)
                    {
                        SetDock(null, null);
                        Current = Phase.Free;
                        GameMessages.Post("msg.dock_lost");
                    }
                    else if (Vector3.Distance(transform.position, Dock.ApproachPoint.position) < autopilotRange)
                    {
                        BeginMove(Phase.Landing);
                    }
                    break;
                case Phase.Landing:
                    if (Move(Dock.ApproachPoint, Dock.LandingPoint, landingSeconds))
                    {
                        Current = Phase.Docked;
                        Station.Docking.MarkDocked(PlayerId);
                        Dock.SetHighlighted(false);
                        Docked?.Invoke();
                    }
                    break;
                case Phase.Docked:
                    transform.SetPositionAndRotation(Dock.LandingPoint.position, Dock.LandingPoint.rotation);
                    break;
                case Phase.Undocking:
                    if (Move(Dock.LandingPoint, Dock.ApproachPoint, undockSeconds))
                    {
                        Station.Docking.Release(PlayerId);
                        body.isKinematic = false;
                        ship.ControlsLocked = false;
                        SetDock(null, null);
                        Current = Phase.Free;
                        Undocked?.Invoke();
                    }
                    break;
            }
        }

        public void Undock()
        {
            if (Current == Phase.Docked) BeginMove(Phase.Undocking);
        }

        void BeginMove(Phase phase)
        {
            Current = phase;
            ship.ControlsLocked = true;
            ship.Stop();
            body.isKinematic = true;
            t = 0f;
            fromPos = transform.position;
            fromRot = transform.rotation;
            if (phase == Phase.Landing) GameMessages.Post("msg.dock_autopilot");
        }

        // Primeira metade: alinha no ponto inicial; segunda: segue ate o destino.
        bool Move(Transform start, Transform end, float seconds)
        {
            t += Time.deltaTime / seconds;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            Vector3 a = Vector3.Lerp(fromPos, start.position, Mathf.Clamp01(k * 2f));
            Vector3 p = k < 0.5f ? a : Vector3.Lerp(start.position, end.position, (k - 0.5f) * 2f);
            Quaternion r = Quaternion.Slerp(fromRot, end.rotation, k);
            transform.SetPositionAndRotation(p, r);
            return t >= 1f;
        }
    }
}
