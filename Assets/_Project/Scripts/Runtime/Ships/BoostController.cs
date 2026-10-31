using UnityEngine;

namespace PulseFactor
{
    // Boost do Shift: +50% na velocidade maxima enquanto houver barra. Nao funciona em dobra nem docado.
    [RequireComponent(typeof(ShipController))]
    public class BoostController : MonoBehaviour
    {
        [SerializeField] float multiplierSmoothing = 3f; // a velocidade extra entra/sai suave

        public PlayerShipInput Input;
        public WarpController Warp;
        public DockingAutopilot Docking;

        public BoostTank Tank { get; private set; }
        public bool Active { get; private set; }

        ShipController ship;

        void Start()
        {
            ship = GetComponent<ShipController>();
            Tank = new BoostTank(ship.ClassDef ? ship.ClassDef.boostCapacity : 150f);
        }

        void Update()
        {
            if (Tank == null || ship.Flight == null) return;
            bool allowed = (!Warp || Warp.Drive.State == WarpState.Idle)
                           && (!Docking || Docking.Current == DockingAutopilot.Phase.Free || Docking.Current == DockingAutopilot.Phase.Authorized);
            Active = Tank.Tick(Time.deltaTime, Input && Input.BoostHeld, allowed);
            float target = Active ? BoostTank.SpeedMultiplier : 1f;
            float k = 1f - Mathf.Exp(-multiplierSmoothing * Time.deltaTime);
            ship.Flight.SpeedMultiplier = Mathf.Lerp(ship.Flight.SpeedMultiplier, target, k);
        }
    }
}
