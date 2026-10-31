using System.Collections.Generic;
using NUnit.Framework;

namespace PulseFactor.Tests
{
    public class NpcNameTests
    {
        [Test]
        public void Each_kind_draws_from_its_own_pool()
        {
            var rng = new System.Random(1);
            var used = new HashSet<string>();
            for (int i = 0; i < 20; i++)
            {
                CollectionAssert.Contains(NpcNames.Patrol, NpcNames.BaseName(NpcNames.Pick(NpcNames.Kind.Patrol, rng, used)));
                CollectionAssert.Contains(NpcNames.SmallTrader, NpcNames.BaseName(NpcNames.Pick(NpcNames.Kind.SmallTrader, rng, used)));
                CollectionAssert.Contains(NpcNames.HeavyTrader, NpcNames.BaseName(NpcNames.Pick(NpcNames.Kind.HeavyTrader, rng, used)));
            }
        }

        [Test]
        public void Pools_do_not_mix_styles()
        {
            CollectionAssert.DoesNotContain(NpcNames.SmallTrader, "Destroyer of Worlds");
            foreach (var n in NpcNames.Patrol) CollectionAssert.DoesNotContain(NpcNames.SmallTrader, n);
            foreach (var n in NpcNames.HeavyTrader) CollectionAssert.DoesNotContain(NpcNames.SmallTrader, n);
        }

        [Test]
        public void Names_do_not_repeat_and_get_a_number_when_the_pool_runs_out()
        {
            var rng = new System.Random(2);
            var used = new HashSet<string>();   // Pick registra aqui o que sorteou
            var seen = new HashSet<string>();
            for (int i = 0; i < NpcNames.HeavyTrader.Length + 3; i++)
                Assert.IsTrue(seen.Add(NpcNames.Pick(NpcNames.Kind.HeavyTrader, rng, used)), "nome repetido");
            Assert.IsTrue(System.Linq.Enumerable.Any(seen, n => n.EndsWith(" II")), "lista esgotada ganha numeracao");
        }
    }
}
