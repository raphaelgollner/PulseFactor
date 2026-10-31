using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Liga a tecla de dobra ao WarpDrive e move a nave em double durante a dobra.
    [RequireComponent(typeof(ShipController), typeof(WorldAnchor))]
    public class WarpController : MonoBehaviour
    {
        public WarpSettings settings = new();
        public PlayerShipInput Input;
        public TargetSelector Targets;

        public WarpDrive Drive { get; private set; }
        public bool MassLocked { get; private set; }
        public double DistanceToTarget { get; private set; }

        public event System.Action<WarpState> StateChanged;

        ShipController ship;
        WorldAnchor anchor;
        WarpState lastState;
        readonly List<MassSource> sources = new();

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
                    bool hasTarget = Targets && Targets.Current != null;
                    if (Drive.TryStartCharge(MassLocked, hasTarget, out var reason))
                        GameMessages.Post("msg.warp_charging");
                    else
                        GameMessages.Post(reason == WarpFailReason.MassLocked ? "msg.warp_masslocked" : "msg.warp_no_target");
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

            sources.Clear();
            foreach (var t in TargetRegistry.All)
            {
                if (t is CelestialBody b) sources.Add(b.AsMassSource());
                else if (t.Kind == TargetKind.Station) sources.Add(MassSource.Station(t.Position.value));
            }
            MassLocked = MassLock.IsLocked(shipWorld.value, sources);
            double nearest = MassLock.NearestDistance(shipWorld.value, sources);

            var target = Targets ? Targets.Current : null;
            DistanceToTarget = target != null ? WorldPosition.Distance(shipWorld, target.Position) : 0;

            if (Drive.State == WarpState.Charging && target == null) Drive.Cancel();
            if (Drive.State == WarpState.Active && target == null) ExitWarp();
            double speed = Drive.Tick(Time.deltaTime, nearest, target != null ? DistanceToTarget : double.PositiveInfinity);

            if (Drive.State == WarpState.Active && target != null)
            {
                ship.ControlsLocked = true;
                ship.Stop();
                double3 dir = math.normalize(target.Position - shipWorld);
                var look = Quaternion.LookRotation((float3)dir, transform.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 30f * Time.deltaTime);
                double step = math.min(speed * Time.deltaTime, DistanceToTarget - settings.exitDistance * 0.9);
                anchor.MoveWorld(dir * math.max(0, step));
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

        void ExitWarp()
        {
            Drive.Complete();
            ship.ControlsLocked = false;
            GameMessages.Post("msg.warp_exit");
        }
    }
}
