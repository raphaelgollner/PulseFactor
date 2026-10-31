using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Paineis fisicos do cockpit: cada ScreenMount recebe um painel de UI renderizado em textura.
    public class CockpitPanels : MonoBehaviour
    {
        public PlayerContext Player;
        public PanelSettings BaseSettings;

        readonly List<Panel> panels = new();

        class Panel
        {
            public UIDocument doc;
            public RenderTexture texture;
            public Label title, a, b, c, d;
        }

        public void Build(AssembledShip ship)
        {
            Clear();
            foreach (var mount in ship.ScreenMounts) panels.Add(Create(mount));
        }

        void Clear()
        {
            foreach (var p in panels)
            {
                if (p.doc) Destroy(p.doc.gameObject);
                if (p.texture) p.texture.Release();
            }
            panels.Clear();
        }

        Panel Create(Transform mount)
        {
            bool center = mount.name.EndsWith("Center");
            var rt = new RenderTexture(center ? 768 : 512, center ? 352 : 352, 0) { name = mount.name };
            var settings = Instantiate(BaseSettings);
            settings.targetTexture = rt;
            settings.clearColor = true;
            settings.colorClearValue = new Color(0.02f, 0.06f, 0.08f, 1f);

            var go = new GameObject("Panel_" + mount.name);
            go.transform.SetParent(transform, false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = settings;

            var display = mount.GetComponentInChildren<Renderer>();
            if (display)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetTexture("_BaseMap", rt);
                display.sharedMaterial = mat;
            }

            var root = doc.rootVisualElement;
            var sheet = Resources.Load<StyleSheet>("PulseUI");
            if (sheet) root.styleSheets.Add(sheet);
            var panel = new VisualElement();
            panel.AddToClassList("panel");
            root.Add(panel);
            var p = new Panel { doc = doc, texture = rt };
            p.title = AddLabel(panel, "panel-title");
            p.a = AddLabel(panel, "label");
            p.b = AddLabel(panel, "label");
            p.c = AddLabel(panel, "label");
            p.d = AddLabel(panel, "label");
            go.name = mount.name;
            return p;
        }

        static Label AddLabel(VisualElement parent, string cls)
        {
            var l = new Label();
            l.AddToClassList(cls);
            parent.Add(l);
            return l;
        }

        void LateUpdate()
        {
            if (Player == null || !Player.Ship) return;
            var ship = Player.Ship;
            var target = Player.Targets ? Player.Targets.Current : null;
            var origin = FloatingOrigin.Instance;

            foreach (var p in panels)
            {
                var name = p.doc.gameObject.name;
                if (name.EndsWith("Center"))
                {
                    var prox = Player.Proximity;
                    bool alert = prox && prox.AlertActive;
                    p.title.text = alert ? Loc.T("hud.proximity_alert", Loc.T("target." + prox.AlertBodyId)) : Loc.T("panel.status");
                    p.title.EnableInClassList("panel-alert", alert);
                    p.a.text = Loc.T("panel.shield_hull", 100, 100);
                    p.b.text = Loc.T("hud.speed", HudModel.FormatSpeed(Player.Warp && Player.Warp.Drive.State == WarpState.Active ? Player.Warp.Drive.Speed : ship.Velocity.magnitude));
                    p.c.text = target != null && origin
                        ? Loc.T("panel.target", Loc.T("target." + target.DisplayId), HudModel.FormatDistance(WorldPosition.Distance(origin.ToWorld(ship.transform.position), target.Position)))
                        : Loc.T("panel.no_target");
                    p.d.text = HudNotice.Active ? HudNotice.Text : Player.SectorName;
                    p.d.EnableInClassList("panel-notice", HudNotice.Active);
                }
                else if (name.EndsWith("Left"))
                {
                    p.title.text = Loc.T("panel.weapons");
                    p.a.text = Loc.T("hud.slot_mining", Player.Laser && Player.Laser.Firing ? "●" : "○");
                    p.b.text = Loc.T("hud.cargo", ship.Cargo.Used, ship.Cargo.Capacity);
                    p.c.text = Loc.T("hud.credits", Player.Wallet.Credits.ToString("N0"));
                    p.d.text = "";
                }
                else
                {
                    p.title.text = Loc.T("panel.systems");
                    p.a.text = Loc.T("hud.skill_empty");
                    p.b.text = WarpHud.Text(Player.Warp);
                    p.c.text = ship.Flight.CruiseActive ? Loc.T("hud.cruise", Mathf.RoundToInt(ship.Flight.CruiseTarget)) : "";
                    p.d.text = "";
                }
            }
        }

        void OnDestroy() => Clear();
    }
}
