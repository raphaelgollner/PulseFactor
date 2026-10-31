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
    }

    [CreateAssetMenu(menuName = "PulseFactor/Station")]
    public class StationDef : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public string corporationId;
        public bool hasShipyard;
        public int dockCount = 4;
        public List<MarketEntry> market = new();
    }
}
