using UnityEngine;
using UnityEngine.Localization;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Corporation")]
    public class CorporationDef : ScriptableObject
    {
        public string id;
        public LocalizedString displayName;
        public Color color = Color.white;
    }
}
