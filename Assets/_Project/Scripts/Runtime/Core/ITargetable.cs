namespace PulseFactor
{
    public enum TargetKind { Body, Station, Asteroid, Pirate, Ship }

    public interface ITargetable
    {
        WorldPosition Position { get; }
        string DisplayId { get; }
        TargetKind Kind { get; }
    }
}
