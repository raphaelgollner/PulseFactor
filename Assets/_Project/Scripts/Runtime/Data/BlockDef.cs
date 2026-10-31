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
        public DockSize size = DockSize.P;   // peca M so monta em corpo M
        public int price;                    // 0 = preco padrao (ShipPricing)
        public GameObject prefab;
        [Tooltip("Somente corpos: define a classe e os atributos da nave.")]
        public ShipClassDef classDef;

        // Modelo de origem ("cruise_wing" -> "cruise"), para o nome da peca.
        public string ModelId => string.IsNullOrEmpty(id) || id.LastIndexOf('_') < 0 ? id : id.Substring(0, id.LastIndexOf('_'));
    }
}
