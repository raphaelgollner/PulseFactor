using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PulseFactor
{
    // Monta naves piratas com blocos do catalogo: corpo sempre Assault,
    // demais pecas exclusivas de Assault ou multiclasse.
    public class PirateShipGenerator
    {
        readonly List<BlockDef> bodies;
        readonly Dictionary<BlockType, List<BlockDef>> parts = new();

        public PirateShipGenerator(IReadOnlyList<BlockDef> catalog)
        {
            bodies = catalog.Where(b => b.type == BlockType.Body && b.classDef != null
                                        && b.classDef.shipClass == ShipClass.Assault).ToList();
            foreach (var t in new[] { BlockType.Cockpit, BlockType.Tail, BlockType.Wing })
                parts[t] = catalog.Where(b => b.type == t && BlockRules.CanMount(b, ShipClass.Assault)).ToList();
        }

        public ShipBlueprint Generate(System.Random rng)
        {
            return new ShipBlueprint
            {
                body = Pick(bodies, "Assault body", rng),
                cockpit = Pick(parts[BlockType.Cockpit], "cockpit", rng),
                tail = Pick(parts[BlockType.Tail], "tail", rng),
                wing = Pick(parts[BlockType.Wing], "wing", rng),
                paint = PiratePalette.Generate(rng),
            };
        }

        static BlockDef Pick(List<BlockDef> list, string what, System.Random rng)
        {
            if (list.Count == 0) throw new InvalidOperationException($"No {what} in catalog for pirates");
            return list[rng.Next(list.Count)];
        }
    }

    public static class PiratePalette
    {
        public static readonly Color Red = new Color32(0xC8, 0x1E, 0x1E, 0xFF);
        public static readonly Color Black = new Color32(0x14, 0x14, 0x14, 0xFF);
        public static readonly Color White = new Color32(0xED, 0xED, 0xED, 0xFF);

        static readonly Color[] colors = { Red, Black, White };

        // 50%: uma cor so (acento de outra); 50%: cada bloco e canal sorteado entre as 3.
        public static PaintScheme Generate(System.Random rng)
        {
            if (rng.NextDouble() < 0.5)
            {
                int baseIndex = rng.Next(3);
                var accent = colors[(baseIndex + 1 + rng.Next(2)) % 3];
                var paint = new BlockPaint { primary = colors[baseIndex], secondary = colors[baseIndex], accent = accent };
                return new PaintScheme { cockpit = paint, body = paint, tail = paint, wing = paint };
            }
            return new PaintScheme { cockpit = Mixed(rng), body = Mixed(rng), tail = Mixed(rng), wing = Mixed(rng) };
        }

        static BlockPaint Mixed(System.Random rng) => new()
        {
            primary = colors[rng.Next(3)],
            secondary = colors[rng.Next(3)],
            accent = colors[rng.Next(3)],
        };
    }
}
