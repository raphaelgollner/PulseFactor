using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Mapa do sistema (M): estrela no centro e setores em quadrantes. No setor, o scroll faz zoom
    // suave em volta do mouse (ate 6x) e arrastar move o mapa; clicar num ponto mostra as
    // informacoes e "Definir rota". Scroll para tras em 1x volta a visao geral.
    [RequireComponent(typeof(UIDocument))]
    public class SystemMapView : MonoBehaviour
    {
        const float MaxZoom = 6f, ZoomStep = 1.3f, ZoomSmoothing = 12f;

        public GalaxyDef Galaxy;
        public PlayerContext Player;

        VisualElement root, canvas, nodeLayer, info;
        Label title, hint, infoName, infoDesc, infoFaction, infoCorp, infoTrade;
        Button routeButton;
        SectorDef zoom;
        (SectorDef sector, string id) selected;

        // Zoom/deslocamento do mapa do setor (atual e alvo, para suavizar).
        float scale = 1f, targetScale = 1f;
        Vector2 offset, targetOffset;
        bool dragging;
        Vector2 dragStart, dragOffsetStart;

        class Entry { public VisualElement node, diamond; public Label label; public Vector2 unit; public float size; }
        readonly List<Entry> entries = new();

        public bool IsOpen { get; private set; }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            var sheet = Resources.Load<StyleSheet>("PulseUI");
            if (sheet) root.styleSheets.Add(sheet);

            var frame = new VisualElement();
            frame.AddToClassList("map");
            root.Add(frame);
            title = Lbl(frame, "map-title");
            hint = Lbl(frame, "map-hint");

            canvas = new VisualElement();
            canvas.AddToClassList("map-canvas");
            canvas.generateVisualContent += Draw;
            canvas.RegisterCallback<WheelEvent>(OnWheel);
            canvas.RegisterCallback<PointerDownEvent>(OnPointerDown);
            canvas.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            canvas.RegisterCallback<PointerUpEvent>(_ => dragging = false);
            canvas.RegisterCallback<GeometryChangedEvent>(_ => Rebuild());
            frame.Add(canvas);
            nodeLayer = new VisualElement { pickingMode = PickingMode.Ignore };
            nodeLayer.AddToClassList("map-nodes");
            canvas.Add(nodeLayer);

            info = new VisualElement();
            info.AddToClassList("map-info");
            frame.Add(info);
            infoName = Lbl(info, "label-big");
            infoDesc = Lbl(info, "map-desc");
            infoFaction = Lbl(info, "label");
            infoCorp = Lbl(info, "label");
            infoTrade = Lbl(info, "map-desc");
            routeButton = new Button(SetRoute);
            routeButton.AddToClassList("btn");
            routeButton.AddToClassList("btn-route");
            info.Add(routeButton);

            Close();
            Loc.LanguageChanged += Rebuild;
        }

        void OnDisable() => Loc.LanguageChanged -= Rebuild;

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            IsOpen = true;
            root.style.display = DisplayStyle.Flex;
            zoom = null;
            selected = default;
            ResetZoom();
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            Rebuild();
        }

        public void Close()
        {
            IsOpen = false;
            dragging = false;
            root.style.display = DisplayStyle.None;
            bool docked = Player?.Docking && Player.Docking.Current == DockingAutopilot.Phase.Docked;
            if (!docked && Player != null)
            {
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
            }
        }

        void ResetZoom()
        {
            scale = targetScale = 1f;
            offset = targetOffset = Vector2.zero;
        }

        void Update()
        {
            if (!IsOpen) return;
            float k = 1f - Mathf.Exp(-ZoomSmoothing * Time.unscaledDeltaTime);
            scale = Mathf.Lerp(scale, targetScale, k);
            offset = Vector2.Lerp(offset, targetOffset, k);
            Layout();
            canvas.MarkDirtyRepaint(); // orbitas acompanham o zoom; a nave se move
        }

        // ---------- Layout ----------

        Vector2 Center => canvas.contentRect.center;
        float Radius => Mathf.Min(canvas.contentRect.width, canvas.contentRect.height) * 0.45f;
        SectorDef CurrentSector => Player?.Sectors ? Player.Sectors.Current : null;

        Vector2 OverviewPlanet(int i)
        {
            float orbit = Radius * (0.38f + 0.17f * i);
            float a = SystemMapLayout.SectorAngle(i, Galaxy.sectors.Count) * Mathf.Deg2Rad;
            return Center + new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * orbit;
        }

        // Ponto do setor (circulo unitario) na tela, com zoom e deslocamento.
        Vector2 SectorPoint(Vector2 unit) => Center + offset + new Vector2(unit.x, -unit.y) * Radius * scale;

        void Rebuild()
        {
            if (!IsOpen || canvas == null) return;
            nodeLayer.Clear();
            entries.Clear();
            var route = Player?.Route;
            if (zoom == null)
            {
                title.text = Loc.T("map.title");
                hint.text = Loc.T("map.hint_overview");
                for (int i = 0; i < Galaxy.sectors.Count; i++)
                {
                    var s = Galaxy.sectors[i];
                    var main = s.bodies.Count > 0 ? s.bodies[0] : null;
                    var e = AddNode(26f, "map-node-planet", main != null ? main.color : Color.gray,
                        Loc.T("sector.name", Loc.T("body." + s.id)), () => EnterSector(s));
                    e.unit = new Vector2(i, 0); // indice do setor (posicao calculada em Layout)
                    if (s == CurrentSector) e.node.AddToClassList("map-node-here");
                    if (route != null && route.HasRoute && route.DestinationSectorId == s.id) e.diamond = AddDiamond();
                }
            }
            else
            {
                title.text = Loc.T("sector.name", Loc.T("body." + zoom.id));
                hint.text = Loc.T("map.hint_sector");
                foreach (var n in SystemMapLayout.BuildSector(zoom))
                {
                    var (cls, size) = n.kind switch
                    {
                        MapNodeKind.MainBody => ("map-node-planet", 40f),
                        MapNodeKind.Moon => ("map-node-moon", 18f),
                        MapNodeKind.Station => ("map-node-station", 16f),
                        MapNodeKind.Field => ("map-node-field", 16f),
                        _ => ("map-node-gate", 18f),
                    };
                    var color = n.kind == MapNodeKind.MainBody || n.kind == MapNodeKind.Moon
                        ? zoom.bodies.First(b => b.id == n.id).color : Color.clear;
                    var sector = zoom;
                    var id = n.id;
                    var e = AddNode(size, cls, color, Loc.T("target." + n.id), () => { selected = (sector, id); Rebuild(); });
                    e.unit = n.position;
                    if (selected.id == n.id) e.node.AddToClassList("map-node-selected");
                    if (route != null && route.DestinationSectorId == zoom.id && route.DestinationId == n.id) e.diamond = AddDiamond();
                }
            }
            Layout();
            ShowInfo();
            canvas.MarkDirtyRepaint();
        }

        void EnterSector(SectorDef s)
        {
            zoom = s;
            selected = default;
            ResetZoom();
            Rebuild();
        }

        Entry AddNode(float size, string cls, Color color, string label, System.Action onClick)
        {
            var e = new Entry { size = size };
            e.node = new VisualElement();
            e.node.AddToClassList("map-node");
            e.node.AddToClassList(cls);
            e.node.style.width = size;
            e.node.style.height = size;
            if (color.a > 0) e.node.style.backgroundColor = color;
            e.node.RegisterCallback<ClickEvent>(ev => { if (!dragMoved) onClick(); ev.StopPropagation(); });
            nodeLayer.Add(e.node);
            e.label = new Label(label) { pickingMode = PickingMode.Ignore };
            e.label.AddToClassList("map-label");
            nodeLayer.Add(e.label);
            entries.Add(e);
            return e;
        }

        VisualElement AddDiamond()
        {
            var d = new VisualElement { pickingMode = PickingMode.Ignore };
            d.AddToClassList("map-route");
            nodeLayer.Add(d);
            return d;
        }

        // Reposiciona os icones (chamado a cada quadro para o zoom suave).
        void Layout()
        {
            foreach (var e in entries)
            {
                var p = zoom == null ? OverviewPlanet((int)e.unit.x) : SectorPoint(e.unit);
                e.node.style.left = p.x - e.size / 2f;
                e.node.style.top = p.y - e.size / 2f;
                e.label.style.left = p.x - 90;
                e.label.style.top = p.y + e.size / 2f + 2;
                if (e.diamond != null)
                {
                    e.diamond.style.left = p.x + e.size / 2f - 4;
                    e.diamond.style.top = p.y - e.size / 2f - 12;
                }
            }
        }

        // ---------- Desenho (orbitas, linhas de setor, estrela, nave) ----------

        void Draw(MeshGenerationContext ctx)
        {
            if (!IsOpen || Galaxy == null) return;
            var p = ctx.painter2D;
            var c = Center;
            float R = Radius;
            if (R <= 0) return;

            if (zoom == null)
            {
                p.strokeColor = new Color(0.4f, 0.6f, 0.7f, 0.35f);
                p.lineWidth = 1.5f;
                for (int k = 0; k < Galaxy.sectors.Count; k++)
                {
                    float a = (SystemMapLayout.SectorAngle(k, Galaxy.sectors.Count) - 45f) * Mathf.Deg2Rad;
                    Dashed(p, c, c + new Vector2(Mathf.Cos(a), -Mathf.Sin(a)) * R * 1.12f);
                }
                p.strokeColor = new Color(0.6f, 0.75f, 0.85f, 0.4f);
                for (int i = 0; i < Galaxy.sectors.Count; i++) Circle(p, c, R * (0.38f + 0.17f * i), false);

                p.fillColor = new Color(1f, 0.85f, 0.35f, 0.25f);
                Circle(p, c, R * 0.13f, true);
                p.fillColor = new Color(1f, 0.9f, 0.5f);
                Circle(p, c, R * 0.08f, true);
            }
            else
            {
                var center = c + offset;
                p.strokeColor = new Color(0.6f, 0.75f, 0.85f, 0.35f);
                p.lineWidth = 1f;
                foreach (var e in entries)
                {
                    float r = e.unit.magnitude * R * scale;
                    if (r > 1f) Circle(p, center, r, false);
                }

                if (zoom == CurrentSector && Player?.Ship && FloatingOrigin.Instance)
                {
                    var world = FloatingOrigin.Instance.ToWorld(Player.Ship.transform.position);
                    var sp = SectorPoint(SystemMapLayout.Project(zoom, world));
                    p.fillColor = new Color(0.3f, 1f, 0.5f);
                    p.BeginPath();
                    p.MoveTo(sp + new Vector2(0, -9)); p.LineTo(sp + new Vector2(7, 7)); p.LineTo(sp + new Vector2(-7, 7));
                    p.ClosePath();
                    p.Fill();
                }
            }
        }

        static void Circle(Painter2D p, Vector2 c, float r, bool fill)
        {
            p.BeginPath();
            p.Arc(c, r, 0, 360);
            if (fill) p.Fill(); else p.Stroke();
        }

        static void Dashed(Painter2D p, Vector2 a, Vector2 b)
        {
            const float dash = 10f, gap = 7f;
            float len = Vector2.Distance(a, b);
            var dir = (b - a) / Mathf.Max(len, 1e-3f);
            for (float t = 0; t < len; t += dash + gap)
            {
                p.BeginPath();
                p.MoveTo(a + dir * t);
                p.LineTo(a + dir * Mathf.Min(t + dash, len));
                p.Stroke();
            }
        }

        // ---------- Interacao ----------

        void OnWheel(WheelEvent e)
        {
            var mouse = (Vector2)e.localMousePosition;
            if (zoom == null)
            {
                if (e.delta.y > 0) return;
                // Entra no setor do quadrante sob o mouse.
                var v = mouse - Center;
                float angle = Mathf.Atan2(-v.y, v.x) * Mathf.Rad2Deg;
                int best = 0;
                float bestDelta = float.MaxValue;
                for (int i = 0; i < Galaxy.sectors.Count; i++)
                {
                    float d = Mathf.Abs(Mathf.DeltaAngle(angle, SystemMapLayout.SectorAngle(i, Galaxy.sectors.Count)));
                    if (d < bestDelta) { bestDelta = d; best = i; }
                }
                EnterSector(Galaxy.sectors[best]);
                return;
            }

            if (e.delta.y > 0 && targetScale <= 1.001f)
            {
                zoom = null; // ja no 1x: volta a visao geral
                selected = default;
                ResetZoom();
                Rebuild();
                return;
            }
            // Zoom em volta do mouse: o ponto sob o cursor fica parado.
            float newScale = Mathf.Clamp(e.delta.y < 0 ? targetScale * ZoomStep : targetScale / ZoomStep, 1f, MaxZoom);
            var anchor = mouse - Center - targetOffset;
            targetOffset = mouse - Center - anchor * (newScale / targetScale);
            targetScale = newScale;
            if (targetScale <= 1.001f) targetOffset = Vector2.zero;
            e.StopPropagation();
        }

        bool dragMoved;

        void OnPointerDown(PointerDownEvent e)
        {
            if (zoom == null) return;
            dragging = true;
            dragMoved = false;
            dragStart = e.localPosition;
            dragOffsetStart = targetOffset;
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (!dragging) return;
            var delta = (Vector2)e.localPosition - dragStart;
            if (delta.sqrMagnitude > 16f) dragMoved = true;
            if (!dragMoved) return;
            targetOffset = dragOffsetStart + delta;
            offset = targetOffset; // arrastar responde na hora
        }

        void ShowInfo()
        {
            info.style.display = selected.sector ? DisplayStyle.Flex : DisplayStyle.None;
            if (!selected.sector) return;
            var id = selected.id;
            infoName.text = Loc.T("target." + id);
            infoDesc.text = Loc.T("desc." + id);
            infoFaction.text = Loc.T("map.faction", Loc.T("corp.federation"));

            var station = selected.sector.stations.FirstOrDefault(s => s.def && s.def.id == id)?.def;
            var field = selected.sector.fields.FirstOrDefault(f => f.def && f.def.id == id)?.def;
            infoCorp.text = Loc.T("map.corporation", station ? Loc.T("corp." + station.CorpId) : "—");
            if (station)
            {
                var sells = string.Join(", ", station.market.Where(m => m.sells && m.resource).Select(m => Loc.T("target." + m.resource.id)));
                var buys = string.Join(", ", station.market.Where(m => m.buys && m.resource).Select(m => Loc.T("target." + m.resource.id)));
                infoTrade.text = Loc.T("map.trade", sells, buys);
            }
            else if (field)
            {
                infoTrade.text = Loc.T("map.resources", string.Join(", ", field.composition.OrderByDescending(c => c.weight)
                    .Where(c => c.resource).Select(c => Loc.T("target." + c.resource.id))));
            }
            else infoTrade.text = "";

            var route = Player?.Route;
            bool isDest = route != null && route.DestinationSectorId == selected.sector.id && route.DestinationId == id;
            routeButton.text = Loc.T(isDest ? "map.route_clear" : "map.route_set");
        }

        void SetRoute()
        {
            var route = Player?.Route;
            if (route == null || !selected.sector) return;
            if (route.DestinationSectorId == selected.sector.id && route.DestinationId == selected.id) route.Clear();
            else route.SetDestination(selected.sector.id, selected.id);
            Rebuild();
        }

        static Label Lbl(VisualElement parent, string classes)
        {
            var l = new Label();
            foreach (var c in classes.Split(' ')) l.AddToClassList(c);
            parent.Add(l);
            return l;
        }
    }
}
