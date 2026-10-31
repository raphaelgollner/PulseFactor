using UnityEngine;
using UnityEngine.Localization;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Manufacturer")]
    public class ManufacturerDef : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public Color primary = Color.white;
        public Color secondary = Color.gray;
        public Color accent = Color.yellow;
    }
}
