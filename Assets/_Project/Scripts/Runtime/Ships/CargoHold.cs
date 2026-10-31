using System;
using System.Collections.Generic;

namespace PulseFactor
{
    public class CargoHold
    {
        readonly Dictionary<ResourceDef, int> contents = new();
        readonly Dictionary<ResourceDef, int> reserved = new(); // carga de contrato: nao pode ser vendida

        public CargoHold(int capacity) { Capacity = capacity; }

        public int Capacity { get; }
        public int Used { get; private set; }
        public int Free => Capacity - Used;
        public float LoadFraction => Capacity > 0 ? (float)Used / Capacity : 0f;
        public IReadOnlyDictionary<ResourceDef, int> Contents => contents;

        public event Action Changed;

        // Retorna quanto coube.
        public int Add(ResourceDef resource, int amount)
        {
            int added = Math.Min(Math.Max(amount, 0), Free);
            if (added == 0) return 0;
            contents[resource] = Count(resource) + added;
            Used += added;
            Changed?.Invoke();
            return added;
        }

        // Retorna quanto saiu. Nunca mexe na carga reservada.
        public int Remove(ResourceDef resource, int amount) =>
            Take(resource, Math.Min(Math.Max(amount, 0), Sellable(resource)));

        // Remove carga reservada (entrega de contrato ou confisco).
        public int RemoveReserved(ResourceDef resource, int amount)
        {
            int n = Math.Min(Math.Max(amount, 0), Reserved(resource));
            if (n == 0) return 0;
            SetReserved(resource, Reserved(resource) - n);
            return Take(resource, n);
        }

        public void Reserve(ResourceDef resource, int amount)
        {
            int n = Math.Min(Math.Max(amount, 0), Sellable(resource));
            if (n > 0) SetReserved(resource, Reserved(resource) + n);
        }

        public void Unreserve(ResourceDef resource, int amount) =>
            SetReserved(resource, Math.Max(0, Reserved(resource) - Math.Max(amount, 0)));

        public int Count(ResourceDef resource) =>
            resource != null && contents.TryGetValue(resource, out var n) ? n : 0;

        public int Reserved(ResourceDef resource) =>
            resource != null && reserved.TryGetValue(resource, out var n) ? n : 0;

        public int Sellable(ResourceDef resource) => Count(resource) - Reserved(resource);

        int Take(ResourceDef resource, int n)
        {
            if (n <= 0) return 0;
            int left = Count(resource) - n;
            if (left == 0) contents.Remove(resource);
            else contents[resource] = left;
            Used -= n;
            Changed?.Invoke();
            return n;
        }

        void SetReserved(ResourceDef resource, int n)
        {
            if (n <= 0) reserved.Remove(resource);
            else reserved[resource] = n;
        }
    }
}
