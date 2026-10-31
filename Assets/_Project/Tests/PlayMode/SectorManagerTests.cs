using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PulseFactor.Tests
{
    public class SectorManagerTests
    {
        SectorManager manager;
        FloatingOrigin origin;
        Transform player;
        SectorDef aster, vela;

        static SectorDef Sector(string id)
        {
            var s = ScriptableObject.CreateInstance<SectorDef>();
            s.id = id;
            s.bodies.Add(new SolarSystemDef.Body { id = "planet_" + id, radius = 6e6 });
            var st = ScriptableObject.CreateInstance<StationDef>();
            st.id = "station_" + id;
            s.stations.Add(new StationPlacement { def = st, at = new Placement { parentId = "planet_" + id, distance = 2e7 } });
            return s;
        }

        [SetUp]
        public void SetUp()
        {
            aster = Sector("aster");
            vela = Sector("vela");
            aster.gates.Add(new GatePlacement { id = "g_av", target = vela, targetGateId = "g_va", at = new Placement { parentId = "planet_aster", distance = 3e7, angleDeg = 45 } });
            vela.gates.Add(new GatePlacement { id = "g_va", target = aster, targetGateId = "g_av", at = new Placement { parentId = "planet_vela", distance = 3e7, angleDeg = 200 } });

            origin = new GameObject("origin").AddComponent<FloatingOrigin>();
            var p = new GameObject("player");
            p.AddComponent<Rigidbody>().useGravity = false;
            p.AddComponent<WorldAnchor>().dynamic = true;
            player = p.transform;
            origin.Focus = player;

            manager = new GameObject("sectors").AddComponent<SectorManager>();
            manager.Player = player;
            manager.Context = new SectorBuildContext { Config = ScriptableObject.CreateInstance<GameConfig>() };
        }

        [TearDown]
        public void TearDown()
        {
            manager.Unload();
            Object.Destroy(manager.gameObject);
            Object.Destroy(player.gameObject);
            Object.Destroy(origin.gameObject);
        }

        [UnityTest]
        public IEnumerator Loading_builds_station_and_gate()
        {
            manager.Load(aster, null);
            yield return null;
            Assert.AreEqual(aster, manager.Current);
            Assert.IsTrue(Contains("station_aster"));
            Assert.IsTrue(Contains("g_av"));
        }

        [UnityTest]
        public IEnumerator Jump_unloads_previous_sector_and_arrives_in_front_of_target_gate()
        {
            manager.Load(aster, null);
            yield return null;
            manager.Jump(vela, "g_va");
            yield return null;
            Assert.AreEqual(vela, manager.Current);
            Assert.IsFalse(Contains("station_aster"));
            Assert.IsTrue(Contains("station_vela"));

            var gate = manager.FindGate("g_va");
            var playerWorld = origin.ToWorld(player.position);
            double dist = WorldPosition.Distance(playerWorld, gate.Position);
            Assert.AreEqual(SectorManager.ArrivalDistance, dist, 5.0);
            Assert.Less(Vector3.Angle(player.forward, gate.transform.forward), 1f);
        }

        static bool Contains(string id)
        {
            foreach (var t in TargetRegistry.All) if (t.DisplayId == id) return true;
            return false;
        }
    }
}
