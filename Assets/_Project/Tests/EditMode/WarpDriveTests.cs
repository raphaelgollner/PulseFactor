using NUnit.Framework;
using Unity.Mathematics;

namespace PulseFactor.Tests
{
    public class WarpDriveTests
    {
        const double Far = 1e12;
        const double NoTarget = double.PositiveInfinity;

        static WarpDrive Drive() => new WarpDrive(new WarpSettings());

        static WarpDrive Active()
        {
            var d = Drive();
            Assert.IsTrue(d.TryStartCharge(false, true, out _));
            d.Tick(5.1f, Far, NoTarget, false);
            Assert.AreEqual(WarpState.Active, d.State);
            return d;
        }

        static double Run(WarpDrive d, float seconds, double body = Far, double target = NoTarget, bool heading = false)
        {
            double v = 0;
            for (int i = 0; i < (int)(seconds / 0.1f); i++) v = d.Tick(0.1f, body, target, heading);
            return v;
        }

        // Decisao do autor (2026-10-31): a dobra so ativa com alvo selecionado ou rota definida.
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
            d.Tick(4.9f, Far, NoTarget, false);
            Assert.AreEqual(WarpState.Charging, d.State);
            d.Tick(0.2f, Far, NoTarget, false);
            Assert.AreEqual(WarpState.Active, d.State);
        }

        [Test]
        public void Damage_delays_charge_capped_at_3s()
        {
            var d = Drive();
            d.TryStartCharge(false, true, out _);
            for (int i = 0; i < 5; i++) d.OnDamage();
            d.Tick(7.9f, Far, NoTarget, false);
            Assert.AreEqual(WarpState.Charging, d.State);
            d.Tick(0.2f, Far, NoTarget, false);
            Assert.AreEqual(WarpState.Active, d.State);
        }

        [Test]
        public void Charge_progress_reports_fraction()
        {
            var d = Drive();
            d.TryStartCharge(false, true, out _);
            d.Tick(3f, Far, NoTarget, false);
            Assert.AreEqual(0.6f, d.ChargeProgress, 0.001f);
        }

        [Test]
        public void Starts_at_level_1_and_ramps_up_quickly()
        {
            var d = Active();
            Assert.AreEqual(1, d.Level);
            Assert.Less(d.Tick(0.1f, Far, NoTarget, false), 5e5 * 0.5);
            Assert.AreEqual(5e5, Run(d, 4f), 1.0);
        }

        [Test]
        public void Levels_cap_at_500_1500_5000_kms()
        {
            var d = Active();
            Assert.AreEqual(5e5, Run(d, 5f), 1.0);
            d.ShiftUp();
            Assert.AreEqual(1.5e6, Run(d, 5f), 1.0);
            d.ShiftUp();
            Assert.AreEqual(5e6, Run(d, 5f), 1.0);
            d.ShiftUp();
            Assert.AreEqual(3, d.Level);
            d.ShiftDown(); d.ShiftDown(); d.ShiftDown();
            Assert.AreEqual(1, d.Level);
            Assert.AreEqual(5e5, Run(d, 5f), 1.0);
        }

        [Test]
        public void Brakes_near_target_when_heading_to_it()
        {
            var d = Active();
            d.ShiftUp(); d.ShiftUp();
            Run(d, 5f);
            Assert.LessOrEqual(Run(d, 0.5f, Far, 1e5, true), d.Settings.brakeK * 1e5 + 1);
        }

        [Test]
        public void Does_not_brake_when_not_heading_to_target()
        {
            var d = Active();
            Assert.AreEqual(5e5, Run(d, 5f, Far, 1e5, false), 1.0);
        }

        [Test]
        public void Nearby_body_caps_speed()
        {
            var d = Active();
            Assert.LessOrEqual(Run(d, 5f, 5e4), 5e4 + 1);
        }

        [Test]
        public void Exits_at_3km_from_target()
        {
            var d = Active();
            d.Tick(0.1f, Far, 3500, true);
            Assert.AreEqual(WarpState.Active, d.State);
            d.Tick(0.1f, Far, 2999, true);
            Assert.AreEqual(WarpState.Exiting, d.State);
            d.Complete();
            Assert.AreEqual(WarpState.Idle, d.State);
            Assert.AreEqual(1, d.Level);
        }

        // Playtest: com a doca recem-deixada como alvo (a poucos km), a dobra saia na hora.
        [Test]
        public void Does_not_exit_near_target_when_not_heading_to_it()
        {
            var d = Active();
            d.Tick(0.1f, Far, 4000, false);
            Assert.AreEqual(WarpState.Active, d.State);
        }

        [TestCase(0, 1f)]
        [TestCase(1, 0.5f)]
        [TestCase(2, 0.3f)]
        [TestCase(3, 0.15f)]
        public void Steering_gets_heavier_per_warp_level(int level, float factor)
        {
            Assert.AreEqual(factor, WarpDrive.SteeringFactor(level), 1e-5f);
        }

        [Test]
        public void Shifting_outside_warp_does_nothing()
        {
            var d = Drive();
            d.ShiftUp();
            Assert.AreEqual(1, d.Level);
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
            d.Tick(5.05f, Far, NoTarget, false);
            Assert.AreEqual(WarpState.Active, d.State);
        }
    }

    public class MassLockTests
    {
        static MassSource Station(double3 p) => MassSource.Station(p);
        static MassSource Planet(double3 p, double r) => MassSource.Body(p, r);

        [Test]
        public void Locked_within_2km_of_station()
        {
            var s = new[] { Station(double3.zero) };
            Assert.IsTrue(MassLock.IsLocked(new double3(1999, 0, 0), s));
            Assert.IsFalse(MassLock.IsLocked(new double3(2001, 0, 0), s));
        }

        [Test]
        public void Gate_mass_lock_is_only_2km()
        {
            var s = new[] { MassSource.Gate(double3.zero) };
            Assert.IsTrue(MassLock.IsLocked(new double3(1999, 0, 0), s));
            Assert.IsFalse(MassLock.IsLocked(new double3(2001, 0, 0), s));
        }

        [Test]
        public void Locked_within_1_1_radii_of_planet()
        {
            var s = new[] { Planet(double3.zero, 6e6) };
            Assert.IsTrue(MassLock.IsLocked(new double3(6.5e6, 0, 0), s));
            Assert.IsFalse(MassLock.IsLocked(new double3(6.7e6, 0, 0), s));
        }

        // Revisao C1: a nave liberada pelo alerta de proximidade nao pode ficar presa no bloqueio de massa.
        // Playtest 2026-10-31: zona de perigo reduzida 80% (0,2 R da superficie).
        [Test]
        public void Ship_released_by_proximity_guard_is_not_mass_locked()
        {
            const double r = 6e6;
            var s = new[] { Planet(double3.zero, r) };
            var edge = new double3(r * (1 + ProximityRules.DefaultDangerFactor) + 1, 0, 0); // primeiro ponto fora do perigo
            Assert.IsFalse(ProximityRules.InDanger(MassLock.NearestDistance(edge, s), r, ProximityRules.DefaultDangerFactor));
            Assert.IsFalse(MassLock.IsLocked(edge, s));
        }

        [Test]
        public void Danger_zone_is_a_fifth_of_the_radius()
        {
            Assert.AreEqual(0.2, ProximityRules.DefaultDangerFactor, 1e-9);
        }

        [Test]
        public void Locking_source_is_reported_for_the_feed()
        {
            var s = new[] { Station(new double3(1e6, 0, 0)), MassSource.Gate(new double3(5000, 0, 0)) };
            Assert.AreEqual(1, MassLock.LockingIndex(new double3(4000, 0, 0), s));
            Assert.AreEqual(-1, MassLock.LockingIndex(new double3(-9000, 0, 0), s));
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
