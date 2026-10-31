using System;
using System.Collections.Generic;
using System.Linq;

namespace PulseFactor
{
    public static class ContractGenerator
    {
        public const double RequisitionSeconds = 7200;

        // 30 min dentro do setor + 25 min por salto de gate.
        public static double DeliveryDuration(int jumps) => 1800 + 1500 * Math.Max(jumps, 0);

        // 3 a 6 ofertas. Entrega: algo que a emissora vende, para outra estacao.
        // Requisicao: algo que a emissora compra, entregue nela mesma.
        public static List<Contract> Generate(StationInfo issuer, IReadOnlyList<StationInfo> others,
            Func<string, string, int> jumps, Func<string, int> price, double now, Random rng)
        {
            var sells = issuer.market.Where(m => m.sells && m.resource).Select(m => m.resource.id).ToList();
            var buys = issuer.market.Where(m => m.buys && m.resource).Select(m => m.resource.id).ToList();
            var targets = others.Where(o => o.id != issuer.id).ToList();

            var list = new List<Contract>();
            int count = rng.Next(3, 7);
            for (int i = 0; i < count; i++)
            {
                bool canDeliver = sells.Count > 0 && targets.Count > 0;
                bool canRequest = buys.Count > 0;
                if (!canDeliver && !canRequest) break;
                bool delivery = canDeliver && (!canRequest || rng.NextDouble() < 0.5);
                var c = delivery
                    ? Delivery(issuer, sells[rng.Next(sells.Count)], targets[rng.Next(targets.Count)], jumps, price, rng)
                    : Requisition(issuer, buys[rng.Next(buys.Count)], price, rng);
                if (c == null) continue;
                c.id = $"{issuer.id}-{(long)now}-{i}-{rng.Next(100000)}";
                list.Add(c);
            }
            return list;
        }

        static Contract Delivery(StationInfo issuer, string resource, StationInfo dest,
            Func<string, string, int> jumps, Func<string, int> price, Random rng)
        {
            int j = jumps(issuer.sectorId, dest.sectorId);
            if (j < 0) return null;
            int qty = rng.Next(10, 41);
            long value = (long)qty * price(resource);
            return new Contract
            {
                type = ContractType.Delivery,
                issuerStationId = issuer.id,
                issuerCorpId = issuer.corpId,
                destinationStationId = dest.id,
                resourceId = resource,
                quantity = qty,
                reward = (long)Math.Round(value * 0.25) + 400 + 600 * j,
                fine = (long)Math.Round(value * 0.5),
                duration = DeliveryDuration(j),
                repGain = 3 + j,
                repLoss = 5,
                state = ContractState.Available,
            };
        }

        static Contract Requisition(StationInfo issuer, string resource, Func<string, int> price, Random rng)
        {
            int qty = rng.Next(40, 151);
            return new Contract
            {
                type = ContractType.Requisition,
                issuerStationId = issuer.id,
                issuerCorpId = issuer.corpId,
                destinationStationId = issuer.id,
                resourceId = resource,
                quantity = qty,
                reward = (long)Math.Round(qty * price(resource) * 1.6),
                fine = 0,
                duration = RequisitionSeconds,
                repGain = 5,
                repLoss = 6,
                state = ContractState.Available,
            };
        }
    }
}
