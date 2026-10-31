using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor.Tests
{
    public class ProximityTests
    {
        [Test]
        public void Danger_within_one_radius_of_surface()
        {
            Assert.IsTrue(ProximityRules.InDanger(5.9e6, 6e6, 1.0));
            Assert.IsFalse(ProximityRules.InDanger(6.1e6, 6e6, 1.0));
        }

        [Test]
        public void Nearest_body_picks_smallest_surface_distance()
        {
            var bodies = new[]
            {
                (center: new double3(0, 0, 0), radius: 6e6),
                (center: new double3(4e8, 0, 0), radius: 1.7e6),
            };
            int i = ProximityRules.Nearest(new double3(3.9e8, 0, 0), bodies, out double surface);
            Assert.AreEqual(1, i);
            Assert.AreEqual(1e7 - 1.7e6, surface, 1.0);
        }

        [Test]
        public void Escape_direction_points_away_from_body()
        {
            var dir = ProximityRules.EscapeDirection(new double3(0, 7e6, 0), double3.zero);
            Assert.AreEqual(1f, Vector3.Dot(dir, Vector3.up), 1e-4f);
        }

        [Test]
        public void No_bodies_means_no_danger()
        {
            int i = ProximityRules.Nearest(double3.zero, new (double3, double)[0], out double surface);
            Assert.AreEqual(-1, i);
            Assert.AreEqual(double.PositiveInfinity, surface);
        }
    }
}
