using System.Globalization;

namespace PulseFactor
{
    public enum WarpHudState { Ready, NoTarget, MassLocked, Charging, Active }

    public struct WarpHud
    {
        public WarpHudState state;
        public float progress;

        public static WarpHud From(WarpDrive d, bool massLocked, bool hasTarget) => d.State switch
        {
            WarpState.Charging => new WarpHud { state = WarpHudState.Charging, progress = d.ChargeProgress },
            WarpState.Active or WarpState.Exiting => new WarpHud { state = WarpHudState.Active, progress = 1f },
            _ => new WarpHud
            {
                state = massLocked ? WarpHudState.MassLocked : !hasTarget ? WarpHudState.NoTarget : WarpHudState.Ready,
            },
        };
    }

    public static class HudModel
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static string FormatDistance(double meters)
        {
            if (meters < 1000) return ((long)meters).ToString(Inv) + " m";
            if (meters < 1e6) return (meters / 1000).ToString("0.0", Inv) + " km";
            if (meters < 0.1 * WorldPosition.LightSecond) return (meters / 1e6).ToString("0.0", Inv) + " Mm";
            return (meters / WorldPosition.LightSecond).ToString("0.00", Inv) + " Ls";
        }

        public static string FormatSpeed(double metersPerSecond)
        {
            if (metersPerSecond < 1000) return ((long)metersPerSecond).ToString(Inv) + " m/s";
            return FormatDistance(metersPerSecond) + "/s";
        }
    }
}
