using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Terminal da estacao (primeira pessoa): mercado, servicos e decolagem.
    [RequireComponent(typeof(UIDocument))]
    public class StationTerminal : MonoBehaviour
    {
        public PlayerContext Player;
        public List<ResourceDef> Resources = new();

        VisualElement root, window, content;
        Label title, credits, testMode, cargoLabel;
        VisualElement cargoFill;
        Button tabMarket, tabServices;
        bool marketTab = true;
        readonly Dictionary<ResourceDef, int> quantities = new();

        public bool IsOpen { get; private set; }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            var sheet = UnityEngine.Resources.Load<StyleSheet>("PulseUI");
            if (sheet) root.styleSheets.Add(sheet);

            window = new VisualElement();
            window.AddToClassList("terminal");
            root.Add(window);

            var header = Row(window, "terminal-header");
            title = Lbl(header, "label-big");
            var right = new VisualElement();
            header.Add(right);
            credits = Lbl(right, "label");
            testMode = Lbl(right, "test-mode");
            testMode.style.position = Position.Relative;
            Btn(right, "PT / EN", () => Loc.SetLanguage(Loc.CurrentLanguage.StartsWith("pt") ? "en" : "pt-BR"), "btn");

            var tabs = Row(window, "terminal-tabs");
            tabMarket = Btn(tabs, "", () => { marketTab = true; Refresh(); }, "tab");
            tabServices = Btn(tabs, "", () => { marketTab = false; Refresh(); }, "tab");

            content = new ScrollView();
            content.style.flexGrow = 1;
            window.Add(content);

            cargoLabel = Lbl(window, "label");
            var bar = new VisualElement();
            bar.AddToClassList("cargo-bar");
            window.Add(bar);
            cargoFill = new VisualElement();
            cargoFill.AddToClassList("cargo-fill");
            bar.Add(cargoFill);

            var footer = Row(window, "terminal-header");
            footer.style.marginTop = 14;
            footer.Add(new VisualElement());
            Btn(footer, "", Launch, "btn btn-launch").name = "launch";

            Close();
            Loc.LanguageChanged += Refresh;
        }

        void OnDisable() => Loc.LanguageChanged -= Refresh;

        public void Open()
        {
            IsOpen = true;
            root.style.display = DisplayStyle.Flex;
            marketTab = true;
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            root.style.display = DisplayStyle.None;
        }

        void Launch()
        {
            Close();
            if (Player?.Docking) Player.Docking.Undock();
        }

        Station Station => Player?.Docking ? Player.Docking.Station : null;

        public void Refresh()
        {
            if (!IsOpen || Player == null || !Station) return;
            var ship = Player.Ship;
            title.text = Loc.T("target." + Station.DisplayId);
            credits.text = Loc.T("hud.credits", Player.Wallet.Credits.ToString("N0"));
            testMode.text = Player.Config && Player.Config.testMode ? Loc.T("hud.test_mode") : "";
            tabMarket.text = Loc.T("terminal.market");
            tabServices.text = Loc.T("terminal.services");
            tabMarket.EnableInClassList("tab-active", marketTab);
            tabServices.EnableInClassList("tab-active", !marketTab);
            window.Q<Button>("launch").text = Loc.T("terminal.launch");

            cargoLabel.text = Loc.T("hud.cargo", ship.Cargo.Used, ship.Cargo.Capacity);
            cargoFill.style.width = Length.Percent(ship.Cargo.LoadFraction * 100f);

            content.Clear();
            if (marketTab) BuildMarket(); else BuildServices();
        }

        void BuildMarket()
        {
            var header = Row(content, "market-row");
            foreach (var key in new[] { "terminal.col_resource", "terminal.col_price", "terminal.col_cargo", "terminal.col_station", "terminal.col_qty" })
                Lbl(header, key == "terminal.col_resource" ? "cell cell-wide" : "cell").text = Loc.T(key);

            var market = Station.Market;
            foreach (var r in Resources)
            {
                var entry = market.Entry(r);
                if (entry == null) continue;
                if (!quantities.ContainsKey(r)) quantities[r] = 10;

                var row = Row(content, "market-row");
                Lbl(row, "cell cell-wide").text = Loc.T("target." + r.id);
                Lbl(row, "cell").text = market.PriceOf(r).ToString("N0");
                Lbl(row, "cell").text = Player.Ship.Cargo.Count(r).ToString();
                Lbl(row, "cell").text = entry.sells && entry.buys ? Loc.T("terminal.buys_sells")
                    : entry.sells ? Loc.T("terminal.sells") : Loc.T("terminal.buys");

                var qtyBox = Row(row, "cell");
                qtyBox.style.flexDirection = FlexDirection.Row;
                Btn(qtyBox, "−", () => { quantities[r] = Mathf.Max(1, quantities[r] - 10); Refresh(); }, "btn");
                Lbl(qtyBox, "qty").text = quantities[r].ToString();
                Btn(qtyBox, "+", () => { quantities[r] += 10; Refresh(); }, "btn");

                var buy = Btn(row, Loc.T("terminal.buy"), () => Trade(r, true), "btn");
                buy.SetEnabled(entry.sells);
                var sell = Btn(row, Loc.T("terminal.sell"), () => Trade(r, false), "btn");
                sell.SetEnabled(entry.buys && Player.Ship.Cargo.Count(r) > 0);
            }
        }

        void BuildServices()
        {
            foreach (var key in new[] { "terminal.fuel", "terminal.repair", "terminal.ammo" })
            {
                var row = Row(content, "market-row");
                Lbl(row, "cell cell-wide").text = Loc.T(key);
                Lbl(row, "label").text = Loc.T("terminal.service_ok");
            }
        }

        void Trade(ResourceDef r, bool buy)
        {
            var m = Station.Market;
            var result = buy
                ? m.Buy(r, quantities[r], Player.Ship.Cargo, Player.Wallet)
                : m.Sell(r, quantities[r], Player.Ship.Cargo, Player.Wallet);
            GameMessages.Post(result.ok
                ? (buy ? "msg.bought" : "msg.sold")
                : "trade." + result.reason, result.quantity, Loc.T("target." + r.id), result.credits);
            Refresh();
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

        static Button Btn(VisualElement parent, string text, System.Action onClick, string classes)
        {
            var b = new Button(onClick) { text = text };
            foreach (var c in classes.Split(' ')) b.AddToClassList(c);
            parent.Add(b);
            return b;
        }
    }
}
