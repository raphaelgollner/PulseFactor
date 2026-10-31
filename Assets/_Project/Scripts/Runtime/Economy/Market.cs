using UnityEngine;

namespace PulseFactor
{
    public enum TradeFailReason { None, InvalidQuantity, NotSold, NotBought, NoCargoSpace, NotEnoughCredits, NotInCargo }

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

        public Market(StationDef def, GameConfig config)
        {
            this.def = def;
            this.config = config;
        }

        public StationDef Def => def;

        public MarketEntry Entry(ResourceDef r) => def.market.Find(e => e.resource == r);

        public int PriceOf(ResourceDef r)
        {
            var e = Entry(r);
            return Mathf.RoundToInt(r.basePrice * (e != null ? e.priceMultiplier : 1f));
        }

        // Modo de teste: comprar nao desconta creditos (o preco ainda e informado).
        public TradeResult Buy(ResourceDef r, int qty, CargoHold cargo, Wallet wallet)
        {
            if (qty <= 0) return TradeResult.Fail(TradeFailReason.InvalidQuantity);
            var e = Entry(r);
            if (e == null || !e.sells) return TradeResult.Fail(TradeFailReason.NotSold);

            qty = Mathf.Min(qty, cargo.Free);
            if (qty == 0) return TradeResult.Fail(TradeFailReason.NoCargoSpace);

            int price = PriceOf(r);
            bool free = config != null && config.testMode;
            if (!free) qty = (int)Mathf.Min(qty, price > 0 ? wallet.Credits / price : qty);
            if (qty == 0) return TradeResult.Fail(TradeFailReason.NotEnoughCredits);

            long cost = (long)price * qty;
            if (!free) wallet.TrySpend(cost);
            cargo.Add(r, qty);
            return new TradeResult { ok = true, quantity = qty, credits = cost };
        }

        public TradeResult Sell(ResourceDef r, int qty, CargoHold cargo, Wallet wallet)
        {
            if (qty <= 0) return TradeResult.Fail(TradeFailReason.InvalidQuantity);
            var e = Entry(r);
            if (e == null || !e.buys) return TradeResult.Fail(TradeFailReason.NotBought);

            qty = cargo.Remove(r, qty);
            if (qty == 0) return TradeResult.Fail(TradeFailReason.NotInCargo);

            long gain = (long)PriceOf(r) * qty;
            wallet.Add(gain);
            return new TradeResult { ok = true, quantity = qty, credits = gain };
        }
    }
}
