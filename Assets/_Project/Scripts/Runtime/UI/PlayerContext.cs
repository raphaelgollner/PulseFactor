namespace PulseFactor
{
    // Referencias do jogador compartilhadas pela interface.
    public class PlayerContext
    {
        public ShipController Ship;
        public AssembledShip Visual;
        public PlayerShipInput Input;
        public WarpController Warp;
        public ProximityGuard Proximity;
        public TargetSelector Targets;
        public DockingAutopilot Docking;
        public MiningLaser Laser;
        public BoostController Boost;
        public Wallet Wallet;
        public SectorManager Sectors;
        public CameraRig CameraRig;
        public GameConfig Config;
        public ContractService Contracts;
        public RouteService Route;
        public FleetService Fleet;

        // Troca de nave ou modificacao (pecas, classe, carga, cockpit): a interface se reconecta.
        public event System.Action ShipChanged;
        public void RaiseShipChanged() => ShipChanged?.Invoke();

        // Nome da nave (max. 18 caracteres; definido na compra, renomeavel no terminal por enquanto).
        public const int MaxShipNameLength = 18;
        public string ShipName = "PULSE-01";

        public static string CleanShipName(string name)
        {
            name = (name ?? "").Trim().ToUpperInvariant();
            return name.Length > MaxShipNameLength ? name.Substring(0, MaxShipNameLength) : name;
        }

        public string SectorName => Sectors && Sectors.Current
            ? Loc.T("sector.name", Loc.T("body." + Sectors.Current.bodies[0].id))
            : "";
    }
}
