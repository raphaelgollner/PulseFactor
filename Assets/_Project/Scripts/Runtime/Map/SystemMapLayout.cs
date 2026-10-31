using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    public enum MapNodeKind { MainBody, Moon, Station, Field, Gate }

    public struct MapNode
    {
        public string id;
        public MapNodeKind kind;
        public Vector2 position; // no circulo unitario do setor (corpo principal no centro)
        public float orbitRadius;
    }

    // Layout esquematico do mapa: setores em quadrantes em volta da estrela e,
    // dentro do setor, distancias em escala logaritmica (tudo cabe na tela, a ordem e mantida).
    public static class SystemMapLayout
    {
        const float MinRadius = 0.12f;

        public static float SectorAngle(int index, int count) => 45f + index * (360f / Mathf.Max(count, 1));

        public static List<MapNode> BuildSector(SectorDef sector)
        {
            var nodes = new List<MapNode>();
            if (sector.bodies.Count == 0) return nodes;
            var bodies = sector.BodyPositions();
            var main = sector.bodies[0];
            var center = bodies[main.id];

            var raw = new List<(string id, MapNodeKind kind, double3 pos)>();
            foreach (var b in sector.bodies)
                raw.Add((b.id, b == main ? MapNodeKind.MainBody : MapNodeKind.Moon, bodies[b.id]));
            foreach (var s in sector.stations)
                if (s.def) raw.Add((s.def.id, MapNodeKind.Station, Resolve(s.at, bodies)));
            foreach (var f in sector.fields)
                if (f.def) raw.Add((f.def.id, MapNodeKind.Field, Resolve(f.at, bodies)));
            foreach (var g in sector.gates)
                raw.Add((g.id, MapNodeKind.Gate, Resolve(g.at, bodies)));

            double max = 1;
            foreach (var r in raw) max = math.max(max, math.length(r.pos - center));
            double scale = math.max(main.radius * 2, 1);

            foreach (var r in raw)
            {
                var d = r.pos - center;
                double dist = math.length(d);
                float radius = r.kind == MapNodeKind.MainBody ? 0f
                    : MinRadius + (1f - MinRadius) * (float)(math.log10(1 + dist / scale) / math.log10(1 + max / scale));
                float angle = (float)math.atan2(d.z, d.x);
                nodes.Add(new MapNode
                {
                    id = r.id,
                    kind = r.kind,
                    orbitRadius = radius,
                    position = r.kind == MapNodeKind.MainBody ? Vector2.zero : new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                });
            }
            return nodes;
        }

        // Posicao de qualquer ponto do mundo do setor no mesmo layout (ex.: a nave do jogador).
        public static Vector2 Project(SectorDef sector, WorldPosition world)
        {
            var bodies = sector.BodyPositions();
            var main = sector.bodies[0];
            var center = bodies[main.id];
            double max = 1;
            foreach (var p in bodies.Values) max = math.max(max, math.length(p - center));
            foreach (var g in sector.gates) max = math.max(max, math.length(Resolve(g.at, bodies) - center));
            foreach (var f in sector.fields) max = math.max(max, math.length(Resolve(f.at, bodies) - center));
            foreach (var s in sector.stations) max = math.max(max, math.length(Resolve(s.at, bodies) - center));
            double scale = math.max(main.radius * 2, 1);
            var d = world.value - center;
            double dist = math.length(d);
            float radius = Mathf.Min(1f, MinRadius + (1f - MinRadius) * (float)(math.log10(1 + dist / scale) / math.log10(1 + max / scale)));
            float angle = (float)math.atan2(d.z, d.x);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        static double3 Resolve(Placement p, Dictionary<string, double3> bodies)
        {
            try { return SectorDef.Resolve(p, bodies).value; }
            catch (System.ArgumentException) { return double3.zero; }
        }
    }
}
