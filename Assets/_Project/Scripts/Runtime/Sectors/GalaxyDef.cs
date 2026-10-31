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
    }
}
