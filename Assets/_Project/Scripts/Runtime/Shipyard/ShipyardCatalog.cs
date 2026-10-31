using System.Collections.Generic;
using System.Linq;

namespace PulseFactor
{
    // O que um estaleiro oferece: os modelos que ele vende e as pecas desses modelos,
    // limitados aos tamanhos de doca que a estacao tem.
    public class ShipyardCatalog
    {
        readonly List<ShipModelDef> models;

        public ShipyardCatalog(StationDef station)
        {
            var sizes = station.dockSizes ?? new DockSize[0];
            models = station.shipyardModels
                .Where(m => m && m.body && sizes.Contains(m.body.size))
                .ToList();
        }

        public bool IsOpen => models.Count > 0;
        public IReadOnlyList<ShipModelDef> Models => models;

        public IEnumerable<BlockDef> Blocks => models
            .SelectMany(m => new[] { m.cockpit, m.body, m.tail, m.wing })
            .Where(b => b).Distinct();

        public IEnumerable<BlockDef> Bodies => Blocks.Where(b => b.type == BlockType.Body);

        // Pecas de um tipo que montam nesse corpo (classe e tamanho).
        public IEnumerable<BlockDef> PartsFor(BlockType type, BlockDef body) =>
            Blocks.Where(b => b.type == type && BlockRules.CanMount(b, body));

        public bool Sells(BlockDef b) => b && Blocks.Contains(b);
    }
}
