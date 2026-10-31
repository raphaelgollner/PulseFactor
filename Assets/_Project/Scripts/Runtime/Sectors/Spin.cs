using UnityEngine;

namespace PulseFactor
{
    // Gira o objeto em volta do proprio eixo Z (vortice dos portais).
    public class Spin : MonoBehaviour
    {
        public float degreesPerSecond = 6f;
        void Update() => transform.localRotation *= Quaternion.Euler(0, 0, degreesPerSecond * Time.deltaTime);
    }
}
