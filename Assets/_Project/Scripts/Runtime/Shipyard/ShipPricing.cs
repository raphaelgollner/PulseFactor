namespace PulseFactor
{
    // Precos do estaleiro. Pecas M sao bem mais caras; pintura e a customizacao mais barata.
    public static class ShipPricing
    {
        public static int DefaultPartPrice(BlockType type, ShipClass shipClass, bool multiclass, DockSize size)
        {
            if (size != DockSize.P)
                return type switch
                {
                    BlockType.Body => 60000,
                    BlockType.Cockpit => 15000,
                    BlockType.Tail => 12000,
                    _ => 10000,
                } * (size == DockSize.G ? 3 : 1);

            int price = type switch
            {
                BlockType.Body => shipClass switch { ShipClass.Scout => 7000, ShipClass.Cargo => 9000, _ => 10000 },
                BlockType.Cockpit => 3000,
                BlockType.Tail => 2500,
                _ => 2000, // par de asas
            };
            return multiclass ? price : price * 6 / 5; // peca exclusiva +20%
        }

        public static int PaintPrice(DockSize size) => size switch { DockSize.P => 500, DockSize.M => 1500, _ => 4000 };

        public static int PartPrice(BlockDef b) =>
            b == null ? 0 : b.price > 0 ? b.price : DefaultPartPrice(b.type, b.originClass, b.multiclass, b.size);

        public static int ShipPrice(ShipBlueprint bp) =>
            PartPrice(bp.cockpit) + PartPrice(bp.body) + PartPrice(bp.tail) + PartPrice(bp.wing);

        // Modificacao: paga so as pecas trocadas (sem reembolso da antiga) e a pintura, se mudou.
        public static int ModifyPrice(ShipBlueprint current, ShipBlueprint next, bool paintChanged)
        {
            int price = 0;
            if (next.cockpit != current.cockpit) price += PartPrice(next.cockpit);
            if (next.body != current.body) price += PartPrice(next.body);
            if (next.tail != current.tail) price += PartPrice(next.tail);
            if (next.wing != current.wing) price += PartPrice(next.wing);
            if (paintChanged) price += PaintPrice(next.body ? next.body.size : DockSize.P);
            return price;
        }
    }
}
