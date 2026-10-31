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
            var r = new DockingService(4).Request("player", 8100);
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
            s.Tick("player", 8100);
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

        // Estacao padrao: 4 P, 2 M, 1 G (docas 1-4, 5-6, 7).
        static DockingService Standard() => new DockingService(new[]
        {
            DockSize.P, DockSize.P, DockSize.P, DockSize.P, DockSize.M, DockSize.M, DockSize.G,
        });

        [Test]
        public void Default_request_range_is_6km()
        {
            var s = Standard();
            Assert.AreEqual(6000f, s.RequestRange);
            Assert.IsTrue(s.Request("player", 5900).granted);
            Assert.AreEqual(DockDenyReason.OutOfRange, s.Request("other", 6100).reason);
        }

        [Test]
        public void Small_ship_gets_small_dock_first()
        {
            var s = Standard();
            var r = s.Request("player", 1000, DockSize.P);
            Assert.AreEqual(DockSize.P, s.SizeOf(r.dockIndex));
        }

        [Test]
        public void Small_ship_falls_back_to_larger_dock_when_small_full()
        {
            var s = Standard();
            for (int i = 0; i < 4; i++) s.Request("p" + i, 1000, DockSize.P);
            var r = s.Request("player", 1000, DockSize.P);
            Assert.IsTrue(r.granted);
            Assert.AreEqual(DockSize.M, s.SizeOf(r.dockIndex));
        }

        [Test]
        public void Medium_ship_never_gets_small_dock()
        {
            var s = Standard();
            var a = s.Request("m1", 1000, DockSize.M);
            var b = s.Request("m2", 1000, DockSize.M);
            var c = s.Request("m3", 1000, DockSize.M);
            Assert.AreEqual(DockSize.M, s.SizeOf(a.dockIndex));
            Assert.AreEqual(DockSize.M, s.SizeOf(b.dockIndex));
            Assert.AreEqual(DockSize.G, s.SizeOf(c.dockIndex));
            Assert.AreEqual(DockDenyReason.NoFreeDock, s.Request("m4", 1000, DockSize.M).reason);
        }
    }
}