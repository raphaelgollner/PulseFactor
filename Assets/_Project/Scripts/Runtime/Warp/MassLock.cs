using System.Collections.Generic;
using Unity.Mathematics;

namespace PulseFactor
{
    public struct MassSource
    {
        public const double StationLockRadius = 5000;
        public const double BodyLockFactor = 1.5; // menor que a zona de perigo (2 R do centro): quem sai do alerta pode dobrar

        public double3 position;
        public double radius;
        public double lockRadius;

        public static MassSource Station(double3 p) => new() { position = p, radius = 0, lockRadius = StationLockRadius };
        public static MassSource Body(double3 p, double r) => new() { position = p, radius = r, lockRadius = r * BodyLockFactor };
    }

    public static class MassLock
    {
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
