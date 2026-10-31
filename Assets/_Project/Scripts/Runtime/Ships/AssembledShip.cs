using System.Collections.Generic;
using UnityEngine;

namespace PulseFactor
{
    // Referencias da nave montada pelo ShipAssembler.
    public class AssembledShip : MonoBehaviour
    {
        public ShipBlueprint Blueprint;
        public Transform Cockpit, Body, Tail, WingL, WingR;
        public Transform EyePoint;
        public readonly List<Transform> Thrusters = new();
        public readonly List<Transform> ScreenMounts = new();

        public Bounds LocalBounds()
        {
            var b = new Bounds(Vector3.zero, Vector3.one);
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                var c = transform.InverseTransformPoint(r.bounds.center);
                b.Encapsulate(new Bounds(c, r.bounds.size));
            }
            return b;
        }
    }
}
