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

        [Test]
        public void Reserved_units_are_not_removed_by_normal_remove()
        {
            var iron = Resource("iron", 10);
            var hold = new CargoHold(50);
            hold.Add(iron, 10);
            hold.Reserve(iron, 6);
            Assert.AreEqual(4, hold.Sellable(iron));
            Assert.AreEqual(4, hold.Remove(iron, 10));
            Assert.AreEqual(6, hold.Count(iron));
            Assert.AreEqual(6, hold.Reserved(iron));
        }

        [Test]
        public void RemoveReserved_takes_reserved_units()
        {
            var iron = Resource("iron", 10);
            var hold = new CargoHold(50);
            hold.Add(iron, 10);
            hold.Reserve(iron, 6);
            Assert.AreEqual(6, hold.RemoveReserved(iron, 10));
            Assert.AreEqual(4, hold.Count(iron));
            Assert.AreEqual(0, hold.Reserved(iron));
        }

        [Test]
        public void Cannot_reserve_more_than_unreserved_units()
        {
            var iron = Resource("iron", 10);
            var hold = new CargoHold(50);
            hold.Add(iron, 5);
            hold.Reserve(iron, 3);
            hold.Reserve(iron, 9);
            Assert.AreEqual(5, hold.Reserved(iron));
        }
    }
}