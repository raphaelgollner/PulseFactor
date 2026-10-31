using NUnit.Framework;
using UnityEngine;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class ReputationTests
    {
        [Test]
        public void Starts_at_zero_and_clamps()
        {
            var r = new Reputation();
            Assert.AreEqual(0, r.Get("nova"));
            r.Change("nova", 150);
            Assert.AreEqual(100, r.Get("nova"));
            r.Change("nova", -500);
            Assert.AreEqual(-100, r.Get("nova"));
        }

        [TestCase(0, 0f)]
        [TestCase(9, 0f)]
        [TestCase(10, 0.02f)]
        [TestCase(25, 0.05f)]
        [TestCase(50, 0.08f)]
        [TestCase(80, 0.12f)]
        [TestCase(-40, 0f)]
        public void Discount_levels(int value, float discount)
        {
            var r = new Reputation();
            r.Change("nova", value);
            Assert.AreEqual(discount, r.DiscountFor("nova"), 1e-5f);
        }

        [Test]
        public void Changed_event_reports_new_value()
        {
            var r = new Reputation();
            string corp = null; int value = 0;
            r.Changed += (c, v) => { corp = c; value = v; };
            r.Change("magna", 3);
            Assert.AreEqual("magna", corp);
            Assert.AreEqual(3, value);
        }

        [Test]
        public void Market_buy_price_applies_corporation_discount()
        {
            var iron = Resource("iron", 100);
            var station = ScriptableObject.CreateInstance<StationDef>();
            station.corporationId = "nova";
            station.market.Add(new MarketEntry { resource = iron, sells = true, buys = true, priceMultiplier = 1f });
            var config = ScriptableObject.CreateInstance<GameConfig>();
            config.testMode = false;
            var rep = new Reputation();
            rep.Change("nova", 25);
            var market = new Market(station, config, rep);
            Assert.AreEqual(95, market.BuyPriceOf(iron));
            Assert.AreEqual(100, market.PriceOf(iron));
            var w = new Wallet(1000);
            market.Buy(iron, 2, new CargoHold(50), w);
            Assert.AreEqual(810, w.Credits);
        }

        [Test]
        public void Game_clock_accumulates_and_ignores_negative()
        {
            var c = new GameClock();
            c.Tick(1.5f);
            c.Tick(-3f);
            c.Tick(0.5f);
            Assert.AreEqual(2.0, c.Seconds, 1e-6);
        }
    }
}
