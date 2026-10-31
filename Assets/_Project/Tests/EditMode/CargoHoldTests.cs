using NUnit.Framework;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class CargoHoldTests
    {
        [Test]
        public void Add_beyond_capacity_returns_partial()
        {
            var hold = new CargoHold(50);
            Assert.AreEqual(50, hold.Add(Resource("iron", 10), 60));
            Assert.AreEqual(0, hold.Free);
        }

        [Test]
        public void Remove_more_than_held_returns_held()
        {
            var iron = Resource("iron", 10);
            var hold = new CargoHold(50);
            hold.Add(iron, 5);
            Assert.AreEqual(5, hold.Remove(iron, 10));
            Assert.AreEqual(0, hold.Count(iron));
        }

        [Test]
        public void LoadFraction_is_used_over_capacity()
        {
            var hold = new CargoHold(500);
            hold.Add(Resource("ice", 8), 250);
            Assert.AreEqual(0.5f, hold.LoadFraction, 1e-5f);
        }

        [Test]
        public void Negative_amounts_change_nothing()
        {
            var iron = Resource("iron", 10);
            var hold = new CargoHold(50);
            hold.Add(iron, 5);
            Assert.AreEqual(0, hold.Add(iron, -3));
            Assert.AreEqual(0, hold.Remove(iron, -3));
            Assert.AreEqual(5, hold.Count(iron));
        }

        [Test]
        public void Changed_fires_on_add()
        {
            var hold = new CargoHold(50);
            int calls = 0;
            hold.Changed += () => calls++;
            hold.Add(Resource("iron", 10), 1);
            Assert.AreEqual(1, calls);
        }
    }
}
