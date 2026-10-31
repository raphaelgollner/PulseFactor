using System;

namespace PulseFactor
{
    public class Wallet
    {
        public Wallet(long credits) { Credits = credits; }

        public long Credits { get; private set; }

        public event Action Changed;

        public bool TrySpend(long amount)
        {
            if (amount < 0 || amount > Credits) return false;
            Credits -= amount;
            Changed?.Invoke();
            return true;
        }

        public void Add(long amount)
        {
            if (amount <= 0) return;
            Credits += amount;
            Changed?.Invoke();
        }
    }
}
