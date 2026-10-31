using UnityEngine;
using UnityEngine.Localization;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Hull Type")]
    public class HullTypeDef : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public int baseHp = 100;
    }
}
