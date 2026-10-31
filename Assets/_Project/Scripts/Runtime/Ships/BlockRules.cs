namespace PulseFactor
{
    public static class BlockRules
    {
        // Pecas multiclasse montam em qualquer classe; exclusivas so na classe de origem.
        public static bool CanMount(BlockDef block, ShipClass shipClass) =>
            block != null && (block.multiclass || block.originClass == shipClass);

        // Com o corpo: tambem precisa ser do mesmo tamanho (P, M, G).
        public static bool CanMount(BlockDef block, BlockDef body) =>
            body != null && body.classDef != null && CanMount(block, body.classDef.shipClass) && block.size == body.size;

        public static bool IsValid(ShipBlueprint bp, out string error)
        {
            error = null;
            if (bp == null) { error = "Blueprint nulo"; return false; }
            if (bp.body == null) { error = "Nave sem corpo"; return false; }
            if (bp.body.type != BlockType.Body) { error = $"'{bp.body.id}' nao e um corpo"; return false; }
            if (bp.body.classDef == null) { error = $"Corpo '{bp.body.id}' sem ShipClassDef"; return false; }

            return CheckPart(bp.cockpit, BlockType.Cockpit, bp.body, ref error)
                && CheckPart(bp.tail, BlockType.Tail, bp.body, ref error)
                && (bp.wing == null || CheckPart(bp.wing, BlockType.Wing, bp.body, ref error)); // asas opcionais
        }

        static bool CheckPart(BlockDef part, BlockType expected, BlockDef body, ref string error)
        {
            if (part == null) { error = $"Falta {expected}"; return false; }
            if (part.type != expected) { error = $"'{part.id}' nao e {expected}"; return false; }
            if (!CanMount(part, body.classDef.shipClass)) { error = $"'{part.id}' nao monta em {body.classDef.shipClass}"; return false; }
            if (part.size != body.size) { error = $"'{part.id}' ({part.size}) nao monta em corpo {body.size}"; return false; }
            return true;
        }
    }
}
