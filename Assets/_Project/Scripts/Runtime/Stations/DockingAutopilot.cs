using UnityEngine;

namespace PulseFactor
{
    // Pedido de docagem (L), pouso automatico perto da doca e decolagem.
    [RequireComponent(typeof(ShipController))]
    public class DockingAutopilot : MonoBehaviour
    {
        public const string PlayerId = "player";

        public enum Phase { Free, Queued, Authorized, Landing, Docked, Undocking }

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

        // Em cima da plataforma (dentro do quadrado dela, com folga) e a ate ~250 m de altura.
        public static bool IsAbovePad(Vector3 localToPad, float padSize, float maxHeight = 250f)
        {
            float half = padSize * 0.6f;
            return Mathf.Abs(localToPad.x) < half && Mathf.Abs(localToPad.z) < half
                && localToPad.y > 5f && localToPad.y < maxHeight;
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
            if (Current != Phase.Free && Current != Phase.Authorized && Current != Phase.Queued) return;
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
            var r = s.Docking.Request(PlayerId, dist, shipSize, Time.time);
            if (r.reason == DockDenyReason.Queued)
            {
                EnterQueue(s, r);
                return;
            }
            if (!r.granted)
            {
                GameMessages.Post(r.reason switch
                {
                    DockDenyReason.OutOfRange => "msg.dock_out_of_range",
                    DockDenyReason.NoDockOfSize => "msg.dock_no_size",
                    _ => "msg.dock_denied",
                });
                return;
            }
            Grant(s, r.dockIndex);
        }

        void Grant(Station s, int dockIndex)
        {
            if (queuedAt) queuedAt.DockGranted -= OnDockGranted;
            queuedAt = null;
            SetDock(s, s.Docks[dockIndex]);
            Current = Phase.Authorized;
            HudNotice.Show("notice.dock_granted", 7f, Dock.Number.ToString("00"));
            if (Targets) Targets.Select(new DockTarget(Dock));
        }

        // Sem vaga do nosso tamanho: fila, com previsao no feed (atualizada a cada 10 s).
        Station queuedAt;
        float queueNoticeTimer;

        void EnterQueue(Station s, DockRequestResult r)
        {
            if (queuedAt != s)
            {
                if (queuedAt) queuedAt.DockGranted -= OnDockGranted;
                queuedAt = s;
                s.DockGranted += OnDockGranted;
            }
            Station = s;
            Current = Phase.Queued;
            queueNoticeTimer = 0f;
            ShowQueueNotice(r);
        }

        static void ShowQueueNotice(DockRequestResult r)
        {
            if (double.IsNaN(r.etaSeconds)) HudNotice.Show("notice.dock_queue_unknown", 6f, r.queuePosition);
            else HudNotice.Show("notice.dock_queue", 6f, r.queuePosition, System.Math.Ceiling(r.etaSeconds));
        }

        void OnDockGranted(DockGrant g)
        {
            if (g.shipId == PlayerId && queuedAt) Grant(queuedAt, g.dockIndex);
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
                case Phase.Queued:
                    float qd = Station.DistanceTo(transform.position);
                    Station.Docking.Tick(PlayerId, qd);
                    if (Station.Docking.QueuePosition(PlayerId) == 0 && !Station.Docking.AssignedDock(PlayerId).HasValue)
                    {
                        if (queuedAt) queuedAt.DockGranted -= OnDockGranted;
                        queuedAt = null;
                        Current = Phase.Free;
                        GameMessages.Post("msg.dock_queue_left");
                        break;
                    }
                    queueNoticeTimer += Time.deltaTime;
                    if (queueNoticeTimer >= 10f)
                    {
                        queueNoticeTimer = 0f;
                        ShowQueueNotice(Station.Docking.Request(PlayerId, qd, ship.ClassDef ? ship.ClassDef.dockSize : DockSize.P, Time.time));
                    }
                    break;
                case Phase.Authorized:
                    float dist = Station.DistanceTo(transform.position);
                    Station.Docking.Tick(PlayerId, dist);
                    if (!Station.Docking.AssignedDock(PlayerId).HasValue)
                    {
                        SetDock(null, null);
                        Current = Phase.Free;
                        GameMessages.Post("msg.dock_lost");
                    }
                    else if (IsAbovePad(Dock.transform.InverseTransformPoint(transform.position), Dock.padSize))
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
                        if (Targets && Targets.Current is DockTarget) Targets.Select(null); // a doca deixa de ser o alvo
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
