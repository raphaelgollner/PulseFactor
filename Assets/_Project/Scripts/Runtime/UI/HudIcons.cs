using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace PulseFactor
{
    // Icones padronizados (HUD e mapa): planeta = circulo; lua = circulo menor; estacao = circulo com $;
    // asteroide = circulo irregular; nave = quadrado; pirata = quadrado vermelho; portal = anel duplo;
    // campo = 3 circulos irregulares. A forma vem do USS (classes icon-*).
    public enum IconKind { Planet, Moon, Station, Asteroid, Ship, Pirate, Gate, Field }

    public static class HudIcons
    {
        static readonly string[] classes = Array.ConvertAll((IconKind[])Enum.GetValues(typeof(IconKind)), Class);

        public static string Class(IconKind k) => "icon-" + k.ToString().ToLowerInvariant();

        public static VisualElement Create()
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("icon");
            icon.Add(new Label("$") { pickingMode = PickingMode.Ignore, name = "dollar" });
            icon.Q<Label>("dollar").AddToClassList("icon-dollar");
            var inner = new VisualElement { pickingMode = PickingMode.Ignore };
            inner.AddToClassList("icon-inner");
            icon.Add(inner);
            float[,] dots = { { -6, 1 }, { 3, -5 }, { 4, 4 } };
            for (int i = 0; i < 3; i++)
            {
                var d = new VisualElement { pickingMode = PickingMode.Ignore };
                d.AddToClassList("icon-dot");
                d.style.left = Length.Percent(50 + dots[i, 0] * 3);
                d.style.top = Length.Percent(50 + dots[i, 1] * 3);
                icon.Add(d);
            }
            return icon;
        }

        public static void Apply(VisualElement icon, IconKind kind)
        {
            foreach (var c in classes) icon.RemoveFromClassList(c);
            icon.AddToClassList(Class(kind));
        }

        public static IconKind For(ITargetable t) => t switch
        {
            CelestialBody b => b.Radius >= 3e6 ? IconKind.Planet : IconKind.Moon,
            _ => t.Kind switch
            {
                TargetKind.Station => IconKind.Station,
                TargetKind.Asteroid => IconKind.Asteroid,
                TargetKind.Pirate => IconKind.Pirate,
                TargetKind.Ship => IconKind.Ship,
                TargetKind.Gate => IconKind.Gate,
                TargetKind.Field => IconKind.Field,
                _ => IconKind.Planet,
            },
        };

        public static IconKind For(MapNodeKind k) => k switch
        {
            MapNodeKind.MainBody => IconKind.Planet,
            MapNodeKind.Moon => IconKind.Moon,
            MapNodeKind.Station => IconKind.Station,
            MapNodeKind.Field => IconKind.Field,
            _ => IconKind.Gate,
        };

        // Triangulo do alvo marcado: inimigo (pirata) vermelho para cima; aliado verde; neutro branco.
        public enum Stance { Neutral, Ally, Enemy }

        public const int AllyReputation = 25;

        public static Stance StanceOf(ITargetable t, Reputation rep)
        {
            if (t == null) return Stance.Neutral;
            if (t.Kind == TargetKind.Pirate) return Stance.Enemy;
            if (t is Station s && rep != null && rep.Get(s.Def ? s.Def.CorpId : null) >= AllyReputation) return Stance.Ally;
            return Stance.Neutral;
        }

        public static string TriangleClass(Stance s) => s switch
        {
            Stance.Ally => "tri-ally",
            Stance.Enemy => "tri-enemy",
            _ => "tri-neutral",
        };
    }

    // Feed da central de mensagens (hora real).
    public static class MessageLog
    {
        public const int Max = 40;
        static readonly List<(DateTime time, string text)> entries = new();

        public static IReadOnlyList<(DateTime time, string text)> Entries => entries;
        public static event Action Changed;

        public static void Add(string text)
        {
            entries.Insert(0, (DateTime.Now, text));
            if (entries.Count > Max) entries.RemoveAt(entries.Count - 1);
            Changed?.Invoke();
        }

        public static void Clear() => entries.Clear();
    }
}
