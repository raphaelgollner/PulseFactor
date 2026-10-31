using UnityEngine;

namespace PulseFactor
{
    public class Dock : MonoBehaviour
    {
        public int index;
        public Transform ApproachPoint;
        public Transform LandingPoint;
        public GameObject Marker;

        public void SetHighlighted(bool on)
        {
            if (Marker) Marker.SetActive(on);
        }
    }
}
