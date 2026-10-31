using System;
using System.Collections.Generic;

namespace PulseFactor
{
    // Reputacao por corporacao (-100..100) e desconto de compra por nivel.
    public class Reputation
    {
        public const int Min = -100, Max = 100;

        readonly Dictionary<string, int> values = new();

        public event Action<string, int> Changed;

        public int Get(string corp) => corp != null && values.TryGetValue(corp, out var v) ? v : 0;

        public void Change(string corp, int delta)
        {
            if (string.IsNullOrEmpty(corp) || delta == 0) return;
            int v = Math.Clamp(Get(corp) + delta, Min, Max);
            values[corp] = v;
            Changed?.Invoke(corp, v);
        }

        public void Set(string corp, int value)
        {
            if (string.IsNullOrEmpty(corp)) return;
            values[corp] = Math.Clamp(value, Min, Max);
        }

        public float DiscountFor(string corp)
        {
            int v = Get(corp);
            return v >= 80 ? 0.12f : v >= 50 ? 0.08f : v >= 25 ? 0.05f : v >= 10 ? 0.02f : 0f;
        }

        public IEnumerable<(string corp, int value)> All
        {
            get { foreach (var kv in values) yield return (kv.Key, kv.Value); }
        }
    }

    // Relogio de jogo: so corre com o jogo aberto.
    public class GameClock
    {
        public double Seconds { get; set; }

        public void Tick(float dt)
        {
            if (dt > 0) Seconds += dt;
        }
    }
}
