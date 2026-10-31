using NUnit.Framework;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class MiningTests
    {
        static int Run(MiningProgress m, AsteroidDeposit d, CargoHold c, float seconds, float dt = 0.1f)
        {
            int total = 0;
            int steps = (int)System.Math.Round(seconds / dt);
            for (int i = 0; i < steps; i++) total += m.Tick(dt, d, c);
            return total;
        }

        [Test]
        public void Mines_two_units_per_second()
        {
            var iron = Resource("iron", 10);
            var d = new AsteroidDeposit(iron, 100);
            var c = new CargoHold(50);
            Assert.AreEqual(6, Run(new MiningProgress(), d, c, 3f));
            Assert.AreEqual(6, c.Count(iron));
            Assert.AreEqual(94, d.Remaining);
        }

        [Test]
        public void Fractional_progress_accumulates()
        {
            var d = new AsteroidDeposit(Resource("ice", 8), 100);
            Assert.AreEqual(2, Run(new MiningProgress(), d, new CargoHold(50), 1f));
        }

        [Test]
        public void Stops_when_cargo_full()
        {
            var iron = Resource("iron", 10);
            var c = new CargoHold(10);
            c.Add(iron, 9);
            var d = new AsteroidDeposit(iron, 100);
            Assert.AreEqual(1, Run(new MiningProgress(), d, c, 3f));
            Assert.AreEqual(99, d.Remaining);
        }

        [Test]
        public void Stops_when_depleted()
        {
            var d = new AsteroidDeposit(Resource("iron", 10), 3);
            Assert.AreEqual(3, Run(new MiningProgress(), d, new CargoHold(50), 5f));
            Assert.IsTrue(d.Depleted);
        }

        [Test]
        public void Extract_never_goes_negative()
        {
            var d = new AsteroidDeposit(Resource("iron", 10), 2);
            Assert.AreEqual(2, d.Extract(5));
            Assert.AreEqual(0, d.Extract(5));
            Assert.AreEqual(0, d.Remaining);
        }
    }
}
