using UnityEngine;

namespace PulseFactor
{
    [CreateAssetMenu(menuName = "PulseFactor/Ship Class")]
    public class ShipClassDef : ScriptableObject
    {
        public ShipClass shipClass;

        [Header("Voo")]
        public float acceleration = 40f;     // m/s²
        public float maxSpeed = 220f;        // m/s, porao vazio
        public float turnRateDeg = 90f;      // graus/s
        [Range(0, 1)] public float fullCargoSpeedPenalty = 0.4f;
        [Range(0, 1)] public float releaseDecelFactor = 0.3f;
        public float cruiseAdjustRate = 60f; // m/s por segundo

        [Header("Capacidades")]
        public int cargoCapacity = 50;
        public int weaponSlots = 1;
        public int skillSlots = 1;
        public HullTypeDef startingHull;
    }
}
