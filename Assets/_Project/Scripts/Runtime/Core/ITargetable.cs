namespace PulseFactor
{
    public enum TargetKind { Body, Station, Asteroid, Pirate, Ship, Gate, Field }

    public interface ITargetable
    {
        WorldPosition Position { get; }
        string DisplayId { get; }
        TargetKind Kind { get; }
    }
}
