using System.Collections.Generic;
using System.Linq;

namespace PulseFactor
{
    public enum DockDenyReason { None, OutOfRange, NoFreeDock, AlreadyDocked }

    // Tamanho de doca / nave: P (pequena), M (media), G (grande).
    public enum DockSize { P, M, G }

    public struct DockRequestResult
    {
        public bool granted;
        public int dockIndex;
        public DockDenyReason reason;
    }

    // Controle de docas de uma estacao: autoriza, cancela fora do alcance e libera.
    public class DockingService
    {
        public const float DefaultRange = 8000f;

        readonly string[] occupant;
        readonly DockSize[] sizes;
        readonly HashSet<string> docked = new();

        public DockingService(int dockCount, float requestRange = DefaultRange)
            : this(System.Linq.Enumerable.Repeat(DockSize.P, dockCount).ToArray(), requestRange) { }

        public DockingService(DockSize[] dockSizes, float requestRange = DefaultRange)
        {
            sizes = dockSizes;
            occupant = new string[dockSizes.Length];
            RequestRange = requestRange;
        }

        public DockSize SizeOf(int dockIndex) => sizes[dockIndex];

        public float RequestRange { get; }
        public int DockCount => occupant.Length;

        // A nave recebe a menor doca livre que comporta seu tamanho (nunca uma menor).
        public DockRequestResult Request(string shipId, float distance, DockSize shipSize = DockSize.P)
        {
            if (docked.Contains(shipId))
                return new DockRequestResult { reason = DockDenyReason.AlreadyDocked, dockIndex = AssignedDock(shipId) ?? -1 };
            if (distance > RequestRange)
                return new DockRequestResult { reason = DockDenyReason.OutOfRange, dockIndex = -1 };

            var existing = AssignedDock(shipId);
            if (existing.HasValue) return new DockRequestResult { granted = true, dockIndex = existing.Value };

            for (var size = shipSize; size <= DockSize.G; size++)
            {
                for (int i = 0; i < occupant.Length; i++)
                {
                    if (occupant[i] != null || sizes[i] != size) continue;
                    occupant[i] = shipId;
                    return new DockRequestResult { granted = true, dockIndex = i };
                }
            }
            return new DockRequestResult { reason = DockDenyReason.NoFreeDock, dockIndex = -1 };
        }

        // Fora do alcance a autorizacao (ainda nao docada) cai.
        public void Tick(string shipId, float distance)
        {
            if (distance > RequestRange && !docked.Contains(shipId)) Release(shipId);
        }

        public void MarkDocked(string shipId)
        {
            if (AssignedDock(shipId).HasValue) docked.Add(shipId);
        }

        public bool IsDocked(string shipId) => docked.Contains(shipId);

        public void Release(string shipId)
        {
            docked.Remove(shipId);
            for (int i = 0; i < occupant.Length; i++)
                if (occupant[i] == shipId) occupant[i] = null;
        }

        public int? AssignedDock(string shipId)
        {
            for (int i = 0; i < occupant.Length; i++)
                if (occupant[i] == shipId) return i;
            return null;
        }
    }
}
