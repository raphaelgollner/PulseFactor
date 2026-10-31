using UnityEngine;

namespace PulseFactor
{
    // Boost do Shift: sem teto de velocidade enquanto houver barra (depois desacelera aos poucos). Nao funciona em dobra nem docado.
    [RequireComponent(typeof(ShipController))]
    public class BoostController : MonoBehaviour
    {
        public PlayerShipInput Input;
        public WarpController Warp;
        public DockingAutopilot Docking;

        public BoostTank Tank { get; private set; }
        public bool Active { get; private set; }

        ShipController ship;

        void Start() => ResetTank();

        // Chamado tambem na troca de nave (capacidade do propulsor muda com a classe).
        public void ResetTank()
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
            ship.Flight.Boosting = Active; // sem teto enquanto houver barra; o FlightModel desacelera aos poucos depois
        }
    }
}
