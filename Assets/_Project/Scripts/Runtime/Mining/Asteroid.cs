using UnityEngine;

namespace PulseFactor
{
    [RequireComponent(typeof(WorldAnchor))]
    public class Asteroid : MonoBehaviour, ITargetable
    {
        public AsteroidDeposit Deposit { get; private set; }
        WorldAnchor anchor;

        public WorldPosition Position => anchor.World;
        public string DisplayId => "asteroid";
        public bool HasResource => Deposit != null && Deposit.Resource && !Deposit.Depleted;
        public TargetKind Kind => TargetKind.Asteroid;

        void Awake() => anchor = GetComponent<WorldAnchor>();
        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);

        public void Initialize(AsteroidDeposit deposit) => Deposit = deposit;

        public void OnMined()
        {
            if (Deposit != null && Deposit.Resource && Deposit.Depleted) Destroy(gameObject);
        }
    }
}
