using System;
using System.Collections.Generic;

namespace PulseFactor
{
    public enum AcceptResult { Ok, TooMany, NoCargoSpace, NotAvailable, UnknownResource }

    public struct DeliveryResult
    {
        public int completed;     // contratos concluidos nesta entrega
        public int partialUnits;  // unidades entregues em requisicoes ainda abertas
        public long credits;      // recompensas pagas
    }

    // Contratos ativos do jogador: aceitar, entregar, vencer e abandonar.
    public class ContractLog
    {
        public const int MaxActive = 5;

        readonly List<Contract> active = new();

        public IReadOnlyList<Contract> Active => active;

        public event Action<Contract> Completed, Failed;

        public AcceptResult Accept(Contract c, CargoHold cargo, Func<string, ResourceDef> res, double now)
        {
            if (c == null || c.state != ContractState.Available) return AcceptResult.NotAvailable;
            if (active.Count >= MaxActive) return AcceptResult.TooMany;
            var r = res(c.resourceId);
            if (!r) return AcceptResult.UnknownResource;
            if (c.type == ContractType.Delivery)
            {
                if (cargo.Free < c.quantity) return AcceptResult.NoCargoSpace;
                cargo.Add(r, c.quantity);
                cargo.Reserve(r, c.quantity);
            }
            c.state = ContractState.Active;
            c.deadline = now + c.duration;
            active.Add(c);
            return AcceptResult.Ok;
        }

        // Entrega tudo o que for possivel na estacao em que o jogador esta docado.
        public DeliveryResult Deliver(string stationId, CargoHold cargo, Wallet wallet, Reputation rep, Func<string, ResourceDef> res)
        {
            var result = new DeliveryResult();
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var c = active[i];
                if (c.destinationStationId != stationId) continue;
                var r = res(c.resourceId);
                if (!r) continue;

                if (c.type == ContractType.Delivery)
                {
                    c.delivered += cargo.RemoveReserved(r, c.Remaining);
                }
                else
                {
                    int given = cargo.Remove(r, c.Remaining);
                    c.delivered += given;
                    if (c.Remaining > 0) result.partialUnits += given;
                }

                if (c.Remaining <= 0)
                {
                    c.state = ContractState.Completed;
                    wallet.Add(c.reward);
                    rep?.Change(c.issuerCorpId, c.repGain);
                    result.credits += c.reward;
                    result.completed++;
                    active.RemoveAt(i);
                    Completed?.Invoke(c);
                }
            }
            return result;
        }

        // Contratos com prazo vencido falham (uma vez so). Retorna os que falharam agora.
        public List<Contract> Tick(double now, CargoHold cargo, Wallet wallet, Reputation rep, Func<string, ResourceDef> res)
        {
            var failed = new List<Contract>();
            for (int i = active.Count - 1; i >= 0; i--)
                if (now > active[i].deadline) failed.Add(Fail(i, cargo, wallet, rep, res));
            return failed;
        }

        public void Abandon(string id, CargoHold cargo, Wallet wallet, Reputation rep, Func<string, ResourceDef> res)
        {
            int i = active.FindIndex(c => c.id == id);
            if (i >= 0) Fail(i, cargo, wallet, rep, res);
        }

        Contract Fail(int index, CargoHold cargo, Wallet wallet, Reputation rep, Func<string, ResourceDef> res)
        {
            var c = active[index];
            active.RemoveAt(index);
            c.state = ContractState.Failed;
            var r = res(c.resourceId);
            if (c.type == ContractType.Delivery && r) cargo.RemoveReserved(r, c.Remaining); // carga confiscada
            if (c.fine > 0) wallet.TrySpend(Math.Min(c.fine, wallet.Credits));           // nunca negativo
            rep?.Change(c.issuerCorpId, -c.repLoss);
            Failed?.Invoke(c);
            return c;
        }

        // Volta os contratos de um save (a carga reservada e restaurada pelo proprio CargoHold).
        public void Restore(IEnumerable<Contract> contracts)
        {
            active.Clear();
            foreach (var c in contracts)
                if (c != null && c.state == ContractState.Active && active.Count < MaxActive) active.Add(c);
        }
    }
}
