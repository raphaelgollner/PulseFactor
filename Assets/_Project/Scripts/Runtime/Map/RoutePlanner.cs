namespace PulseFactor
{
    public static class RoutePlanner
    {
        // Proximo ponto da rota no setor atual: o destino (mesmo setor) ou o portal do caminho.
        public static string NextWaypoint(GalaxyDef galaxy, string currentSectorId, string destSectorId, string destId)
        {
            if (currentSectorId == destSectorId) return destId;
            var from = galaxy.sectors.Find(s => s.id == currentSectorId);
            var to = galaxy.sectors.Find(s => s.id == destSectorId);
            return galaxy.NextGate(from, to)?.id;
        }
    }
}
