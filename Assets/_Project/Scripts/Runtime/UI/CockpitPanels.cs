using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Paineis fisicos do cockpit (UI desenhada em textura nos ScreenMount_*):
    // baixo: Central (status + sistema), Esquerdo (alvo), Direito (abas); cima: TopLeft (hora), TopRight (nave).
    // O conteudo e o mesmo em toda nave; o layout se adapta ao tamanho de cada tela.
    public class CockpitPanels : MonoBehaviour
    {
        public enum RightTab { Messages, Contracts, Reputation, Modules }

        public PlayerContext Player;
        public PanelSettings BaseSettings;

        public RightTab Tab { get; private set; } = RightTab.Messages;
        public bool MouseMode { get; private set; }

        readonly List<Panel> panels = new();

        class Panel
        {
            public string kind;
            public UIDocument doc;
            public RenderTexture texture;
            public Transform mount;
            public VisualElement root;
            public readonly Dictionary<string, Label> labels = new();
            public readonly Dictionary<string, VisualElement> fills = new();
            public readonly List<Button> tabs = new();
            public VisualElement list;
        }

        public void Build(AssembledShip ship)
        {
            Clear();
            foreach (var mount in ship.ScreenMounts) panels.Add(Create(mount));
            MessageLog.Changed -= MarkDirty;
            MessageLog.Changed += MarkDirty;
        }

        void OnDestroy()
        {
            MessageLog.Changed -= MarkDirty;
            Clear();
        }

        void Clear()
        {
            foreach (var p in panels)
            {
                if (p.doc) Destroy(p.doc.gameObject);
                if (p.texture) { p.texture.Release(); Destroy(p.texture); }
            }
            panels.Clear();
        }

        bool listDirty = true;
        void MarkDirty() => listDirty = true;

        static string KindOf(Transform mount) => mount.name.Replace("ScreenMount_", "");

        static Vector2Int SizeFor(string kind) => kind switch
        {
            "Center" => new Vector2Int(900, 600),
            "Left" or "Right" => new Vector2Int(640, 480),
            _ => new Vector2Int(512, 192),
        };

        Panel Create(Transform mount)
        {
            var kind = KindOf(mount);
            var size = SizeFor(kind);
            var rt = new RenderTexture(size.x, size.y, 0) { name = mount.name };
            var settings = Instantiate(BaseSettings);
            settings.targetTexture = rt;
            settings.clearColor = true;
            settings.colorClearValue = new Color(0.02f, 0.06f, 0.08f, 1f);

            var p = new Panel { kind = kind, texture = rt, mount = mount };
            // Painel em textura: nao recebe cliques da tela (senao "rouba" os do terminal). So no modo mouse (F),
            // mirando a propria tela fisica, o painel da direita aceita clique (abas).
            settings.SetScreenToPanelSpaceFunction(_ => ScreenToPanel(p));

            var go = new GameObject("Panel_" + mount.name);
            go.transform.SetParent(transform, false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = settings;
            p.doc = doc;

            var display = mount.GetComponentInChildren<Renderer>();
            if (display)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                mat.SetTexture("_BaseMap", rt);
                display.sharedMaterial = mat;
            }

            p.root = new VisualElement();
            p.root.AddToClassList("panel");
            p.root.AddToClassList("panel-" + kind.ToLowerInvariant());
            var sheet = Resources.Load<StyleSheet>("PulseUI");
            if (sheet) doc.rootVisualElement.styleSheets.Add(sheet);
            doc.rootVisualElement.Add(p.root);
            Layout(p);
            return p;
        }

        // ---------- Layouts ----------

        void Layout(Panel p)
        {
            switch (p.kind)
            {
                case "Center":
                    Title(p, "status_title");
                    BarRow(p, "hull", "bar-hull");
                    BarRow(p, "shield", "bar-shield");
                    BarRow(p, "boost", "bar-boost");
                    Line(p, "speed");
                    Title(p, "system_title");
                    Line(p, "system");
                    Line(p, "sector");
                    Line(p, "power");
                    Line(p, "corp");
                    break;
                case "Left":
                    Title(p, "target_title");
                    Line(p, "target");
                    Line(p, "distance");
                    Line(p, "target_hull");
                    Line(p, "target_shield");
                    Line(p, "affiliation");
                    break;
                case "Right":
                    var tabs = new VisualElement();
                    tabs.AddToClassList("panel-tabs");
                    p.root.Add(tabs);
                    foreach (RightTab t in Enum.GetValues(typeof(RightTab)))
                    {
                        var tab = t;
                        var b = new Button(() => SetTab(tab));
                        b.AddToClassList("panel-tab");
                        tabs.Add(b);
                        p.tabs.Add(b);
                    }
                    Title(p, "tab_title");
                    p.list = new VisualElement();
                    p.list.AddToClassList("panel-list");
                    p.root.Add(p.list);
                    break;
                case "TopLeft":
                    p.root.AddToClassList("panel-centered");
                    Line(p, "clock", "panel-clock");
                    break;
                case "TopRight":
                    p.root.AddToClassList("panel-centered");
                    Line(p, "ship_name", "panel-ship-name");
                    Line(p, "ship_class", "panel-ship-sub");
                    Line(p, "ship_maker", "panel-ship-sub");
                    break;
            }
        }

        static void Title(Panel p, string key)
        {
            var l = new Label();
            l.AddToClassList("panel-title");
            p.root.Add(l);
            p.labels[key] = l;
        }

        static void Line(Panel p, string key, string cls = "label")
        {
            var l = new Label();
            l.AddToClassList(cls);
            p.root.Add(l);
            p.labels[key] = l;
        }

        static void BarRow(Panel p, string key, string fillClass)
        {
            Line(p, key + "_label", "panel-bar-label");
            var bar = new VisualElement();
            bar.AddToClassList("panel-bar");
            var fill = new VisualElement();
            fill.AddToClassList("bar-fill");
            fill.AddToClassList(fillClass);
            bar.Add(fill);
            p.root.Add(bar);
            p.fills[key] = fill;
        }

        // ---------- Abas e modo mouse ----------

        public void SetTab(RightTab t)
        {
            Tab = t;
            listDirty = true;
        }

        // 6 = aba a direita, 7 = aba a esquerda (sem dar a volta).
        public void NextTab() => SetTab((RightTab)Mathf.Min((int)Tab + 1, (int)RightTab.Modules));
        public void PrevTab() => SetTab((RightTab)Mathf.Max((int)Tab - 1, (int)RightTab.Messages));

        public void SetMouseMode(bool on)
        {
            MouseMode = on;
            UnityEngine.Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
            UnityEngine.Cursor.visible = on;
        }

        // Converte a posicao do mouse para o painel: raio da camera ate o plano da tela fisica.
        Vector2 ScreenToPanel(Panel p)
        {
            var invalid = new Vector2(float.NaN, float.NaN);
            var cam = Camera.main;
            if (!MouseMode || p.kind != "Right" || !cam || Mouse.current == null || !p.mount) return invalid;
            var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(p.mount.forward, p.mount.position);
            if (!plane.Raycast(ray, out float t)) return invalid;
            var local = p.mount.InverseTransformPoint(ray.GetPoint(t));
            if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.y) > 0.5f) return invalid;
            return new Vector2((local.x + 0.5f) * p.texture.width, (0.5f - local.y) * p.texture.height);
        }

        // ---------- Conteudo ----------

        float listTimer;

        void LateUpdate()
        {
            if (Player == null || !Player.Ship) return;
            // Modo mouse so faz sentido olhando as telas do cockpit.
            if (MouseMode && Player.CameraRig && Player.CameraRig.CurrentMode != CameraRig.Mode.Cockpit) SetMouseMode(false);

            listTimer += Time.unscaledDeltaTime;
            bool refreshList = listDirty || listTimer > 1f; // prazos dos contratos correm
            if (refreshList) { listTimer = 0f; listDirty = false; }

            foreach (var p in panels)
            {
                switch (p.kind)
                {
                    case "Center": UpdateCenter(p); break;
                    case "Left": UpdateLeft(p); break;
                    case "Right": if (refreshList) UpdateRight(p); break;
                    case "TopLeft": p.labels["clock"].text = DateTime.Now.ToString("HH:mm:ss"); break;
                    case "TopRight": UpdateShip(p); break;
                }
            }
        }

        static void SetFill(VisualElement fill, float f) => fill.style.width = Length.Percent(Mathf.Clamp01(f) * 100f);

        void UpdateCenter(Panel p)
        {
            var ship = Player.Ship;
            var L = p.labels;
            L["status_title"].text = Loc.T("panel.status_title");
            L["hull_label"].text = Loc.T("panel.hull");
            L["shield_label"].text = Loc.T("panel.shields");
            L["boost_label"].text = Loc.T("panel.boost");
            SetFill(p.fills["hull"], 1f);   // sem dano nesta fase
            SetFill(p.fills["shield"], 1f);
            SetFill(p.fills["boost"], Player.Boost && Player.Boost.Tank != null ? Player.Boost.Tank.Fraction : 1f);
            bool warp = Player.Warp && Player.Warp.Drive.State == WarpState.Active;
            L["speed"].text = Loc.T("panel.speed", HudModel.FormatSpeed(warp ? Player.Warp.Drive.Speed : ship.Velocity.magnitude));
            L["system_title"].text = Loc.T("panel.system_title");
            L["system"].text = Loc.T("panel.system_name", Loc.T("system.name"));
            var sector = Player.Sectors ? Player.Sectors.Current : null;
            L["sector"].text = Loc.T("panel.sector_name", Player.SectorName);
            L["power"].text = Loc.T("panel.power", Loc.T("corp.federation"));
            L["corp"].text = Loc.T("panel.sector_corp", sector && !string.IsNullOrEmpty(sector.dominantCorporationId)
                ? Loc.T("corp." + sector.dominantCorporationId) : "—");
        }

        void UpdateLeft(Panel p)
        {
            var L = p.labels;
            var target = Player.Targets ? Player.Targets.Current : null;
            if (target == null && Player.Route) target = Player.Route.Waypoint;
            L["target_title"].text = Loc.T("panel.target_title");
            if (target == null)
            {
                L["target"].text = Loc.T("panel.no_target");
                L["distance"].text = L["target_hull"].text = L["target_shield"].text = L["affiliation"].text = "";
                L["affiliation"].RemoveFromClassList("panel-hostile");
                return;
            }
            var origin = FloatingOrigin.Instance;
            double dist = origin ? WorldPosition.Distance(origin.ToWorld(Player.Ship.transform.position), target.Position) : 0;
            L["target"].text = Loc.T("panel.target_name", Loc.T("target." + target.DisplayId));
            L["distance"].text = Loc.T("panel.target_distance", HudModel.FormatDistance(dist));
            bool isShip = target.Kind is TargetKind.Pirate or TargetKind.Ship;
            L["target_hull"].text = isShip ? Loc.T("panel.target_hull", 100) : "";   // sem dano nesta fase
            L["target_shield"].text = isShip ? Loc.T("panel.target_shield", 100) : "";
            bool pirate = target.Kind == TargetKind.Pirate;
            string affiliation = pirate ? Loc.T("panel.pirate")
                : target is Station s && s.Def ? Loc.T("corp." + s.Def.CorpId)
                : Loc.T("panel.neutral");
            L["affiliation"].text = Loc.T("panel.affiliation", affiliation);
            L["affiliation"].EnableInClassList("panel-hostile", pirate);
        }

        void UpdateRight(Panel p)
        {
            var names = new[] { "panel.tab_messages", "panel.tab_contracts", "panel.tab_reputation", "panel.tab_modules" };
            for (int i = 0; i < p.tabs.Count; i++)
            {
                p.tabs[i].text = Loc.T(names[i]);
                p.tabs[i].EnableInClassList("panel-tab-active", i == (int)Tab);
            }
            p.labels["tab_title"].text = Loc.T(names[(int)Tab] + "_title");
            p.list.Clear();
            switch (Tab)
            {
                case RightTab.Messages:
                    foreach (var (time, text) in MessageLog.Entries)
                    {
                        Add(p.list, $"[{time:HH:mm:ss}] {text}", "panel-feed");
                        if (p.list.childCount >= 9) break;
                    }
                    break;
                case RightTab.Contracts:
                    var service = Player.Contracts;
                    if (!service || service.Log.Active.Count == 0) { Add(p.list, Loc.T("panel.no_contracts")); break; }
                    foreach (var c in service.Log.Active) Add(p.list, ScreenHud.ContractLine(c, service.Clock.Seconds), "panel-feed");
                    break;
                case RightTab.Reputation:
                    var rep = Player.Contracts ? Player.Contracts.Reputation : null;
                    foreach (var corp in new[] { "nova", "magna", "atlas", "federation" })
                        Add(p.list, Loc.T("panel.rep_line", Loc.T("corp." + corp), rep?.Get(corp) ?? 0,
                            Mathf.RoundToInt((rep?.DiscountFor(corp) ?? 0f) * 100)));
                    break;
                case RightTab.Modules:
                    var def = Player.Ship.ClassDef;
                    Add(p.list, Loc.T("panel.slot_mining"));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_generator"), "T1"));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_engine"), "T1"));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_hull"),
                        def && def.startingHull ? $"{def.startingHull.baseHp} HP" : "—"));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_booster"), def ? $"{def.boostCapacity:0}" : "150"));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_scanner"),
                        HudModel.FormatDistance(def ? def.scannerRange : Scanner.DefaultRange)));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_skill"), "—"));
                    break;
            }
        }

        void UpdateShip(Panel p)
        {
            var bp = Player.Visual ? Player.Visual.Blueprint : null;
            p.labels["ship_name"].text = Player.ShipName;
            p.labels["ship_class"].text = bp != null && bp.body && bp.body.classDef ? Loc.T("class." + bp.body.classDef.shipClass) : "";
            p.labels["ship_maker"].text = bp != null && bp.body && bp.body.manufacturer ? Loc.T("maker." + bp.body.manufacturer.id) : "";
        }

        static void Add(VisualElement list, string text, string cls = "label")
        {
            var l = new Label(text);
            l.AddToClassList(cls);
            list.Add(l);
        }
    }
}
