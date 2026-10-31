namespace PulseFactor
{
    public static class BlockRules
    {
        // Pecas multiclasse montam em qualquer classe; exclusivas so na classe de origem.
        public static bool CanMount(BlockDef block, ShipClass shipClass) =>
            block != null && (block.multiclass || block.originClass == shipClass);

        public static bool IsValid(ShipBlueprint bp, out string error)
        {
            error = null;
            if (bp == null) { error = "Blueprint nulo"; return false; }
            if (bp.body == null) { error = "Nave sem corpo"; return false; }
            if (bp.body.type != BlockType.Body) { error = $"'{bp.body.id}' nao e um corpo"; return false; }
            if (bp.body.classDef == null) { error = $"Corpo '{bp.body.id}' sem ShipClassDef"; return false; }

            var shipClass = bp.ShipClass;
            return CheckPart(bp.cockpit, BlockType.Cockpit, shipClass, ref error)
                && CheckPart(bp.tail, BlockType.Tail, shipClass, ref error)
                && (bp.wing == null || CheckPart(bp.wing, BlockType.Wing, shipClass, ref error)); // asas opcionais
        }

        static bool CheckPart(BlockDef part, BlockType expected, ShipClass shipClass, ref string error)
        {
            if (part == null) { error = $"Falta {expected}"; return false; }
            if (part.type != expected) { error = $"'{part.id}' nao e {expected}"; return false; }
            if (!CanMount(part, shipClass)) { error = $"'{part.id}' nao monta em {shipClass}"; return false; }
            return true;
        }
    }
}
