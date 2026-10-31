using UnityEngine;
using UnityEngine.Localization;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Resource")]
    public class ResourceDef : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public int basePrice = 10;
    }
}
