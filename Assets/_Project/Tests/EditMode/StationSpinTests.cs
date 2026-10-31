using NUnit.Framework;
using UnityEngine;

namespace PulseFactor.Tests
{
    public class StationSpinTests
    {
        [Test]
        public void Co_rotation_moves_point_around_center()
        {
            var r = StationSpin.CoRotate(new Vector3(10, 0, 0), Vector3.up, 90f,
                new Vector3(110, 5, 0), Quaternion.identity, new Vector3(1, 0, 0));
            Assert.AreEqual(10f, r.position.x, 1e-3f);
            Assert.AreEqual(5f, r.position.y, 1e-3f);
            Assert.AreEqual(-100f, r.position.z, 1e-3f);
            Assert.AreEqual(-1f, r.velocity.z, 1e-4f);
            Assert.AreEqual(90f, Quaternion.Angle(Quaternion.identity, r.rotation), 1e-2f);
        }

        [Test]
        public void Influence_zone_is_8km()
        {
            Assert.IsTrue(StationSpin.InInfluence(7999f));
            Assert.IsFalse(StationSpin.InInfluence(8001f));
        }
    }
}
