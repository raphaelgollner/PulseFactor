using UnityEngine;

namespace PulseFactor
{
    // Patrulha pirata: voa entre pontos aleatorios do campo; seguidores ficam em volta do lider.
    [RequireComponent(typeof(ShipController), typeof(WorldAnchor))]
    public class PiratePatrol : MonoBehaviour, ITargetable
    {
        [SerializeField] float throttle = 0.6f;
        [SerializeField] float arriveDistance = 300f;

        public AsteroidField Field;
        public PiratePatrol Leader;
        public Vector3 FormationOffset;
        public System.Random Rng = new();

        ShipController ship;
        WorldAnchor anchor;
        Vector3 waypointLocal;
        bool hasWaypoint;

        public WorldPosition Position => anchor.World;
        public string DisplayId => "pirate";
        public TargetKind Kind => TargetKind.Pirate;

        void Awake()
        {
            ship = GetComponent<ShipController>();
            anchor = GetComponent<WorldAnchor>();
        }

        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);

        void Update()
        {
            var origin = FloatingOrigin.Instance;
            if (!origin || !Field) return;

            Vector3 goal;
            if (Leader)
            {
                goal = Leader.transform.TransformPoint(FormationOffset);
            }
            else
            {
                if (!hasWaypoint || Vector3.Distance(transform.position, waypointLocal) < arriveDistance) PickWaypoint(origin);
                waypointLocal = origin.ToLocal(waypointWorld);
                goal = waypointLocal;
            }

            var toGoal = goal - transform.position;
            float dist = toGoal.magnitude;
            var input = ship.Input;
            input.aimDirection = dist > 1f ? toGoal / dist : transform.forward;
            input.throttle = Leader ? Mathf.Clamp01(dist / 200f) : throttle;
            ship.Input = input;
        }

        WorldPosition waypointWorld;

        void PickWaypoint(FloatingOrigin origin)
        {
            var r = Field.Def.radius;
            var offset = new Unity.Mathematics.double3(
                (Rng.NextDouble() * 2 - 1) * r, (Rng.NextDouble() * 2 - 1) * r * 0.3, (Rng.NextDouble() * 2 - 1) * r);
            waypointWorld = Field.Center + offset;
            waypointLocal = origin.ToLocal(waypointWorld);
            hasWaypoint = true;
        }
    }
}
