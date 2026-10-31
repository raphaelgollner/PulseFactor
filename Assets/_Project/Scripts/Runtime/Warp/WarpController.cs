using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Velocidade de Dobra: J aquece/cancela/sai; W/S trocam o nivel; o mouse continua pilotando.
    // A nave anda em double na direcao do nariz e a origem flutuante acompanha a cada frame.
    [RequireComponent(typeof(ShipController), typeof(WorldAnchor))]
    public class WarpController : MonoBehaviour
    {
        const float HeadingDot = 0.9f;

        public WarpSettings settings = new();
        public PlayerShipInput Input;
        public TargetSelector Targets;
        public RouteService Route;

        // Alvo da dobra: o selecionado com T ou, se nenhum, o proximo ponto da rota.
        public ITargetable Target => (Targets ? Targets.Current : null) ?? (Route ? Route.Waypoint : null);

        public WarpDrive Drive { get; private set; }
        public bool MassLocked { get; private set; }
        public double DistanceToTarget { get; private set; }

        public event System.Action<WarpState> StateChanged;

        ShipController ship;
        WorldAnchor anchor;
        WarpState lastState;
        float lastThrottle;
        readonly List<MassSource> lockSources = new();
        readonly List<MassSource> bodies = new();

        void Awake()
        {
            ship = GetComponent<ShipController>();
            anchor = GetComponent<WorldAnchor>();
            Drive = new WarpDrive(settings);
        }

        void Start()
        {
            if (Input) Input.OnWarp += OnWarpPressed;
        }

        public void OnDamage() => Drive.OnDamage();

        void OnWarpPressed()
        {
            switch (Drive.State)
            {
                case WarpState.Idle:
                    if (Drive.TryStartCharge(MassLocked, Target != null, out var reason)) GameMessages.Post("msg.warp_charging");
                    else GameMessages.Post(reason == WarpFailReason.NoTarget ? "msg.warp_no_target" : "msg.warp_masslocked");
                    break;
                case WarpState.Charging:
                    Drive.Cancel();
                    GameMessages.Post("msg.warp_cancelled");
                    break;
                case WarpState.Active:
                    ExitWarp();
                    break;
            }
        }

        void Update()
        {
            var origin = FloatingOrigin.Instance;
            if (!origin) return;
            var shipWorld = origin.ToWorld(transform.position);

            lockSources.Clear();
            bodies.Clear();
            foreach (var t in TargetRegistry.All)
            {
                if (t is CelestialBody b) { var m = b.AsMassSource(); lockSources.Add(m); bodies.Add(m); }
                else if (t.Kind == TargetKind.Station) lockSources.Add(MassSource.Station(t.Position.value));
                else if (t.Kind == TargetKind.Gate) lockSources.Add(MassSource.Gate(t.Position.value));
            }
            MassLocked = MassLock.IsLocked(shipWorld.value, lockSources);
            double nearestBody = MassLock.NearestDistance(shipWorld.value, bodies);

            var target = Target;
            if (Drive.State == WarpState.Charging && target == null) { Drive.Cancel(); GameMessages.Post("msg.warp_cancelled"); }
            bool heading = false;
            DistanceToTarget = double.PositiveInfinity;
            if (target != null)
            {
                var delta = target.Position - shipWorld;
                DistanceToTarget = math.length(delta);
                if (DistanceToTarget > 1)
                    heading = Vector3.Dot(transform.forward, (Vector3)(float3)(delta / DistanceToTarget)) > HeadingDot;
            }

            if (Drive.State == WarpState.Active) ReadGearShift();
            ship.Flight.SteeringScale = Drive.State == WarpState.Active ? WarpDrive.SteeringFactor(Drive.Level) : 1f;
            double speed = Drive.Tick(Time.deltaTime, nearestBody, DistanceToTarget, heading);

            if (Drive.State == WarpState.Active)
            {
                ship.TranslationLocked = true;
                ship.Stop();
                double3 dir = (float3)transform.forward;
                anchor.MoveWorld(dir * speed * Time.deltaTime);
                origin.Rebase(anchor.World);
            }
            else if (Drive.State == WarpState.Exiting)
            {
                ExitWarp();
            }

            if (Drive.State != lastState)
            {
                lastState = Drive.State;
                StateChanged?.Invoke(lastState);
            }
        }

        // W/S em dobra: um toque sobe/desce um nivel.
        void ReadGearShift()
        {
            float t = Input ? Input.Controls.Throttle.ReadValue<float>() : 0f;
            if (t > 0.5f && lastThrottle <= 0.5f) { Drive.ShiftUp(); GameMessages.Post("msg.warp_level", Drive.Level); }
            if (t < -0.5f && lastThrottle >= -0.5f) { Drive.ShiftDown(); GameMessages.Post("msg.warp_level", Drive.Level); }
            lastThrottle = t;
        }

        // Saida forcada (ex.: perto demais de um planeta).
        public void ForceExit()
        {
            if (Drive.State == WarpState.Idle) return;
            bool wasActive = Drive.State != WarpState.Charging;
            Drive.Complete();
            ship.TranslationLocked = false;
            if (wasActive) ship.SetVelocity(transform.forward * ship.Flight.EffectiveMaxSpeed(ship.Cargo.LoadFraction) * 0.5f);
            GameMessages.Post("msg.warp_emergency");
        }

        void ExitWarp()
        {
            Drive.Complete();
            ship.TranslationLocked = false;
            ship.SetVelocity(transform.forward * ship.Flight.EffectiveMaxSpeed(ship.Cargo.LoadFraction));
            GameMessages.Post("msg.warp_exit");
        }
    }
}
