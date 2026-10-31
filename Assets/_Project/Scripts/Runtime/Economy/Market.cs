using UnityEngine;

namespace PulseFactor
{
    public enum TradeFailReason { None, InvalidQuantity, NotSold, NotBought, NoCargoSpace, NotEnoughCredits, NotInCargo, OutOfStock, StationFull }

    public struct TradeResult
    {
        public bool ok;
        public int quantity;
        public long credits;
        public TradeFailReason reason;

        public static TradeResult Fail(TradeFailReason r) => new() { reason = r };
    }

    public class Market
    {
        readonly StationDef def;
        readonly GameConfig config;
        readonly Reputation reputation;
        readonly Economy economy;

        public Market(StationDef def, GameConfig config, Reputation reputation = null, Economy economy = null)
        {
            this.def = def;
            this.config = config;
            this.reputation = reputation;
            this.economy = economy;
        }

        // Sem economia (ou produto sem estoque) o comercio e ilimitado.
        public bool HasStock(ResourceDef r) => economy != null && economy.Tracks(def.id, r);
        public int Stock(ResourceDef r) => economy?.Stock(def.id, r) ?? 0;
        public int Capacity(ResourceDef r) => economy?.Capacity(def.id, r) ?? 0;
        public DemandLevel Demand(ResourceDef r) => economy?.Demand(def.id, r) ?? DemandLevel.Medium;

        public float Discount => reputation != null ? reputation.DiscountFor(def.CorpId) : 0f;

        // Preco de compra (a estacao vende): sobe com estoque baixo; desconto de reputacao da corporacao dona.
        public int BuyPriceOf(ResourceDef r)
        {
            float price = BasePrice(r);
            if (HasStock(r)) price *= economy.BuyFactor(def.id, r);
            return Mathf.RoundToInt(price * (1f - Discount));
        }

        public StationDef Def => def;

        public MarketEntry Entry(ResourceDef r) => def.market.Find(e => e.resource == r);

        // Preco de venda (a estacao compra): sobe com a demanda.
        public int PriceOf(ResourceDef r)
        {
            float price = BasePrice(r);
            if (HasStock(r)) price *= economy.SellFactor(def.id, r);
            return Mathf.RoundToInt(price);
        }

        float BasePrice(ResourceDef r)
        {
            var e = Entry(r);
            return r.basePrice * (e != null ? e.priceMultiplier : 1f);
        }

        // Modo de teste: comprar nao desconta creditos (o preco ainda e informado).
        public TradeResult Buy(ResourceDef r, int qty, CargoHold cargo, Wallet wallet)
        {
            if (qty <= 0) return TradeResult.Fail(TradeFailReason.InvalidQuantity);
            var e = Entry(r);
            if (e == null || !e.sells) return TradeResult.Fail(TradeFailReason.NotSold);
            bool stocked = HasStock(r);
            if (stocked)
            {
                qty = Mathf.Min(qty, Stock(r));
                if (qty == 0) return TradeResult.Fail(TradeFailReason.OutOfStock);
            }

            qty = Mathf.Min(qty, cargo.Free);
            if (qty == 0) return TradeResult.Fail(TradeFailReason.NoCargoSpace);

            int price = BuyPriceOf(r);
            bool free = config != null && config.testMode;
            if (!free) qty = (int)Mathf.Min(qty, price > 0 ? wallet.Credits / price : qty);
            if (qty == 0) return TradeResult.Fail(TradeFailReason.NotEnoughCredits);

            long cost = (long)price * qty;
            if (!free) wallet.TrySpend(cost);
            cargo.Add(r, qty);
            if (stocked) economy.Add(def.id, r, -qty);
            return new TradeResult { ok = true, quantity = qty, credits = cost };
        }

        public TradeResult Sell(ResourceDef r, int qty, CargoHold cargo, Wallet wallet)
        {
            if (qty <= 0) return TradeResult.Fail(TradeFailReason.InvalidQuantity);
            var e = Entry(r);
            if (e == null || !e.buys) return TradeResult.Fail(TradeFailReason.NotBought);
            bool stocked = HasStock(r);
            if (stocked)
            {
                qty = Mathf.Min(qty, economy.Room(def.id, r));
                if (qty <= 0) return TradeResult.Fail(TradeFailReason.StationFull);
            }

            int price = PriceOf(r);
            qty = cargo.Remove(r, qty);
            if (qty == 0) return TradeResult.Fail(TradeFailReason.NotInCargo);

            long gain = (long)price * qty;
            wallet.Add(gain);
            if (stocked) economy.Add(def.id, r, qty);
            return new TradeResult { ok = true, quantity = qty, credits = gain };
        }
    }
}
