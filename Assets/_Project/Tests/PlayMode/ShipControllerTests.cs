using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PulseFactor.Tests
{
    public class ShipControllerTests
    {
        ShipController ship;

        [SetUp]
        public void SetUp()
        {
            ship = new GameObject("ship").AddComponent<ShipController>();
            ship.Initialize(PlayModeFixtures.Class(ShipClass.Scout));
        }

        [TearDown]
        public void TearDown() => Object.Destroy(ship.gameObject);

        [UnityTest]
        public IEnumerator Throttle_input_moves_ship_forward()
        {
            ship.Input = new FlightInput { throttle = 1, aimDirection = Vector3.forward };
            yield return new WaitForSeconds(1f);
            Assert.AreEqual(40f, ship.Velocity.z, 3f);
            Assert.Greater(ship.transform.position.z, 15f);
        }

        [UnityTest]
        public IEnumerator Locked_controls_ignore_input()
        {
            ship.ControlsLocked = true;
            ship.Input = new FlightInput { throttle = 1, aimDirection = Vector3.forward };
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(0f, ship.Velocity.magnitude, 0.01f);
        }

        [UnityTest]
        public IEnumerator Cargo_hold_matches_class_capacity()
        {
            yield return null;
            Assert.AreEqual(50, ship.Cargo.Capacity);
        }
    }
}
