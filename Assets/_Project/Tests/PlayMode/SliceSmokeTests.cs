#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PulseFactor.Tests
{
    // Carrega a cena da fatia gerada por "PulseFactor/Generate Slice Content".
    public class SliceSmokeTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Slice.unity";

        GameBootstrap boot;

        [UnitySetUp]
        public IEnumerator Load()
        {
            if (!System.IO.File.Exists(ScenePath)) Assert.Ignore("Cena Slice ainda nao gerada");
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            boot = Object.FindAnyObjectByType<GameBootstrap>();
        }

        [UnityTest]
        public IEnumerator Scene_has_player_station_and_field()
        {
            yield return null;
            Assert.IsNotNull(boot.Player?.Ship, "nave do jogador");
            var sectors = boot.Player.Sectors;
            Assert.AreEqual("aster", sectors.Current.id);
            Assert.GreaterOrEqual(sectors.Stations.Count, 1);
            Assert.GreaterOrEqual(sectors.Fields.Count, 1);
            Assert.AreEqual(2, sectors.Gates.Count);
        }

        [UnityTest]
        public IEnumerator Player_can_request_dock_from_spawn()
        {
            yield return null;
            boot.Player.Docking.RequestDock();
            Assert.AreEqual(DockingAutopilot.Phase.Authorized, boot.Player.Docking.Current);
        }

        [UnityTest]
        public IEnumerator Pirates_spawn_far_from_station()
        {
            var sectors = boot.Player.Sectors;
            var venera = boot.galaxy.sectors.First(s => s.id == "venera");
            sectors.Jump(venera, "gate_venera_aster");
            yield return null;
            var pirates = Object.FindObjectsByType<PiratePatrol>(FindObjectsSortMode.None);
            Assert.Greater(pirates.Length, 0);
            foreach (var p in pirates)
                foreach (var s in sectors.Stations)
                    Assert.Greater(WorldPosition.Distance(p.Position, s.Position), 50000.0);
        }

        [UnityTest]
        public IEnumerator Every_gate_leads_to_its_pair()
        {
            var sectors = boot.Player.Sectors;
            foreach (var sector in boot.galaxy.sectors)
            {
                foreach (var g in sector.gates)
                {
                    sectors.Jump(g.target, g.targetGateId);
                    yield return null;
                    Assert.AreEqual(g.target, sectors.Current);
                    Assert.IsNotNull(sectors.FindGate(g.targetGateId), g.targetGateId);
                }
            }
        }
    }
}
#endif
