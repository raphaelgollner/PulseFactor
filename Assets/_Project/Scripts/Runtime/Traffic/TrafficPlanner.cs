using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace PulseFactor
{
    // Regras puras do trafego NPC: destinos, composicao de grupos, tempo docado e fuga de piratas.
    public static class TrafficPlanner
    {
        public struct Destination
        {
            public string id;
            public bool isGate;
        }

        public struct Group
        {
            public int traders;
            public int escorts;
            public string corpId; // todas as naves do grupo tem a mesma afiliacao (nunca pirata)
        }

        public const double PatrolFleeRadius = 4000;

        // Proximo destino aleatorio: outra estacao do setor ou (comercio) um portal para outro setor.
        public static Destination NextDestination(System.Random rng, string currentStationId, IReadOnlyList<string> stations,
            IReadOnlyList<string> gates, bool allowOtherSectors)
        {
            var options = new List<Destination>();
            foreach (var s in stations) if (s != currentStationId) options.Add(new Destination { id = s });
            if (allowOtherSectors) foreach (var g in gates) options.Add(new Destination { id = g, isGate = true });
            if (options.Count == 0)
                return stations.Count > 0 ? new Destination { id = stations[0] } : new Destination { id = currentStationId };
            return options[rng.Next(options.Count)];
        }

        // Comercio P: 1 a 3; com 3, uma patrulha acompanha.
        public static Group SmallTraderGroup(System.Random rng, IReadOnlyList<string> corps)
        {
            int n = rng.Next(1, 4);
            return new Group { traders = n, escorts = n == 3 ? 1 : 0, corpId = corps[rng.Next(corps.Count)] };
        }

        // Comercio M (Hulker): chance de 1 ou 2 patrulhas de escolta.
        public static Group MediumTraderGroup(System.Random rng, IReadOnlyList<string> corps) =>
            new() { traders = 1, escorts = rng.Next(0, 3), corpId = corps[rng.Next(corps.Count)] };

        // Patrulhas nascem como os piratas: grupos de 1 a 5.
        public static int PatrolGroupSize(System.Random rng) => rng.Next(1, 6);

        // Tempo docado de NPC: entre metade e o maximo do tamanho (P 20 s, M 45 s, G 60 s).
        public static float NpcStay(System.Random rng, DockSize size)
        {
            float max = DockingService.MaxNpcStay(size);
            return max * (0.5f + 0.5f * (float)rng.NextDouble());
        }

        // Pirata com patrulha por perto foge na direcao oposta (sem combate por enquanto).
        public static bool ShouldFlee(double3 pirate, IReadOnlyList<double3> patrols, double radius, out double3 away)
        {
            away = double3.zero;
            double best = radius;
            bool found = false;
            foreach (var p in patrols)
            {
                double d = math.distance(pirate, p);
                if (d >= best) continue;
                best = d;
                away = d > 1e-3 ? math.normalize(pirate - p) : new double3(1, 0, 0);
                found = true;
            }
            return found;
        }
    }
}
