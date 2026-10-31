using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class ContractGeneratorTests
    {
        static StationInfo Station(string id, string sector, string corp, string[] sells, string[] buys)
        {
            var market = new List<MarketEntry>();
            foreach (var r in sells.Union(buys))
                market.Add(new MarketEntry { resource = Resource(r, 10), sells = sells.Contains(r), buys = buys.Contains(r), priceMultiplier = 1 });
            return new StationInfo { id = id, sectorId = sector, corpId = corp, market = market };
        }

        static readonly StationInfo Gollner = Station("gollner", "aster", "nova", new[] { "iron", "ice" }, new[] { "copper", "cobalt" });
        static readonly StationInfo Apollo = Station("apollo", "aster", "nova", new[] { "copper" }, new[] { "iron" });
        static readonly StationInfo Magna = Station("magna", "venera", "magna", new[] { "copper" }, new[] { "ice" });

        static int Jumps(string a, string b) => a == b ? 0 : 1;
        static int Price(string r) => 10;

        static List<Contract> Gen(int seed, StationInfo issuer, params StationInfo[] others) =>
            ContractGenerator.Generate(issuer, others, Jumps, Price, 100, new System.Random(seed));

        [Test]
        public void Generates_three_to_six_offers()
        {
            for (int seed = 0; seed < 100; seed++)
            {
                int n = Gen(seed, Gollner, Apollo, Magna).Count;
                Assert.That(n, Is.InRange(3, 6));
            }
        }

        [Test]
        public void Delivery_uses_what_issuer_sells_and_never_targets_itself()
        {
            foreach (var c in Enumerable.Range(0, 100).SelectMany(s => Gen(s, Gollner, Apollo, Magna)).Where(c => c.type == ContractType.Delivery))
            {
                CollectionAssert.Contains(new[] { "iron", "ice" }, c.resourceId);
                Assert.AreNotEqual("gollner", c.destinationStationId);
                Assert.AreEqual("gollner", c.issuerStationId);
                Assert.AreEqual("nova", c.issuerCorpId);
            }
        }

        [Test]
        public void Requisition_uses_what_issuer_buys_and_lasts_two_hours()
        {
            var reqs = Enumerable.Range(0, 100).SelectMany(s => Gen(s, Gollner, Apollo, Magna)).Where(c => c.type == ContractType.Requisition).ToList();
            Assert.IsNotEmpty(reqs);
            foreach (var c in reqs)
            {
                CollectionAssert.Contains(new[] { "copper", "cobalt" }, c.resourceId);
                Assert.AreEqual("gollner", c.destinationStationId);
                Assert.AreEqual(7200, c.duration, 1e-6);
                Assert.That(c.quantity, Is.InRange(40, 150));
            }
        }

        [Test]
        public void Delivery_duration_depends_on_jumps()
        {
            Assert.AreEqual(1800, ContractGenerator.DeliveryDuration(0), 1e-6);
            Assert.AreEqual(3300, ContractGenerator.DeliveryDuration(1), 1e-6);
            var all = Enumerable.Range(0, 100).SelectMany(s => Gen(s, Gollner, Apollo, Magna)).Where(c => c.type == ContractType.Delivery).ToList();
            Assert.IsTrue(all.Any(c => c.destinationStationId == "apollo" && c.duration == 1800));
            Assert.IsTrue(all.Any(c => c.destinationStationId == "magna" && c.duration == 3300));
        }

        [Test]
        public void Without_other_stations_only_requisitions()
        {
            foreach (var c in Gen(1, Gollner))
                Assert.AreEqual(ContractType.Requisition, c.type);
        }

        [Test]
        public void Same_seed_same_offers()
        {
            var a = Gen(42, Gollner, Apollo, Magna);
            var b = Gen(42, Gollner, Apollo, Magna);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].resourceId, b[i].resourceId);
                Assert.AreEqual(a[i].quantity, b[i].quantity);
                Assert.AreEqual(a[i].destinationStationId, b[i].destinationStationId);
            }
        }
    }

    public class ContractLogTests
    {
        ResourceDef iron, copper;
        CargoHold cargo;
        Wallet wallet;
        Reputation rep;
        ContractLog log;

        ResourceDef Res(string id) => id == "iron" ? iron : id == "copper" ? copper : null;

        [SetUp]
        public void SetUp()
        {
            iron = Resource("iron", 10);
            copper = Resource("copper", 25);
            cargo = new CargoHold(50);
            wallet = new Wallet(1000);
            rep = new Reputation();
            log = new ContractLog();
        }

        static Contract Delivery(int qty = 20, string dest = "apollo") => new Contract
        {
            id = "d" + qty + dest, type = ContractType.Delivery, issuerStationId = "gollner", issuerCorpId = "nova",
            destinationStationId = dest, resourceId = "iron", quantity = qty, reward = 900, fine = 100,
            duration = 1800, repGain = 3, repLoss = 5, state = ContractState.Available,
        };

        static Contract Requisition(int qty = 80) => new Contract
        {
            id = "r" + qty, type = ContractType.Requisition, issuerStationId = "gollner", issuerCorpId = "nova",
            destinationStationId = "gollner", resourceId = "copper", quantity = qty, reward = 3000,
            duration = 7200, repGain = 5, repLoss = 6, state = ContractState.Available,
        };

        [Test]
        public void Accepting_delivery_loads_and_reserves_cargo()
        {
            Assert.AreEqual(AcceptResult.Ok, log.Accept(Delivery(), cargo, Res, 100));
            Assert.AreEqual(20, cargo.Count(iron));
            Assert.AreEqual(20, cargo.Reserved(iron));
            Assert.AreEqual(1900, log.Active[0].deadline, 1e-6);
            Assert.AreEqual(ContractState.Active, log.Active[0].state);
        }

        [Test]
        public void Delivery_that_does_not_fit_is_refused()
        {
            cargo.Add(copper, 40);
            Assert.AreEqual(AcceptResult.NoCargoSpace, log.Accept(Delivery(20), cargo, Res, 0));
            Assert.AreEqual(0, log.Active.Count);
            Assert.AreEqual(0, cargo.Count(iron));
        }

        [Test]
        public void Sixth_contract_is_refused()
        {
            for (int i = 0; i < 5; i++) Assert.AreEqual(AcceptResult.Ok, log.Accept(Requisition(40 + i), cargo, Res, 0));
            Assert.AreEqual(AcceptResult.TooMany, log.Accept(Requisition(99), cargo, Res, 0));
        }

        [Test]
        public void Delivering_at_destination_pays_and_raises_reputation()
        {
            log.Accept(Delivery(), cargo, Res, 0);
            var r = log.Deliver("apollo", cargo, wallet, rep, Res);
            Assert.AreEqual(1, r.completed);
            Assert.AreEqual(1900, wallet.Credits);
            Assert.AreEqual(0, cargo.Count(iron));
            Assert.AreEqual(3, rep.Get("nova"));
            Assert.AreEqual(0, log.Active.Count);
        }

        [Test]
        public void Delivering_at_wrong_station_does_nothing()
        {
            log.Accept(Delivery(), cargo, Res, 0);
            var r = log.Deliver("magna", cargo, wallet, rep, Res);
            Assert.AreEqual(0, r.completed);
            Assert.AreEqual(20, cargo.Count(iron));
            Assert.AreEqual(1, log.Active.Count);
        }

        [Test]
        public void Requisition_accumulates_partial_deliveries()
        {
            log.Accept(Requisition(80), cargo, Res, 0);
            cargo.Add(copper, 50);
            var first = log.Deliver("gollner", cargo, wallet, rep, Res);
            Assert.AreEqual(50, first.partialUnits);
            Assert.AreEqual(0, first.completed);
            Assert.AreEqual(1000, wallet.Credits);
            Assert.AreEqual(50, log.Active[0].delivered);
            cargo.Add(copper, 50);
            var second = log.Deliver("gollner", cargo, wallet, rep, Res);
            Assert.AreEqual(1, second.completed);
            Assert.AreEqual(4000, wallet.Credits);
            Assert.AreEqual(20, cargo.Count(copper));
        }

        [Test]
        public void Requisition_does_not_take_cargo_reserved_by_a_delivery()
        {
            log.Accept(Requisition(80), cargo, Res, 0);
            var d = Delivery(); d.resourceId = "copper";
            log.Accept(d, cargo, Res, 0);
            var r = log.Deliver("gollner", cargo, wallet, rep, Res);
            Assert.AreEqual(0, r.partialUnits);
            Assert.AreEqual(20, cargo.Reserved(copper));
        }

        [Test]
        public void Expired_delivery_fails_once_confiscates_and_fines()
        {
            log.Accept(Delivery(), cargo, Res, 0);
            var failed = log.Tick(1801, cargo, wallet, rep, Res);
            Assert.AreEqual(1, failed.Count);
            Assert.AreEqual(ContractState.Failed, failed[0].state);
            Assert.AreEqual(0, cargo.Count(iron));
            Assert.AreEqual(900, wallet.Credits);
            Assert.AreEqual(-5, rep.Get("nova"));
            Assert.AreEqual(0, log.Tick(5000, cargo, wallet, rep, Res).Count);
            Assert.AreEqual(900, wallet.Credits);
        }

        [Test]
        public void Fine_never_makes_credits_negative()
        {
            var poor = new Wallet(30);
            log.Accept(Delivery(), cargo, Res, 0);
            log.Tick(9999, cargo, poor, rep, Res);
            Assert.AreEqual(0, poor.Credits);
        }

        [Test]
        public void Abandon_counts_as_failure()
        {
            log.Accept(Requisition(), cargo, Res, 0);
            log.Abandon(log.Active[0].id, cargo, wallet, rep, Res);
            Assert.AreEqual(0, log.Active.Count);
            Assert.AreEqual(-6, rep.Get("nova"));
        }

        [Test]
        public void Unknown_resource_is_refused_without_exception()
        {
            var c = Delivery(); c.resourceId = "unobtainium";
            Assert.AreEqual(AcceptResult.UnknownResource, log.Accept(c, cargo, Res, 0));
        }

        [Test]
        public void Restore_keeps_active_contracts()
        {
            log.Accept(Requisition(), cargo, Res, 0);
            var other = new ContractLog();
            other.Restore(log.Active);
            Assert.AreEqual(1, other.Active.Count);
            Assert.AreEqual(log.Active[0].id, other.Active[0].id);
        }
    }
}
