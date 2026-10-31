namespace PulseFactor
{
    // Referencias do jogador compartilhadas pela interface.
    public class PlayerContext
    {
        public ShipController Ship;
        public AssembledShip Visual;
        public PlayerShipInput Input;
        public WarpController Warp;
        public TargetSelector Targets;
        public DockingAutopilot Docking;
        public MiningLaser Laser;
        public Wallet Wallet;
        public SectorManager Sectors;
        public CameraRig CameraRig;
        public GameConfig Config;

        public string SectorName => Sectors && Sectors.Current
            ? Loc.T("sector.name", Loc.T("body." + Sectors.Current.bodies[0].id))
            : "";
    }
}
