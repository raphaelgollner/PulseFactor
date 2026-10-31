using UnityEngine;

namespace PulseFactor
{
    // Sol, planeta ou lua. A posicao em double e a verdade; o visual e posicionado pelo ScaledProxy.
    public class CelestialBody : MonoBehaviour, ITargetable
    {
        [SerializeField] string id;
        [SerializeField] double radius;
        [SerializeField] bool emitsLight;
        WorldPosition position;

        public string Id => id;
        public double Radius => radius;
        public bool EmitsLight => emitsLight;
        public WorldPosition Position => position;
        public string DisplayId => id;
        public TargetKind Kind => TargetKind.Body;

        public void Configure(string id, double radius, WorldPosition position, bool emitsLight)
        {
            this.id = id;
            this.radius = radius;
            this.position = position;
            this.emitsLight = emitsLight;
            name = id;
        }

        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);

        public MassSource AsMassSource() => MassSource.Body(position.value, radius);
    }
}
