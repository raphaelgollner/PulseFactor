using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PulseFactor.Tests
{
    public class ShipAssemblerTests
    {
        GameObject ship;

        [TearDown]
        public void TearDown() { if (ship) Object.Destroy(ship); }

        [UnityTest]
        public IEnumerator Builds_all_five_parts()
        {
            ship = ShipAssembler.Build(PlayModeFixtures.Blueprint(ShipClass.Scout), null);
            yield return null;
            foreach (var n in new[] { "Cockpit", "Body", "Tail", "WingL", "WingR" })
                Assert.IsNotNull(ship.transform.Find(n), n);
            Assert.IsTrue(ship.activeSelf);
        }

        [UnityTest]
        public IEnumerator Right_wing_is_mirrored()
        {
            ship = ShipAssembler.Build(PlayModeFixtures.Blueprint(ShipClass.Scout), null);
            yield return null;
            Assert.Less(ship.transform.Find("WingR").lossyScale.x, 0f);
            Assert.Greater(ship.transform.Find("WingL").lossyScale.x, 0f);
        }

        [Test]
        public void Invalid_blueprint_throws()
        {
            var bp = PlayModeFixtures.Blueprint(ShipClass.Scout, multiclassParts: false, partsOrigin: ShipClass.Assault);
            Assert.Throws<ArgumentException>(() => ShipAssembler.Build(bp, null));
        }

        [UnityTest]
        public IEnumerator Paint_sets_region_color()
        {
            var bp = PlayModeFixtures.Blueprint(ShipClass.Scout);
            ship = ShipAssembler.Build(bp, null);
            var paint = new PaintScheme();
            paint.body.primary = Color.red;
            ShipAssembler.ApplyPaint(ship, paint);
            yield return null;
            var region = ship.transform.Find("Body").GetComponentInChildren<PaintableRegion>();
            var mpb = new MaterialPropertyBlock();
            region.GetComponent<Renderer>().GetPropertyBlock(mpb);
            Assert.AreEqual(Color.red, mpb.GetColor("_BaseColor"));
        }

        [UnityTest]
        public IEnumerator Mixed_manufacturer_parts_attach_at_sockets()
        {
            var bp = PlayModeFixtures.Blueprint(ShipClass.Cargo);
            ship = ShipAssembler.Build(bp, null);
            yield return null;
            var socket = ship.transform.Find("Body/Socket_WingL");
            var wing = ship.transform.Find("WingL");
            Assert.Less(Vector3.Distance(socket.position, wing.position), 0.001f);
            var front = ship.transform.Find("Body/Socket_Front");
            Assert.Less(Vector3.Distance(front.position, ship.transform.Find("Cockpit").position), 0.001f);
        }

        [UnityTest]
        public IEnumerator Assembled_ship_exposes_eye_point_and_thrusters()
        {
            ship = ShipAssembler.Build(PlayModeFixtures.Blueprint(ShipClass.Scout), null);
            yield return null;
            var a = ship.GetComponent<AssembledShip>();
            Assert.IsNotNull(a.EyePoint);
            Assert.AreEqual(4, a.Thrusters.Count); // cockpit, tail, 2 asas
        }
    }
}
