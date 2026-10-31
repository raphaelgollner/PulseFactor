using System;
using UnityEngine;

namespace PulseFactor
{
    public enum WarpState { Idle, Charging, Active, Exiting }

    public enum WarpFailReason { None, MassLocked, AlreadyActive }

    [Serializable]
    public class WarpSettings
    {
        public float chargeTime = 5f;
        public float damageDelay = 1f;
        public float maxDamageDelay = 3f;
        public double[] levelSpeeds = { 2e5, 5e5, 1e6 }; // m/s: niveis 1, 2 e 3 da Velocidade de Dobra
        public double rampSeconds = 3;                  // tempo para ir de 0 ao teto do nivel
        public double brakeK = 0.5;                     // 1/s: v <= brakeK * distancia ao alvo (indo na direcao dele)
        public double bodyK = 1.0;                      // 1/s: v <= bodyK * distancia a superficie do corpo mais proximo
        public double exitDistance = 10000;
    }

    // Logica pura da Velocidade de Dobra: aquecimento, 3 niveis (marchas), freio perto do alvo e de corpos.
    public class WarpDrive
    {
        readonly WarpSettings s;
        float elapsed, addedDelay;

        public WarpDrive(WarpSettings settings) { s = settings; }

        public WarpState State { get; private set; }
        public WarpSettings Settings => s;
        public int Level { get; private set; } = 1;
        public int MaxLevel => s.levelSpeeds.Length;
        public double LevelSpeed => s.levelSpeeds[Level - 1];
        public float ChargeTotal => s.chargeTime + addedDelay;
        public float ChargeRemaining => State == WarpState.Charging ? Mathf.Max(0f, ChargeTotal - elapsed) : 0f;
        public float ChargeProgress => State == WarpState.Charging ? Mathf.Clamp01(elapsed / ChargeTotal) : 0f;
        public double Speed { get; private set; }

        public bool TryStartCharge(bool massLocked, out WarpFailReason reason)
        {
            reason = State != WarpState.Idle ? WarpFailReason.AlreadyActive
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

        public void ShiftUp() { if (State == WarpState.Active) Level = Math.Min(Level + 1, MaxLevel); }
        public void ShiftDown() { if (State == WarpState.Active) Level = Math.Max(Level - 1, 1); }

        public void Cancel()
        {
            State = WarpState.Idle;
            Speed = 0;
            Level = 1;
        }

        public void Complete() => Cancel();

        // Retorna a velocidade de dobra (m/s); 0 fora do estado Active.
        // distToTarget = infinito quando nao ha alvo.
        public double Tick(float dt, double distToNearestBody, double distToTarget, bool headingToTarget)
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
                    // Troca de nivel acelera/desacelera em rampa; freios de alvo e de corpo valem na hora.
                    double ramp = s.levelSpeeds[MaxLevel - 1] / s.rampSeconds * dt;
                    double v = Speed + Math.Clamp(LevelSpeed - Speed, -ramp, ramp);
                    if (headingToTarget) v = Math.Min(v, s.brakeK * distToTarget);
                    v = Math.Min(v, s.bodyK * distToNearestBody);
                    Speed = Math.Max(v, 0);
                    break;
                default:
                    Speed = 0;
                    break;
            }
            return Speed;
        }
    }
}
