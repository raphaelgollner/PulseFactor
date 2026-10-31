using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace PulseFactor
{
    public static class ProximityRules
    {
        // Playtest 2026-10-31: zona reduzida 80% (antes 1 R). Pode chegar perto, so nao pode encostar.
        public const double DefaultDangerFactor = 0.2;

        // Perigo: mais perto da superficie do que (fator x raio).
        public static bool InDanger(double surfaceDistance, double radius, double factor) => surfaceDistance < radius * factor;

        public static int Nearest(double3 ship, IReadOnlyList<(double3 center, double radius)> bodies, out double surface)
        {
            int best = -1;
            surface = double.PositiveInfinity;
            for (int i = 0; i < bodies.Count; i++)
            {
                double d = math.max(0, math.distance(ship, bodies[i].center) - bodies[i].radius);
                if (d < surface) { surface = d; best = i; }
            }
            return best;
        }

        public static Vector3 EscapeDirection(double3 ship, double3 bodyCenter) =>
            (Vector3)(float3)math.normalize(ship - bodyCenter);
    }

    // Sem pouso em planetas: muito perto de um corpo celeste, a nave sai da dobra,
    // um alerta e emitido e o piloto automatico aponta para longe do corpo por alguns segundos.
    [RequireComponent(typeof(ShipController))]
    public class ProximityGuard : MonoBehaviour
    {
        [SerializeField] double dangerFactor = ProximityRules.DefaultDangerFactor;
        [SerializeField] float evasionSeconds = 4f;
        [SerializeField] double minBodyRadius = 5e5; // so planetas e luas; o asteroide gigante do Cinturao tem estacao colada nele

        public WarpController Warp;

        public bool AlertActive { get; private set; }
        public string AlertBodyId { get; private set; }

        ShipController ship;
        float evasionLeft;
        Vector3 escape;
        readonly List<(double3 center, double radius)> bodies = new();
        readonly List<string> ids = new();

        void Awake() => ship = GetComponent<ShipController>();

        void Update()
        {
            var origin = FloatingOrigin.Instance;
            if (!origin) return;

            bodies.Clear(); ids.Clear();
            foreach (var t in TargetRegistry.All)
                if (t is CelestialBody b && b.Radius >= minBodyRadius) { bodies.Add((b.Position.value, b.Radius)); ids.Add(b.Id); }

            var shipWorld = origin.ToWorld(transform.position).value;
            int i = ProximityRules.Nearest(shipWorld, bodies, out double surface);
            bool danger = i >= 0 && ProximityRules.InDanger(surface, bodies[i].radius, dangerFactor);

            if (danger)
            {
                if (!AlertActive) GameMessages.Post("msg.proximity_alert", Loc.T("target." + ids[i]));
                AlertActive = true;
                AlertBodyId = ids[i];
                escape = ProximityRules.EscapeDirection(shipWorld, bodies[i].center);
                evasionLeft = evasionSeconds;
                if (Warp && Warp.Drive.State != WarpState.Idle) Warp.ForceExit();
            }

            if (evasionLeft > 0f)
            {
                evasionLeft -= Time.deltaTime;
                ship.AimOverride = escape;
                ship.ThrottleOverride = 1f;
            }
            else
            {
                ship.AimOverride = null;
                ship.ThrottleOverride = null;
                AlertActive = danger;
            }
        }
    }
}
