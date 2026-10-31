using UnityEngine;

namespace PulseFactor
{
    [RequireComponent(typeof(WorldAnchor))]
    public class Station : MonoBehaviour, ITargetable, IAffiliated
    {
        public StationDef Def;
        public Dock[] Docks;
        public DockingService Docking { get; private set; }
        public Market Market { get; private set; }

        WorldAnchor anchor;

        public WorldPosition Position => anchor.World;
        public string DisplayId => Def ? Def.id : name;
        public TargetKind Kind => TargetKind.Station;
        public string CorpId => Def ? Def.CorpId : null;

        void Awake() => anchor = GetComponent<WorldAnchor>();

        public void Initialize(StationDef def, GameConfig config, Dock[] docks, Reputation reputation = null)
        {
            Def = def;
            Docks = docks;
            Docking = new DockingService(System.Array.ConvertAll(docks, d => d.size));
            Market = new Market(def, config, reputation);
        }

        // Vaga liberada para quem estava na fila (shipId, indice da doca).
        public event System.Action<DockGrant> DockGranted;

        void Update()
        {
            if (Docking == null) return;
            foreach (var g in Docking.ProcessQueue(Time.time)) DockGranted?.Invoke(g);
        }

        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);

        public float DistanceTo(Vector3 localPosition) => Vector3.Distance(transform.position, localPosition);
    }
}
