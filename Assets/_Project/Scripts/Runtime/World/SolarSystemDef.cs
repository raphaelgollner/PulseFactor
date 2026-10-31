using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Solar System")]
    public class SolarSystemDef : ScriptableObject
    {
        [Serializable]
        public class Body
        {
            public string id;
            public string parentId;
            public double radius;
            public double orbitDistance;
            public float orbitAngleDeg;
            public Color color = Color.gray;
            public bool isStar;
        }

        public List<Body> bodies = new();

        // Posicoes absolutas (orbitas estaticas no plano XZ).
        public Dictionary<string, double3> ResolvePositions()
        {
            var result = new Dictionary<string, double3>();
            var byId = new Dictionary<string, Body>();
            foreach (var b in bodies) byId[b.id] = b;
            foreach (var b in bodies) Resolve(b, byId, result, 0);
            return result;
        }

        static double3 Resolve(Body b, Dictionary<string, Body> byId, Dictionary<string, double3> result, int depth)
        {
            if (result.TryGetValue(b.id, out var p)) return p;
            if (depth > 16) throw new InvalidOperationException($"Ciclo de orbitas em '{b.id}'");
            double3 parent = string.IsNullOrEmpty(b.parentId) ? double3.zero
                : Resolve(byId[b.parentId], byId, result, depth + 1);
            double a = math.radians((double)b.orbitAngleDeg); // em double: em float o erro chega a centenas de metros
            p = parent + new double3(math.cos(a), 0, math.sin(a)) * b.orbitDistance;
            result[b.id] = p;
            return p;
        }
    }
}
