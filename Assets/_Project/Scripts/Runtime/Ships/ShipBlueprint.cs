using System;

namespace PulseFactor
{
    [Serializable]
    public class ShipBlueprint
    {
        public BlockDef cockpit;
        public BlockDef body;
        public BlockDef tail;
        public BlockDef wing;
        public PaintScheme paint;

        // O corpo define a classe da nave.
        public ShipClass ShipClass => body.classDef.shipClass;
    }
}
