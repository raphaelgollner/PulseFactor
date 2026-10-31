using System.Collections;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace PulseFactor.Tests
{
    public class FloatingOriginTests
    {
        FloatingOrigin origin;
        Transform focus;

        [SetUp]
        public void SetUp()
        {
            origin = new GameObject("origin").AddComponent<FloatingOrigin>();
            focus = new GameObject("focus").AddComponent<WorldAnchor>().transform;
            focus.GetComponent<WorldAnchor>().dynamic = true;
            origin.Focus = focus;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(origin.gameObject);
            Object.Destroy(focus.gameObject);
        }

        [UnityTest]
        public IEnumerator Rebase_keeps_world_position()
        {
            bool fired = false;
            origin.Rebased += _ => fired = true;
            focus.position = new Vector3(2500, 0, 0);
            var before = origin.ToWorld(focus.position);
            yield return null;
            Assert.Less(focus.position.magnitude, 1f);
            var after = origin.ToWorld(focus.position);
            Assert.AreEqual(before.value.x, after.value.x, 0.01);
            Assert.IsTrue(fired);
        }

        [Test]
        public void Rebase_after_1e9_travel_has_no_jitter()
        {
            var neighbour = new GameObject("neighbour").AddComponent<WorldAnchor>();
            neighbour.dynamic = true;
            neighbour.transform.position = new Vector3(100, 0, 0);
            for (int i = 0; i < 10000; i++)
            {
                focus.position += new Vector3(0, 0, 100000);
                neighbour.transform.position += new Vector3(0, 0, 100000);
                origin.CheckRebase();
            }
            Assert.AreEqual(1e9, origin.Origin.value.z, 1.0);
            Assert.AreEqual(100f, Vector3.Distance(focus.position, neighbour.transform.position), 0.01f);
            Object.Destroy(neighbour.gameObject);
        }

        [Test]
        public void Static_anchor_returns_exactly_after_long_trip()
        {
            var station = new GameObject("station").AddComponent<WorldAnchor>();
            station.SetWorld(new WorldPosition(new double3(0, 0, 50)), origin);
            for (int i = 0; i < 1000; i++) origin.MoveOrigin(new double3(0, 0, 1e7));
            for (int i = 0; i < 1000; i++) origin.MoveOrigin(new double3(0, 0, -1e7));
            Assert.AreEqual(50f, station.transform.position.z, 0.001f);
            Object.Destroy(station.gameObject);
        }

        [UnityTest]
        public IEnumerator Far_body_drawn_at_proxy_distance()
        {
            var cam = new GameObject("cam").AddComponent<Camera>();
            var body = new GameObject("aster").AddComponent<CelestialBody>();
            body.Configure("aster", 6e6, new WorldPosition(new double3(1e10, 0, 0)), false);
            var proxy = body.gameObject.AddComponent<ScaledProxy>();
            proxy.Camera = cam;
            yield return null;
            Assert.AreEqual(40000f, proxy.Visual.position.magnitude, 1f);
            Assert.AreEqual(24f, proxy.DrawnRadius, 0.1f);
            Object.Destroy(cam.gameObject);
            Object.Destroy(body.gameObject);
        }

        [UnityTest]
        public IEnumerator Near_body_drawn_at_true_scale()
        {
            var cam = new GameObject("cam").AddComponent<Camera>();
            var body = new GameObject("rock").AddComponent<CelestialBody>();
            body.Configure("rock", 500, new WorldPosition(new double3(2000, 0, 0)), false);
            var proxy = body.gameObject.AddComponent<ScaledProxy>();
            proxy.Camera = cam;
            yield return null;
            Assert.AreEqual(2000f, proxy.Visual.position.x, 0.5f);
            Assert.AreEqual(500f, proxy.DrawnRadius, 0.01f);
            Object.Destroy(cam.gameObject);
            Object.Destroy(body.gameObject);
        }
    }
}
