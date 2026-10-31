using NUnit.Framework;
using UnityEngine;

namespace PulseFactor.Tests
{
    public class GalaxyRouteTests
    {
        GalaxyDef galaxy;
        SectorDef aster, venera, cinturao, centaurus, isolated;

        static SectorDef Sector(string id)
        {
            var s = ScriptableObject.CreateInstance<SectorDef>();
            s.id = id;
            s.bodies.Add(new SolarSystemDef.Body { id = id, radius = 6e6 });
            return s;
        }

        static void Link(SectorDef a, SectorDef b)
        {
            a.gates.Add(new GatePlacement { id = a.id + "_" + b.id, target = b, targetGateId = b.id + "_" + a.id });
            b.gates.Add(new GatePlacement { id = b.id + "_" + a.id, target = a, targetGateId = a.id + "_" + b.id });
        }

        [SetUp]
        public void SetUp()
        {
            aster = Sector("aster"); venera = Sector("venera"); cinturao = Sector("cinturao");
            centaurus = Sector("centaurus"); isolated = Sector("isolated");
            Link(aster, venera); Link(aster, cinturao); Link(cinturao, centaurus);
            var st = ScriptableObject.CreateInstance<StationDef>();
            st.id = "gollner";
            aster.stations.Add(new StationPlacement { def = st });
            galaxy = ScriptableObject.CreateInstance<GalaxyDef>();
            galaxy.sectors.AddRange(new[] { aster, venera, cinturao, centaurus, isolated });
        }

        [Test] public void Same_sector_is_zero_jumps() => Assert.AreEqual(0, galaxy.JumpsBetween(aster, aster));
        [Test] public void Neighbour_is_one_jump() => Assert.AreEqual(1, galaxy.JumpsBetween(aster, venera));
        [Test] public void Two_hops_counts_two() => Assert.AreEqual(2, galaxy.JumpsBetween(aster, centaurus));
        [Test] public void Disconnected_is_minus_one() => Assert.AreEqual(-1, galaxy.JumpsBetween(aster, isolated));
        [Test] public void Jumps_by_id() => Assert.AreEqual(2, galaxy.JumpsBetween("venera", "cinturao"));

        [Test]
        public void FindStation_returns_sector_and_null_for_unknown()
        {
            var found = galaxy.FindStation("gollner");
            Assert.IsTrue(found.HasValue);
            Assert.AreEqual(aster, found.Value.sector);
            Assert.IsFalse(galaxy.FindStation("nope").HasValue);
        }
    }
}
