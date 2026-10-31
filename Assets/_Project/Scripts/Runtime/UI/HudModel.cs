using System.Globalization;

namespace PulseFactor
{
    public enum WarpHudState { Ready, NoTarget, MassLocked, Charging, Active }

    public struct WarpHud
    {
        public WarpHudState state;
        public float progress;
        public int level;

        public static WarpHud From(WarpDrive d, bool massLocked, bool hasTarget) => d.State switch
        {
            WarpState.Charging => new WarpHud { state = WarpHudState.Charging, progress = d.ChargeProgress, level = d.Level },
            WarpState.Active or WarpState.Exiting => new WarpHud { state = WarpHudState.Active, progress = 1f, level = d.Level },
            _ => new WarpHud
            {
                state = massLocked ? WarpHudState.MassLocked : !hasTarget ? WarpHudState.NoTarget : WarpHudState.Ready,
                level = d.Level,
            },
        };

        // Texto localizado do estado da dobra (HUD e paineis).
        public static string Text(WarpController warp)
        {
            if (!warp) return "";
            var w = From(warp.Drive, warp.MassLocked, warp.Target != null);
            return w.state switch
            {
                WarpHudState.Charging => Loc.T("hud.warp_charging", UnityEngine.Mathf.RoundToInt(w.progress * 100)),
                WarpHudState.Active => Loc.T("hud.warp_active", w.level),
                WarpHudState.MassLocked => Loc.T("hud.warp_masslocked"),
                WarpHudState.NoTarget => Loc.T("hud.warp_no_target"),
                _ => Loc.T("hud.warp_ready"),
            };
        }
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
