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

        // So as pecas da nave: o raio do laser de mineracao (LineRenderer em coordenadas do mundo) e outros
        // efeitos filhos da nave nao contam, senao a camera em terceira pessoa se afasta quilometros.
        public Bounds LocalBounds()
        {
            var b = new Bounds(Vector3.zero, Vector3.one);
            foreach (var part in new[] { Cockpit, Body, Tail, WingL, WingR })
            foreach (var r in part ? part.GetComponentsInChildren<Renderer>() : new Renderer[0])
            {
                if (r is LineRenderer or TrailRenderer or ParticleSystemRenderer) continue;
                var c = transform.InverseTransformPoint(r.bounds.center);
                b.Encapsulate(new Bounds(c, r.bounds.size));
            }
            return b;
        }
    }
}
