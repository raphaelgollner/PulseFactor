using NUnit.Framework;
using UnityEngine;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class MarketTests
    {
        ResourceDef iron, copper, cobalt;
        StationDef station;

        [SetUp]
        public void SetUp()
        {
            iron = Resource("iron", 10);
            copper = Resource("copper", 25);
            cobalt = Resource("cobalt", 80);
            station = ScriptableObject.CreateInstance<StationDef>();
            station.market.Add(new MarketEntry { resource = iron, sells = true, buys = true, priceMultiplier = 1f });
            station.market.Add(new MarketEntry { resource = copper, sells = false, buys = true, priceMultiplier = 1.2f });
            station.market.Add(new MarketEntry { resource = cobalt, sells = false, buys = true, priceMultiplier = 1.2f });
        }

        static GameConfig Config(bool testMode)
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.testMode = testMode;
            return c;
        }

        [Test]
        public void Buy_is_free_in_test_mode()
        {
            var m = new Market(station, Config(true));
            var w = new Wallet(1000);
            var hold = new CargoHold(50);
            var r = m.Buy(iron, 10, hold, w);
            Assert.IsTrue(r.ok);
            Assert.AreEqual(1000, w.Credits);
            Assert.AreEqual(10, hold.Count(iron));
            Assert.AreEqual(100, r.credits);
        }

        [Test]
        public void Buy_charges_outside_test_mode()
        {
            var m = new Market(station, Config(false));
            var w = new Wallet(1000);
            m.Buy(iron, 10, new CargoHold(50), w);
            Assert.AreEqual(900, w.Credits);
        }

        [Test]
        public void Buy_clamps_to_free_cargo()
        {
            var m = new Market(station, Config(true));
            var hold = new CargoHold(50);
            hold.Add(iron, 45);
            var r = m.Buy(iron, 10, hold, new Wallet(1000));
            Assert.AreEqual(5, r.quantity);
            Assert.AreEqual(50, hold.Used);
        }

        [Test]
        public void Buy_unsold_resource_fails()
        {
            var r = new Market(station, Config(true)).Buy(cobalt, 1, new CargoHold(50), new Wallet(1000));
            Assert.IsFalse(r.ok);
            Assert.AreEqual(TradeFailReason.NotSold, r.reason);
        }

        [Test]
        public void Buy_with_full_cargo_fails()
        {
            var hold = new CargoHold(5);
            hold.Add(iron, 5);
            var r = new Market(station, Config(true)).Buy(iron, 1, hold, new Wallet(1000));
            Assert.IsFalse(r.ok);
            Assert.AreEqual(TradeFailReason.NoCargoSpace, r.reason);
        }

        [Test]
        public void Buy_without_credits_fails_outside_test_mode()
        {
            var hold = new CargoHold(50);
            var w = new Wallet(5);
            var r = new Market(station, Config(false)).Buy(iron, 1, hold, w);
            Assert.IsFalse(r.ok);
            Assert.AreEqual(TradeFailReason.NotEnoughCredits, r.reason);
            Assert.AreEqual(0, hold.Used);
            Assert.AreEqual(5, w.Credits);
        }

        [Test]
        public void Sell_credits_wallet()
        {
            var m = new Market(station, Config(true));
            var hold = new CargoHold(50);
            hold.Add(copper, 10);
            var w = new Wallet(0);
            var r = m.Sell(copper, 10, hold, w);
            Assert.IsTrue(r.ok);
            Assert.AreEqual(300, w.Credits);
            Assert.AreEqual(30, m.PriceOf(copper));
        }

        [Test]
        public void Sell_more_than_held_clamps()
        {
            var hold = new CargoHold(50);
            hold.Add(copper, 3);
            var r = new Market(station, Config(true)).Sell(copper, 10, hold, new Wallet(0));
            Assert.AreEqual(3, r.quantity);
            Assert.AreEqual(0, hold.Used);
        }

        [Test]
        public void Sell_nothing_held_fails()
        {
            var r = new Market(station, Config(true)).Sell(copper, 1, new CargoHold(50), new Wallet(0));
            Assert.IsFalse(r.ok);
            Assert.AreEqual(TradeFailReason.NotInCargo, r.reason);
        }

        [Test]
        public void Zero_or_negative_quantity_fails()
        {
            var r = new Market(station, Config(true)).Buy(iron, 0, new CargoHold(50), new Wallet(10));
            Assert.IsFalse(r.ok);
        }

        [Test]
        public void Sell_ignores_contract_reserved_cargo()
        {
            var hold = new CargoHold(50);
            hold.Add(copper, 10);
            hold.Reserve(copper, 6);
            var r = new Market(station, Config(true)).Sell(copper, 10, hold, new Wallet(0));
            Assert.AreEqual(4, r.quantity);
            Assert.AreEqual(6, hold.Count(copper));
        }

        [Test]
        public void Sell_with_only_reserved_cargo_fails()
        {
            var hold = new CargoHold(50);
            hold.Add(copper, 5);
            hold.Reserve(copper, 5);
            var r = new Market(station, Config(true)).Sell(copper, 5, hold, new Wallet(0));
            Assert.IsFalse(r.ok);
            Assert.AreEqual(TradeFailReason.NotInCargo, r.reason);
        }
    }
}