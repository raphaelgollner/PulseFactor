using UnityEngine;

namespace PulseFactor
{
    // Rota marcada no mapa: destino (setor + ponto) e o proximo ponto no setor atual (destino ou portal).
    public class RouteService : MonoBehaviour
    {
        public GalaxyDef Galaxy;
        public SectorManager Sectors;

        public string DestinationSectorId { get; private set; }
        public string DestinationId { get; private set; }
        public bool HasRoute => !string.IsNullOrEmpty(DestinationId);

        // Ponto a seguir no setor atual (null se sem rota ou ainda nao carregado).
        public ITargetable Waypoint
        {
            get
            {
                if (!HasRoute || !Sectors || !Sectors.Current) return null;
                var id = RoutePlanner.NextWaypoint(Galaxy, Sectors.Current.id, DestinationSectorId, DestinationId);
                if (id == null) return null;
                if (cached != null && cachedId == id && !(cached is Object o && !o)) return cached;
                cachedId = id;
                cached = null;
                foreach (var t in TargetRegistry.All)
                    if (t.DisplayId == id) { cached = t; break; }
                return cached;
            }
        }

        public bool WaypointIsGate => HasRoute && Sectors && Sectors.Current && Sectors.Current.id != DestinationSectorId;

        public event System.Action Changed;

        ITargetable cached;
        string cachedId;

        public void SetDestination(string sectorId, string id)
        {
            DestinationSectorId = sectorId;
            DestinationId = id;
            cached = null;
            Changed?.Invoke();
            GameMessages.Post("msg.route_set", Loc.T("target." + id));
        }

        public void Clear()
        {
            if (!HasRoute) return;
            DestinationSectorId = DestinationId = null;
            cached = null;
            Changed?.Invoke();
        }

        // Chegou: docou na estacao de destino ou parou perto do destino.
        public void ArrivedAt(string id)
        {
            if (HasRoute && id == DestinationId && Sectors.Current && Sectors.Current.id == DestinationSectorId)
            {
                Clear();
                GameMessages.Post("msg.route_arrived", Loc.T("target." + id));
            }
        }
    }
}
