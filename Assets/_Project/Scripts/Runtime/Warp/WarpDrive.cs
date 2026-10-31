using System;
using UnityEngine;

namespace PulseFactor
{
    public enum WarpState { Idle, Charging, Active, Exiting }

    public enum WarpFailReason { None, NoTarget, MassLocked, AlreadyActive }

    [Serializable]
    public class WarpSettings
    {
        public float chargeTime = 5f;
        public float damageDelay = 1f;
        public float maxDamageDelay = 3f;
        public double k = 0.1;          // 1/s
        public double minSpeed = 1000;  // m/s
        public double maxSpeed = 3e8;   // m/s
        public double exitDistance = 10000;
    }

    // Logica pura da dobra: aquecimento, atraso por dano e velocidade proporcional a distancia.
    public class WarpDrive
    {
        readonly WarpSettings s;
        float elapsed, addedDelay;

        public WarpDrive(WarpSettings settings) { s = settings; }

        public WarpState State { get; private set; }
        public WarpSettings Settings => s;
        public float ChargeTotal => s.chargeTime + addedDelay;
        public float ChargeRemaining => State == WarpState.Charging ? Mathf.Max(0f, ChargeTotal - elapsed) : 0f;
        public float ChargeProgress => State == WarpState.Charging ? Mathf.Clamp01(elapsed / ChargeTotal) : 0f;
        public double Speed { get; private set; }

        public bool TryStartCharge(bool massLocked, bool hasTarget, out WarpFailReason reason)
        {
            reason = State != WarpState.Idle ? WarpFailReason.AlreadyActive
                : !hasTarget ? WarpFailReason.NoTarget
                : massLocked ? WarpFailReason.MassLocked
                : WarpFailReason.None;
            if (reason != WarpFailReason.None) return false;
            State = WarpState.Charging;
            elapsed = 0f;
            addedDelay = 0f;
            return true;
        }

        public void OnDamage()
        {
            if (State == WarpState.Charging)
                addedDelay = Mathf.Min(addedDelay + s.damageDelay, s.maxDamageDelay);
        }

        public void Cancel()
        {
            State = WarpState.Idle;
            Speed = 0;
        }

        public void Complete() => Cancel();

        // Retorna a velocidade de dobra (m/s); 0 fora do estado Active.
        public double Tick(float dt, double distToNearestMass, double distToTarget)
        {
            switch (State)
            {
                case WarpState.Charging:
                    elapsed += dt;
                    if (elapsed >= ChargeTotal) State = WarpState.Active;
                    Speed = 0;
                    break;
                case WarpState.Active:
                    if (distToTarget < s.exitDistance)
                    {
                        State = WarpState.Exiting;
                        Speed = 0;
                        break;
                    }
                    double d = Math.Min(distToNearestMass, distToTarget);
                    Speed = Math.Clamp(s.k * d, s.minSpeed, s.maxSpeed);
                    break;
                default:
                    Speed = 0;
                    break;
            }
            return Speed;
        }
    }
}
