using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor.Tests
{
    public class GateTransitTests
    {
        const float Inner = 850f;

        [Test]
        public void Crossing_through_the_hole_counts()
        {
            Assert.IsTrue(GateTransit.Crossed(new Vector3(10, 0, -20), new Vector3(10, 0, 20), Inner));
        }

        [Test]
        public void Crossing_in_reverse_direction_counts()
        {
            Assert.IsTrue(GateTransit.Crossed(new Vector3(0, 300, 30), new Vector3(0, 300, -30), Inner));
        }

        [Test]
        public void Crossing_the_plane_outside_the_ring_does_not_count()
        {
            Assert.IsFalse(GateTransit.Crossed(new Vector3(1200, 0, -20), new Vector3(1200, 0, 20), Inner));
        }

        [Test]
        public void Moving_without_crossing_does_not_count()
        {
            Assert.IsFalse(GateTransit.Crossed(new Vector3(0, 0, -50), new Vector3(0, 0, -10), Inner));
        }

        [Test]
        public void Fast_ship_crossing_in_one_frame_counts()
        {
            Assert.IsTrue(GateTransit.Crossed(new Vector3(0, 0, -3000), new Vector3(0, 0, 3000), Inner));
        }
    }

    public class GalaxyTests
    {
        static SectorDef Sector(string id)
        {
            var s = ScriptableObject.CreateInstance<SectorDef>();
            s.id = id;
            s.bodies.Add(new SolarSystemDef.Body { id = "planet_" + id, radius = 6e6 });
            return s;
        }

        static void Link(SectorDef a, string gateA, SectorDef b, string gateB)
        {
            a.gates.Add(new GatePlacement { id = gateA, target = b, targetGateId = gateB, at = new Placement { parentId = "planet_" + a.id, distance = 3e7 } });
            b.gates.Add(new GatePlacement { id = gateB, target = a, targetGateId = gateA, at = new Placement { parentId = "planet_" + b.id, distance = 3e7 } });
        }

        [Test]
        public void Linked_gates_are_valid()
        {
            var a = Sector("aster"); var b = Sector("vela");
            Link(a, "g_ab", b, "g_ba");
            var galaxy = ScriptableObject.CreateInstance<GalaxyDef>();
            galaxy.sectors.Add(a); galaxy.sectors.Add(b); galaxy.start = a;
            Assert.IsTrue(galaxy.Validate(out var errors), string.Join("; ", errors));
        }

        [Test]
        public void Gate_without_return_link_is_invalid()
        {
            var a = Sector("aster"); var b = Sector("vela");
            a.gates.Add(new GatePlacement { id = "g_ab", target = b, targetGateId = "missing" });
            var galaxy = ScriptableObject.CreateInstance<GalaxyDef>();
            galaxy.sectors.Add(a); galaxy.sectors.Add(b); galaxy.start = a;
            Assert.IsFalse(galaxy.Validate(out var errors));
            Assert.IsNotEmpty(errors);
        }

        [Test]
        public void Placement_resolves_relative_to_parent_body()
        {
            var s = Sector("aster");
            s.bodies.Add(new SolarSystemDef.Body { id = "moon", parentId = "planet_aster", radius = 1.7e6, orbitDistance = 4e8 });
            var p = new Placement { parentId = "moon", distance = 1e6, angleDeg = 90 };
            var pos = s.Resolve(p);
            Assert.AreEqual(4e8, pos.value.x, 1.0);
            Assert.AreEqual(1e6, pos.value.z, 1.0);
        }

        [Test]
        public void Placement_with_unknown_parent_throws()
        {
            var s = Sector("aster");
            Assert.Throws<System.ArgumentException>(() => s.Resolve(new Placement { parentId = "nope", distance = 1 }));
        }

        [Test]
        public void Elevation_lifts_placement_out_of_plane()
        {
            var s = Sector("aster");
            var pos = s.Resolve(new Placement { parentId = "planet_aster", distance = 1000, angleDeg = 0, elevationDeg = 90 });
            Assert.AreEqual(1000.0, pos.value.y, 1e-6);
            Assert.AreEqual(0.0, math.abs(pos.value.x), 1e-6);
        }
    }
}
