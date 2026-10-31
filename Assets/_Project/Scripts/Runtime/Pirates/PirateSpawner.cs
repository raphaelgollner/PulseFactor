using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Gera grupos de 1-5 piratas no campo de asteroides, sempre longe das estacoes.
    public class PirateSpawner : MonoBehaviour
    {
        [SerializeField] double minDistanceFromStations = 50000;
        [SerializeField] Vector2Int groupSize = new(1, 5);
        [SerializeField] int groups = 2;
        [SerializeField] float respawnSeconds = 120f;

        public AsteroidField Field;
        public List<BlockDef> Catalog = new();
        public int Seed = 7;

        public readonly List<PiratePatrol> Pirates = new();

        System.Random rng;
        PirateShipGenerator generator;
        float respawnTimer;

        public void Spawn()
        {
            rng ??= new System.Random(Seed);
            generator ??= new PirateShipGenerator(Catalog);
            if (!Field || !FarFromStations(Field.Center)) return;
            for (int g = 0; g < groups; g++) SpawnGroup();
        }

        bool FarFromStations(WorldPosition p)
        {
            foreach (var t in TargetRegistry.All)
                if (t.Kind == TargetKind.Station && WorldPosition.Distance(p, t.Position) < minDistanceFromStations)
                    return false;
            return true;
        }

        void SpawnGroup()
        {
            int size = rng.Next(groupSize.x, groupSize.y + 1);
            var r = Field.Def.radius;
            var center = Field.Center + new double3((rng.NextDouble() * 2 - 1) * r, 0, (rng.NextDouble() * 2 - 1) * r);
            PiratePatrol leader = null;
            for (int i = 0; i < size; i++)
            {
                var offset = i == 0 ? Vector3.zero : new Vector3((i % 2 == 0 ? 1 : -1) * 40f * ((i + 1) / 2), 0, -35f * ((i + 1) / 2));
                var p = SpawnShip(center + (double3)(float3)offset, leader, offset);
                leader ??= p;
            }
        }

        PiratePatrol SpawnShip(WorldPosition pos, PiratePatrol leader, Vector3 formation)
        {
            var bp = generator.Generate(rng);
            var visual = ShipAssembler.Build(bp, null);
            visual.name = "Pirate";
            var body = visual.AddComponent<Rigidbody>();
            var ship = visual.AddComponent<ShipController>();
            ship.Initialize(bp.body.classDef);
            var anchor = visual.AddComponent<WorldAnchor>();
            anchor.dynamic = true;
            anchor.SetWorld(pos);
            var box = visual.AddComponent<BoxCollider>();
            var b = visual.GetComponent<AssembledShip>().LocalBounds();
            box.center = b.center; box.size = b.size;

            var patrol = visual.AddComponent<PiratePatrol>();
            patrol.Field = Field;
            patrol.Leader = leader;
            patrol.FormationOffset = formation;
            patrol.Rng = new System.Random(rng.Next());
            Pirates.Add(patrol);
            return patrol;
        }

        void Update()
        {
            Pirates.RemoveAll(p => !p);
            if (Pirates.Count > 0 || generator == null) return;
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnSeconds) { respawnTimer = 0; Spawn(); }
        }
    }
}
