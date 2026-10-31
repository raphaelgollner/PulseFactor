using System;

namespace PulseFactor
{
    public class AsteroidDeposit
    {
        public AsteroidDeposit(ResourceDef resource, int amount)
        {
            Resource = resource;
            Remaining = amount;
        }

        public ResourceDef Resource { get; }
        public int Remaining { get; private set; }
        public bool Depleted => Remaining <= 0;

        public int Extract(int amount)
        {
            int n = Math.Min(Math.Max(amount, 0), Remaining);
            Remaining -= n;
            return n;
        }
    }

    // Converte tempo de laser em unidades inteiras, acumulando a fracao.
    public class MiningProgress
    {
        public float rate = 2f; // unidades por segundo
        double accumulated;

        public int Tick(float dt, AsteroidDeposit deposit, CargoHold cargo)
        {
            if (deposit == null || deposit.Depleted || cargo.Free <= 0)
            {
                accumulated = 0;
                return 0;
            }
            accumulated += rate * (double)dt;
            int whole = (int)Math.Floor(accumulated + 1e-6);
            if (whole <= 0) return 0;
            accumulated -= whole;

            int n = Math.Min(whole, Math.Min(deposit.Remaining, cargo.Free));
            n = deposit.Extract(n);
            cargo.Add(deposit.Resource, n);
            return n;
        }
    }
}
