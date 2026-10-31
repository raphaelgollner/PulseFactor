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
        VisualElement shieldFill, hullFill, fuelFill, boostFill, shieldArc, hullArc;
        VisualElement reticle, centerDot, bracket, messages, targetIcon, targetTriangle;
        Label alert, notice;
        VisualElement contractList, routeMarker, routeArrow;
        Label routeLabel;
        readonly List<Label> contractLabels = new();
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
            boostFill = Bar(bars, "bar-boost");

            credits = Text(hud, "label credits");
            testMode = Text(hud, "test-mode");
            sector = Text(hud, "sector");
            messages = Add(hud, "messages");
            alert = Text(hud, "alert");
            notice = Text(hud, "notice");
            contractList = Add(hud, "contracts");
            for (int i = 0; i < 3; i++) contractLabels.Add(Text(contractList, "contract-line"));

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
                var m = HudIcons.Create();
                hud.Add(m);
                var l = Text(hud, "marker-label");
                markers.Add((m, l));
            }
            bracket = Add(hud, "target-bracket");
            targetIcon = HudIcons.Create();
            hud.Add(targetIcon);
            targetTriangle = Add(hud, "target-tri");
            targetLabel = Text(hud, "label marker-label");
            arrow = Add(hud, "arrow");
            routeMarker = Add(hud, "route-marker");
            routeLabel = Text(hud, "label marker-label route-label");
            routeArrow = Add(hud, "arrow route-arrow");

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

            // Alerta de proximidade: no topo em terceira pessoa; no cockpit vai para o painel central.
            var prox = Player.Proximity;
            bool alertOn = prox && prox.AlertActive && !cockpit;
            alert.style.display = alertOn ? DisplayStyle.Flex : DisplayStyle.None;
            if (alertOn) alert.text = Loc.T("hud.proximity_alert", Loc.T("target." + prox.AlertBodyId));
            bool noticeOn = HudNotice.Active && !cockpit && !alertOn;
            notice.style.display = noticeOn ? DisplayStyle.Flex : DisplayStyle.None;
            if (noticeOn) notice.text = HudNotice.Text;

            contractList.style.display = full;
            UpdateContracts();
            UpdateBars();
            UpdateTexts();
            UpdateReticle();
            UpdateMarkers();
            UpdateRoute();
            ExpireMessages();
        }

        void UpdateContracts()
        {
            var service = Player.Contracts;
            var active = service ? service.Log.Active : null;
            for (int i = 0; i < contractLabels.Count; i++)
            {
                var l = contractLabels[i];
                bool show = active != null && i < active.Count;
                l.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (show) l.text = ContractLine(active[i], service.Clock.Seconds);
            }
        }

        // "ENTREGA  Ferro ×20 → Posto Avançado Apollo   28:41"
        public static string ContractLine(Contract c, double now) =>
            $"{Loc.T(c.type == ContractType.Delivery ? "contract.type_delivery" : "contract.type_requisition")}  " +
            (c.type == ContractType.Requisition
                ? $"{Loc.T("target." + c.resourceId)} {c.delivered}/{c.quantity} → {Loc.T("target." + c.destinationStationId)}"
                : $"{Loc.T("target." + c.resourceId)} ×{c.quantity} → {Loc.T("target." + c.destinationStationId)}") +
            $"   {ContractService.FormatTime(c.TimeLeft(now))}";

        void UpdateBars()
        {
            // Sem dano nesta fatia: escudo, casco e combustivel cheios.
            SetFill(shieldFill, 1f); SetFill(hullFill, 1f); SetFill(fuelFill, 1f);
            SetFill(boostFill, Player.Boost && Player.Boost.Tank != null ? Player.Boost.Tank.Fraction : 1f);
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

        string WarpText() => WarpHud.Text(Player.Warp);

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

        // Retangulos (no painel do HUD) das telas fisicas do cockpit: o que cair ali fica escondido.
        readonly List<Rect> blocked = new();

        void UpdateBlockedAreas(Camera cam)
        {
            blocked.Clear();
            bool cockpit = Player.CameraRig && Player.CameraRig.CurrentMode == CameraRig.Mode.Cockpit;
            if (!cockpit || !Player.Visual) return;
            foreach (var mount in Player.Visual.ScreenMounts)
            {
                if (!mount) continue;
                float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
                bool visible = false;
                foreach (var c in new[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) })
                {
                    var sp = cam.WorldToScreenPoint(mount.TransformPoint(c));
                    if (sp.z <= 0) continue;
                    visible = true;
                    var p = ToPanel(sp);
                    xMin = Mathf.Min(xMin, p.x); xMax = Mathf.Max(xMax, p.x);
                    yMin = Mathf.Min(yMin, p.y); yMax = Mathf.Max(yMax, p.y);
                }
                if (visible) blocked.Add(Rect.MinMaxRect(xMin, yMin, xMax, yMax));
            }
        }

        bool Hidden(Vector2 panelPoint)
        {
            foreach (var r in blocked) if (r.Contains(panelPoint)) return true;
            return false;
        }

        double ScannerRange => Player.Ship && Player.Ship.ClassDef ? Player.Ship.ClassDef.scannerRange : Scanner.DefaultRange;

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
            UpdateBlockedAreas(cam);

            int used = 0;
            foreach (var t in TargetRegistry.All)
            {
                if (used >= MaxMarkers) break;
                // Scanner: naves e asteroides so aparecem dentro do alcance; estacoes, corpos e portais sempre.
                if (ReferenceEquals(t, current)) continue;
                if (t.Kind == TargetKind.Asteroid) continue; // asteroides so aparecem quando marcados
                if (!Project(cam, camWorld, t, out var screen, out double dist)) continue;
                if (!Scanner.Detects(t.Kind, dist, ScannerRange)) continue;
                var (marker, label) = markers[used++];
                marker.style.display = DisplayStyle.Flex;
                label.style.display = DisplayStyle.Flex;
                var p = ToPanel(screen);
                if (Hidden(p))
                {
                    marker.style.display = DisplayStyle.None;
                    label.style.display = DisplayStyle.None;
                    used--;
                    continue;
                }
                HudIcons.Apply(marker, HudIcons.For(t));
                label.style.color = KindColor(t.Kind);
                Place(marker, p);
                Place(label, p);
                label.text = $"{TargetNames.Of(t)}  {HudModel.FormatDistance(dist)}";
            }
            for (int i = used; i < markers.Count; i++)
            {
                markers[i].marker.style.display = DisplayStyle.None;
                markers[i].label.style.display = DisplayStyle.None;
            }

            bracket.style.display = DisplayStyle.None;
            targetLabel.style.display = DisplayStyle.None;
            targetIcon.style.display = DisplayStyle.None;
            targetTriangle.style.display = DisplayStyle.None;
            arrow.style.display = DisplayStyle.None;
            if (current == null) return;

            var delta = current.Position - camWorld;
            double d = Unity.Mathematics.math.length(delta);
            var dir = (Vector3)(Unity.Mathematics.float3)(delta / System.Math.Max(d, 1e-3));
            var sp = cam.WorldToScreenPoint(cam.transform.position + dir * 1000f);
            bool onScreen = sp.z > 0 && sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;
            if (onScreen && Hidden(ToPanel(sp))) onScreen = false; // atras de uma tela do cockpit: so a seta na borda
            if (onScreen)
            {
                var p = ToPanel(sp);
                bracket.style.display = DisplayStyle.Flex;
                targetLabel.style.display = DisplayStyle.Flex;
                targetIcon.style.display = DisplayStyle.Flex;
                targetTriangle.style.display = DisplayStyle.Flex;
                HudIcons.Apply(targetIcon, HudIcons.For(current));
                Place(targetIcon, p);
                // Triangulo do alvo marcado: neutro branco, aliado verde, inimigo vermelho.
                var stance = HudIcons.StanceOf(current, Player.Contracts ? Player.Contracts.Reputation : null);
                foreach (var c in new[] { "tri-neutral", "tri-ally", "tri-enemy" }) targetTriangle.RemoveFromClassList(c);
                targetTriangle.AddToClassList(HudIcons.TriangleClass(stance));
                Place(targetTriangle, p + new Vector2(0, -38));
                Place(bracket, p);
                Place(targetLabel, p + new Vector2(18, 10));
                targetLabel.text = $"{TargetNames.WithKind(current)}\n{HudModel.FormatDistance(d)}";
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

        // Rota marcada no mapa: losango roxo no proximo ponto (destino ou portal do caminho).
        void UpdateRoute()
        {
            routeMarker.style.display = DisplayStyle.None;
            routeLabel.style.display = DisplayStyle.None;
            routeArrow.style.display = DisplayStyle.None;
            var route = Player.Route;
            var cam = Camera.main;
            var origin = FloatingOrigin.Instance;
            var wp = route ? route.Waypoint : null;
            if (wp == null || !cam || !origin) return;

            var camWorld = origin.ToWorld(cam.transform.position);
            var delta = wp.Position - camWorld;
            double d = Unity.Mathematics.math.length(delta);
            var dir = (Vector3)(Unity.Mathematics.float3)(delta / System.Math.Max(d, 1e-3));
            var sp = cam.WorldToScreenPoint(cam.transform.position + dir * 1000f);
            bool onScreen = sp.z > 0 && sp.x >= 0 && sp.x <= Screen.width && sp.y >= 0 && sp.y <= Screen.height;
            if (onScreen)
            {
                var p = ToPanel(sp);
                routeMarker.style.display = DisplayStyle.Flex;
                routeLabel.style.display = DisplayStyle.Flex;
                Place(routeMarker, p);
                Place(routeLabel, p + new Vector2(16, 12));
                routeLabel.text = route.WaypointIsGate
                    ? Loc.T("hud.route_via", Loc.T("target." + route.DestinationId), Loc.T("target." + wp.DisplayId), HudModel.FormatDistance(d))
                    : Loc.T("hud.route", Loc.T("target." + route.DestinationId), HudModel.FormatDistance(d));
            }
            else
            {
                var local = cam.transform.InverseTransformDirection(dir);
                var v = new Vector2(local.x, local.y);
                if (v.sqrMagnitude < 1e-6f) v = Vector2.down;
                v.Normalize();
                var center = new Vector2(Screen.width, Screen.height) * 0.5f;
                var edge = center + v * (Mathf.Min(Screen.width, Screen.height) * 0.38f);
                routeArrow.style.display = DisplayStyle.Flex;
                Place(routeArrow, ToPanel(edge));
                routeArrow.style.rotate = new Rotate(new Angle(Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg));
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
