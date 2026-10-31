using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class ShipyardTests
    {
        static BlockDef Sized(BlockDef b, DockSize size) { b.size = size; return b; }

        static ShipModelDef Model(string id, ShipClass c, bool multi, DockSize size = DockSize.P)
        {
            var m = ScriptableObject.CreateInstance<ShipModelDef>();
            m.id = id;
            var cls = c == ShipClass.Cargo ? Cargo() : c == ShipClass.Scout ? Scout() : Assault();
            m.body = Sized(Block(BlockType.Body, c, multi, cls, id + "_body"), size);
            m.cockpit = Sized(Block(BlockType.Cockpit, c, multi, null, id + "_cockpit"), size);
            m.tail = Sized(Block(BlockType.Tail, c, multi, null, id + "_tail"), size);
            m.wing = size == DockSize.P ? Block(BlockType.Wing, c, multi, null, id + "_wing") : null;
            return m;
        }

        [Test]
        public void M_parts_only_mount_on_M_bodies()
        {
            var hulker = Model("hulker", ShipClass.Cargo, false, DockSize.M);
            var typec = Model("typec", ShipClass.Cargo, true);
            Assert.IsFalse(BlockRules.CanMount(hulker.cockpit, typec.body));
            Assert.IsFalse(BlockRules.CanMount(typec.cockpit, hulker.body));
            Assert.IsTrue(BlockRules.CanMount(hulker.cockpit, hulker.body));
            var bad = hulker.ToBlueprint();
            bad.cockpit = typec.cockpit;
            Assert.IsFalse(BlockRules.IsValid(bad, out _));
        }

        [Test]
        public void Prices_M_cost_more_and_paint_is_cheapest()
        {
            var scout = Model("cruise", ShipClass.Scout, true);
            var hulker = Model("hulker", ShipClass.Cargo, false, DockSize.M);
            int p = ShipPricing.ShipPrice(scout.ToBlueprint());
            int m = ShipPricing.ShipPrice(hulker.ToBlueprint());
            Assert.Greater(m, p * 3);
            Assert.Less(ShipPricing.PaintPrice(DockSize.P), ShipPricing.PartPrice(scout.wing));
            Assert.Less(ShipPricing.PaintPrice(DockSize.P), ShipPricing.PaintPrice(DockSize.M));
        }

        [Test]
        public void Exclusive_parts_cost_20_percent_more()
        {
            Assert.AreEqual(3000, ShipPricing.DefaultPartPrice(BlockType.Cockpit, ShipClass.Scout, true, DockSize.P));
            Assert.AreEqual(3600, ShipPricing.DefaultPartPrice(BlockType.Cockpit, ShipClass.Scout, false, DockSize.P));
        }

        [Test]
        public void Modify_charges_only_changed_parts_and_paint()
        {
            var a = Model("cruise", ShipClass.Scout, true);
            var b = Model("crusader", ShipClass.Scout, true);
            var current = a.ToBlueprint();
            var next = a.ToBlueprint();
            Assert.AreEqual(0, ShipPricing.ModifyPrice(current, next, false));
            next.wing = b.wing;
            Assert.AreEqual(ShipPricing.PartPrice(b.wing), ShipPricing.ModifyPrice(current, next, false));
            Assert.AreEqual(ShipPricing.PartPrice(b.wing) + 500, ShipPricing.ModifyPrice(current, next, true));
        }

        [Test]
        public void Catalog_offers_parts_of_its_models_within_dock_sizes()
        {
            var station = ScriptableObject.CreateInstance<StationDef>();
            station.dockSizes = new[] { DockSize.P, DockSize.P };
            var cruise = Model("cruise", ShipClass.Scout, true);
            var typea = Model("typea", ShipClass.Assault, false);
            var hulker = Model("hulker", ShipClass.Cargo, false, DockSize.M);
            station.shipyardModels.AddRange(new[] { cruise, typea, hulker });
            var catalog = new ShipyardCatalog(station);

            Assert.AreEqual(2, catalog.Models.Count, "sem doca M, nao vende a Hulker");
            Assert.IsFalse(catalog.Sells(hulker.body));
            // No corpo Scout: cabine multiclasse do Cruise sim; a exclusiva Assault do Type A nao.
            var cockpits = catalog.PartsFor(BlockType.Cockpit, cruise.body).ToList();
            CollectionAssert.Contains(cockpits, cruise.cockpit);
            CollectionAssert.DoesNotContain(cockpits, typea.cockpit);
            // No corpo Assault: as duas.
            Assert.AreEqual(2, catalog.PartsFor(BlockType.Cockpit, typea.body).Count());
        }

        [Test]
        public void Buying_stores_the_current_ship_at_the_station()
        {
            var fleet = new Fleet();
            fleet.SetActive(new ShipRecord { id = fleet.NewId(), name = "PULSE-01" });
            Assert.AreEqual(FleetError.None, fleet.Add(new ShipRecord { id = fleet.NewId(), name = "NOVA" }, "gollner"));
            Assert.AreEqual("NOVA", fleet.Active.name);
            Assert.AreEqual(1, fleet.StoredAt("gollner").Count());
            Assert.AreEqual(2, fleet.Count);
        }

        [Test]
        public void Swap_only_with_ships_stored_here()
        {
            var fleet = new Fleet();
            fleet.SetActive(new ShipRecord { id = fleet.NewId(), name = "A" });
            fleet.Add(new ShipRecord { id = fleet.NewId(), name = "B" }, "gollner"); // A fica na Gollner
            var a = fleet.Stored[0];
            Assert.AreEqual(FleetError.NotHere, fleet.Swap(a.id, "magna"));
            Assert.AreEqual(FleetError.None, fleet.Swap(a.id, "gollner"));
            Assert.AreEqual("A", fleet.Active.name);
            Assert.AreEqual("B", fleet.StoredAt("gollner").Single().name);
            Assert.IsTrue(string.IsNullOrEmpty(fleet.Active.stationId));
        }

        [Test]
        public void Fleet_is_limited_to_15_ships()
        {
            var fleet = new Fleet();
            fleet.SetActive(new ShipRecord { id = fleet.NewId() });
            for (int i = 1; i < Fleet.MaxShips; i++)
                Assert.AreEqual(FleetError.None, fleet.Add(new ShipRecord { id = fleet.NewId() }, "gollner"));
            Assert.AreEqual(Fleet.MaxShips, fleet.Count);
            Assert.AreEqual(FleetError.FleetFull, fleet.Add(new ShipRecord { id = fleet.NewId() }, "gollner"));
        }

        [Test]
        public void Restored_fleet_keeps_ids_unique()
        {
            var fleet = new Fleet();
            fleet.Restore(new ShipRecord { id = "ship_7" }, new[] { new ShipRecord { id = "ship_3", stationId = "gollner" } });
            Assert.AreEqual("ship_8", fleet.NewId());
        }

        [Test]
        public void Record_round_trips_the_blueprint()
        {
            var m = Model("cruise", ShipClass.Scout, true);
            var bp = m.ToBlueprint();
            bp.paint = new PaintScheme();
            var rec = ShipRecord.From(bp, "ship_1", "X");
            var blocks = new[] { m.cockpit, m.body, m.tail, m.wing }.ToDictionary(b => b.id);
            var back = rec.ToBlueprint(id => id != null && blocks.TryGetValue(id, out var b) ? b : null);
            Assert.AreSame(m.body, back.body);
            Assert.AreSame(m.wing, back.wing);
            Assert.IsNotNull(back.paint);
            Assert.AreEqual(DockSize.P, rec.Size(id => blocks[id]));
        }
    }
}
