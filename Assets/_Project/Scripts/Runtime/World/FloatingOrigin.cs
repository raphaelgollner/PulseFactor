using System;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Mantem o jogador perto da origem do Unity: quando o foco se afasta demais,
    // move a origem do mundo e reposiciona todos os WorldAnchor.
    [DefaultExecutionOrder(-1000)]
    public class FloatingOrigin : MonoBehaviour
    {
        public static FloatingOrigin Instance { get; private set; }

        public Transform Focus;
        public float rebaseDistance = 2000f;

        public WorldPosition Origin { get; private set; }

        // Deslocamento local aplicado (posicao antiga do foco).
        public event Action<Vector3> Rebased;

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void LateUpdate() => CheckRebase();

        public WorldPosition ToWorld(Vector3 local) => WorldPosition.FromLocal(local, Origin);
        public Vector3 ToLocal(WorldPosition world) => world.ToLocal(Origin);

        public void CheckRebase()
        {
            if (Focus && Focus.position.magnitude > rebaseDistance)
                Rebase(ToWorld(Focus.position));
        }

        public void MoveOrigin(double3 delta) => Rebase(Origin + delta);

        public void Rebase(WorldPosition newOrigin)
        {
            var old = Origin;
            foreach (var a in WorldAnchor.All) a.BeforeRebase(old);
            Origin = newOrigin;
            foreach (var a in WorldAnchor.All) a.Place(newOrigin);
            Physics.SyncTransforms();
            Rebased?.Invoke((float3)(newOrigin.value - old.value));
        }
    }
}
