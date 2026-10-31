using System;
using System.Collections.Generic;
using UnityEngine;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Asteroid Field")]
    public class AsteroidFieldDef : ScriptableObject
    {
        [Serializable]
        public class Composition
        {
            public ResourceDef resource;
            public float weight = 1f;
        }

        public string id;
        public int count = 150;
        public float radius = 3000f;
        public Vector2 sizeRange = new(10f, 120f);
        public Vector2Int amountRange = new(100, 400);
        public int seed = 1234;
        public List<Composition> composition = new();
        [Range(0, 1)] public float resourceChance = 0.25f; // o resto e rocha sem recurso

        // null = asteroide sem recursos mineraveis.
        public ResourceDef PickResource(System.Random rng)
        {
            if (rng.NextDouble() >= resourceChance) return null;
            float total = 0;
            foreach (var c in composition) total += c.weight;
            double roll = rng.NextDouble() * total;
            foreach (var c in composition)
            {
                roll -= c.weight;
                if (roll <= 0) return c.resource;
            }
            return composition.Count > 0 ? composition[^1].resource : null;
        }
    }
}
