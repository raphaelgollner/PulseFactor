using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    // Objeto que vive no espaco do mundo e e reposicionado a cada rebase da origem.
    // Estatico: a posicao em double e a verdade. Dinamico (naves): a verdade e o transform.
    public class WorldAnchor : MonoBehaviour
    {
        public static readonly List<WorldAnchor> All = new();

        public bool dynamic;

        WorldPosition world;
        bool worldAuthoritative; // MoveWorld/SetWorld neste frame: nao reler do transform
        Rigidbody body;

        public WorldPosition World => world;

        void Awake() => body = GetComponent<Rigidbody>();

        void OnEnable()
        {
            All.Add(this);
            var o = FloatingOrigin.Instance;
            if (!worldAuthoritative) world = WorldPosition.FromLocal(transform.position, o ? o.Origin : default);
        }

        void OnDisable() => All.Remove(this);

        public void SetWorld(WorldPosition position, FloatingOrigin origin = null)
        {
            world = position;
            worldAuthoritative = true;
            origin ??= FloatingOrigin.Instance;
            Place(origin ? origin.Origin : default);
        }

        // Move em double sem passar pelo float (usado pela dobra).
        public void MoveWorld(double3 delta)
        {
            world = world + delta;
            worldAuthoritative = true;
        }

        public WorldPosition CurrentWorld(WorldPosition origin) =>
            dynamic && !worldAuthoritative ? WorldPosition.FromLocal(transform.position, origin) : world;

        internal void BeforeRebase(WorldPosition oldOrigin)
        {
            world = CurrentWorld(oldOrigin);
        }

        internal void Place(WorldPosition origin)
        {
            var local = world.ToLocal(origin);
            transform.position = local;
            if (body) body.position = local;
            worldAuthoritative = false;
        }
    }
}
