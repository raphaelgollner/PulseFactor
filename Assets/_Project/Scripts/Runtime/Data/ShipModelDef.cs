using UnityEngine;
using UnityEngine.Localization;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Ship Model")]
    public class ShipModelDef : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public BlockDef cockpit;
        public BlockDef body;
        public BlockDef tail;
        public BlockDef wing;

        public ShipBlueprint ToBlueprint() => new ShipBlueprint
        {
            cockpit = cockpit, body = body, tail = tail, wing = wing,
        };
    }
}
