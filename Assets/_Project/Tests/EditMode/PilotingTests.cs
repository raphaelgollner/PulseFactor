using NUnit.Framework;
using UnityEngine;

namespace PulseFactor.Tests
{
    public class BoostTankTests
    {
        const float Dt = 0.1f;

        static int Run(BoostTank b, float seconds, bool want, bool allowed = true)
        {
            int activeFrames = 0;
            for (int i = 0; i < Mathf.RoundToInt(seconds / Dt); i++) if (b.Tick(Dt, want, allowed)) activeFrames++;
            return activeFrames;
        }

        [Test]
        public void Small_ships_start_with_150()
        {
            var b = new BoostTank(150);
            Assert.AreEqual(150f, b.Value);
            Assert.AreEqual(1f, b.Fraction);
        }

        [Test]
        public void Holding_drains_30_per_second_until_empty()
        {
            var b = new BoostTank(150);
            Run(b, 2f, true);
            Assert.AreEqual(90f, b.Value, 0.01f);
            Run(b, 4f, true);
            Assert.AreEqual(0f, b.Value, 0.01f);
            Assert.IsFalse(b.Tick(Dt, true, true), "vazio nao impulsiona");
        }

        [Test]
        public void Recharges_15_per_second_after_one_second()
        {
            var b = new BoostTank(150);
            Run(b, 2f, true);           // 90
            Run(b, 1f, false);          // espera de 1 s, sem recarga
            Assert.AreEqual(90f, b.Value, 0.01f);
            Run(b, 2f, false);          // +30
            Assert.AreEqual(120f, b.Value, 0.01f);
        }

        [Test]
        public void Not_active_when_not_allowed_in_warp()
        {
            var b = new BoostTank(150);
            Assert.IsFalse(b.Tick(Dt, true, false));
            Assert.AreEqual(150f, b.Value);
        }
    }

    public class DockApproachTests
    {
        [Test]
        public void Above_the_pad_and_close_starts_autopilot()
        {
            Assert.IsTrue(DockingAutopilot.IsAbovePad(new Vector3(5, 120, -8), 44f));
        }

        [Test]
        public void Beside_the_pad_does_not_start()
        {
            Assert.IsFalse(DockingAutopilot.IsAbovePad(new Vector3(60, 120, 0), 44f));
        }

        [Test]
        public void Too_high_or_below_does_not_start()
        {
            Assert.IsFalse(DockingAutopilot.IsAbovePad(new Vector3(0, 400, 0), 44f));
            Assert.IsFalse(DockingAutopilot.IsAbovePad(new Vector3(0, -20, 0), 44f));
        }
    }

    public class ScannerTests
    {
        [TestCase(TargetKind.Pirate, 4000, true)]
        [TestCase(TargetKind.Pirate, 6000, false)]
        [TestCase(TargetKind.Ship, 6000, false)]
        [TestCase(TargetKind.Asteroid, 4999, true)]
        [TestCase(TargetKind.Asteroid, 5001, false)]
        [TestCase(TargetKind.Station, 1e9, true)]
        [TestCase(TargetKind.Body, 1e10, true)]
        [TestCase(TargetKind.Gate, 1e9, true)]
        public void Scanner_hides_ships_and_asteroids_out_of_range(TargetKind kind, double distance, bool visible)
        {
            Assert.AreEqual(visible, Scanner.Detects(kind, distance, Scanner.DefaultRange));
        }

        [Test]
        public void Default_range_is_5km() => Assert.AreEqual(5000.0, Scanner.DefaultRange);
    }
}
