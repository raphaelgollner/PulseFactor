using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PulseFactor.Tests
{
    // Testes que reproduzem os achados da revisao final (C2, C3, I1, I2, I2b).
    public class ReviewFixTests
    {
        FloatingOrigin origin;

        [SetUp]
        public void SetUp() => origin = new GameObject("origin").AddComponent<FloatingOrigin>();

        [TearDown]
        public void TearDown() => Object.Destroy(origin.gameObject);

        // C2 + I2: alvo destruido (troca de setor, asteroide esgotado, doca) deixa de ser o alvo.
        [UnityTest]
        public IEnumerator Destroyed_target_is_cleared()
        {
            var selector = new GameObject("sel").AddComponent<TargetSelector>();
            var rock = new GameObject("rock");
            rock.AddComponent<WorldAnchor>();
            var asteroid = rock.AddComponent<Asteroid>();
            selector.Select(asteroid);
            Object.Destroy(rock);
            yield return null;
            Assert.IsNull(selector.Current);
            Object.Destroy(selector.gameObject);
        }

        [UnityTest]
        public IEnumerator Dock_target_is_cleared_when_dock_is_destroyed()
        {
            var selector = new GameObject("sel").AddComponent<TargetSelector>();
            var dockGo = new GameObject("dock");
            var dock = dockGo.AddComponent<Dock>();
            dock.LandingPoint = dockGo.transform;
            selector.Select(new DockTarget(dock));
            Object.Destroy(dockGo);
            yield return null;
            yield return null;
            Assert.IsNull(selector.Current);
            Object.Destroy(selector.gameObject);
        }

        // C3: no quadro do rebase a camera continua junto da nave.
        [UnityTest]
        public IEnumerator Camera_stays_with_ship_on_rebase_frame()
        {
            var ship = ShipAssembler.Build(PlayModeFixtures.Blueprint(ShipClass.Scout), null);
            ship.AddComponent<WorldAnchor>().dynamic = true;
            origin.Focus = ship.transform;
            var rig = new GameObject("cam").AddComponent<Camera>().gameObject.AddComponent<CameraRig>();
            rig.Attach(ship);
            rig.SetMode(CameraRig.Mode.ThirdPerson);
            yield return null;
            ship.transform.position = new Vector3(2500, 0, 0);
            yield return null;
            Assert.Less(Vector3.Distance(rig.transform.position, ship.transform.position), 100f);
            Object.Destroy(ship);
            Object.Destroy(rig.gameObject);
        }

        // I1: posicao de ancora dinamica (piratas) e lida ao vivo, nao so no rebase.
        [Test]
        public void Dynamic_anchor_world_position_is_live()
        {
            var go = new GameObject("pirate");
            var anchor = go.AddComponent<WorldAnchor>();
            anchor.dynamic = true;
            go.transform.position = new Vector3(10, 0, 0);
            Assert.AreEqual(10.0, anchor.World.value.x, 1e-3);
            Object.Destroy(go);
        }

        // Entrar em dobra parte da posicao atual da nave, nao da ultima posicao gravada no rebase.
        [Test]
        public void MoveWorld_starts_from_current_position()
        {
            var go = new GameObject("ship");
            var anchor = go.AddComponent<WorldAnchor>();
            anchor.dynamic = true;
            go.transform.position = new Vector3(1500, 0, 0);
            anchor.MoveWorld(new Unity.Mathematics.double3(100, 0, 0));
            Assert.AreEqual(1600.0, anchor.World.value.x, 0.01);
            Object.Destroy(go);
        }

        // I2b: o rebase usa a posicao fisica (rigidbody), nao a interpolada do transform.
        [Test]
        public void Rebase_reads_rigidbody_position()
        {
            var go = new GameObject("ship");
            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            var anchor = go.AddComponent<WorldAnchor>();
            anchor.dynamic = true;
            body.position = new Vector3(3000, 0, 0); // ainda nao sincronizado com o transform
            origin.Rebase(origin.Origin);
            Assert.AreEqual(3000.0, origin.ToWorld(go.transform.position).value.x, 0.01);
            Object.Destroy(go);
        }
    }
}
