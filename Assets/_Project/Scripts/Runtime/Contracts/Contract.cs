using System;
using System.Collections.Generic;

namespace PulseFactor
{
    public enum ContractType { Delivery, Requisition }
    public enum ContractState { Available, Active, Completed, Failed }

    // Entrega: a estacao fornece a carga e o jogador leva ao destino.
    // Requisicao: a estacao pede um recurso; o jogador consegue como quiser e entrega nela (varias viagens).
    [Serializable]
    public class Contract
    {
        public string id;
        public ContractType type;
        public string issuerStationId;
        public string issuerCorpId;
        public string destinationStationId;
        public string resourceId;
        public int quantity;
        public int delivered;
        public long reward;
        public long fine;
        public double duration; // segundos de jogo
        public double deadline; // segundos de jogo (absoluto), definido ao aceitar
        public int repGain;
        public int repLoss;
        public ContractState state;

        public int Remaining => quantity - delivered;
        public double TimeLeft(double now) => deadline - now;
    }

    // Dados de uma estacao para gerar contratos (sem MonoBehaviour).
    public class StationInfo
    {
        public string id;
        public string corpId;
        public string sectorId;
        public IReadOnlyList<MarketEntry> market;
    }
}
