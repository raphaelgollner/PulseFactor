using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Terminal da estacao (primeira pessoa): mercado, contratos, servicos, estaleiro, armazenamento e decolagem.
    [RequireComponent(typeof(UIDocument))]
    public class StationTerminal : MonoBehaviour
    {
        enum Tab { Market, Contracts, Services, Shipyard, Storage }

        const long WelcomeMs = 2000;

        public PlayerContext Player;
        public List<ResourceDef> Resources = new();

        VisualElement root, window, content, fixedContent, welcome;
        Label title, corp, credits, testMode, cargoLabel, welcomeLabel;
        VisualElement cargoFill;
        Button tabMarket, tabContracts, tabServices, tabShipyard, tabStorage, language;
        ShipyardView shipyard;
        Tab tab = Tab.Market;
        readonly Dictionary<ResourceDef, int> quantities = new();

        public bool IsOpen { get; private set; }

        void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            var sheet = UnityEngine.Resources.Load<StyleSheet>("PulseUI");
            if (sheet) root.styleSheets.Add(sheet);

            welcome = new VisualElement();
            welcome.AddToClassList("welcome");
            welcomeLabel = Lbl(welcome, "welcome-text");
            root.Add(welcome);

            window = new VisualElement();
            window.AddToClassList("terminal");
            root.Add(window);

            var header = Row(window, "terminal-header");
            var left = new VisualElement();
            header.Add(left);
            title = Lbl(left, "label-big");
            corp = Lbl(left, "label-small");
            var right = new VisualElement();
            header.Add(right);
            credits = Lbl(right, "label");
            testMode = Lbl(right, "test-mode");
            testMode.style.position = Position.Relative;
            language = Btn(right, "", () => Loc.SetLanguage(Loc.CurrentLanguage.StartsWith("pt") ? "en" : "pt-BR"), "btn");

            var tabs = Row(window, "terminal-tabs");
            tabMarket = Btn(tabs, "", () => SelectTab(Tab.Market), "tab");
            tabContracts = Btn(tabs, "", () => SelectTab(Tab.Contracts), "tab");
            tabServices = Btn(tabs, "", () => SelectTab(Tab.Services), "tab");
            tabShipyard = Btn(tabs, "", () => SelectTab(Tab.Shipyard), "tab");
            tabStorage = Btn(tabs, "", () => SelectTab(Tab.Storage), "tab");

            content = new ScrollView();
            content.style.flexGrow = 1;
            window.Add(content);
            // Estaleiro: sem rolagem geral (a previa fica parada; so a coluna de pecas rola).
            fixedContent = new VisualElement();
            fixedContent.AddToClassList("terminal-fixed");
            window.Add(fixedContent);

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

        void SelectTab(Tab t)
        {
            tab = t;
            Refresh();
        }

        // "Bem-vindo a <estacao>" por 2 s e depois o terminal.
        public void OpenWithWelcome()
        {
            if (!Station) { Open(); return; }
            IsOpen = false;
            root.style.display = DisplayStyle.Flex;
            window.style.display = DisplayStyle.None;
            welcome.style.display = DisplayStyle.Flex;
            welcomeLabel.text = Loc.T("terminal.welcome", Loc.T("target." + Station.DisplayId));
            root.schedule.Execute(Open).ExecuteLater(WelcomeMs);
        }

        public void Open()
        {
            if (Player?.Docking && Player.Docking.Current != DockingAutopilot.Phase.Docked) return;
            IsOpen = true;
            root.style.display = DisplayStyle.Flex;
            welcome.style.display = DisplayStyle.None;
            window.style.display = DisplayStyle.Flex;
            tab = Tab.Market;
            Refresh();
        }

        public void Close()
        {
            IsOpen = false;
            root.style.display = DisplayStyle.None;
            shipyard?.Hide();
        }

        void Launch()
        {
            if (Player?.Fleet) Player.Fleet.CancelPreparing();
            Close();
            if (Player?.Docking) Player.Docking.Undock();
        }

        Station Station => Player?.Docking ? Player.Docking.Station : null;

        public void Refresh()
        {
            if (!IsOpen || Player == null || !Station) return;
            var ship = Player.Ship;
            title.text = Loc.T("target." + Station.DisplayId);
            var corpId = Station.Def.CorpId;
            var rep = Player.Contracts ? Player.Contracts.Reputation : null;
            corp.text = Loc.T("terminal.corp_rep", Loc.T("corp." + corpId), rep?.Get(corpId) ?? 0,
                Mathf.RoundToInt((rep?.DiscountFor(corpId) ?? 0f) * 100));
            credits.text = Loc.T("hud.credits", Player.Wallet.Credits.ToString("N0"));
            testMode.text = Player.Config && Player.Config.testMode ? Loc.T("hud.test_mode") : "";
            language.text = Loc.T("terminal.language");
            tabMarket.text = Loc.T("terminal.market");
            tabContracts.text = Loc.T("terminal.contracts");
            tabServices.text = Loc.T("terminal.services");
            tabMarket.EnableInClassList("tab-active", tab == Tab.Market);
            tabContracts.EnableInClassList("tab-active", tab == Tab.Contracts);
            tabServices.EnableInClassList("tab-active", tab == Tab.Services);
            bool hasShipyard = ShipyardView.HasShipyard(Station) && Player.Fleet;
            if (!hasShipyard && tab == Tab.Shipyard) tab = Tab.Market;
            if (!Player.Fleet && tab == Tab.Storage) tab = Tab.Market;
            tabShipyard.text = Loc.T("terminal.shipyard");
            tabStorage.text = Loc.T("terminal.storage");
            tabShipyard.style.display = hasShipyard ? DisplayStyle.Flex : DisplayStyle.None;
            tabStorage.style.display = Player.Fleet ? DisplayStyle.Flex : DisplayStyle.None;
            tabShipyard.EnableInClassList("tab-active", tab == Tab.Shipyard);
            tabStorage.EnableInClassList("tab-active", tab == Tab.Storage);
            tabMarket.EnableInClassList("tab-active", tab == Tab.Market);
            window.Q<Button>("launch").text = Loc.T("terminal.launch");

            cargoLabel.text = Loc.T("hud.cargo", ship.Cargo.Used, ship.Cargo.Capacity);
            cargoFill.style.width = Length.Percent(ship.Cargo.LoadFraction * 100f);

            content.Clear();
            fixedContent.Clear();
            bool fixedTab = tab == Tab.Shipyard;
            content.style.display = fixedTab ? DisplayStyle.None : DisplayStyle.Flex;
            fixedContent.style.display = fixedTab ? DisplayStyle.Flex : DisplayStyle.None;
            shipyard ??= new ShipyardView(Player, Refresh);
            if (tab != Tab.Shipyard) shipyard.Hide();
            switch (tab)
            {
                case Tab.Market: BuildMarket(); break;
                case Tab.Contracts: BuildContracts(); break;
                case Tab.Shipyard: shipyard.BuildShipyard(fixedContent, Station); break;
                case Tab.Storage: shipyard.BuildStorage(content, Station); break;
                default: BuildServices(); break;
            }
        }

        void BuildMarket()
        {
            var header = Row(content, "market-row");
            foreach (var key in new[] { "terminal.col_resource", "terminal.col_stock", "terminal.col_demand", "terminal.col_buy", "terminal.col_sell", "terminal.col_cargo", "terminal.col_qty" })
                Lbl(header, key == "terminal.col_resource" ? "cell cell-wide" : key is "terminal.col_stock" or "terminal.col_demand" ? "cell cell-narrow" : "cell").text = Loc.T(key);

            var market = Station.Market;
            var cargo = Player.Ship.Cargo;
            foreach (var r in Resources)
            {
                var entry = market.Entry(r);
                if (entry == null) continue;
                if (!quantities.ContainsKey(r)) quantities[r] = 10;

                var row = Row(content, "market-row");
                Lbl(row, "cell cell-wide").text = Loc.T("target." + r.id);
                bool stocked = market.HasStock(r);
                Lbl(row, "cell cell-narrow").text = stocked ? $"{market.Stock(r)}/{market.Capacity(r)}" : "—";
                var demand = Lbl(row, "cell cell-narrow");
                if (stocked && entry.buys)
                {
                    var level = market.Demand(r);
                    demand.text = Loc.T("demand." + level);
                    demand.AddToClassList("demand-" + level.ToString().ToLowerInvariant());
                }
                else demand.text = "—";
                Lbl(row, "cell").text = entry.sells ? market.BuyPriceOf(r).ToString("N0") : "—";
                Lbl(row, "cell").text = entry.buys ? market.PriceOf(r).ToString("N0") : "—";
                int reserved = cargo.Reserved(r);
                Lbl(row, "cell").text = reserved > 0 ? Loc.T("terminal.reserved", cargo.Count(r), reserved) : cargo.Count(r).ToString();

                var qtyBox = Row(row, "cell");
                qtyBox.style.flexDirection = FlexDirection.Row;
                Btn(qtyBox, "−", () => { quantities[r] = Step(quantities[r], -1); Refresh(); }, "btn");
                Lbl(qtyBox, "qty").text = quantities[r].ToString();
                Btn(qtyBox, "+", () => { quantities[r] = Step(quantities[r], +1); Refresh(); }, "btn");

                var buy = Btn(row, Loc.T("terminal.buy"), () => Trade(r, true), "btn");
                buy.SetEnabled(entry.sells);
                var sell = Btn(row, Loc.T("terminal.sell"), () => Trade(r, false), "btn");
                sell.SetEnabled(entry.buys && cargo.Sellable(r) > 0);
            }
        }

        // Passos de 1 ate 10 e de 10 em 10 acima disso.
        static int Step(int q, int dir) =>
            dir > 0 ? (q < 10 ? q + 1 : q + 10) : Mathf.Max(1, q <= 10 ? q - 1 : q - 10);

        void BuildContracts()
        {
            var service = Player.Contracts;
            if (!service) return;
            var stationId = Station.DisplayId;
            double now = service.Clock.Seconds;

            // Ativos (com Entregar nesta estacao quando for o destino).
            Lbl(content, "section-title").text = Loc.T("terminal.contracts_active", service.Log.Active.Count, ContractLog.MaxActive);
            bool anyHere = false;
            foreach (var c in service.Log.Active)
            {
                var row = Row(content, "contract-row");
                ContractCells(row, c, now, true);
                bool here = c.destinationStationId == stationId;
                anyHere |= here;
                Btn(row, Loc.T("terminal.abandon"), () => { service.Abandon(c); Refresh(); }, "btn btn-danger");
            }
            if (anyHere)
                Btn(content, Loc.T("terminal.deliver_all"), () => { service.DeliverAt(stationId); Refresh(); }, "btn btn-deliver");

            // Mural da estacao.
            Lbl(content, "section-title").text = Loc.T("terminal.contracts_board");
            foreach (var c in service.Offers(stationId))
            {
                var row = Row(content, "contract-row");
                ContractCells(row, c, now, false);
                var accept = Btn(row, Loc.T("terminal.accept"), () => { service.Accept(stationId, c); Refresh(); }, "btn");
                bool fits = c.type != ContractType.Delivery || Player.Ship.Cargo.Free >= c.quantity;
                accept.SetEnabled(fits && service.Log.Active.Count < ContractLog.MaxActive);
            }
        }

        void ContractCells(VisualElement row, Contract c, double now, bool active)
        {
            Lbl(row, "cell").text = Loc.T(c.type == ContractType.Delivery ? "contract.type_delivery" : "contract.type_requisition");
            var what = c.type == ContractType.Requisition && active
                ? $"{Loc.T("target." + c.resourceId)} {c.delivered}/{c.quantity}"
                : $"{Loc.T("target." + c.resourceId)} ×{c.quantity}";
            Lbl(row, "cell cell-wide").text = what;
            Lbl(row, "cell cell-wide").text = c.type == ContractType.Delivery
                ? $"{Loc.T("target." + c.issuerStationId)} → {Loc.T("target." + c.destinationStationId)}"
                : Loc.T("target." + c.destinationStationId);
            Lbl(row, "cell").text = $"{c.reward:N0} cr";
            Lbl(row, "cell").text = active ? ContractService.FormatTime(c.TimeLeft(now)) : ContractService.FormatTime(c.duration);
        }

        void BuildServices()
        {
            // Nome da nave (max. 18 caracteres). Na compra de nave, futuramente, o nome e pedido ali.
            var nameRow = Row(content, "market-row");
            Lbl(nameRow, "cell cell-wide").text = Loc.T("terminal.ship_name");
            var field = new TextField { value = Player.ShipName, maxLength = PlayerContext.MaxShipNameLength };
            field.AddToClassList("name-field");
            nameRow.Add(field);
            Btn(nameRow, Loc.T("terminal.rename"), () =>
            {
                var clean = PlayerContext.CleanShipName(field.value);
                if (clean.Length == 0) return;
                Player.ShipName = clean;
                GameMessages.Post("msg.ship_renamed", clean);
                Refresh();
            }, "btn");

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
