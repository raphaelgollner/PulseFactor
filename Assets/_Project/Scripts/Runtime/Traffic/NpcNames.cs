using System.Collections.Generic;

namespace PulseFactor
{
    // Nomes de naves NPC por tipo (nomes proprios, nao traduzidos). Nao repetem no setor;
    // se a lista acabar, ganham numeracao ("Falcon II").
    public static class NpcNames
    {
        public enum Kind { Patrol, SmallTrader, HeavyTrader }

        public static readonly string[] Patrol =
        {
            "Vigilant", "Sentinel", "Aegis", "Warden", "Lance", "Bulwark", "Ranger", "Paladin", "Watchtower",
            "Steadfast", "Halberd", "Guardian", "Bastion", "Valor", "Resolute", "Spearhead", "Defiant", "Vanguard",
        };

        public static readonly string[] SmallTrader =
        {
            "Falcon", "Legacy", "Hauler Baby", "Lucky Penny", "Rusty Mule", "Sunny Days", "Second Chance",
            "Little Dipper", "Copper Kettle", "Busy Bee", "Wanderer", "Good Fortune", "Pocket Rocket",
            "Starling", "Odd Job", "Nightingale", "Quick Silver", "Last Coin", "Tumbleweed", "Daydream",
        };

        public static readonly string[] HeavyTrader =
        {
            "Big Bertha", "Iron Mule", "Mother Hen", "Long Haul", "Old Reliable", "Heavy Heart",
            "Atlas Ox", "Grand Caravan", "Steady Hand", "Slow Thunder", "Deep Pockets", "Mammoth",
        };

        public static string[] Pool(Kind k) => k switch
        {
            Kind.Patrol => Patrol,
            Kind.SmallTrader => SmallTrader,
            _ => HeavyTrader,
        };

        public static string Pick(Kind kind, System.Random rng, HashSet<string> used)
        {
            var pool = Pool(kind);
            var free = new List<string>();
            foreach (var n in pool) if (!used.Contains(n)) free.Add(n);
            string name;
            if (free.Count > 0) name = free[rng.Next(free.Count)];
            else
            {
                var baseName = pool[rng.Next(pool.Length)];
                int n = 2;
                do name = $"{baseName} {Roman(n++)}"; while (used.Contains(name));
            }
            used.Add(name);
            return name;
        }

        // "Falcon II" -> "Falcon"
        public static string BaseName(string name)
        {
            int i = name.LastIndexOf(' ');
            if (i < 0) return name;
            var suffix = name.Substring(i + 1);
            foreach (var c in suffix) if ("IVXLC".IndexOf(c) < 0) return name;
            return name.Substring(0, i);
        }

        static string Roman(int n)
        {
            string[] r = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            return n < r.Length ? r[n] : n.ToString();
        }
    }

    // Nome de exibicao de qualquer alvo: proprio (naves NPC) ou da tabela de textos.
    public interface INamed
    {
        string DisplayName { get; }
    }

    public static class TargetNames
    {
        public static string Of(ITargetable t) =>
            t is INamed n && !string.IsNullOrEmpty(n.DisplayName) ? n.DisplayName : Loc.T("target." + t.DisplayId);

        // Com o tipo junto, para naves NPC: "Falcon — Cargueiro".
        public static string WithKind(ITargetable t) =>
            t is INamed n && !string.IsNullOrEmpty(n.DisplayName) ? $"{n.DisplayName} — {Loc.T("target." + t.DisplayId)}" : Of(t);
    }
}
