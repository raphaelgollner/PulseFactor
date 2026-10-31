using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Piloto NPC: viaja entre estacoes (dobra simplificada nas distancias longas), pede doca no tamanho certo,
    // espera na fila, pousa, fica um tempo e decola para o proximo destino. Destino em outro setor: entra no
    // portal e some. Escoltas seguem o lider e orbitam a estacao enquanto ele esta docado.
    [RequireComponent(typeof(ShipController), typeof(WorldAnchor), typeof(NpcShip))]
    public class NpcPilot : MonoBehaviour
    {
        public enum Role { Patrol, Trader, Escort }
        enum State { Travel, Warp, Queue, Approach, Landing, Docked, Undocking }

        const double WarpAbove = 25000; // abaixo disso vai em subluz
        const float OrbitRadius = 1500f, LandSeconds = 5f, UndockSeconds = 4f;

        public Role role;
        public NpcPilot Leader;
        public Vector3 FormationOffset;
        public SectorManager Sectors;
        public System.Random Rng = new();
        public string FirstStationId;   // nasce chegando a esta estacao (estacoes vivas desde o inicio)
        public int WarpLevel = 2;       // nivel de dobra que o NPC usa (mesmas regras do jogador)

        ShipController ship;
        WorldAnchor anchor;
        NpcShip info;
        Rigidbody body;
        State state;
        TrafficPlanner.Destination dest;
        string currentStationId;
        Station station;
        Dock dock;
        float timer, orbitAngle;
        Vector3 moveFrom;
        Quaternion rotFrom;

        // Dobra com as mesmas regras do jogador: bloqueio de massa, aquecimento, niveis, freio e saida a 3 km.
        readonly WarpDrive drive = new(new WarpSettings());
        readonly List<MassSource> lockSources = new(), bodies = new(), artificial = new();
        float massTimer;
        bool massLocked;
        double nearestBody = double.PositiveInfinity;

        bool Busy => state is State.Queue or State.Approach or State.Landing or State.Docked or State.Undocking;
        public Station DockingStation => Busy ? station : null;
        public bool Warping => state == State.Warp;
        public bool ChargingWarp => drive.State == WarpState.Charging;

        void Awake()
        {
            ship = GetComponent<ShipController>();
            anchor = GetComponent<WorldAnchor>();
            info = GetComponent<NpcShip>();
            body = GetComponent<Rigidbody>();
            orbitAngle = UnityEngine.Random.value * 360f;
        }

        void Start()
        {
            if (role == Role.Escort) return;
            var first = string.IsNullOrEmpty(FirstStationId) ? null : Sectors.Stations.Find(s => s && s.DisplayId == FirstStationId);
            if (first)
            {
                dest = new TrafficPlanner.Destination { id = FirstStationId };
                station = first;
                state = State.Travel;
            }
            else PickDestination();
        }

        void OnDestroy() => ReleaseDock();

        DockSize Size => ship.ClassDef ? ship.ClassDef.dockSize : DockSize.P;

        void PickDestination()
        {
            var stations = new List<string>();
            foreach (var s in Sectors.Stations) if (s) stations.Add(s.DisplayId);
            var gates = new List<string>();
            foreach (var g in Sectors.Gates) if (g) gates.Add(g.DisplayId);
            dest = TrafficPlanner.NextDestination(Rng, currentStationId, stations, gates, role == Role.Trader);
            station = dest.isGate ? null : Sectors.Stations.Find(s => s && s.DisplayId == dest.id);
            state = State.Travel;
        }

        WorldPosition DestinationPosition()
        {
            if (dest.isGate)
            {
                var gate = Sectors.FindGate(dest.id);
                return gate ? gate.Position : anchor.World;
            }
            return station ? station.Position : anchor.World;
        }

        void Update()
        {
            if (!Sectors || FloatingOrigin.Instance == null) return;
            if (role == Role.Escort) { UpdateEscort(); return; }

            switch (state)
            {
                case State.Travel: Travel(); break;
                case State.Warp: Warp(); break;
                case State.Queue: Orbit(station ? station.transform.position : transform.position); WaitInQueue(); break;
                case State.Approach: Approach(); break;
                case State.Landing:
                    if (Move(dock.ApproachPoint.position, dock.LandingPoint, LandSeconds)) Docked();
                    break;
                case State.Docked:
                    transform.SetPositionAndRotation(dock.LandingPoint.position, dock.LandingPoint.rotation);
                    timer -= Time.deltaTime;
                    if (timer <= 0f) { BeginMove(); state = State.Undocking; }
                    break;
                case State.Undocking:
                    if (Move(dock.LandingPoint.position, dock.ApproachPoint, UndockSeconds)) Undocked();
                    break;
            }
        }

        // ---------- Viagem ----------

        void UpdateMass(WorldPosition world)
        {
            massTimer -= Time.deltaTime;
            if (massTimer > 0f) return;
            massTimer = 0.25f;
            lockSources.Clear(); bodies.Clear(); artificial.Clear();
            foreach (var t in TargetRegistry.All)
            {
                if (t is CelestialBody b) { var m = b.AsMassSource(); lockSources.Add(m); bodies.Add(m); }
                else if (t.Kind == TargetKind.Station) { var m = MassSource.Station(t.Position.value); lockSources.Add(m); artificial.Add(m); }
                else if (t.Kind == TargetKind.Gate) { var m = MassSource.Gate(t.Position.value); lockSources.Add(m); artificial.Add(m); }
            }
            massLocked = MassLock.IsLocked(world.value, lockSources);
            nearestBody = MassLock.NearestDistance(world.value, bodies);
        }

        void Travel()
        {
            var target = DestinationPosition();
            var world = anchor.World;
            double dist = WorldPosition.Distance(world, target);
            UpdateMass(world);

            // Longe do destino: sai do bloqueio de massa em subluz, aquece 5 s e entra em dobra.
            if (dist > WarpAbove)
            {
                if (drive.State == WarpState.Idle && !massLocked) drive.TryStartCharge(false, true, out _);
                if (drive.State == WarpState.Charging && massLocked) drive.Cancel();
                drive.Tick(Time.deltaTime, nearestBody, dist, true);
                if (drive.State == WarpState.Active)
                {
                    for (int i = 1; i < WarpLevel; i++) drive.ShiftUp();
                    state = State.Warp;
                    ship.TranslationLocked = true;
                    return;
                }
            }
            else if (drive.State == WarpState.Charging) drive.Cancel();

            Steer(FloatingOrigin.Instance.ToLocal(target), 1f);
            if (dest.isGate)
            {
                if (dist < 600) Destroy(gameObject); // atravessou o portal para outro setor
                return;
            }
            if (station && dist < station.Docking.RequestRange * 0.9f) RequestDock((float)dist);
        }

        void Warp()
        {
            var target = DestinationPosition();
            var world = anchor.World;
            double dist = WorldPosition.Distance(world, target);
            UpdateMass(world);
            double3 dir = math.normalize(target - world);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation((float3)dir), 20f * Time.deltaTime);
            double speed = drive.Tick(Time.deltaTime, nearestBody, dist, true);

            // Chegou (3 km) ou cruzou o bloqueio de massa de outra estacao/portal: sai da dobra.
            bool crossedLock = MassLock.LockingIndex(world.value, artificial) >= 0 && dist > WarpAbove;
            if (drive.State != WarpState.Active || crossedLock)
            {
                drive.Complete();
                ship.TranslationLocked = false;
                ship.SetVelocity(transform.forward * ship.Flight.EffectiveMaxSpeed(ship.Cargo.LoadFraction));
                state = State.Travel;
                return;
            }
            anchor.SetWorld(world + (double3)(float3)transform.forward * speed * Time.deltaTime);
        }

        void Steer(Vector3 goalLocal, float throttle)
        {
            var to = goalLocal - transform.position;
            var input = ship.Input;
            input.aimDirection = to.sqrMagnitude > 1f ? to.normalized : transform.forward;
            input.throttle = throttle;
            ship.Input = input;
        }

        void Orbit(Vector3 center)
        {
            orbitAngle += 6f * Time.deltaTime;
            var goal = center + Quaternion.Euler(0, orbitAngle, 0) * Vector3.forward * OrbitRadius;
            Steer(goal, 0.5f);
        }

        // ---------- Docagem ----------

        void RequestDock(float dist)
        {
            var r = station.Docking.Request(info.Id, dist, Size, Time.time);
            if (r.granted) { Assign(r.dockIndex); return; }
            if (r.reason == DockDenyReason.Queued)
            {
                state = State.Queue;
                station.DockGranted += OnGranted;
            }
            else PickDestination(); // estacao sem doca do nosso tamanho: segue viagem
        }

        void OnGranted(DockGrant g)
        {
            if (g.shipId != info.Id || !station) return;
            station.DockGranted -= OnGranted;
            Assign(g.dockIndex);
        }

        void WaitInQueue()
        {
            if (!station) { PickDestination(); return; }
        }

        void Assign(int index)
        {
            dock = station.Docks[index];
            state = State.Approach;
        }

        void Approach()
        {
            Steer(dock.ApproachPoint.position, 0.7f);
            if (Vector3.Distance(transform.position, dock.ApproachPoint.position) < 80f)
            {
                BeginMove();
                state = State.Landing;
            }
        }

        void Docked()
        {
            state = State.Docked;
            float stay = TrafficPlanner.NpcStay(Rng, Size);
            timer = stay;
            station.Docking.MarkDocked(info.Id);
            station.Docking.SetStayUntil(info.Id, Time.time + stay);
        }

        void Undocked()
        {
            ReleaseDock();
            body.isKinematic = false;
            ship.ControlsLocked = false;
            currentStationId = station ? station.DisplayId : null;
            dock = null;
            PickDestination();
        }

        void ReleaseDock()
        {
            if (station && station.Docking != null)
            {
                station.DockGranted -= OnGranted;
                station.Docking.Release(info.Id);
            }
        }

        void BeginMove()
        {
            ship.Stop();
            ship.ControlsLocked = true;
            body.isKinematic = true;
            timer = 0f;
            moveFrom = transform.position;
            rotFrom = transform.rotation;
        }

        bool Move(Vector3 from, Transform to, float seconds)
        {
            timer += Time.deltaTime / seconds;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer));
            transform.SetPositionAndRotation(Vector3.Lerp(moveFrom, to.position, k), Quaternion.Slerp(rotFrom, to.rotation, k));
            return timer >= 1f;
        }

        // ---------- Escolta ----------

        void UpdateEscort()
        {
            if (!Leader) { Destroy(gameObject); return; } // lider entrou no portal ou sumiu
            var leaderStation = Leader.DockingStation;
            if (leaderStation)
            {
                ship.TranslationLocked = false;
                Orbit(leaderStation.transform.position); // nao doca: orbita ate o lider decolar
                return;
            }
            if (Leader.Warping)
            {
                ship.TranslationLocked = true;
                var offset = (double3)(float3)(Leader.transform.rotation * FormationOffset);
                anchor.SetWorld(Leader.GetComponent<WorldAnchor>().World + offset);
                transform.rotation = Leader.transform.rotation;
                return;
            }
            ship.TranslationLocked = false;
            var goal = Leader.transform.TransformPoint(FormationOffset);
            float d = Vector3.Distance(transform.position, goal);
            Steer(goal, Mathf.Clamp01(d / 300f));
        }
    }
}
