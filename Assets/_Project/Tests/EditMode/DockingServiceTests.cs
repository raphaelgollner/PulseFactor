using NUnit.Framework;

namespace PulseFactor.Tests
{
    public class DockingServiceTests
    {
        [Test]
        public void Grants_free_dock_in_range()
        {
            var s = new DockingService(4);
            var r = s.Request("player", 5000);
            Assert.IsTrue(r.granted);
            Assert.AreEqual(0, r.dockIndex);
            Assert.AreEqual(0, s.AssignedDock("player"));
        }

        [Test]
        public void Denies_out_of_range()
        {
            var r = new DockingService(4).Request("player", 8000);
            Assert.IsFalse(r.granted);
            Assert.AreEqual(DockDenyReason.OutOfRange, r.reason);
        }

        [Test]
        public void Denies_when_all_docks_taken()
        {
            var s = new DockingService(4);
            for (int i = 0; i < 4; i++) Assert.IsTrue(s.Request("ship" + i, 1000).granted);
            var r = s.Request("ship4", 1000);
            Assert.IsFalse(r.granted);
            Assert.AreEqual(DockDenyReason.NoFreeDock, r.reason);
        }

        [Test]
        public void Repeat_request_returns_same_dock()
        {
            var s = new DockingService(4);
            s.Request("a", 1000);
            var r = s.Request("a", 1000);
            Assert.IsTrue(r.granted);
            Assert.AreEqual(0, r.dockIndex);
        }

        [Test]
        public void Leaving_range_cancels_authorization()
        {
            var s = new DockingService(4);
            s.Request("player", 5000);
            s.Tick("player", 8000);
            Assert.IsNull(s.AssignedDock("player"));
            Assert.AreEqual(0, s.Request("other", 1000).dockIndex);
        }

        [Test]
        public void Docked_ship_is_not_cancelled_by_tick()
        {
            var s = new DockingService(4);
            s.Request("player", 5000);
            s.MarkDocked("player");
            s.Tick("player", 9000);
            Assert.AreEqual(0, s.AssignedDock("player"));
            Assert.AreEqual(DockDenyReason.AlreadyDocked, s.Request("player", 100).reason);
        }

        [Test]
        public void Release_frees_dock()
        {
            var s = new DockingService(1);
            s.Request("a", 1000);
            s.Release("a");
            Assert.IsNull(s.AssignedDock("a"));
            Assert.IsTrue(s.Request("b", 1000).granted);
        }
    }
}
