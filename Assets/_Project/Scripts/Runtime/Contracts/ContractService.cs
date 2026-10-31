using System.Collections.Generic;
using UnityEngine;

namespace PulseFactor
{
    // Relogio de jogo, murais de contratos por estacao e contratos ativos do jogador.
    public class ContractService : MonoBehaviour
    {
        public const double BoardRefreshSeconds = 1200; // 20 min de jogo

        public GalaxyDef Galaxy;
        public List<ResourceDef> Resources = new();
        public PlayerContext Player;

        public GameClock Clock { get; } = new();
        public ContractLog Log { get; } = new();
        public Reputation Reputation { get; set; } = new();

        class Board { public List<Contract> offers = new(); public double nextRefresh; }

        readonly Dictionary<string, Board> boards = new();
        readonly Dictionary<string, ResourceDef> byId = new();
        List<StationInfo> stations;
        float tickTimer;

        void Awake()
        {
            Log.Completed += c => GameMessages.Post("msg.contract_completed", Describe(c), c.reward, c.repGain);
            Log.Failed += c => GameMessages.Post("msg.contract_failed", Describe(c), c.fine, c.repLoss);
        }

        public ResourceDef Res(string id)
        {
            if (byId.Count == 0) foreach (var r in Resources) if (r) byId[r.id] = r;
            return id != null && byId.TryGetValue(id, out var res) ? res : null;
        }

        void Update()
        {
            Clock.Tick(Time.deltaTime);
            tickTimer += Time.deltaTime;
            if (tickTimer < 1f || Player == null || !Player.Ship) return;
            tickTimer = 0f;
            Log.Tick(Clock.Seconds, Player.Ship.Cargo, Player.Wallet, Reputation, Res);
        }

        // Ofertas do mural da estacao (renova a cada 20 min de jogo).
        public IReadOnlyList<Contract> Offers(string stationId)
        {
            if (!boards.TryGetValue(stationId, out var b)) boards[stationId] = b = new Board();
            if (Clock.Seconds >= b.nextRefresh)
            {
                b.offers = Generate(stationId);
                b.nextRefresh = Clock.Seconds + BoardRefreshSeconds;
            }
            return b.offers;
        }

        List<Contract> Generate(string stationId)
        {
            stations ??= BuildStationInfos();
            var issuer = stations.Find(s => s.id == stationId);
            if (issuer == null) return new List<Contract>();
            var rng = new System.Random(stationId.GetHashCode() ^ (int)(Clock.Seconds / BoardRefreshSeconds));
            return ContractGenerator.Generate(issuer, stations, (a, b) => Galaxy.JumpsBetween(a, b),
                id => PriceAt(issuer, id), Clock.Seconds, rng);
        }

        int PriceAt(StationInfo station, string resourceId)
        {
            var r = Res(resourceId);
            if (!r) return 0;
            foreach (var e in station.market)
                if (e.resource == r) return Mathf.RoundToInt(r.basePrice * e.priceMultiplier);
            return r.basePrice;
        }

        List<StationInfo> BuildStationInfos()
        {
            var list = new List<StationInfo>();
            foreach (var s in Galaxy.sectors)
                foreach (var p in s.stations)
                    if (p.def) list.Add(new StationInfo { id = p.def.id, corpId = p.def.CorpId, sectorId = s.id, market = p.def.market });
            return list;
        }

        public AcceptResult Accept(string stationId, Contract c)
        {
            var result = Log.Accept(c, Player.Ship.Cargo, Res, Clock.Seconds);
            if (result == AcceptResult.Ok && boards.TryGetValue(stationId, out var b)) b.offers.Remove(c);
            GameMessages.Post(result == AcceptResult.Ok ? "msg.contract_accepted" : "contract." + result, Describe(c));
            return result;
        }

        public DeliveryResult DeliverAt(string stationId)
        {
            var r = Log.Deliver(stationId, Player.Ship.Cargo, Player.Wallet, Reputation, Res);
            if (r.partialUnits > 0) GameMessages.Post("msg.contract_partial", r.partialUnits);
            return r;
        }

        public void Abandon(Contract c) => Log.Abandon(c.id, Player.Ship.Cargo, Player.Wallet, Reputation, Res);

        // "Ferro ×20 → Posto Avançado Apollo"
        public static string Describe(Contract c) =>
            $"{Loc.T("target." + c.resourceId)} ×{c.quantity} → {Loc.T("target." + c.destinationStationId)}";

        public static string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var t = System.TimeSpan.FromSeconds(seconds);
            return t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes:00}:{t.Seconds:00}";
        }
    }
}
