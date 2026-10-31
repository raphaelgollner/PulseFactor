using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PulseFactor.Tests
{
    public class JumpGateTests
    {
        // Playtest: o veu do portal tinha um colisor invisivel de um lado (copia feita antes do Destroy).
        [UnityTest]
        public IEnumerator Only_the_ring_has_a_collider_so_both_sides_are_open()
        {
            var go = new GameObject("gate");
            go.AddComponent<WorldAnchor>();
            var gate = go.AddComponent<JumpGate>();
            gate.Initialize(new GatePlacement { id = "g" }, null, null, null);
            yield return null;
            var colliders = go.GetComponentsInChildren<Collider>();
            Assert.AreEqual(1, colliders.Length, "so o anel colide");
            Assert.AreEqual("Ring", colliders[0].name);
            Object.Destroy(go);
        }
    }
}
