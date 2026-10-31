using UnityEngine;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Block")]
    public class BlockDef : ScriptableObject
    {
        public string id;
        public BlockType type;
        public ManufacturerDef manufacturer;
        public ShipClass originClass;
        public bool multiclass;
        public GameObject prefab;
        [Tooltip("Somente corpos: define a classe e os atributos da nave.")]
        public ShipClassDef classDef;
    }
}
