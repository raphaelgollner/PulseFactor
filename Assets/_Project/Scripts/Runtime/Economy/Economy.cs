using System;
using System.Collections.Generic;
using UnityEngine;

namespace PulseFactor
{
    public enum DemandLevel { Low, Medium, High }

    // Estoque de cada estacao por produto. O que a estacao vende ela produz (o estoque sobe com o tempo);
    // o que ela compra ela consome (o estoque desce e a demanda cresce). Taxas por hora de jogo.
    public class Economy
    {
        [Serializable] public class StockEntry { public string stationId; public string resourceId; public float stock; }

        class Slot { public MarketEntry entry; public float stock; }

        readonly Dictionary<(string station, string resource), Slot> slots = new();

        public void Register(StationDef def)
        {
            if (!def) return;
            foreach (var e in def.market)
            {
                if (!e.resource || e.capacity <= 0) continue;
                var key = (def.id, e.resource.id);
                if (!slots.ContainsKey(key)) slots[key] = new Slot { entry = e, stock = Mathf.Clamp(e.startStock, 0, e.capacity) };
            }
        }

        public bool Tracks(string stationId, ResourceDef r) => Find(stationId, r) != null;

        public int Stock(string stationId, ResourceDef r) => Find(stationId, r) is { } s ? (int)(s.stock + 1e-4f) : 0;
        public int Capacity(string stationId, ResourceDef r) => Find(stationId, r)?.entry.capacity ?? 0;
        public int Room(string stationId, ResourceDef r) => Capacity(stationId, r) - Stock(stationId, r);

        public void SetStock(string stationId, ResourceDef r, float value)
        {
            if (Find(stationId, r) is { } s) s.stock = Mathf.Clamp(value, 0, s.entry.capacity);
        }

        public void Add(string stationId, ResourceDef r, int delta)
        {
            if (Find(stationId, r) is { } s) s.stock = Mathf.Clamp(s.stock + delta, 0, s.entry.capacity);
        }

        float Ratio(string stationId, ResourceDef r) => Find(stationId, r) is { } s ? s.stock / s.entry.capacity : 0.5f;

        // Estacao vendendo: estoque cheio = 85% do preco, vazio = 135%.
        public float BuyFactor(string stationId, ResourceDef r) => 1.35f - 0.5f * Ratio(stationId, r);

        // Estacao comprando: demanda alta (estoque vazio) = 140%, estoque cheio = 60%.
        public float SellFactor(string stationId, ResourceDef r) => 1.4f - 0.8f * Ratio(stationId, r);

        public DemandLevel Demand(string stationId, ResourceDef r)
        {
            float ratio = Ratio(stationId, r);
            return ratio < 1f / 3f ? DemandLevel.High : ratio < 2f / 3f ? DemandLevel.Medium : DemandLevel.Low;
        }

        public void Tick(float gameSeconds)
        {
            if (gameSeconds <= 0) return;
            float hours = gameSeconds / 3600f;
            foreach (var s in slots.Values)
            {
                float delta = s.entry.ratePerHour * hours;
                if (s.entry.sells) s.stock += delta;
                else if (s.entry.buys) s.stock -= delta;
                s.stock = Mathf.Clamp(s.stock, 0, s.entry.capacity);
            }
        }

        public List<StockEntry> Snapshot()
        {
            var list = new List<StockEntry>();
            foreach (var kv in slots)
                list.Add(new StockEntry { stationId = kv.Key.station, resourceId = kv.Key.resource, stock = kv.Value.stock });
            return list;
        }

        // Entradas de estacoes/produtos que nao existem mais sao ignoradas.
        public void Restore(IEnumerable<StockEntry> entries)
        {
            if (entries == null) return;
            foreach (var e in entries)
                if (slots.TryGetValue((e.stationId, e.resourceId), out var s))
                    s.stock = Mathf.Clamp(e.stock, 0, s.entry.capacity);
        }

        Slot Find(string stationId, ResourceDef r) =>
            r && stationId != null && slots.TryGetValue((stationId, r.id), out var s) ? s : null;
    }
}
