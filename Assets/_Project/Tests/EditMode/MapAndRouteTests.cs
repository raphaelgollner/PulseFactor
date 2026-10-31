using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PulseFactor.Tests
{
    public class RoutePlannerTests
    {
        GalaxyDef galaxy;
        SectorDef aster, venera, cinturao, centaurus;

        static SectorDef Sector(string id)
        {
            var s = ScriptableObject.CreateInstance<SectorDef>();
            s.id = id;
            s.bodies.Add(new SolarSystemDef.Body { id = id, radius = 6e6 });
            return s;
        }

        static void Link(SectorDef a, SectorDef b)
        {
            a.gates.Add(new GatePlacement { id = $"gate_{a.id}_{b.id}", target = b, targetGateId = $"gate_{b.id}_{a.id}" });
            b.gates.Add(new GatePlacement { id = $"gate_{b.id}_{a.id}", target = a, targetGateId = $"gate_{a.id}_{b.id}" });
        }

        [SetUp]
        public void SetUp()
        {
            aster = Sector("aster"); venera = Sector("venera"); cinturao = Sector("cinturao"); centaurus = Sector("centaurus");
            Link(aster, venera); Link(aster, cinturao); Link(cinturao, centaurus);
            galaxy = ScriptableObject.CreateInstance<GalaxyDef>();
            galaxy.sectors.AddRange(new[] { aster, venera, cinturao, centaurus });
        }

        [Test]
        public void Same_sector_waypoint_is_the_destination()
        {
            Assert.AreEqual("apollo", RoutePlanner.NextWaypoint(galaxy, "aster", "aster", "apollo"));
        }

        [Test]
        public void Other_sector_waypoint_is_the_gate_toward_it()
        {
            Assert.AreEqual("gate_aster_venera", RoutePlanner.NextWaypoint(galaxy, "aster", "venera", "magna"));
        }

        [Test]
        public void Two_hops_goes_through_the_first_gate_of_the_path()
        {
            Assert.AreEqual("gate_aster_cinturao", RoutePlanner.NextWaypoint(galaxy, "aster", "centaurus", "centaurus_ii_yard"));
            Assert.AreEqual("gate_cinturao_centaurus", RoutePlanner.NextWaypoint(galaxy, "cinturao", "centaurus", "centaurus_ii_yard"));
        }

        [Test]
        public void Unreachable_or_unknown_sector_returns_null()
        {
            Assert.IsNull(RoutePlanner.NextWaypoint(galaxy, "aster", "nowhere", "x"));
        }
    }

    public class SystemMapLayoutTests
    {
        static SectorDef SampleSector()
        {
            var s = ScriptableObject.CreateInstance<SectorDef>();
            s.id = "aster";
            s.bodies.Add(new SolarSystemDef.Body { id = "aster", radius = 6e6 });
            s.bodies.Add(new SolarSystemDef.Body { id = "aster_i", parentId = "aster", radius = 1.7e6, orbitDistance = 4e8, orbitAngleDeg = 30 });
            var st = ScriptableObject.CreateInstance<StationDef>(); st.id = "gollner";
            s.stations.Add(new StationPlacement { def = st, at = new Placement { parentId = "aster", distance = 2e7 } });
            var f = ScriptableObject.CreateInstance<AsteroidFieldDef>(); f.id = "aster_field";
            s.fields.Add(new FieldPlacement { def = f, at = new Placement { parentId = "aster", distance = 2.15e7, angleDeg = 3 } });
            s.gates.Add(new GatePlacement { id = "gate_aster_venera", at = new Placement { parentId = "aster", distance = 4e7, angleDeg = 120 } });
            return s;
        }

        [Test]
        public void Sectors_get_distinct_quadrants()
        {
            var angles = Enumerable.Range(0, 4).Select(i => SystemMapLayout.SectorAngle(i, 4)).ToArray();
            Assert.AreEqual(4, angles.Distinct().Count());
            Assert.AreEqual(90f, Mathf.DeltaAngle(angles[0], angles[1]), 1e-3f);
        }

        [Test]
        public void Sector_nodes_include_every_point_of_interest()
        {
            var nodes = SystemMapLayout.BuildSector(SampleSector());
            CollectionAssert.AreEquivalent(new[] { "aster", "aster_i", "gollner", "aster_field", "gate_aster_venera" }, nodes.Select(n => n.id));
            Assert.AreEqual(MapNodeKind.Station, nodes.First(n => n.id == "gollner").kind);
            Assert.AreEqual(MapNodeKind.Gate, nodes.First(n => n.id == "gate_aster_venera").kind);
        }

        [Test]
        public void Main_body_is_centered_and_everything_fits_the_view()
        {
            var nodes = SystemMapLayout.BuildSector(SampleSector());
            Assert.AreEqual(Vector2.zero, nodes.First(n => n.id == "aster").position);
            foreach (var n in nodes) Assert.LessOrEqual(n.position.magnitude, 1.0001f, n.id);
        }

        [Test]
        public void Farther_objects_are_drawn_farther_out()
        {
            var nodes = SystemMapLayout.BuildSector(SampleSector());
            float station = nodes.First(n => n.id == "gollner").position.magnitude;
            float gate = nodes.First(n => n.id == "gate_aster_venera").position.magnitude;
            float moon = nodes.First(n => n.id == "aster_i").position.magnitude;
            Assert.Less(station, gate);
            Assert.Less(gate, moon);
            Assert.Greater(station, 0.05f, "nao cola no planeta");
        }
    }
}
