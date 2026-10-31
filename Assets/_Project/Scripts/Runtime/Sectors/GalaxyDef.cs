using System.Collections.Generic;
using UnityEngine;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Galaxy")]
    public class GalaxyDef : ScriptableObject
    {
        public List<SectorDef> sectors = new();
        public SectorDef start;

        // Todo gate precisa apontar para um gate existente que aponte de volta.
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            if (!start) errors.Add("Sem setor inicial");
            foreach (var s in sectors)
            {
                foreach (var g in s.gates)
                {
                    if (!g.target) { errors.Add($"{s.id}/{g.id}: sem setor de destino"); continue; }
                    var back = g.target.Gate(g.targetGateId);
                    if (back == null) errors.Add($"{s.id}/{g.id}: gate '{g.targetGateId}' nao existe em {g.target.id}");
                    else if (back.target != s || back.targetGateId != g.id) errors.Add($"{s.id}/{g.id}: o gate de destino nao aponta de volta");
                }
            }
            return errors.Count == 0;
        }

        // Numero de saltos de gate entre dois setores (-1 se nao ha caminho).
        public int JumpsBetween(SectorDef from, SectorDef to)
        {
            if (!from || !to) return -1;
            if (from == to) return 0;
            var dist = new Dictionary<SectorDef, int> { [from] = 0 };
            var queue = new Queue<SectorDef>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                var s = queue.Dequeue();
                foreach (var g in s.gates)
                {
                    if (!g.target || dist.ContainsKey(g.target)) continue;
                    dist[g.target] = dist[s] + 1;
                    if (g.target == to) return dist[g.target];
                    queue.Enqueue(g.target);
                }
            }
            return -1;
        }

        public int JumpsBetween(string fromId, string toId) =>
            JumpsBetween(sectors.Find(s => s.id == fromId), sectors.Find(s => s.id == toId));

        public (SectorDef sector, StationPlacement placement)? FindStation(string stationId)
        {
            foreach (var s in sectors)
                foreach (var p in s.stations)
                    if (p.def && p.def.id == stationId) return (s, p);
            return null;
        }
    }
}