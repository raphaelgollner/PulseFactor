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

        public Dictionary<string, double3> ResolvePositions() => Orbits.Resolve(bodies);
    }

    public static class Orbits
    {
        // Posicoes absolutas (orbitas estaticas no plano XZ).
        public static Dictionary<string, double3> Resolve(IEnumerable<SolarSystemDef.Body> bodies)
        {
            var result = new Dictionary<string, double3>();
            var byId = new Dictionary<string, SolarSystemDef.Body>();
            foreach (var b in bodies) byId[b.id] = b;
            foreach (var b in byId.Values) Resolve(b, byId, result, 0);
            return result;
        }

        static double3 Resolve(SolarSystemDef.Body b, Dictionary<string, SolarSystemDef.Body> byId, Dictionary<string, double3> result, int depth)
        {
            if (result.TryGetValue(b.id, out var p)) return p;
            if (depth > 16) throw new InvalidOperationException($"Ciclo de orbitas em '{b.id}'");
            double3 parent = string.IsNullOrEmpty(b.parentId) ? double3.zero
                : Resolve(byId[b.parentId], byId, result, depth + 1);
            p = parent + Direction(b.orbitAngleDeg, 0) * b.orbitDistance;
            result[b.id] = p;
            return p;
        }

        // Em double: em float o erro chega a centenas de metros nessas distancias.
        public static double3 Direction(double angleDeg, double elevationDeg)
        {
            double a = math.radians(angleDeg), e = math.radians(elevationDeg);
            return new double3(math.cos(e) * math.cos(a), math.sin(e), math.cos(e) * math.sin(a));
        }
    }
}
