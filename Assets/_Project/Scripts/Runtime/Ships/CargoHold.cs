using System;
using System.Collections.Generic;

namespace PulseFactor
{
    public class CargoHold
    {
        readonly Dictionary<ResourceDef, int> contents = new();

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

        // Retorna quanto saiu.
        public int Remove(ResourceDef resource, int amount)
        {
            int removed = Math.Min(Math.Max(amount, 0), Count(resource));
            if (removed == 0) return 0;
            int left = Count(resource) - removed;
            if (left == 0) contents.Remove(resource);
            else contents[resource] = left;
            Used -= removed;
            Changed?.Invoke();
            return removed;
        }

        public int Count(ResourceDef resource) =>
            resource != null && contents.TryGetValue(resource, out var n) ? n : 0;
    }
}
