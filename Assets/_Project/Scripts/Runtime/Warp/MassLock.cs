using System.Collections.Generic;
using Unity.Mathematics;

namespace PulseFactor
{
    public struct MassSource
    {
        public const double StationLockRadius = 2000;
        public const double GateLockRadius = 2000;
        public const double BodyLockFactor = 1.1; // dentro da zona de perigo (1,2 R do centro): quem sai do alerta pode dobrar

        public double3 position;
        public double radius;
        public double lockRadius;

        public static MassSource Station(double3 p) => new() { position = p, radius = 0, lockRadius = StationLockRadius };
        public static MassSource Gate(double3 p) => new() { position = p, radius = 0, lockRadius = GateLockRadius };
        public static MassSource Body(double3 p, double r) => new() { position = p, radius = r, lockRadius = r * BodyLockFactor };
    }

    public static class MassLock
    {
        // Indice da fonte que bloqueia (para dizer no feed quem tirou da dobra); -1 se nenhuma.
        public static int LockingIndex(double3 ship, IReadOnlyList<MassSource> sources)
        {
            for (int i = 0; i < sources.Count; i++)
                if (math.distance(ship, sources[i].position) < sources[i].lockRadius) return i;
            return -1;
        }

        public static bool IsLocked(double3 ship, IEnumerable<MassSource> sources)
        {
            foreach (var s in sources)
                if (math.distance(ship, s.position) < s.lockRadius) return true;
            return false;
        }

        // Distancia ate a superficie da massa mais proxima.
        public static double NearestDistance(double3 ship, IEnumerable<MassSource> sources)
        {
            double best = double.PositiveInfinity;
            foreach (var s in sources)
                best = math.min(best, math.max(0, math.distance(ship, s.position) - s.radius));
            return best;
        }
    }
}
