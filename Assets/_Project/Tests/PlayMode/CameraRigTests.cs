using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PulseFactor.Tests
{
    public class CameraRigTests
    {
        GameObject ship;
        CameraRig rig;

        [SetUp]
        public void SetUp()
        {
            ship = ShipAssembler.Build(PlayModeFixtures.Blueprint(ShipClass.Scout), null);
            rig = new GameObject("rig").AddComponent<Camera>().gameObject.AddComponent<CameraRig>();
            rig.Attach(ship);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(ship);
            Object.Destroy(rig.gameObject);
        }

        [UnityTest]
        public IEnumerator Toggle_switches_between_cockpit_and_third_person()
        {
            rig.SetMode(CameraRig.Mode.Cockpit);
            yield return null;
            rig.Toggle();
            Assert.AreEqual(CameraRig.Mode.ThirdPerson, rig.CurrentMode);
            rig.Toggle();
            Assert.AreEqual(CameraRig.Mode.Cockpit, rig.CurrentMode);
        }

        [UnityTest]
        public IEnumerator Cockpit_mode_hides_exterior_cockpit_layer()
        {
            int layer = LayerMask.NameToLayer("ShipExteriorCockpit");
            Assert.GreaterOrEqual(layer, 0, "layer ShipExteriorCockpit precisa existir");
            rig.SetMode(CameraRig.Mode.Cockpit);
            yield return null;
            var cam = rig.GetComponent<Camera>();
            Assert.AreEqual(0, cam.cullingMask & (1 << layer));
            rig.SetMode(CameraRig.Mode.ThirdPerson);
            yield return null;
            Assert.AreNotEqual(0, cam.cullingMask & (1 << layer));
        }

        [UnityTest]
        public IEnumerator Cockpit_camera_sits_at_eye_point()
        {
            rig.SetMode(CameraRig.Mode.Cockpit);
            yield return null;
            var eye = ship.GetComponent<AssembledShip>().EyePoint;
            Assert.Less(Vector3.Distance(eye.position, rig.transform.position), 0.01f);
        }
    }
}
