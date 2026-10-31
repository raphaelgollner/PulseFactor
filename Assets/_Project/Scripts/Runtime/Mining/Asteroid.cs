using UnityEngine;

namespace PulseFactor
{
    [RequireComponent(typeof(WorldAnchor))]
    public class Asteroid : MonoBehaviour, ITargetable
    {
        public AsteroidDeposit Deposit { get; private set; }
        WorldAnchor anchor;

        public WorldPosition Position => anchor.World;
        public string DisplayId => Deposit != null && Deposit.Resource ? Deposit.Resource.id : "asteroid";
        public TargetKind Kind => TargetKind.Asteroid;

        void Awake() => anchor = GetComponent<WorldAnchor>();
        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);

        public void Initialize(AsteroidDeposit deposit) => Deposit = deposit;

        public void OnMined()
        {
            if (Deposit.Depleted) Destroy(gameObject);
        }
    }
}
