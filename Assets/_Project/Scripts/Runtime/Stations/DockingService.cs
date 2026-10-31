using System.Collections.Generic;

namespace PulseFactor
{
    public enum DockDenyReason { None, OutOfRange, NoFreeDock, AlreadyDocked }

    public struct DockRequestResult
    {
        public bool granted;
        public int dockIndex;
        public DockDenyReason reason;
    }

    // Controle de docas de uma estacao: autoriza, cancela fora do alcance e libera.
    public class DockingService
    {
        readonly string[] occupant;
        readonly HashSet<string> docked = new();

        public DockingService(int dockCount, float requestRange = 7500f)
        {
            occupant = new string[dockCount];
            RequestRange = requestRange;
        }

        public float RequestRange { get; }
        public int DockCount => occupant.Length;

        public DockRequestResult Request(string shipId, float distance)
        {
            if (docked.Contains(shipId))
                return new DockRequestResult { reason = DockDenyReason.AlreadyDocked, dockIndex = AssignedDock(shipId) ?? -1 };
            if (distance > RequestRange)
                return new DockRequestResult { reason = DockDenyReason.OutOfRange, dockIndex = -1 };

            var existing = AssignedDock(shipId);
            if (existing.HasValue) return new DockRequestResult { granted = true, dockIndex = existing.Value };

            for (int i = 0; i < occupant.Length; i++)
            {
                if (occupant[i] != null) continue;
                occupant[i] = shipId;
                return new DockRequestResult { granted = true, dockIndex = i };
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
