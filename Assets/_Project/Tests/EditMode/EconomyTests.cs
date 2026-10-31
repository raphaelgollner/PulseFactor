using NUnit.Framework;
using UnityEngine;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class EconomyTests
    {
        ResourceDef iron, parts;
        StationDef station;
        Economy economy;
        GameConfig config;

        [SetUp]
        public void SetUp()
        {
            iron = Resource("iron", 100);
            parts = Resource("ship_parts", 100);
            station = ScriptableObject.CreateInstance<StationDef>();
            station.id = "gollner";
            station.market.Add(new MarketEntry { resource = iron, sells = true, priceMultiplier = 1f, capacity = 200, startStock = 100, ratePerHour = 60 });
            station.market.Add(new MarketEntry { resource = parts, buys = true, priceMultiplier = 1f, capacity = 100, startStock = 50, ratePerHour = 30 });
            economy = new Economy();
            economy.Register(station);
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.testMode = false;
        }

        Market M() => new Market(station, config, null, economy);

        [Test]
        public void Stock_starts_at_configured_value()
        {
            Assert.AreEqual(100, economy.Stock("gollner", iron));
            Assert.AreEqual(50, economy.Stock("gollner", parts));
        }

        [Test]
        public void Buy_price_rises_as_stock_falls()
        {
            economy.SetStock("gollner", iron, 200);
            Assert.AreEqual(85, M().BuyPriceOf(iron));
            economy.SetStock("gollner", iron, 0);
            Assert.AreEqual(135, M().BuyPriceOf(iron));
        }

        [Test]
        public void Sell_price_follows_demand()
        {
            economy.SetStock("gollner", parts, 0);
            Assert.AreEqual(140, M().PriceOf(parts));
            Assert.AreEqual(DemandLevel.High, economy.Demand("gollner", parts));
            economy.SetStock("gollner", parts, 100);
            Assert.AreEqual(60, M().PriceOf(parts));
            Assert.AreEqual(DemandLevel.Low, economy.Demand("gollner", parts));
            economy.SetStock("gollner", parts, 50);
            Assert.AreEqual(DemandLevel.Medium, economy.Demand("gollner", parts));
        }

        [Test]
        public void Cannot_buy_more_than_stock()
        {
            economy.SetStock("gollner", iron, 5);
            var r = M().Buy(iron, 20, new CargoHold(50), new Wallet(100000));
            Assert.AreEqual(5, r.quantity);
            Assert.AreEqual(0, economy.Stock("gollner", iron));
            var again = M().Buy(iron, 1, new CargoHold(50), new Wallet(100000));
            Assert.AreEqual(TradeFailReason.OutOfStock, again.reason);
        }

        [Test]
        public void Selling_fills_stock_up_to_capacity()
        {
            economy.SetStock("gollner", parts, 95);
            var hold = new CargoHold(50);
            hold.Add(parts, 20);
            var r = M().Sell(parts, 20, hold, new Wallet(0));
            Assert.AreEqual(5, r.quantity);
            Assert.AreEqual(15, hold.Count(parts));
            Assert.AreEqual(100, economy.Stock("gollner", parts));
            Assert.AreEqual(TradeFailReason.StationFull, M().Sell(parts, 1, hold, new Wallet(0)).reason);
        }

        [Test]
        public void Production_refills_and_consumption_drains_over_game_time()
        {
            economy.SetStock("gollner", iron, 0);
            economy.SetStock("gollner", parts, 100);
            economy.Tick(1800f); // meia hora
            Assert.AreEqual(30, economy.Stock("gollner", iron));
            Assert.AreEqual(85, economy.Stock("gollner", parts));
            economy.Tick(100000f);
            Assert.AreEqual(200, economy.Stock("gollner", iron));
            Assert.AreEqual(0, economy.Stock("gollner", parts));
        }

        [Test]
        public void Snapshot_restores_stocks()
        {
            economy.SetStock("gollner", iron, 7);
            var other = new Economy();
            other.Register(station);
            other.Restore(economy.Snapshot());
            Assert.AreEqual(7, other.Stock("gollner", iron));
        }

        [Test]
        public void Market_without_economy_keeps_old_behaviour()
        {
            var m = new Market(station, config);
            Assert.AreEqual(100, m.BuyPriceOf(iron));
        }
    }
}
