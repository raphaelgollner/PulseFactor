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
        public float angularResponse = 4f;   // aceleracao angular = turnRate * isto (1/s): peso da nave
        public float steerAngleDeg = 25f;    // erro de mira que pede giro maximo
        [Range(0, 1)] public float fullCargoSpeedPenalty = 0.15f;
        [Range(0, 1)] public float releaseDecelFactor = 0.3f;
        public float cruiseAdjustRate = 60f; // m/s por segundo
        [Range(0, 1)] public float strafeAccelFactor = 0.3f;  // strafe/vertical: ajuste fino, bem mais lento que o W
        [Range(0, 1)] public float strafeMaxFactor = 0.15f;   // velocidade lateral maxima = vMax * isto

        [Header("Capacidades")]
        public int cargoCapacity = 50;
        public int weaponSlots = 1;
        public int skillSlots = 1;
        public DockSize dockSize = DockSize.P; // todas as naves atuais sao P

        [Header("Modulos padrao")]
        public float boostCapacity = 150f;        // modulo Propulsor (P padrao)
        public double scannerRange = 5000;        // modulo Scanner de sinal (m)
        public HullTypeDef startingHull;
    }
}
