using UnityEngine;

namespace PulseFactor
{
    [RequireComponent(typeof(WorldAnchor))]
    public class Station : MonoBehaviour, ITargetable
    {
        public StationDef Def;
        public Dock[] Docks;
        public DockingService Docking { get; private set; }
        public Market Market { get; private set; }

        WorldAnchor anchor;

        public WorldPosition Position => anchor.World;
        public string DisplayId => Def ? Def.id : name;
        public TargetKind Kind => TargetKind.Station;

        void Awake() => anchor = GetComponent<WorldAnchor>();

        public void Initialize(StationDef def, GameConfig config, Dock[] docks, Reputation reputation = null)
        {
            Def = def;
            Docks = docks;
            Docking = new DockingService(System.Array.ConvertAll(docks, d => d.size));
            Market = new Market(def, config, reputation);
        }

        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);

        public float DistanceTo(Vector3 localPosition) => Vector3.Distance(transform.position, localPosition);
    }
}
