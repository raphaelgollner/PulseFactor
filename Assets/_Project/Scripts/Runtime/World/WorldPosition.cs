using System;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Posicao no mundo em precisao dupla (metros). O Unity so ve posicoes locais em float,
    // relativas a origem flutuante.
    [Serializable]
    public struct WorldPosition
    {
        public const double LightSecond = 299792458.0;

        public double3 value;

        public WorldPosition(double3 value) { this.value = value; }

        public Vector3 ToLocal(WorldPosition origin) => (float3)(value - origin.value);

        public static WorldPosition FromLocal(Vector3 local, WorldPosition origin) =>
            new(origin.value + (double3)(float3)local);

        public static double Distance(WorldPosition a, WorldPosition b) => math.distance(a.value, b.value);

        public static WorldPosition operator +(WorldPosition p, double3 d) => new(p.value + d);
        public static double3 operator -(WorldPosition a, WorldPosition b) => a.value - b.value;

        public override string ToString() => $"({value.x:0.0}, {value.y:0.0}, {value.z:0.0})";
    }
}
