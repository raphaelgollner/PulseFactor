using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Paineis fisicos do cockpit (UI desenhada em textura nos ScreenMount_*):
    // baixo: Central (minimapa/radar), Esquerdo (alvo), Direito (abas); cima: TopLeft (feed), TopRight (sistema).
    // O conteudo e o mesmo em toda nave; o layout se adapta ao tamanho de cada tela.
    public class CockpitPanels : MonoBehaviour
    {
        public enum RightTab { Ship, Cargo, Contracts, Reputation, Modules, Slots }

        static readonly string[] TabKeys =
            { "panel.tab_ship", "panel.tab_cargo", "panel.tab_contracts", "panel.tab_reputation", "panel.tab_modules", "panel.tab_slots" };

        public PlayerContext Player;
        public PanelSettings BaseSettings;

        public RightTab Tab { get; private set; } = RightTab.Ship;
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
            public readonly List<Button> tabs = new();
            public VisualElement list;
            public VisualElement radar;
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
            // mirando a propria tela fisica, o painel da direita aceita clique (abas e encaixes).
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
                    Title(p, "radar_title");
                    p.radar = new VisualElement();
                    p.radar.AddToClassList("panel-radar");
                    p.radar.generateVisualContent += ctx => DrawRadar(ctx, p.radar);
                    p.root.Add(p.radar);
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
                    p.list = new VisualElement();
                    p.list.AddToClassList("panel-list");
                    p.root.Add(p.list);
                    break;
                case "TopRight":
                    Line(p, "system", "panel-small");
                    Line(p, "sector", "panel-small");
                    Line(p, "power", "panel-small");
                    Line(p, "corp", "panel-small");
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

        // ---------- Abas e modo mouse ----------

        public void SetTab(RightTab t)
        {
            Tab = t;
            listDirty = true;
        }

        // 6 = aba a direita, 7 = aba a esquerda (sem dar a volta).
        public void NextTab() => SetTab((RightTab)Mathf.Min((int)Tab + 1, (int)RightTab.Slots));
        public void PrevTab() => SetTab((RightTab)Mathf.Max((int)Tab - 1, (int)RightTab.Ship));

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
            bool refresh = listDirty || listTimer > 0.5f; // barras, carga e prazos mudam
            if (refresh) { listTimer = 0f; listDirty = false; }

            foreach (var p in panels)
            {
                switch (p.kind)
                {
                    case "Center":
                        p.labels["radar_title"].text = Loc.T("panel.radar_title");
                        p.radar.MarkDirtyRepaint();
                        break;
                    case "Left": UpdateLeft(p); break;
                    case "Right": if (refresh) UpdateRight(p); break;
                    case "TopLeft": if (refresh) UpdateFeed(p); break;
                    case "TopRight": UpdateSystem(p); break;
                }
            }
        }

        void UpdateFeed(Panel p)
        {
            p.list.Clear();
            foreach (var (time, text) in MessageLog.Entries)
            {
                Add(p.list, $"[{time:HH:mm:ss}] {text}", "panel-feed-small");
                if (p.list.childCount >= 5) break;
            }
        }

        void UpdateSystem(Panel p)
        {
            var L = p.labels;
            var sector = Player.Sectors ? Player.Sectors.Current : null;
            L["system"].text = Loc.T("panel.system_name", Loc.T("system.name"));
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
            L["affiliation"].RemoveFromClassList("panel-hostile");
            if (target == null)
            {
                L["target"].text = Loc.T("panel.no_target");
                L["distance"].text = L["target_hull"].text = L["target_shield"].text = L["affiliation"].text = "";
                return;
            }
            var origin = FloatingOrigin.Instance;
            double dist = origin ? WorldPosition.Distance(origin.ToWorld(Player.Ship.transform.position), target.Position) : 0;
            L["target"].text = Loc.T("panel.target_name", Loc.T("target." + target.DisplayId));
            L["distance"].text = Loc.T("panel.target_distance", HudModel.FormatDistance(dist));
            if (target is Asteroid rock)
            {
                L["target_hull"].text = rock.HasResource
                    ? Loc.T("panel.asteroid_resource", Loc.T("target." + rock.Deposit.Resource.id), rock.Deposit.Remaining)
                    : Loc.T("panel.asteroid_barren");
                L["target_shield"].text = "";
                L["affiliation"].text = Loc.T("panel.affiliation", Loc.T("panel.neutral"));
                return;
            }
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
            for (int i = 0; i < p.tabs.Count; i++)
            {
                p.tabs[i].text = Loc.T(TabKeys[i]);
                p.tabs[i].EnableInClassList("panel-tab-active", i == (int)Tab);
            }
            p.labels["tab_title"].text = Loc.T(TabKeys[(int)Tab] + "_title");
            p.list.Clear();
            var ship = Player.Ship;
            var def = ship.ClassDef;
            switch (Tab)
            {
                case RightTab.Ship:
                    var bp = Player.Visual ? Player.Visual.Blueprint : null;
                    string model = bp != null && bp.body ? Loc.T("model." + bp.body.id.Replace("_body", "")) : "";
                    Add(p.list, $"{Player.ShipName} - {model}", "panel-ship-header");
                    Bar(p.list, Loc.T("panel.hull"), 1f, "bar-hull");      // sem dano nesta fase
                    Bar(p.list, Loc.T("panel.shields"), 1f, "bar-shield");
                    Bar(p.list, Loc.T("panel.boost"), Player.Boost && Player.Boost.Tank != null ? Player.Boost.Tank.Fraction : 1f, "bar-boost");
                    bool warp = Player.Warp && Player.Warp.Drive.State == WarpState.Active;
                    Add(p.list, Loc.T("panel.speed", HudModel.FormatSpeed(warp ? Player.Warp.Drive.Speed : ship.Velocity.magnitude)));
                    break;
                case RightTab.Cargo:
                    var cargo = ship.Cargo;
                    Bar(p.list, Loc.T("hud.cargo", cargo.Used, cargo.Capacity), cargo.LoadFraction, "cargo-fill");
                    if (cargo.Used == 0) Add(p.list, Loc.T("panel.cargo_empty"));
                    foreach (var kv in cargo.Contents)
                    {
                        int reserved = cargo.Reserved(kv.Key);
                        Add(p.list, reserved > 0
                            ? Loc.T("panel.cargo_line_reserved", Loc.T("target." + kv.Key.id), kv.Value, reserved)
                            : Loc.T("panel.cargo_line", Loc.T("target." + kv.Key.id), kv.Value));
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
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_generator"), "T1"));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_engine"), "T1"));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_hull"),
                        def && def.startingHull ? $"{def.startingHull.baseHp} HP" : "—"));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_booster"), def ? $"{def.boostCapacity:0}" : "150"));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_scanner"),
                        HudModel.FormatDistance(def ? def.scannerRange : Scanner.DefaultRange)));
                    Add(p.list, Loc.T("panel.module_line", Loc.T("panel.module_skill"), "—"));
                    break;
                case RightTab.Slots:
                    var loadout = Player.Laser ? Player.Laser.Loadout : null;
                    if (loadout == null) break;
                    for (int i = 0; i < loadout.slots.Length; i++)
                    {
                        var slot = loadout.slots[i];
                        var row = new VisualElement();
                        row.AddToClassList("panel-slot-row");
                        p.list.Add(row);
                        Add(row, Loc.T("panel.slot", i + 1, Loc.T(slot.kind == WeaponKind.MiningLaser ? "panel.weapon_mining" : "panel.weapon_empty")));
                        foreach (var group in new[] { FireGroup.Primary, FireGroup.Secondary })
                        {
                            var g = group;
                            var b = new Button(() => { slot.group = g; listDirty = true; })
                            { text = Loc.T(g == FireGroup.Primary ? "panel.group1" : "panel.group2") };
                            b.AddToClassList("panel-tab");
                            b.EnableInClassList("panel-tab-active", slot.group == g);
                            b.SetEnabled(slot.kind != WeaponKind.None);
                            row.Add(b);
                        }
                    }
                    break;
            }
        }

        static void Bar(VisualElement list, string label, float fraction, string fillClass)
        {
            Add(list, label, "panel-bar-label");
            var bar = new VisualElement();
            bar.AddToClassList("panel-bar");
            var fill = new VisualElement();
            fill.AddToClassList("bar-fill");
            fill.AddToClassList(fillClass);
            fill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
            bar.Add(fill);
            list.Add(bar);
        }

        static void Add(VisualElement list, string text, string cls = "label")
        {
            var l = new Label(text);
            l.AddToClassList(cls);
            list.Add(l);
        }

        // ---------- Minimapa (radar) ----------

        // Vista de cima com a nave no centro e o nariz para cima. Distancias em escala logaritmica
        // (de 1 km ao raio do setor) para caber tudo; piratas so dentro do scanner.
        void DrawRadar(MeshGenerationContext ctx, VisualElement radar)
        {
            var origin = FloatingOrigin.Instance;
            if (Player == null || !Player.Ship || !origin) return;
            var rect = radar.contentRect;
            var c = rect.center;
            float R = Mathf.Min(rect.width, rect.height) * 0.47f;
            if (R <= 0) return;
            var pz = ctx.painter2D;

            pz.strokeColor = new Color(0.3f, 0.7f, 0.8f, 0.35f);
            pz.lineWidth = 1.5f;
            foreach (float k in new[] { 0.33f, 0.66f, 1f }) { pz.BeginPath(); pz.Arc(c, R * k, 0, 360); pz.Stroke(); }
            pz.BeginPath(); pz.MoveTo(c + new Vector2(-R, 0)); pz.LineTo(c + new Vector2(R, 0)); pz.Stroke();
            pz.BeginPath(); pz.MoveTo(c + new Vector2(0, -R)); pz.LineTo(c + new Vector2(0, R)); pz.Stroke();

            var ship = Player.Ship.transform;
            var shipWorld = origin.ToWorld(ship.position);
            var fwd = Vector3.ProjectOnPlane(ship.forward, ship.up).normalized;
            var right = Vector3.Cross(ship.up, fwd);
            double range = Player.Ship.ClassDef ? Player.Ship.ClassDef.scannerRange : Scanner.DefaultRange;
            var current = Player.Targets ? Player.Targets.Current : null;
            var route = Player.Route ? Player.Route.Waypoint : null;

            foreach (var t in TargetRegistry.All)
            {
                if (t.Kind == TargetKind.Asteroid && !ReferenceEquals(t, current)) continue;
                var d = t.Position - shipWorld;
                double dist = math.length(d);
                if (!Scanner.Detects(t.Kind, dist, range)) continue;
                var dir = (Vector3)(float3)d;
                var flat = new Vector2(Vector3.Dot(dir, right), Vector3.Dot(dir, fwd));
                if (flat.sqrMagnitude < 1e-6f) continue;
                float r = RadarRadius(dist);
                var p = c + new Vector2(flat.normalized.x, -flat.normalized.y) * (r * R);
                DrawBlip(pz, p, t);
                if (ReferenceEquals(t, current)) Ring(pz, p, 11f, new Color(1f, 0.85f, 0.3f));
                if (ReferenceEquals(t, route)) Diamond(pz, p, 12f, new Color(0.75f, 0.45f, 1f));
            }

            // A propria nave no centro (nariz para cima).
            pz.fillColor = new Color(0.4f, 1f, 0.6f);
            pz.BeginPath();
            pz.MoveTo(c + new Vector2(0, -12)); pz.LineTo(c + new Vector2(9, 10)); pz.LineTo(c + new Vector2(-9, 10));
            pz.ClosePath();
            pz.Fill();
        }

        public static float RadarRadius(double distance)
        {
            const double inner = 1000, outer = 1e9; // 1 km .. 1 milhao de km
            if (distance <= inner) return (float)(distance / inner) * 0.1f;
            return Mathf.Clamp01(0.1f + 0.9f * (float)(math.log10(distance / inner) / math.log10(outer / inner)));
        }

        static void DrawBlip(Painter2D pz, Vector2 p, ITargetable t)
        {
            switch (HudIcons.For(t))
            {
                case IconKind.Planet: Ring(pz, p, 7f, new Color(0.9f, 0.92f, 0.95f)); break;
                case IconKind.Moon: Ring(pz, p, 5f, new Color(0.75f, 0.75f, 0.8f)); break;
                case IconKind.Station:
                    Ring(pz, p, 6f, new Color(0.3f, 0.86f, 1f));
                    pz.fillColor = new Color(0.3f, 0.86f, 1f); pz.BeginPath(); pz.Arc(p, 2f, 0, 360); pz.Fill();
                    break;
                case IconKind.Gate: Ring(pz, p, 7f, new Color(0.6f, 0.67f, 1f)); Ring(pz, p, 4f, new Color(0.6f, 0.67f, 1f)); break;
                case IconKind.Field:
                    foreach (var o in new[] { new Vector2(-4, 2), new Vector2(3, -3), new Vector2(3, 4) }) Ring(pz, p + o, 2.5f, new Color(1f, 0.75f, 0.3f));
                    break;
                case IconKind.Asteroid: Ring(pz, p, 4f, new Color(1f, 0.75f, 0.3f)); break;
                case IconKind.Pirate: Square(pz, p, 6f, new Color(1f, 0.25f, 0.2f)); break;
                default: Square(pz, p, 6f, new Color(0.9f, 0.9f, 0.95f)); break;
            }
        }

        static void Ring(Painter2D pz, Vector2 p, float r, Color c)
        {
            pz.strokeColor = c; pz.lineWidth = 2f;
            pz.BeginPath(); pz.Arc(p, r, 0, 360); pz.Stroke();
        }

        static void Square(Painter2D pz, Vector2 p, float h, Color c)
        {
            pz.strokeColor = c; pz.lineWidth = 2f;
            pz.BeginPath();
            pz.MoveTo(p + new Vector2(-h, -h)); pz.LineTo(p + new Vector2(h, -h)); pz.LineTo(p + new Vector2(h, h)); pz.LineTo(p + new Vector2(-h, h));
            pz.ClosePath();
            pz.Stroke();
        }

        static void Diamond(Painter2D pz, Vector2 p, float h, Color c)
        {
            pz.strokeColor = c; pz.lineWidth = 2.5f;
            pz.BeginPath();
            pz.MoveTo(p + new Vector2(0, -h)); pz.LineTo(p + new Vector2(h, 0)); pz.LineTo(p + new Vector2(0, h)); pz.LineTo(p + new Vector2(-h, 0));
            pz.ClosePath();
            pz.Stroke();
        }
    }
}
