using NUnit.Framework;
using UnityEngine;
using static PulseFactor.Tests.TestDefs;

namespace PulseFactor.Tests
{
    public class FlightModelTests
    {
        const float Dt = 0.02f;

        static Vector3 Run(FlightModel m, Vector3 v, FlightInput input, float load, float seconds)
        {
            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps; i++) v = m.StepVelocity(v, Quaternion.identity, input, load, Dt);
            return v;
        }

        static FlightInput Throttle(float t) => new FlightInput { throttle = t, aimDirection = Vector3.forward };

        [Test]
        public void Full_cargo_reduces_max_speed_by_40_percent()
        {
            var m = new FlightModel(Cargo());
            Assert.AreEqual(78f, m.EffectiveMaxSpeed(1f), 0.01f);
            Assert.AreEqual(130f, m.EffectiveMaxSpeed(0f), 0.01f);
        }

        [Test]
        public void Cargo_does_not_change_acceleration()
        {
            var m = new FlightModel(Cargo());
            var empty = Run(m, Vector3.zero, Throttle(1), 0f, 1f);
            var full = Run(m, Vector3.zero, Throttle(1), 1f, 1f);
            Assert.AreEqual(15f, empty.z, 0.01f);
            Assert.AreEqual(15f, full.z, 0.01f);
        }

        [Test]
        public void Speed_never_exceeds_effective_max()
        {
            var m = new FlightModel(Cargo());
            var v = Run(m, Vector3.zero, Throttle(1), 1f, 60f);
            Assert.LessOrEqual(v.magnitude, 78f + 0.01f);
        }

        [Test]
        public void Releasing_throttle_decelerates_slowly()
        {
            var m = new FlightModel(Scout());
            var v = Run(m, new Vector3(0, 0, 100), Throttle(0), 0f, 1f);
            Assert.AreEqual(88f, v.z, 0.01f);
        }

        [Test]
        public void Cruise_holds_target_speed()
        {
            var m = new FlightModel(Scout());
            var v = new Vector3(0, 0, 100);
            v = m.StepVelocity(v, Quaternion.identity, new FlightInput { cruiseToggle = true, aimDirection = Vector3.forward }, 0f, Dt);
            Assert.IsTrue(m.CruiseActive);
            v = Run(m, v, Throttle(0), 0f, 10f);
            Assert.AreEqual(100f, v.z, 0.5f);
            Run(m, v, Throttle(1), 0f, 1f);
            Assert.AreEqual(160f, m.CruiseTarget, 0.5f);
        }

        // Mira "presa" a nave (como o reticulo do jogador): erro constante em relacao ao nariz.
        static Quaternion Steer(FlightModel m, Quaternion r, float yawDeg, float seconds)
        {
            int steps = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < steps; i++)
            {
                var aim = r * Quaternion.Euler(0, yawDeg, 0) * Vector3.forward;
                r = m.StepRotation(r, new FlightInput { aimDirection = aim }, Dt);
            }
            return r;
        }

        [Test]
        public void Turn_rate_ramps_up_instead_of_jumping()
        {
            var m = new FlightModel(Scout());
            Steer(m, Quaternion.identity, 90f, Dt);
            Assert.AreEqual(90f * 4f * Dt, m.AngularVelocity.y, 0.01f);
        }

        [Test]
        public void Large_error_saturates_at_turn_rate()
        {
            var m = new FlightModel(Scout());
            Steer(m, Quaternion.identity, 90f, 0.5f);
            Assert.AreEqual(90f, m.AngularVelocity.y, 0.01f);
        }

        [Test]
        public void Small_error_turns_proportionally()
        {
            var m = new FlightModel(Scout());
            Steer(m, Quaternion.identity, 5f, 0.5f);
            Assert.AreEqual(90f * 5f / 25f, m.AngularVelocity.y, 0.1f);
        }

        [Test]
        public void Centering_the_aim_stops_the_turn_smoothly()
        {
            var m = new FlightModel(Scout());
            var r = Steer(m, Quaternion.identity, 90f, 0.5f);
            Steer(m, r, 0f, 0.1f);
            Assert.Greater(m.AngularVelocity.y, 0f, "ainda desacelerando");
            Steer(m, r, 0f, 0.3f);
            Assert.AreEqual(0f, m.AngularVelocity.y, 0.01f);
        }

        [Test]
        public void Turning_right_rotates_toward_right()
        {
            var m = new FlightModel(Scout());
            var r = Steer(m, Quaternion.identity, 90f, 0.5f);
            Assert.Greater((r * Vector3.forward).x, 0.1f);
        }

        [Test]
        public void Roll_input_rolls_around_forward()
        {
            var m = new FlightModel(Scout());
            var r = Quaternion.identity;
            for (int i = 0; i < 25; i++) r = m.StepRotation(r, new FlightInput { roll = 1, aimDirection = r * Vector3.forward }, Dt);
            Assert.Less(m.AngularVelocity.z, 0f);
            Assert.AreEqual(1f, Vector3.Dot(r * Vector3.forward, Vector3.forward), 0.01f);
        }

        [Test]
        public void Strafe_accelerates_sideways()
        {
            var m = new FlightModel(Scout());
            var v = Run(m, Vector3.zero, new FlightInput { strafe = new Vector2(1, 0), aimDirection = Vector3.forward }, 0f, 1f);
            Assert.AreEqual(40f, v.x, 0.01f);
        }
    }
}
