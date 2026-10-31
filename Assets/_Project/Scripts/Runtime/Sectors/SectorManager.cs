using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // O que o SectorManager precisa para construir um setor (materiais podem ser nulos em testes).
    public class SectorBuildContext
    {
        public GameConfig Config;
        public List<BlockDef> Catalog = new();
        public Material PlanetMaterial, SunMaterial, HullMaterial, LightMaterial, AsteroidMaterial, GateMaterial, PortalMaterial;
        public Camera Camera;
    }

    // Carrega um setor por vez: corpos celestes, sol, estacoes, campos (com piratas) e jump gates.
    public class SectorManager : MonoBehaviour
    {
        public const double ArrivalDistance = 1500;

        public Transform Player;
        public SectorBuildContext Context = new();

        public SectorDef Current { get; private set; }
        public readonly List<Station> Stations = new();
        public readonly List<AsteroidField> Fields = new();
        public readonly List<JumpGate> Gates = new();

        public event Action<SectorDef> Loaded;

        GameObject root;

        public void Load(SectorDef sector, string arrivalGateId)
        {
            Unload();
            Current = sector;
            root = new GameObject("Sector_" + sector.id);
            var bodies = sector.BodyPositions();

            BuildSun(sector);
            foreach (var b in sector.bodies) BuildBody(b, bodies[b.id]);
            foreach (var s in sector.stations)
            {
                var station = StationBuilder.Build(s.def, Context.Config, SectorDef.Resolve(s.at, bodies), Context.HullMaterial, Context.LightMaterial);
                station.transform.SetParent(root.transform, true);
                Stations.Add(station);
            }
            foreach (var f in sector.fields) BuildField(f, SectorDef.Resolve(f.at, bodies));

            var planetPos = bodies.Count > 0 ? bodies[sector.bodies[0].id] : double3.zero;
            foreach (var g in sector.gates) BuildGate(g, SectorDef.Resolve(g.at, bodies), planetPos);

            if (!string.IsNullOrEmpty(arrivalGateId)) PlaceAtGate(arrivalGateId);
            Loaded?.Invoke(sector);
        }

        public void Jump(SectorDef target, string arrivalGateId)
        {
            float speed = 0f;
            var body = Player ? Player.GetComponent<Rigidbody>() : null;
            if (body && !body.isKinematic) speed = body.linearVelocity.magnitude;

            Load(target, arrivalGateId);

            if (body && !body.isKinematic) body.linearVelocity = Player.forward * speed;
            GameMessages.Post("msg.gate_jump", target.id);
        }

        public void Unload()
        {
            Stations.Clear();
            Fields.Clear();
            Gates.Clear();
            if (root)
            {
                root.SetActive(false); // tira do TargetRegistry ja
                Destroy(root);
            }
            root = null;
            Current = null;
        }

        public JumpGate FindGate(string id) => Gates.Find(g => g.DisplayId == id);

        void PlaceAtGate(string gateId)
        {
            var gate = FindGate(gateId);
            if (!gate || !Player) return;
            var arrival = gate.Position + (double3)(float3)gate.transform.forward * ArrivalDistance;
            var anchor = Player.GetComponent<WorldAnchor>();
            Player.rotation = gate.transform.rotation;
            if (anchor) anchor.SetWorld(arrival);
            if (FloatingOrigin.Instance) FloatingOrigin.Instance.Rebase(arrival);
        }

        void BuildSun(SectorDef sector)
        {
            var go = new GameObject("Sun");
            go.transform.SetParent(root.transform, false);
            var dir = math.normalize((double3)(float3)sector.sunDirection);
            var body = go.AddComponent<CelestialBody>();
            body.Configure("sun", sector.sunRadius, new WorldPosition(dir * sector.sunDistance), true);
            var proxy = go.AddComponent<ScaledProxy>();
            proxy.Camera = Context.Camera;
            if (Context.SunMaterial) proxy.SetMaterial(Context.SunMaterial);
            TargetRegistry.Unregister(body); // o sol nao e alvo de dobra
            go.AddComponent<SunLight>();
        }

        void BuildBody(SolarSystemDef.Body b, double3 pos)
        {
            var go = new GameObject(b.id);
            go.transform.SetParent(root.transform, false);
            var body = go.AddComponent<CelestialBody>();
            body.Configure(b.id, b.radius, new WorldPosition(pos), b.isStar);
            var proxy = go.AddComponent<ScaledProxy>();
            proxy.Camera = Context.Camera;
            if (Context.PlanetMaterial)
            {
                var m = new Material(Context.PlanetMaterial) { color = b.color };
                m.SetColor("_BaseColor", b.color);
                proxy.SetMaterial(m);
            }
        }

        void BuildField(FieldPlacement f, WorldPosition center)
        {
            var go = new GameObject("Field");
            go.transform.SetParent(root.transform, false);
            var field = go.AddComponent<AsteroidField>();
            field.Build(f.def, center, Context.AsteroidMaterial);
            Fields.Add(field);
            if (!f.pirates || Context.Catalog.Count == 0) return;
            var spawner = go.AddComponent<PirateSpawner>();
            spawner.Field = field;
            spawner.Catalog = Context.Catalog;
            spawner.Seed = f.def.seed + 17;
            spawner.Spawn();
        }

        void BuildGate(GatePlacement g, WorldPosition pos, double3 planetPos)
        {
            var go = new GameObject("Gate");
            go.transform.SetParent(root.transform, false);
            var gate = go.AddComponent<JumpGate>();
            // O anel "olha" para o planeta: quem chega sai de frente para o interior do setor.
            var toPlanet = planetPos - pos.value;
            if (math.lengthsq(toPlanet) > 1) go.transform.rotation = Quaternion.LookRotation((float3)math.normalize(toPlanet), Vector3.up);
            go.GetComponent<WorldAnchor>().SetWorld(pos);
            gate.Initialize(g, this, Context.GateMaterial, Context.PortalMaterial);
            Gates.Add(gate);
        }
    }
}
