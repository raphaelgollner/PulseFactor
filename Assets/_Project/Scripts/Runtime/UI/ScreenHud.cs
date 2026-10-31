using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // HUD de tela (terceira pessoa) e "vidro" do cockpit (so mira, marcadores e arcos).
    [RequireComponent(typeof(UIDocument))]
    public class ScreenHud : MonoBehaviour
    {
        const int MaxMarkers = 24;
        const float MessageSeconds = 3.5f;

        public PlayerContext Player;

        VisualElement root, bars, bottomLeft, bottomRight, bottomCenter, glassArcs;
        VisualElement shieldFill, hullFill, fuelFill, shieldArc, hullArc;
        VisualElement reticle, centerDot, bracket, messages;
        Label credits, testMode, sector, speed, cruise, warp, cargo, slotLabel, skillLabel, targetLabel;
        readonly List<(VisualElement marker, Label label)> markers = new();
        readonly List<(Label label, float until)> messageItems = new();
        VisualElement arrow;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>();
            root = doc.rootVisualElement;
            root.Clear();
            var sheet = Resources.Load<StyleSheet>("PulseUI");
            if (sheet) root.styleSheets.Add(sheet);
            root.pickingMode = PickingMode.Ignore;

            var hud = Add(root, "hud-root");

            bars = Add(hud, "bars");
            shieldFill = Bar(bars, "bar-shield");
            hullFill = Bar(bars, "bar-hull");
            fuelFill = Bar(bars, "bar-fuel");

            credits = Text(hud, "label credits");
            testMode = Text(hud, "test-mode");
            sector = Text(hud, "sector");
            messages = Add(hud, "messages");

            bottomCenter = Add(hud, "bottom-center");
            speed = Text(bottomCenter, "label label-big");
            cruise = Text(bottomCenter, "label label-small");
            warp = Text(bottomCenter, "label");

            bottomLeft = Add(hud, "bottom-left");
            var slot = Add(bottomLeft, "slot");
            slotLabel = Text(slot, "label");

            bottomRight = Add(hud, "bottom-right");
            cargo = Text(bottomRight, "label");
            var skill = Add(bottomRight, "slot slot-secondary");
            skillLabel = Text(skill, "label");

            for (int i = 0; i < MaxMarkers; i++)
            {
                var m = Add(hud, "marker");
                var l = Text(hud, "marker-label");
                markers.Add((m, l));
            }
            bracket = Add(hud, "target-bracket");
            targetLabel = Text(hud, "label marker-label");
            arrow = Add(hud, "arrow");

            centerDot = Add(hud, "center-dot");
            reticle = Add(hud, "reticle");
            glassArcs = Add(hud, "glass-arcs");
            shieldArc = Bar(glassArcs, "bar-shield", "arc");
            hullArc = Bar(glassArcs, "bar-hull", "arc");

            GameMessages.Posted += OnMessage;
        }

        void OnDisable() => GameMessages.Posted -= OnMessage;

        static VisualElement Add(VisualElement parent, string classes)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            foreach (var c in classes.Split(' ')) e.AddToClassList(c);
            parent.Add(e);
            return e;
        }

        static Label Text(VisualElement parent, string classes)
        {
            var l = new Label { pickingMode = PickingMode.Ignore };
            foreach (var c in classes.Split(' ')) l.AddToClassList(c);
            parent.Add(l);
            return l;
        }

        static VisualElement Bar(VisualElement parent, string fillClass, string barClass = "bar")
        {
            var bar = Add(parent, barClass);
            var fill = Add(bar, "bar-fill " + fillClass);
            return fill;
        }

        void OnMessage(string key, object[] args)
        {
            var l = Text(messages, "message");
            l.text = Loc.T(key, args);
            messageItems.Add((l, Time.unscaledTime + MessageSeconds));
            while (messageItems.Count > 4) { messageItems[0].label.RemoveFromHierarchy(); messageItems.RemoveAt(0); }
        }

        void LateUpdate()
        {
            if (Player == null || !Player.Ship || root == null) return;
            var docking = Player.Docking;
            bool docked = docking && docking.Current != DockingAutopilot.Phase.Free && docking.Current != DockingAutopilot.Phase.Authorized;
            root.style.display = docked && docking.Current == DockingAutopilot.Phase.Docked ? DisplayStyle.None : DisplayStyle.Flex;

            bool cockpit = Player.CameraRig && Player.CameraRig.CurrentMode == CameraRig.Mode.Cockpit;
            // No cockpit as informacoes vao para os paineis fisicos; no vidro so o essencial.
            var full = cockpit ? DisplayStyle.None : DisplayStyle.Flex;
            bars.style.display = full; bottomLeft.style.display = full; bottomRight.style.display = full; bottomCenter.style.display = full;
            credits.style.display = full;
            glassArcs.style.display = cockpit ? DisplayStyle.Flex : DisplayStyle.None;

            UpdateBars();
            UpdateTexts();
            UpdateReticle();
            UpdateMarkers();
            ExpireMessages();
        }

        void UpdateBars()
        {
            // Sem dano nesta fatia: escudo, casco e combustivel cheios.
            SetFill(shieldFill, 1f); SetFill(hullFill, 1f); SetFill(fuelFill, 1f);
            SetFill(shieldArc, 1f); SetFill(hullArc, 1f);
        }

        static void SetFill(VisualElement fill, float f) => fill.style.width = Length.Percent(Mathf.Clamp01(f) * 100f);

        void UpdateTexts()
        {
            var ship = Player.Ship;
            credits.text = Loc.T("hud.credits", Player.Wallet.Credits.ToString("N0"));
            testMode.text = Player.Config && Player.Config.testMode ? Loc.T("hud.test_mode") : "";
            sector.text = Player.SectorName;

            float max = ship.Flight.EffectiveMaxSpeed(ship.Cargo.LoadFraction);
            bool inWarp = Player.Warp && Player.Warp.Drive.State == WarpState.Active;
            speed.text = inWarp ? HudModel.FormatSpeed(Player.Warp.Drive.Speed) : HudModel.FormatSpeed(ship.Velocity.magnitude);
            cruise.text = ship.Flight.CruiseActive
                ? Loc.T("hud.cruise", Mathf.RoundToInt(ship.Flight.CruiseTarget))
                : Loc.T("hud.max_speed", Mathf.RoundToInt(max));
            warp.text = WarpText();

            cargo.text = Loc.T("hud.cargo", ship.Cargo.Used, ship.Cargo.Capacity);
            slotLabel.text = Loc.T("hud.slot_mining", Player.Laser && Player.Laser.Firing ? "●" : "○");
            skillLabel.text = Loc.T("hud.skill_empty");
        }

        string WarpText()
        {
            if (!Player.Warp) return "";
            var w = WarpHud.From(Player.Warp.Drive, Player.Warp.MassLocked, Player.Targets && Player.Targets.Current != null);
            return w.state switch
            {
                WarpHudState.Charging => Loc.T("hud.warp_charging", Mathf.RoundToInt(w.progress * 100)),
                WarpHudState.Active => Loc.T("hud.warp_active"),
                WarpHudState.MassLocked => Loc.T("hud.warp_masslocked"),
                WarpHudState.NoTarget => Loc.T("hud.warp_no_target"),
                _ => Loc.T("hud.warp_ready"),
            };
        }

        float PanelScale => root.panel != null && Screen.width > 0 ? root.layout.width / Screen.width : 1f;

        Vector2 ToPanel(Vector2 screen) => new Vector2(screen.x, Screen.height - screen.y) * PanelScale;

        static void Place(VisualElement e, Vector2 p)
        {
            e.style.left = p.x;
            e.style.top = p.y;
        }

        void UpdateReticle()
        {
            Place(centerDot, ToPanel(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)));
            var r = Player.Input ? Player.Input.ReticleScreenPosition : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Place(reticle, ToPanel(r));
            Place(glassArcs, ToPanel(r));
        }

        static Color KindColor(TargetKind k) => k switch
        {
            TargetKind.Station => new Color(0.3f, 0.9f, 1f),
            TargetKind.Gate => new Color(0.6f, 0.7f, 1f),
            TargetKind.Pirate => new Color(1f, 0.3f, 0.25f),
            TargetKind.Asteroid => new Color(1f, 0.75f, 0.3f),
            _ => new Color(0.9f, 0.9f, 0.9f),
        };

        void UpdateMarkers()
        {
            var cam = Camera.main;
            var origin = FloatingOrigin.Instance;
            if (!cam || !origin) return;
            var camWorld = origin.ToWorld(cam.transform.position);
            var current = Player.Targets ? Player.Targets.Current : null;

            int used = 0;
            foreach (var t in TargetRegistry.All)
            {
                if (used >= MaxMarkers) break;
                // Asteroides so aparecem quando sao o alvo; o resto sempre (estacoes, gates, corpos, piratas).
                if (t.Kind == TargetKind.Asteroid && !ReferenceEquals(t, current)) continue;
                if (ReferenceEquals(t, current)) continue;
                if (!Project(cam, camWorld, t, out var screen, out double dist)) continue;
                var (marker, label) = markers[used++];
                marker.style.display = DisplayStyle.Flex;
                label.style.display = DisplayStyle.Flex;
                var c = KindColor(t.Kind);
                marker.style.borderTopColor = c; marker.style.borderBottomColor = c;
                marker.style.borderLeftColor = c; marker.style.borderRightColor = c;
                label.style.color = c;
                var p = ToPanel(screen);
                Place(marker, p);
                Place(label, p);
                label.text = $"{Loc.T("target." + t.DisplayId)}  {HudModel.FormatDistance(dist)}";
            }
            for (int i = used; i < markers.Count; i++)
            {
                markers[i].marker.style.display = DisplayStyle.None;
                markers[i].label.style.display = DisplayStyle.None;
            }

            bracket.style.display = DisplayStyle.None;
            targetLabel.style.display = DisplayStyle.None;
            arrow.style.display = DisplayStyle.None;
            if (current == null) return;

            var delta = current.Position - camWorld;
            double d = Unity.Mathematics.math.length(delta);
            var dir = (Vector3)(Unity.Mathematics.float3)(delta / System.Math.Max(d, 1e-3));
            var sp = cam.WorldToScreenPoint(cam.transform.position + dir * 1000f);
            bool onScreen = sp.z > 0 && sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;
            if (onScreen)
            {
                var p = ToPanel(sp);
                bracket.style.display = DisplayStyle.Flex;
                targetLabel.style.display = DisplayStyle.Flex;
                Place(bracket, p);
                Place(targetLabel, p + new Vector2(18, 10));
                targetLabel.text = $"{Loc.T("target." + current.DisplayId)}\n{HudModel.FormatDistance(d)}";
            }
            else
            {
                // Seta na borda apontando para o alvo fora da tela.
                var local = cam.transform.InverseTransformDirection(dir);
                var v = new Vector2(local.x, local.y);
                if (v.sqrMagnitude < 1e-6f) v = Vector2.down;
                v.Normalize();
                var center = new Vector2(Screen.width, Screen.height) * 0.5f;
                var edge = center + v * (Mathf.Min(Screen.width, Screen.height) * 0.42f);
                arrow.style.display = DisplayStyle.Flex;
                arrow.style.borderBottomColor = KindColor(current.Kind);
                Place(arrow, ToPanel(edge));
                arrow.style.rotate = new Rotate(new Angle(Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg));
            }
        }

        static bool Project(Camera cam, WorldPosition camWorld, ITargetable t, out Vector2 screen, out double dist)
        {
            var delta = t.Position - camWorld;
            dist = Unity.Mathematics.math.length(delta);
            screen = default;
            if (dist < 1) return false;
            var dir = (Vector3)(Unity.Mathematics.float3)(delta / dist);
            var sp = cam.WorldToScreenPoint(cam.transform.position + dir * 1000f);
            if (sp.z <= 0 || sp.x < 0 || sp.x > Screen.width || sp.y < 0 || sp.y > Screen.height) return false;
            screen = sp;
            return true;
        }

        void ExpireMessages()
        {
            for (int i = messageItems.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < messageItems[i].until) continue;
                messageItems[i].label.RemoveFromHierarchy();
                messageItems.RemoveAt(i);
            }
        }
    }
}
