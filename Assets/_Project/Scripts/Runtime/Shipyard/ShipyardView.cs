using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Abas ESTALEIRO e ARMAZENAMENTO do terminal. Estaleiro: comprar nave (modelo base + pecas + cores + nome)
    // ou modificar a atual, tudo com miniaturas e a previa 3D girando. O estado da edicao sobrevive aos Refresh.
    public class ShipyardView
    {
        enum Mode { Buy, Modify }

        public static readonly Color[] Palette =
        {
            new(0.92f, 0.93f, 0.95f), new(0.55f, 0.58f, 0.62f), new(0.18f, 0.19f, 0.22f), new(0.85f, 0.15f, 0.12f),
            new(0.95f, 0.5f, 0.1f), new(0.95f, 0.82f, 0.15f), new(0.25f, 0.65f, 0.25f), new(0.33f, 0.38f, 0.22f),
            new(0.15f, 0.65f, 0.85f), new(0.15f, 0.25f, 0.6f), new(0.55f, 0.25f, 0.75f), new(0.9f, 0.4f, 0.65f),
        };

        static readonly string[] PaintSlots = { "shipyard.color_primary", "shipyard.color_secondary", "shipyard.color_accent" };

        readonly PlayerContext player;
        readonly Action refresh;

        Station station;
        ShipyardCatalog catalog;
        Mode mode = Mode.Buy;
        ShipBlueprint draft;
        bool customPaint;
        readonly int[] colors = { 0, 2, 4 };
        bool startCustom;
        readonly int[] startColors = { 0, 2, 4 };
        string shipName = "";
        float scroll; // posicao da rolagem das pecas (o terminal reconstroi a tela a cada clique)

        public ShipyardView(PlayerContext player, Action refresh)
        {
            this.player = player;
            this.refresh = refresh;
        }

        public static bool HasShipyard(Station s) => s && s.Def && new ShipyardCatalog(s.Def).IsOpen;

        public void Hide() => ShipPreview.Instance.Hide();

        // ---------- ESTALEIRO ----------

        public void BuildShipyard(VisualElement content, Station current)
        {
            if (station != current) { station = current; catalog = new ShipyardCatalog(current.Def); ResetBuy(); }
            if (mode == Mode.Modify && modifyingId != player.Fleet.Fleet.Active?.id) ResetModify(); // trocou de nave
            var fleet = player.Fleet;
            if (PreparingBanner(content)) return;

            var modes = Row(content, "shipyard-modes");
            Btn(modes, Loc.T("shipyard.mode_buy"), () => { ResetBuy(); refresh(); }, "tab" + (mode == Mode.Buy ? " tab-active" : ""));
            Btn(modes, Loc.T("shipyard.mode_modify"), () => { ResetModify(); refresh(); }, "tab" + (mode == Mode.Modify ? " tab-active" : ""));

            var layout = Row(content, "shipyard-layout");
            var left = Row(layout, "shipyard-left");
            var right = new ScrollView(); // so as pecas rolam; a previa fica sempre visivel
            right.AddToClassList("shipyard-right");
            layout.Add(right);
            // Mantem a rolagem entre reconstrucoes (restaura depois do layout, so entao passa a acompanhar).
            right.schedule.Execute(() =>
            {
                right.scrollOffset = new Vector2(0, scroll);
                right.verticalScroller.valueChanged += v => scroll = v;
            });

            var preview = new Image { image = ShipPreview.Instance.Texture, scaleMode = ScaleMode.ScaleToFit };
            preview.AddToClassList("shipyard-preview");
            left.Add(preview);
            ShipPreview.Instance.Show(WithPaint(draft));
            Stats(left);

            if (mode == Mode.Buy)
            {
                Section(right, "shipyard.base_model");
                var models = Row(right, "card-row");
                foreach (var m in catalog.Models)
                    Card(models, m.body, Loc.T("model." + m.id), ShipPricing.ShipPrice(m.ToBlueprint()).ToString("N0") + " cr",
                        draft.body == m.body && draft.cockpit == m.cockpit && draft.tail == m.tail && draft.wing == m.wing,
                        () => { draft = m.ToBlueprint(); refresh(); });
            }

            PartRow(right, "shipyard.part_body", BlockType.Body, Bodies());
            PartRow(right, "shipyard.part_cockpit", BlockType.Cockpit, Parts(BlockType.Cockpit));
            var wings = Parts(BlockType.Wing).ToList();
            if (wings.Count > 0) PartRow(right, "shipyard.part_wing", BlockType.Wing, wings);
            PartRow(right, "shipyard.part_tail", BlockType.Tail, Parts(BlockType.Tail));
            PaintRow(right);

            if (mode == Mode.Buy)
            {
                Section(right, "shipyard.name");
                var field = new TextField { value = shipName, maxLength = PlayerContext.MaxShipNameLength };
                field.AddToClassList("name-field");
                field.RegisterValueChangedCallback(e => shipName = e.newValue);
                right.Add(field);
            }

            var footer = Row(right, "shipyard-footer");
            int price = Price();
            Lbl(footer, "shipyard-price").text = Loc.T("shipyard.total", price.ToString("N0"));
            if (mode == Mode.Buy)
            {
                Lbl(footer, "label-small").text = Loc.T("shipyard.fleet", fleet.Fleet.Count, Fleet.MaxShips);
                var buy = Btn(footer, Loc.T("shipyard.buy"), Buy, "btn btn-deliver");
                buy.SetEnabled(player.Wallet.Credits >= price && fleet.Fleet.CanAdd);
            }
            else
            {
                bool changed = price > 0;
                var apply = Btn(footer, Loc.T("shipyard.apply"), Apply, "btn btn-deliver");
                apply.SetEnabled(changed && player.Wallet.Credits >= price);
            }
        }

        void ResetBuy()
        {
            scroll = 0f;
            mode = Mode.Buy;
            var first = catalog.Models.FirstOrDefault();
            draft = first ? first.ToBlueprint() : Copy(player.Fleet.ActiveBlueprint);
            customPaint = false;
            shipName = NextName();
        }

        string modifyingId;

        void ResetModify()
        {
            scroll = 0f;
            mode = Mode.Modify;
            modifyingId = player.Fleet.Fleet.Active?.id;
            draft = Copy(player.Fleet.ActiveBlueprint);
            customPaint = draft.paint != null;
            if (customPaint)
            {
                colors[0] = Nearest(draft.paint.body.primary);
                colors[1] = Nearest(draft.paint.body.secondary);
                colors[2] = Nearest(draft.paint.body.accent);
            }
            startCustom = customPaint;
            Array.Copy(colors, startColors, 3);
        }

        bool PaintChanged => customPaint != startCustom || (customPaint && !colors.SequenceEqual(startColors));

        int Price() => mode == Mode.Buy
            ? ShipPricing.ShipPrice(draft) + (customPaint ? ShipPricing.PaintPrice(draft.body.size) : 0)
            : player.Fleet.ModifyPrice(draft, PaintChanged);

        void Buy()
        {
            var e = player.Fleet.Buy(WithPaint(draft), shipName, station);
            if (e != ShipyardError.None) GameMessages.Post("shipyard." + e);
            else shipName = NextName();
            refresh();
        }

        void Apply()
        {
            var e = player.Fleet.Modify(WithPaint(draft), PaintChanged);
            if (e != ShipyardError.None) GameMessages.Post("shipyard." + e);
            else ResetModify();
            refresh();
        }

        // Corpos: na compra, todos do estaleiro; na modificacao, so do mesmo tamanho (e o atual).
        IEnumerable<BlockDef> Bodies()
        {
            var list = catalog.Bodies.ToList();
            if (mode == Mode.Modify)
            {
                var current = player.Fleet.ActiveBlueprint.body;
                list = list.Where(b => b.size == current.size).ToList();
                if (!list.Contains(current)) list.Insert(0, current);
            }
            return list;
        }

        IEnumerable<BlockDef> Parts(BlockType type)
        {
            var list = catalog.PartsFor(type, draft.body).ToList();
            var current = mode == Mode.Modify ? Slot(player.Fleet.ActiveBlueprint, type) : null;
            if (current && BlockRules.CanMount(current, draft.body) && !list.Contains(current)) list.Insert(0, current);
            return list;
        }

        void PartRow(VisualElement parent, string titleKey, BlockType type, IEnumerable<BlockDef> parts)
        {
            Section(parent, titleKey);
            var row = Row(parent, "card-row");
            var current = mode == Mode.Modify ? Slot(player.Fleet.ActiveBlueprint, type) : null;
            foreach (var b in parts)
            {
                bool owned = b == current;
                string price = owned ? Loc.T("shipyard.installed") : ShipPricing.PartPrice(b).ToString("N0") + " cr";
                string label = Loc.T("model." + b.ModelId) + (type == BlockType.Body ? $" · {b.size}" : "");
                Card(row, b, label, price, Slot(draft, type) == b, () => Select(type, b));
            }
        }

        void Select(BlockType type, BlockDef b)
        {
            switch (type)
            {
                case BlockType.Body:
                    draft.body = b;
                    // Pecas que nao montam no corpo novo: troca pela do mesmo modelo, ou a primeira que monta.
                    draft.cockpit = Fit(BlockType.Cockpit, draft.cockpit);
                    draft.tail = Fit(BlockType.Tail, draft.tail);
                    draft.wing = Fit(BlockType.Wing, draft.wing);
                    break;
                case BlockType.Cockpit: draft.cockpit = b; break;
                case BlockType.Tail: draft.tail = b; break;
                default: draft.wing = b; break;
            }
            refresh();
        }

        BlockDef Fit(BlockType type, BlockDef current)
        {
            var options = Parts(type).ToList();
            if (current && options.Contains(current)) return current;
            return options.FirstOrDefault(p => p.ModelId == draft.body.ModelId) ?? options.FirstOrDefault();
        }

        void PaintRow(VisualElement parent)
        {
            Section(parent, "shipyard.paint", ShipPricing.PaintPrice(draft.body.size).ToString("N0"));
            var toggle = Row(parent, "shipyard-modes");
            Btn(toggle, Loc.T("shipyard.paint_factory"), () => { customPaint = false; refresh(); }, "tab" + (!customPaint ? " tab-active" : ""));
            Btn(toggle, Loc.T("shipyard.paint_custom"), () => { customPaint = true; refresh(); }, "tab" + (customPaint ? " tab-active" : ""));
            if (!customPaint) return;
            for (int slot = 0; slot < 3; slot++)
            {
                var row = Row(parent, "swatch-row");
                Lbl(row, "swatch-label").text = Loc.T(PaintSlots[slot]);
                for (int i = 0; i < Palette.Length; i++)
                {
                    int s = slot, c = i;
                    var sw = new VisualElement();
                    sw.AddToClassList("swatch");
                    if (colors[slot] == i) sw.AddToClassList("swatch-selected");
                    sw.style.backgroundColor = Palette[i];
                    sw.RegisterCallback<ClickEvent>(_ => { colors[s] = c; refresh(); });
                    row.Add(sw);
                }
            }
        }

        void Stats(VisualElement parent)
        {
            var def = draft.body.classDef;
            Lbl(parent, "shipyard-stat-title").text = $"{Loc.T("class." + def.shipClass)} · {draft.body.size}";
            Lbl(parent, "shipyard-stat").text = Loc.T("shipyard.stats", def.cargoCapacity, Mathf.RoundToInt(def.maxSpeed), def.weaponSlots);
            if (mode == Mode.Modify && player.Ship.Cargo.Used > def.cargoCapacity)
                Lbl(parent, "shipyard-warning").text = Loc.T("shipyard.CargoTooBig");
        }

        ShipBlueprint WithPaint(ShipBlueprint bp)
        {
            var copy = Copy(bp);
            copy.paint = null;
            if (customPaint)
            {
                var p = new BlockPaint { primary = Palette[colors[0]], secondary = Palette[colors[1]], accent = Palette[colors[2]] };
                copy.paint = new PaintScheme { cockpit = p, body = p, tail = p, wing = p };
            }
            return copy;
        }

        static ShipBlueprint Copy(ShipBlueprint bp) => new()
        {
            cockpit = bp.cockpit, body = bp.body, tail = bp.tail, wing = bp.wing, paint = bp.paint,
        };

        static BlockDef Slot(ShipBlueprint bp, BlockType type) => type switch
        {
            BlockType.Body => bp.body,
            BlockType.Cockpit => bp.cockpit,
            BlockType.Tail => bp.tail,
            _ => bp.wing,
        };

        static int Nearest(Color c)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < Palette.Length; i++)
            {
                var p = Palette[i];
                float d = (p.r - c.r) * (p.r - c.r) + (p.g - c.g) * (p.g - c.g) + (p.b - c.b) * (p.b - c.b);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        string NextName() => $"PULSE-{player.Fleet.Fleet.Count + 1:00}";

        // Troca esperando doca: so a faixa de "preparando" e o botao de cancelar.
        bool PreparingBanner(VisualElement content)
        {
            if (!player.Fleet.Preparing) return false;
            var box = Row(content, "preparing");
            Lbl(box, "preparing-title").text = Loc.T("shipyard.preparing");
            Lbl(box, "label").text = Loc.T("shipyard.preparing_queue", player.Fleet.PreparingQueuePosition);
            Btn(box, Loc.T("shipyard.cancel"), () => { player.Fleet.CancelPreparing(); refresh(); }, "btn btn-danger");
            return true;
        }

        // ---------- ARMAZENAMENTO ----------

        public void BuildStorage(VisualElement content, Station current)
        {
            Hide();
            if (PreparingBanner(content)) return;
            var service = player.Fleet;
            var fleet = service.Fleet;
            Lbl(content, "section-title").text = Loc.T("storage.title", fleet.Count, Fleet.MaxShips);

            ShipRow(content, fleet.Active, true, current);
            foreach (var s in fleet.Stored.Where(s => s.stationId == current.DisplayId)) ShipRow(content, s, false, current);
            foreach (var s in fleet.Stored.Where(s => s.stationId != current.DisplayId)) ShipRow(content, s, false, current);
            if (fleet.Stored.Count == 0) Lbl(content, "label-small").text = Loc.T("storage.empty");
        }

        void ShipRow(VisualElement parent, ShipRecord ship, bool active, Station current)
        {
            var service = player.Fleet;
            var body = service.Block(ship.body);
            var row = Row(parent, "storage-row");
            var thumb = new Image { image = ShipPreview.Instance.Thumbnail(body), scaleMode = ScaleMode.ScaleToFit };
            thumb.AddToClassList("storage-thumb");
            row.Add(thumb);
            var info = Row(row, "storage-info");
            Lbl(info, "label").text = active ? player.ShipName : ship.name;
            Lbl(info, "label-small").text = body
                ? $"{Loc.T("model." + body.ModelId)} · {Loc.T("class." + body.classDef.shipClass)} · {body.size}"
                : ship.body;
            int cargo = active ? player.Ship.Cargo.Used : ship.cargo.Sum(c => c.count);
            Lbl(info, "label-small").text = Loc.T("storage.cargo", cargo);

            if (active)
            {
                Lbl(row, "storage-state").text = Loc.T("storage.in_use");
                return;
            }
            bool here = ship.stationId == current.DisplayId;
            Lbl(row, "storage-state").text = here ? Loc.T("storage.here") : Loc.T("storage.at", Loc.T("target." + ship.stationId));
            if (here)
            {
                Btn(row, Loc.T("storage.use"), () =>
                {
                    var e = service.Swap(ship.id, current);
                    if (e != ShipyardError.None) GameMessages.Post("shipyard." + e);
                    refresh();
                }, "btn btn-deliver");
            }
        }

        // ---------- Elementos ----------

        static void Section(VisualElement parent, string key, params object[] args) => Lbl(parent, "shipyard-section").text = Loc.T(key, args);

        static void Card(VisualElement parent, BlockDef thumbOf, string label, string price, bool selected, Action onClick)
        {
            var card = new Button(onClick);
            card.AddToClassList("part-card");
            if (selected) card.AddToClassList("part-card-selected");
            var img = new Image { image = ShipPreview.Instance.Thumbnail(thumbOf), scaleMode = ScaleMode.ScaleToFit };
            img.AddToClassList("part-thumb");
            card.Add(img);
            Lbl(card, "part-name").text = label;
            Lbl(card, "part-price").text = price;
            parent.Add(card);
        }

        static VisualElement Row(VisualElement parent, string cls)
        {
            var e = new VisualElement();
            e.AddToClassList(cls);
            parent.Add(e);
            return e;
        }

        static Label Lbl(VisualElement parent, string classes)
        {
            var l = new Label();
            foreach (var c in classes.Split(' ')) l.AddToClassList(c);
            parent.Add(l);
            return l;
        }

        static Button Btn(VisualElement parent, string text, Action onClick, string classes)
        {
            var b = new Button(onClick) { text = text };
            foreach (var c in classes.Split(' ')) b.AddToClassList(c);
            parent.Add(b);
            return b;
        }
    }
}
