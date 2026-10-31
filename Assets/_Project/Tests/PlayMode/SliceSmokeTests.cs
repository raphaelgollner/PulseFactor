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
            GameBootstrap.SkipSave = true; // nao le nem grava o save do jogador
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
            Assert.AreEqual(4, sectors.Stations.Count, "Gollner + Apollo + Kepler + Halden");
        }

        [UnityTest]
        public IEnumerator Every_sector_has_two_new_outposts_with_three_pads()
        {
            var styles = new[] { StationStyle.IndustrialOutpost, StationStyle.ScienceOutpost, StationStyle.MilitaryOutpost };
            foreach (var sector in boot.galaxy.sectors)
                Assert.GreaterOrEqual(sector.stations.Count(p => styles.Contains(p.def.style)), 2, sector.id);
            var halden = boot.Player.Sectors.Stations.First(s => s.DisplayId == "halden_foundry");
            Assert.AreEqual(3, halden.Docks.Length);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Venera_has_Magna_station()
        {
            var sectors = boot.Player.Sectors;
            var venera = boot.galaxy.sectors.First(s => s.id == "venera");
            sectors.Jump(venera, "gate_venera_aster");
            yield return null;
            Assert.IsTrue(sectors.Stations.Any(s => s.DisplayId == "magna"));
            Assert.AreEqual(7, sectors.Stations.First(s => s.DisplayId == "magna").Docks.Length);
        }

        [UnityTest]
        public IEnumerator Apollo_has_one_pad_of_each_size()
        {
            yield return null;
            var apollo = boot.Player.Sectors.Stations.First(s => s.DisplayId == "apollo");
            CollectionAssert.AreEqual(new[] { DockSize.P, DockSize.M, DockSize.G }, apollo.Docks.Select(d => d.size).ToArray());
        }

        [UnityTest]
        public IEnumerator Station_offers_contracts()
        {
            yield return null;
            var offers = boot.Player.Contracts.Offers("gollner");
            Assert.That(offers.Count, Is.InRange(3, 6));
        }

        [UnityTest]
        public IEnumerator Player_can_request_dock_from_spawn()
        {
            yield return null;
            boot.Player.Docking.RequestDock();
            Assert.AreEqual(DockingAutopilot.Phase.Authorized, boot.Player.Docking.Current);
        }

        // Fase 3: docar na Gollner, comprar uma Hulker (M) e trocar de nave no mesmo GameObject.
        [UnityTest]
        public IEnumerator Buying_a_Hulker_switches_ship_and_pad()
        {
            yield return null;
            var player = boot.Player;
            player.Docking.RequestDock();
            Assert.AreEqual(DockingAutopilot.Phase.Authorized, player.Docking.Current);
            var anchor = player.Ship.GetComponent<WorldAnchor>();
            anchor.SetWorld(FloatingOrigin.Instance.ToWorld(player.Docking.Dock.ApproachPoint.position));
            float timeout = 15f;
            while (player.Docking.Current != DockingAutopilot.Phase.Docked && (timeout -= Time.deltaTime) > 0) yield return null;
            Assert.AreEqual(DockingAutopilot.Phase.Docked, player.Docking.Current, "docou");

            var station = player.Docking.Station;
            var hulker = new ShipyardCatalog(station.Def).Models.First(m => m.id == "hulker");
            player.Wallet.Add(500000);
            long before = player.Wallet.Credits;
            var shipObject = player.Ship.gameObject;
            Assert.AreEqual(ShipyardError.None, player.Fleet.Buy(hulker.ToBlueprint(), "TESTE", station));
            timeout = 70f; // sem doca M livre: espera um NPC sair (max. 45 s)
            while (player.Fleet.Preparing && (timeout -= Time.deltaTime) > 0) yield return null;
            yield return null;

            Assert.AreSame(shipObject, player.Ship.gameObject, "mesmo GameObject");
            Assert.AreEqual(hulker.body, player.Visual.Blueprint.body);
            Assert.AreEqual(600, player.Ship.Cargo.Capacity);
            Assert.AreEqual(DockSize.M, player.Docking.Dock.size);
            Assert.AreEqual(DockingAutopilot.Phase.Docked, player.Docking.Current);
            Assert.AreEqual(2, player.Fleet.Fleet.Count);
            Assert.AreEqual(station.DisplayId, player.Fleet.Fleet.Stored[0].stationId);
            Assert.AreEqual(before - ShipPricing.ShipPrice(hulker.ToBlueprint()), player.Wallet.Credits);
            Assert.AreEqual("TESTE", player.ShipName);
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
