using System;
using UnityEngine;

namespace PulseFactor
{
    [Serializable]
    public struct BlockPaint
    {
        public Color primary;
        public Color secondary;
        public Color accent;
        public int pattern; // 0 = liso; padroes de pintura vem depois

        public Color Channel(int channel) => channel switch
        {
            1 => secondary,
            2 => accent,
            _ => primary,
        };

        public static BlockPaint From(ManufacturerDef m) => m == null
            ? new BlockPaint { primary = Color.white, secondary = Color.gray, accent = Color.yellow }
            : new BlockPaint { primary = m.primary, secondary = m.secondary, accent = m.accent };
    }

    [Serializable]
    public class PaintScheme
    {
        public BlockPaint cockpit;
        public BlockPaint body;
        public BlockPaint tail;
        public BlockPaint wing;

        // Pintura de fabrica: cada bloco com as cores do seu fabricante.
        public static PaintScheme FromManufacturers(ShipBlueprint bp) => new PaintScheme
        {
            cockpit = BlockPaint.From(bp.cockpit?.manufacturer),
            body = BlockPaint.From(bp.body?.manufacturer),
            tail = BlockPaint.From(bp.tail?.manufacturer),
            wing = BlockPaint.From(bp.wing?.manufacturer),
        };
    }
}
