using NUnit.Framework;

namespace PulseFactor.Tests
{
    public class HudModelTests
    {
        [TestCase(850, "850 m")]
        [TestCase(12400, "12.4 km")]
        [TestCase(2.5e6, "2.5 Mm")]
        [TestCase(1.5e10, "50.03 Ls")]
        [TestCase(0, "0 m")]
        public void FormatDistance(double meters, string expected)
        {
            Assert.AreEqual(expected, HudModel.FormatDistance(meters));
        }

        [Test]
        public void FormatDistance_switches_to_Ls_at_a_tenth_of_a_light_second()
        {
            StringAssert.EndsWith("Mm", HudModel.FormatDistance(0.099 * WorldPosition.LightSecond));
            StringAssert.EndsWith("Ls", HudModel.FormatDistance(0.1 * WorldPosition.LightSecond));
        }

        [Test]
        public void Warp_charging_maps_progress()
        {
            var d = new WarpDrive(new WarpSettings());
            d.TryStartCharge(false, out _);
            d.Tick(3f, 1e12, double.PositiveInfinity, false);
            var w = WarpHud.From(d, massLocked: false);
            Assert.AreEqual(WarpHudState.Charging, w.state);
            Assert.AreEqual(0.6f, w.progress, 0.001f);
        }

        [Test]
        public void Warp_idle_states()
        {
            var d = new WarpDrive(new WarpSettings());
            Assert.AreEqual(WarpHudState.MassLocked, WarpHud.From(d, true).state);
            Assert.AreEqual(WarpHudState.Ready, WarpHud.From(d, false).state);
        }

        [Test]
        public void Warp_active_state()
        {
            var d = new WarpDrive(new WarpSettings());
            d.TryStartCharge(false, out _);
            d.Tick(5.1f, 1e12, double.PositiveInfinity, false);
            var w = WarpHud.From(d, false);
            Assert.AreEqual(WarpHudState.Active, w.state);
            Assert.AreEqual(1, w.level);
        }
    }
}
