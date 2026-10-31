using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor.Tests
{
    public class WorldTests
    {
        [Test]
        public void Round_trip_keeps_millimetre_precision_at_1e10()
        {
            var origin = new WorldPosition(new double3(1.5e10, 0, 0));
            var local = new Vector3(1.25f, -3.5f, 7f);
            var world = WorldPosition.FromLocal(local, origin);
            var back = world.ToLocal(origin);
            Assert.AreEqual(local.x, back.x, 0.001f);
            Assert.AreEqual(local.y, back.y, 0.001f);
            Assert.AreEqual(local.z, back.z, 0.001f);
            Assert.AreEqual(1.5e10 + 1.25, world.value.x, 0.001);
        }

        [Test]
        public void Distance_between_far_points_is_exact()
        {
            var a = new WorldPosition(new double3(1e10, 0, 0));
            var b = new WorldPosition(new double3(1e10 + 2.5, 0, 0));
            Assert.AreEqual(2.5, WorldPosition.Distance(a, b), 1e-6);
        }

        [Test]
        public void Child_position_is_parent_plus_orbit()
        {
            var def = ScriptableObject.CreateInstance<SolarSystemDef>();
            def.bodies.Add(new SolarSystemDef.Body { id = "sun", radius = 7e8, isStar = true });
            def.bodies.Add(new SolarSystemDef.Body { id = "aster", parentId = "sun", radius = 6e6, orbitDistance = 1.5e10, orbitAngleDeg = 90 });
            def.bodies.Add(new SolarSystemDef.Body { id = "moon", parentId = "aster", radius = 1.7e6, orbitDistance = 4e8, orbitAngleDeg = 0 });

            var pos = def.ResolvePositions();
            Assert.AreEqual(0.0, math.length(pos["sun"]), 1e-6);
            Assert.AreEqual(1.5e10, pos["aster"].z, 1.0);
            Assert.AreEqual(0.0, pos["aster"].x, 1.0);
            Assert.AreEqual(4e8, pos["moon"].x, 1.0);
            Assert.AreEqual(1.5e10, pos["moon"].z, 1.0);
        }

        [Test]
        public void Ls_conversion()
        {
            Assert.AreEqual(1.0, WorldPosition.LightSecond / 299792458.0, 1e-12);
        }
    }
}
