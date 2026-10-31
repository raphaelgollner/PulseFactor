using System.Collections.Generic;
using System.Linq;

namespace PulseFactor
{
    public enum DockDenyReason { None, OutOfRange, NoFreeDock, AlreadyDocked, Queued, NoDockOfSize }

    // Tamanho de doca / nave: P (pequena), M (media), G (grande).
    public enum DockSize { P, M, G }

    public struct DockRequestResult
    {
        public bool granted;
        public int dockIndex;
        public DockDenyReason reason;
        public int queuePosition;   // 1 = proximo da fila (quando reason == Queued)
        public double etaSeconds;   // previsao de vaga (NaN se desconhecida)
    }

    public struct DockGrant
    {
        public string shipId;
        public int dockIndex;
    }

    // Controle de docas de uma estacao: cada nave so pousa na plataforma do seu tamanho;
    // sem vaga, entra numa fila por tamanho. NPCs informam ate quando ficam (previsao da fila).
    public class DockingService
    {
        public const float DefaultRange = 2000f;

        public static float MaxNpcStay(DockSize size) => size switch { DockSize.P => 20f, DockSize.M => 45f, _ => 60f };

        readonly string[] occupant;
        readonly double[] stayUntil;
        readonly DockSize[] sizes;
        readonly HashSet<string> docked = new();
        readonly Dictionary<DockSize, List<string>> queues = new();

        public DockingService(int dockCount, float requestRange = DefaultRange)
            : this(Enumerable.Repeat(DockSize.P, dockCount).ToArray(), requestRange) { }

        public DockingService(DockSize[] dockSizes, float requestRange = DefaultRange)
        {
            sizes = dockSizes;
            occupant = new string[dockSizes.Length];
            stayUntil = new double[dockSizes.Length];
            RequestRange = requestRange;
            foreach (DockSize s in System.Enum.GetValues(typeof(DockSize))) queues[s] = new List<string>();
        }

        public DockSize SizeOf(int dockIndex) => sizes[dockIndex];
        public float RequestRange { get; }
        public int DockCount => occupant.Length;

        public DockRequestResult Request(string shipId, float distance, DockSize shipSize = DockSize.P,
            double now = 0, double stayUntil = double.PositiveInfinity)
        {
            if (docked.Contains(shipId))
                return new DockRequestResult { reason = DockDenyReason.AlreadyDocked, dockIndex = AssignedDock(shipId) ?? -1 };
            if (distance > RequestRange)
                return new DockRequestResult { reason = DockDenyReason.OutOfRange, dockIndex = -1 };
            if (!sizes.Contains(shipSize))
                return new DockRequestResult { reason = DockDenyReason.NoDockOfSize, dockIndex = -1 };

            var existing = AssignedDock(shipId);
            if (existing.HasValue) return new DockRequestResult { granted = true, dockIndex = existing.Value };

            var queue = queues[shipSize];
            if (!queue.Contains(shipId))
            {
                int free = queue.Count == 0 ? FreeDock(shipSize) : -1; // quem esta na fila tem prioridade
                if (free >= 0)
                {
                    Occupy(free, shipId, stayUntil);
                    return new DockRequestResult { granted = true, dockIndex = free };
                }
                queue.Add(shipId);
            }
            int position = queue.IndexOf(shipId) + 1;
            return new DockRequestResult
            {
                reason = DockDenyReason.Queued, dockIndex = -1, queuePosition = position,
                etaSeconds = Estimate(shipSize, position, now),
            };
        }

        // Previsao: a k-esima saida prevista de NPC nas docas desse tamanho.
        double Estimate(DockSize size, int position, double now)
        {
            var departures = Enumerable.Range(0, occupant.Length)
                .Where(i => sizes[i] == size && occupant[i] != null && !double.IsInfinity(stayUntil[i]))
                .Select(i => stayUntil[i] - now).OrderBy(t => t).ToList();
            if (departures.Count == 0) return double.NaN;
            int k = position - 1;
            return k < departures.Count
                ? System.Math.Max(0, departures[k])
                : System.Math.Max(0, departures[departures.Count - 1]) + MaxNpcStay(size) * (k - departures.Count + 1);
        }

        // Entrega vagas livres para a fila (por ordem de chegada). Chamar a cada quadro.
        public List<DockGrant> ProcessQueue(double now = 0)
        {
            var granted = new List<DockGrant>();
            foreach (var kv in queues)
            {
                var queue = kv.Value;
                while (queue.Count > 0)
                {
                    int free = FreeDock(kv.Key);
                    if (free < 0) break;
                    var ship = queue[0];
                    queue.RemoveAt(0);
                    Occupy(free, ship, double.PositiveInfinity);
                    granted.Add(new DockGrant { shipId = ship, dockIndex = free });
                }
            }
            return granted;
        }

        public void SetStayUntil(string shipId, double until)
        {
            var i = AssignedDock(shipId);
            if (i.HasValue) stayUntil[i.Value] = until;
        }

        public int QueuePosition(string shipId)
        {
            foreach (var q in queues.Values)
            {
                int i = q.IndexOf(shipId);
                if (i >= 0) return i + 1;
            }
            return 0;
        }

        // Fora do alcance: perde a autorizacao (ainda nao docada) e sai da fila.
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
            foreach (var q in queues.Values) q.Remove(shipId);
            for (int i = 0; i < occupant.Length; i++)
                if (occupant[i] == shipId) { occupant[i] = null; stayUntil[i] = 0; }
        }

        public int? AssignedDock(string shipId)
        {
            for (int i = 0; i < occupant.Length; i++)
                if (occupant[i] == shipId) return i;
            return null;
        }

        int FreeDock(DockSize size)
        {
            for (int i = 0; i < occupant.Length; i++)
                if (occupant[i] == null && sizes[i] == size) return i;
            return -1;
        }

        void Occupy(int dock, string shipId, double until)
        {
            occupant[dock] = shipId;
            stayUntil[dock] = until;
        }
    }
}
