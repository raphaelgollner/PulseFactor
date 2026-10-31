using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;

namespace PulseFactor.Tests
{
    public class TrafficPlannerTests
    {
        static readonly string[] Stations = { "gollner", "apollo" };
        static readonly string[] Gates = { "gate_aster_venera", "gate_aster_cinturao" };
        static readonly string[] Corps = { "nova", "magna", "atlas", "federation" };

        [Test]
        public void Next_destination_is_never_the_current_station()
        {
            var rng = new System.Random(1);
            for (int i = 0; i < 500; i++)
            {
                var d = TrafficPlanner.NextDestination(rng, "gollner", Stations, Gates, allowOtherSectors: true);
                Assert.AreNotEqual("gollner", d.id);
            }
        }

        [Test]
        public void Traders_sometimes_leave_through_gates_but_patrols_never()
        {
            var rng = new System.Random(2);
            var trader = Enumerable.Range(0, 500).Select(_ => TrafficPlanner.NextDestination(rng, "gollner", Stations, Gates, true)).ToList();
            Assert.IsTrue(trader.Any(d => d.isGate));
            Assert.IsTrue(trader.Any(d => !d.isGate));
            var patrol = Enumerable.Range(0, 500).Select(_ => TrafficPlanner.NextDestination(rng, "gollner", Stations, Gates, false)).ToList();
            Assert.IsFalse(patrol.Any(d => d.isGate));
        }

        [Test]
        public void Small_trader_groups_have_up_to_three_and_three_brings_an_escort()
        {
            var rng = new System.Random(3);
            bool sawThree = false;
            for (int i = 0; i < 500; i++)
            {
                var g = TrafficPlanner.SmallTraderGroup(rng, Corps);
                Assert.That(g.traders, Is.InRange(1, 3));
                Assert.AreEqual(g.traders == 3 ? 1 : 0, g.escorts);
                sawThree |= g.traders == 3;
            }
            Assert.IsTrue(sawThree);
        }

        [Test]
        public void Medium_trader_has_zero_to_two_escorts()
        {
            var rng = new System.Random(4);
            var escorts = Enumerable.Range(0, 500).Select(_ => TrafficPlanner.MediumTraderGroup(rng, Corps).escorts).ToList();
            Assert.IsTrue(escorts.All(e => e >= 0 && e <= 2));
            Assert.IsTrue(escorts.Contains(0) && escorts.Contains(1) && escorts.Contains(2));
        }

        [Test]
        public void A_group_shares_one_affiliation_and_it_is_never_pirate()
        {
            var rng = new System.Random(5);
            for (int i = 0; i < 200; i++)
            {
                var g = TrafficPlanner.SmallTraderGroup(rng, Corps);
                CollectionAssert.Contains(Corps, g.corpId);
            }
        }

        [Test]
        public void Patrol_groups_follow_pirate_style_sizes()
        {
            var rng = new System.Random(6);
            var sizes = Enumerable.Range(0, 500).Select(_ => TrafficPlanner.PatrolGroupSize(rng)).ToList();
            Assert.IsTrue(sizes.All(s => s >= 1 && s <= 5));
        }

        [Test]
        public void Npc_stay_is_between_half_and_max_for_its_size()
        {
            var rng = new System.Random(7);
            for (int i = 0; i < 200; i++)
            {
                float p = TrafficPlanner.NpcStay(rng, DockSize.P), m = TrafficPlanner.NpcStay(rng, DockSize.M);
                Assert.That(p, Is.InRange(10f, 20f));
                Assert.That(m, Is.InRange(22.5f, 45f));
            }
        }
    }

    public class PirateAvoidanceTests
    {
        [Test]
        public void Pirate_flees_from_nearby_patrol()
        {
            var patrols = new List<double3> { new(1000, 0, 0) };
            Assert.IsTrue(TrafficPlanner.ShouldFlee(double3.zero, patrols, 4000, out var away));
            Assert.Less(away.x, 0, "foge para o lado oposto");
        }

        [Test]
        public void Pirate_ignores_far_patrol()
        {
            var patrols = new List<double3> { new(9000, 0, 0) };
            Assert.IsFalse(TrafficPlanner.ShouldFlee(double3.zero, patrols, 4000, out _));
        }
    }
}
