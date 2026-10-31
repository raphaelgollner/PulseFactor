using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Gera o trafego NPC do setor carregado: patrulhas (estilo dos piratas) e comercio P/M com escoltas.
    // Grupos nascem saindo de uma estacao ou chegando por um portal; afiliacao aleatoria por grupo.
    public class TrafficSpawner : MonoBehaviour
    {
        [SerializeField] int maxGroups = 8;
        [SerializeField] Vector2 spawnInterval = new(45f, 90f);
        [SerializeField] int patrolGroups = 2;

        public SectorManager Sectors;
        public List<BlockDef> Catalog = new();
        public List<ShipModelDef> SmallTraders = new();
        public ShipModelDef MediumTrader;
        public List<CorporationDef> Corporations = new();
        public int Seed = 11;

        public const string Neutral = "neutral";
        // Afiliacoes sorteadas para grupos de comercio (pirata entra no futuro).
        static readonly string[] CorpIds = { "nova", "magna", "atlas", "federation", Neutral };

        readonly List<NpcPilot> leaders = new();
        readonly HashSet<string> usedNames = new();
        System.Random rng;
        PirateShipGenerator patrolBodies;
        float timer;
        int counter;

        public void Begin()
        {
            rng = new System.Random(Seed);
            patrolBodies = new PirateShipGenerator(Catalog); // patrulhas usam corpo de Assault
            for (int i = 0; i < patrolGroups; i++) SpawnPatrolGroup();
            // Cada estacao comeca com um grupo chegando para pousar.
            foreach (var st in Sectors.Stations) SpawnTraderGroup(st);
            timer = Next();
        }

        float Next() => Mathf.Lerp(spawnInterval.x, spawnInterval.y, (float)rng.NextDouble());

        void Update()
        {
            if (rng == null) return;
            leaders.RemoveAll(l => !l);
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Next();
            if (leaders.Count < maxGroups) SpawnTraderGroup(null);
        }

        void SpawnPatrolGroup()
        {
            var corp = Sectors.Current && !string.IsNullOrEmpty(Sectors.Current.dominantCorporationId)
                ? Sectors.Current.dominantCorporationId : CorpIds[rng.Next(CorpIds.Length)];
            int size = TrafficPlanner.PatrolGroupSize(rng);
            var start = SpawnPoint();
            NpcPilot leader = null;
            for (int i = 0; i < size; i++)
            {
                var bp = patrolBodies.Generate(rng);
                bp.paint = CorpPaint(corp);
                var p = SpawnShip(bp, start, i, leader == null ? NpcPilot.Role.Patrol : NpcPilot.Role.Escort, "npc_patrol", corp, leader);
                p.WarpLevel = 3;
                if (leader == null) { leader = p; leaders.Add(p); }
            }
        }

        // arriving: grupo chegando a esta estacao; null = nasce num portal ou perto de uma estacao qualquer.
        void SpawnTraderGroup(Station arriving)
        {
            bool medium = MediumTrader && rng.NextDouble() < 0.35;
            var g = medium ? TrafficPlanner.MediumTraderGroup(rng, CorpIds) : TrafficPlanner.SmallTraderGroup(rng, CorpIds);
            var start = arriving ? NearStation(arriving, 6000) : SpawnPoint();
            string first = arriving ? arriving.DisplayId : Sectors.Stations.Count > 0 ? Sectors.Stations[rng.Next(Sectors.Stations.Count)].DisplayId : null;
            NpcPilot leader = null;
            for (int i = 0; i < g.traders; i++)
            {
                var model = medium ? MediumTrader : SmallTraders[rng.Next(SmallTraders.Count)];
                var bp = model.ToBlueprint();
                bp.paint = CorpPaint(g.corpId);
                var p = SpawnShip(bp, start, i, NpcPilot.Role.Trader, "npc_trader", g.corpId, null);
                p.FirstStationId = first;
                p.WarpLevel = medium ? 2 : 2 + rng.Next(2);
                leader ??= p;
                if (i == 0) leaders.Add(p);
            }
            for (int e = 0; e < g.escorts; e++)
            {
                var bp = patrolBodies.Generate(rng);
                bp.paint = CorpPaint(g.corpId);
                SpawnShip(bp, start, g.traders + e, NpcPilot.Role.Escort, "npc_patrol", g.corpId, leader);
            }
        }

        // Saindo de uma estacao (perto, ja voando) ou chegando por um portal.
        WorldPosition SpawnPoint()
        {
            bool fromGate = Sectors.Gates.Count > 0 && (Sectors.Stations.Count == 0 || rng.NextDouble() < 0.5);
            if (fromGate)
            {
                var gate = Sectors.Gates[rng.Next(Sectors.Gates.Count)];
                return gate.Position + (double3)(float3)gate.transform.forward * SectorManager.ArrivalDistance;
            }
            return NearStation(Sectors.Stations[rng.Next(Sectors.Stations.Count)], 2500);
        }

        WorldPosition NearStation(Station st, double distance)
        {
            var dir = math.normalize(new double3(rng.NextDouble() - 0.5, rng.NextDouble() * 0.3, rng.NextDouble() - 0.5));
            return st.Position + dir * distance;
        }

        NpcPilot SpawnShip(ShipBlueprint bp, WorldPosition start, int index, NpcPilot.Role role, string displayRole, string corp, NpcPilot leader)
        {
            var go = ShipAssembler.Build(bp, transform);
            go.name = $"NPC_{displayRole}_{counter}";
            go.AddComponent<Rigidbody>();
            var ship = go.AddComponent<ShipController>();
            ship.Initialize(bp.body.classDef);
            var anchor = go.AddComponent<WorldAnchor>();
            anchor.dynamic = true;
            var offset = new Vector3((index % 2 == 0 ? 1 : -1) * 45f * ((index + 1) / 2), 0, -40f * ((index + 1) / 2));
            anchor.SetWorld(start + (double3)(float3)offset);
            var box = go.AddComponent<BoxCollider>();
            var b = go.GetComponent<AssembledShip>().LocalBounds();
            box.center = b.center;
            box.size = b.size;

            var npc = go.AddComponent<NpcShip>();
            npc.Id = "npc" + counter++;
            npc.Role = displayRole;
            npc.Corp = corp;
            var kind = displayRole == "npc_patrol" ? NpcNames.Kind.Patrol
                : ship.ClassDef && ship.ClassDef.dockSize != DockSize.P ? NpcNames.Kind.HeavyTrader : NpcNames.Kind.SmallTrader;
            npc.Name = NpcNames.Pick(kind, rng, usedNames);
            var pilot = go.AddComponent<NpcPilot>();
            pilot.role = role;
            pilot.Leader = leader;
            pilot.FormationOffset = offset;
            pilot.Sectors = Sectors;
            pilot.Rng = new System.Random(rng.Next());
            return pilot;
        }

        // Afiliadas: cor da corporacao. Neutras: cores aleatorias por bloco, para dar diversidade.
        PaintScheme CorpPaint(string corpId)
        {
            if (corpId == Neutral)
                return new PaintScheme { cockpit = RandomPaint(), body = RandomPaint(), tail = RandomPaint(), wing = RandomPaint() };
            var corp = Corporations.Find(c => c && c.id == corpId);
            var color = corp ? corp.color : Color.white;
            var paint = new BlockPaint { primary = color * 0.85f, secondary = new Color(0.22f, 0.24f, 0.28f), accent = Color.white };
            return new PaintScheme { cockpit = paint, body = paint, tail = paint, wing = paint };
        }

        BlockPaint RandomPaint() => new()
        {
            primary = Color.HSVToRGB((float)rng.NextDouble(), 0.15f + 0.5f * (float)rng.NextDouble(), 0.45f + 0.5f * (float)rng.NextDouble()),
            secondary = Color.HSVToRGB((float)rng.NextDouble(), 0.1f + 0.3f * (float)rng.NextDouble(), 0.2f + 0.4f * (float)rng.NextDouble()),
            accent = Color.HSVToRGB((float)rng.NextDouble(), 0.7f, 0.95f),
        };
    }
}
