using UnityEngine;

namespace PulseFactor
{
    // Barra de Boost (Shift): consome enquanto segura e recarrega depois de um tempo solto.
    public class BoostTank
    {
        public const float DrainPerSecond = 30f;
        public const float RechargePerSecond = 15f;
        public const float RechargeDelay = 1f;
        public const float SpeedMultiplier = 1.5f;
        const float Empty = 0.01f; // abaixo disso a barra conta como vazia (resto de arredondamento do float)

        float idle;

        public BoostTank(float capacity)
        {
            Capacity = capacity;
            Value = capacity;
        }

        public float Capacity { get; }
        public float Value { get; private set; }
        public float Fraction => Capacity > 0 ? Value / Capacity : 0f;

        // Retorna true se o boost esta ativo neste passo.
        public bool Tick(float dt, bool wantBoost, bool allowed)
        {
            if (wantBoost && allowed && Value > Empty)
            {
                idle = 0f;
                Value -= DrainPerSecond * dt;
                if (Value <= Empty) Value = 0f;
                return true;
            }
            // Recarga so conta o tempo que passou do atraso (exato mesmo com passos grandes).
            float before = idle;
            idle += dt;
            float recharging = Mathf.Max(0f, idle - Mathf.Max(RechargeDelay, before));
            Value = Mathf.Min(Capacity, Value + RechargePerSecond * recharging);
            return false;
        }
    }

    // Scanner de sinal: naves e asteroides so aparecem dentro do alcance; o resto sempre.
    public static class Scanner
    {
        public const double DefaultRange = 5000;

        public static bool Detects(TargetKind kind, double distance, double range) =>
            kind is not (TargetKind.Ship or TargetKind.Pirate or TargetKind.Asteroid) || distance <= range;
    }
}
