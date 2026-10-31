using UnityEngine;

namespace PulseFactor
{
    public struct FlightInput
    {
        public float throttle;        // -1..1 (W/S)
        public Vector2 strafe;        // x = A/D, y = Espaco/C
        public float roll;            // -1..1 (Q/E)
        public Vector3 aimDirection;  // mundo, normalizado
        public bool cruiseToggle;     // pulso de um frame
    }
}
