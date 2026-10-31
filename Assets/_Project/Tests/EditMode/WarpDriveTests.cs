using NUnit.Framework;
using Unity.Mathematics;

namespace PulseFactor.Tests
{
    public class WarpDriveTests
    {
        static WarpDrive Drive() => new WarpDrive(new WarpSettings());

        static WarpDrive Active()
        {
            var d = Drive();
            Assert.IsTrue(d.TryStartCharge(false, true, out _));
            d.Tick(5.1f, 1e9, 1e10);
            Assert.AreEqual(WarpState.Active, d.State);
            return d;
        }

        [Test]
        public void Cannot_charge_without_target()
        {
            var d = Drive();
            Assert.IsFalse(d.TryStartCharge(false, false, out var reason));
            Assert.AreEqual(WarpFailReason.NoTarget, reason);
            Assert.AreEqual(WarpState.Idle, d.State);
        }

        [Test]
        public void Cannot_charge_when_mass_locked()
        {
            var d = Drive();
            Assert.IsFalse(d.TryStartCharge(true, true, out var reason));
            Assert.AreEqual(WarpFailReason.MassLocked, reason);
            Assert.AreEqual(WarpState.Idle, d.State);
        }

        [Test]
        public void Charge_takes_five_seconds()
        {
            var d = Drive();
            d.TryStartCharge(false, true, out _);
            d.Tick(4.9f, 1e9, 1e10);
            Assert.AreEqual(WarpState.Charging, d.State);
            d.Tick(0.2f, 1e9, 1e10);
            Assert.AreEqual(WarpState.Active, d.State);
        }

        [Test]
        public void Damage_delays_charge_capped_at_3s()
        {
            var d = Drive();
            d.TryStartCharge(false, true, out _);
            for (int i = 0; i < 5; i++) d.OnDamage();
            d.Tick(7.9f, 1e9, 1e10);
            Assert.AreEqual(WarpState.Charging, d.State);
            d.Tick(0.2f, 1e9, 1e10);
            Assert.AreEqual(WarpState.Active, d.State);
        }

        [Test]
        public void Charge_progress_reports_fraction()
        {
            var d = Drive();
            d.TryStartCharge(false, true, out _);
            d.Tick(3f, 1e9, 1e10);
            Assert.AreEqual(0.6f, d.ChargeProgress, 0.001f);
        }

        [Test]
        public void Speed_scales_with_nearest_distance()
        {
            var d = Active();
            Assert.AreEqual(1e7, d.Tick(0.02f, 1e8, 1e10), 1.0);
        }

        [Test]
        public void Speed_clamped_to_min()
        {
            var d = Active();
            Assert.AreEqual(1000.0, d.Tick(0.02f, 5000, 15000), 1e-6);
        }

        [Test]
        public void Speed_clamped_to_max()
        {
            var d = Active();
            Assert.AreEqual(3e8, d.Tick(0.02f, 1e12, 1e12), 1.0);
        }

        [Test]
        public void Exits_at_10km()
        {
            var d = Active();
            d.Tick(0.02f, 1e8, 9999);
            Assert.AreEqual(WarpState.Exiting, d.State);
            d.Complete();
            Assert.AreEqual(WarpState.Idle, d.State);
        }

        [Test]
        public void Starting_while_charging_is_rejected_and_cancel_returns_idle()
        {
            var d = Drive();
            d.TryStartCharge(false, true, out _);
            Assert.IsFalse(d.TryStartCharge(false, true, out var reason));
            Assert.AreEqual(WarpFailReason.AlreadyActive, reason);
            d.Cancel();
            Assert.AreEqual(WarpState.Idle, d.State);
        }

        [Test]
        public void Damage_outside_charging_does_nothing()
        {
            var d = Drive();
            d.OnDamage();
            d.TryStartCharge(false, true, out _);
            d.Tick(5.05f, 1e9, 1e10);
            Assert.AreEqual(WarpState.Active, d.State);
        }
    }

    public class MassLockTests
    {
        static MassSource Station(double3 p) => MassSource.Station(p);
        static MassSource Planet(double3 p, double r) => MassSource.Body(p, r);

        [Test]
        public void Locked_within_5km_of_station()
        {
            var s = new[] { Station(double3.zero) };
            Assert.IsTrue(MassLock.IsLocked(new double3(4999, 0, 0), s));
            Assert.IsFalse(MassLock.IsLocked(new double3(5001, 0, 0), s));
        }

        [Test]
        public void Locked_within_three_radii_of_planet()
        {
            var s = new[] { Planet(double3.zero, 6e6) };
            Assert.IsTrue(MassLock.IsLocked(new double3(1.79e7, 0, 0), s));
            Assert.IsFalse(MassLock.IsLocked(new double3(1.81e7, 0, 0), s));
        }

        [Test]
        public void Nearest_distance_is_to_surface()
        {
            var s = new[] { Planet(double3.zero, 6e6), Station(new double3(1e9, 0, 0)) };
            Assert.AreEqual(1e8 - 6e6, MassLock.NearestDistance(new double3(1e8, 0, 0), s), 1.0);
        }

        [Test]
        public void No_sources_means_unlocked_and_infinite_distance()
        {
            var s = new MassSource[0];
            Assert.IsFalse(MassLock.IsLocked(double3.zero, s));
            Assert.AreEqual(double.PositiveInfinity, MassLock.NearestDistance(double3.zero, s));
        }
    }
}
