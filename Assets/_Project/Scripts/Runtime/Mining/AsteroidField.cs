using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Gera os asteroides de um campo (seed fixa) em volta de um centro no mundo.
    public class AsteroidField : MonoBehaviour, ITargetable
    {
        public AsteroidFieldDef Def { get; private set; }
        public WorldPosition Center { get; private set; }
        public readonly List<Asteroid> Asteroids = new();

        public WorldPosition Position => Center;
        public string DisplayId => Def ? Def.id : name;
        public TargetKind Kind => TargetKind.Body; // alvo de navegacao (dobra), nao de mineracao

        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);

        public void Build(AsteroidFieldDef def, WorldPosition center, Material material)
        {
            Def = def;
            Center = center;
            name = "Field_" + def.id;
            var rng = new System.Random(def.seed);
            for (int i = 0; i < def.count; i++)
            {
                var offset = RandomInSphere(rng) * def.radius;
                float size = Mathf.Lerp(def.sizeRange.x, def.sizeRange.y, (float)System.Math.Pow(rng.NextDouble(), 2));
                var resource = def.PickResource(rng);
                int amount = def.amountRange.x + rng.Next(def.amountRange.y - def.amountRange.x + 1);
                Asteroids.Add(Spawn(i, center + (double3)offset, size, resource, amount, rng, material));
            }
        }

        Asteroid Spawn(int i, WorldPosition pos, float size, ResourceDef resource, int amount, System.Random rng, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = $"Asteroid_{i}_{resource?.id}";
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(
                size * (0.7f + (float)rng.NextDouble() * 0.6f),
                size * (0.6f + (float)rng.NextDouble() * 0.5f),
                size * (0.7f + (float)rng.NextDouble() * 0.6f));
            go.transform.rotation = UnityEngine.Random.rotationUniform;
            if (material) go.GetComponent<Renderer>().sharedMaterial = material;
            go.AddComponent<WorldAnchor>().SetWorld(pos);
            var a = go.AddComponent<Asteroid>();
            a.Initialize(new AsteroidDeposit(resource, amount));
            return a;
        }

        static float3 RandomInSphere(System.Random rng)
        {
            while (true)
            {
                var p = new float3((float)rng.NextDouble() * 2 - 1, ((float)rng.NextDouble() * 2 - 1) * 0.35f, (float)rng.NextDouble() * 2 - 1);
                if (math.lengthsq(p) <= 1f) return p;
            }
        }
    }
}
