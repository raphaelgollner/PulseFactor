using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace PulseFactor
{
    [Serializable]
    public class MarketEntry
    {
        public ResourceDef resource;
        public bool sells;
        public bool buys;
        public float priceMultiplier = 1f;
        // Estoque: capacidade, estoque inicial e producao (se vende) ou consumo (se compra) por hora de jogo.
        // Capacidade 0 = sem estoque (comercio ilimitado, como antes).
        public int capacity;
        public int startStock;
        public float ratePerHour;
    }

    public enum StationStyle { Ring, Outpost, Industrial }

    [CreateAssetMenu(menuName = "PulseFactor/Station")]
    public class StationDef : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public string corporationId;
        public CorporationDef corporation;
        public StationStyle style = StationStyle.Ring;
        public bool hasShipyard;
        public DockSize[] dockSizes = { DockSize.P, DockSize.P, DockSize.P, DockSize.P, DockSize.M, DockSize.M, DockSize.G };

        public string CorpId => corporation ? corporation.id : corporationId;
        public List<MarketEntry> market = new();
    }
}
