using NUnit.Framework;

namespace PulseFactor.Tests
{
    public class DockingServiceTests
    {
        [Test]
        public void Grants_free_dock_in_range()
        {
            var s = new DockingService(4);
            var r = s.Request("player", 1500);
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
        public void Queues_when_all_docks_taken()
        {
            var s = new DockingService(4);
            for (int i = 0; i < 4; i++) Assert.IsTrue(s.Request("ship" + i, 1000).granted);
            var r = s.Request("ship4", 1000);
            Assert.IsFalse(r.granted);
            Assert.AreEqual(DockDenyReason.Queued, r.reason);
        }

        [Test]
        public void No_dock_of_that_size_at_all_is_denied()
        {
            var s = new DockingService(new[] { DockSize.P, DockSize.M });
            Assert.AreEqual(DockDenyReason.NoDockOfSize, s.Request("big", 1000, DockSize.G).reason);
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
            s.Request("player", 1500);
            s.Tick("player", 8100);
            Assert.IsNull(s.AssignedDock("player"));
            Assert.AreEqual(0, s.Request("other", 1000).dockIndex);
        }

        [Test]
        public void Docked_ship_is_not_cancelled_by_tick()
        {
            var s = new DockingService(4);
            s.Request("player", 1500);
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
        public void Default_request_range_is_2km()
        {
            var s = Standard();
            Assert.AreEqual(2000f, s.RequestRange);
            Assert.IsTrue(s.Request("player", 1900).granted);
            Assert.AreEqual(DockDenyReason.OutOfRange, s.Request("other", 2100).reason);
        }

        [Test]
        public void Small_ship_gets_small_dock_first()
        {
            var s = Standard();
            var r = s.Request("player", 1000, DockSize.P);
            Assert.AreEqual(DockSize.P, s.SizeOf(r.dockIndex));
        }

        // Fase 2D: cada nave so pousa na plataforma do seu tamanho; sem vaga, entra na fila.
        [Test]
        public void Ship_only_gets_dock_of_its_own_size()
        {
            var s = Standard();
            for (int i = 0; i < 4; i++) Assert.AreEqual(DockSize.P, s.SizeOf(s.Request("p" + i, 1000, DockSize.P).dockIndex));
            var r = s.Request("player", 1000, DockSize.P);
            Assert.IsFalse(r.granted);
            Assert.AreEqual(DockDenyReason.Queued, r.reason);
            Assert.AreEqual(1, r.queuePosition);
            var m = s.Request("m1", 1000, DockSize.M);
            Assert.AreEqual(DockSize.M, s.SizeOf(m.dockIndex));
        }

        [Test]
        public void Queue_estimate_uses_soonest_npc_departure()
        {
            var s = Standard();
            for (int i = 0; i < 4; i++) s.Request("npc" + i, 1000, DockSize.P, now: 0, stayUntil: 10 + i * 5);
            var r = s.Request("player", 1000, DockSize.P, now: 3);
            Assert.AreEqual(DockDenyReason.Queued, r.reason);
            Assert.AreEqual(7.0, r.etaSeconds, 1e-6);
        }

        [Test]
        public void Freed_dock_goes_to_the_first_in_queue()
        {
            var s = Standard();
            for (int i = 0; i < 4; i++) s.Request("npc" + i, 1000, DockSize.P);
            s.Request("player", 1000, DockSize.P);
            s.Request("npc9", 1000, DockSize.P);
            s.Release("npc2");
            var granted = s.ProcessQueue();
            Assert.AreEqual(1, granted.Count);
            Assert.AreEqual("player", granted[0].shipId);
            Assert.AreEqual(s.AssignedDock("player"), granted[0].dockIndex);
            Assert.AreEqual(1, s.QueuePosition("npc9"));
        }

        [Test]
        public void Leaving_range_also_leaves_the_queue()
        {
            var s = Standard();
            for (int i = 0; i < 4; i++) s.Request("npc" + i, 1000, DockSize.P);
            s.Request("player", 1000, DockSize.P);
            s.Tick("player", 9000);
            Assert.AreEqual(0, s.QueuePosition("player"));
        }

        [Test]
        public void Npc_stay_time_depends_on_size()
        {
            Assert.AreEqual(20f, DockingService.MaxNpcStay(DockSize.P));
            Assert.AreEqual(45f, DockingService.MaxNpcStay(DockSize.M));
            Assert.AreEqual(60f, DockingService.MaxNpcStay(DockSize.G));
        }
    }
}