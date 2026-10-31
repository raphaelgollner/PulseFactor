using System;
using System.Collections.Generic;
using UnityEngine;

namespace PulseFactor
{
    public enum ShipyardError { None, NotEnoughCredits, FleetFull, NoDockOfSize, CargoTooBig, NotHere, Invalid, Busy }

    // Frota do jogador em jogo: compra, modificacao e troca de nave. A nave do jogador e sempre o mesmo
    // GameObject; trocar de nave remonta as pecas, a classe e a carga. Se a nova nave for de outro tamanho,
    // o jogador vai para uma doca livre desse tamanho; sem vaga, espera na fila ("preparando sua nave").
    public class FleetService : MonoBehaviour
    {
        const string SwapId = "player_swap";

        public PlayerContext Player;
        public List<BlockDef> Catalog = new();
        public CockpitPanels Panels;

        public Fleet Fleet { get; } = new();

        // Troca esperando doca livre.
        public bool Preparing => pendingStation;
        public int PreparingQueuePosition => pendingStation ? Math.Max(1, pendingStation.Docking.QueuePosition(SwapId)) : 0;
        public event Action PreparingChanged;

        readonly Dictionary<string, BlockDef> blocks = new();
        Station pendingStation;
        Action pendingCommit;
        Func<ShipyardError> pendingCheck;

        public BlockDef Block(string id)
        {
            if (blocks.Count == 0) foreach (var b in Catalog) if (b) blocks[b.id] = b;
            return id != null && blocks.TryGetValue(id, out var block) ? block : null;
        }

        public ShipBlueprint ActiveBlueprint => Player.Visual.Blueprint;

        // Inicio de jogo / save: registra a nave em uso.
        public void Init(ShipRecord active, IEnumerable<ShipRecord> stored)
        {
            if (active == null)
            {
                active = ShipRecord.From(ActiveBlueprint, Fleet.NewId(), Player.ShipName);
                active.customPaint = ActiveBlueprint.paint != null;
            }
            Fleet.Restore(active, stored);
        }

        // ---------- Compra ----------

        public ShipyardError Buy(ShipBlueprint bp, string name, Station station)
        {
            if (Preparing) return ShipyardError.Busy;
            if (!BlockRules.IsValid(bp, out _)) return ShipyardError.Invalid;
            if (!Fleet.CanAdd) return ShipyardError.FleetFull;
            int price = ShipPricing.ShipPrice(bp) + (bp.paint != null ? ShipPricing.PaintPrice(bp.body.size) : 0);
            ShipyardError Check() => Player.Wallet.Credits < price ? ShipyardError.NotEnoughCredits
                : !Fleet.CanAdd ? ShipyardError.FleetFull : ShipyardError.None;
            var e = Check();
            if (e != ShipyardError.None) return e;

            var record = ShipRecord.From(bp, Fleet.NewId(), PlayerContext.CleanShipName(string.IsNullOrWhiteSpace(name) ? "PULSE" : name));
            return Transfer(station, bp.body.size, Check, () =>
            {
                Player.Wallet.TrySpend(price);
                StoreCargo();
                Fleet.Add(record, station.DisplayId);
                Activate(record);
                GameMessages.Post("msg.ship_bought", record.name, price);
            });
        }

        // ---------- Modificacao (mesmo tamanho; paga so o que mudou) ----------

        public int ModifyPrice(ShipBlueprint next, bool paintChanged) => ShipPricing.ModifyPrice(ActiveBlueprint, next, paintChanged);

        public ShipyardError Modify(ShipBlueprint next, bool paintChanged)
        {
            if (Preparing) return ShipyardError.Busy;
            if (!BlockRules.IsValid(next, out _)) return ShipyardError.Invalid;
            if (next.body.size != ActiveBlueprint.body.size) return ShipyardError.Invalid;
            if (Player.Ship.Cargo.Used > next.body.classDef.cargoCapacity) return ShipyardError.CargoTooBig;
            int price = ModifyPrice(next, paintChanged);
            if (Player.Wallet.Credits < price) return ShipyardError.NotEnoughCredits;

            Player.Wallet.TrySpend(price);
            var record = Fleet.Active;
            var updated = ShipRecord.From(next, record.id, Player.ShipName);
            record.cockpit = updated.cockpit; record.body = updated.body; record.tail = updated.tail; record.wing = updated.wing;
            record.customPaint = updated.customPaint; record.paint = updated.paint;
            Apply(next, CopyCargo(Player.Ship.Cargo, next.body.classDef.cargoCapacity));
            GameMessages.Post("msg.ship_modified", price);
            return ShipyardError.None;
        }

        // ---------- Troca por nave guardada ----------

        public ShipyardError Swap(string shipId, Station station)
        {
            if (Preparing) return ShipyardError.Busy;
            ShipRecord target = null;
            foreach (var s in Fleet.Stored) if (s.id == shipId) target = s;
            if (target == null || target.stationId != station.DisplayId) return ShipyardError.NotHere;

            return Transfer(station, target.Size(Block), () => ShipyardError.None, () =>
            {
                StoreCargo();
                Fleet.Swap(shipId, station.DisplayId);
                Activate(target);
                GameMessages.Post("msg.ship_swapped", target.name);
            });
        }

        public void CancelPreparing()
        {
            if (!pendingStation) return;
            pendingStation.Docking.Release(SwapId);
            pendingStation.DockGranted -= OnSwapDockGranted;
            pendingStation = null;
            pendingCommit = null;
            pendingCheck = null;
            PreparingChanged?.Invoke();
        }

        // Mesmo tamanho de doca: troca na hora. Outro tamanho: reserva uma doca (ou entra na fila).
        ShipyardError Transfer(Station station, DockSize size, Func<ShipyardError> check, Action commit)
        {
            var dock = Player.Docking.Dock;
            if (dock && dock.size == size)
            {
                commit();
                return ShipyardError.None;
            }
            var r = station.Docking.Request(SwapId, 0f, size, Time.time);
            if (r.reason == DockDenyReason.NoDockOfSize) return ShipyardError.NoDockOfSize;
            pendingStation = station;
            pendingCommit = commit;
            pendingCheck = check;
            if (r.granted) Finish(r.dockIndex);
            else
            {
                station.DockGranted += OnSwapDockGranted;
                PreparingChanged?.Invoke();
            }
            return ShipyardError.None;
        }

        void OnSwapDockGranted(DockGrant g)
        {
            if (g.shipId == SwapId) Finish(g.dockIndex);
        }

        void Finish(int dockIndex)
        {
            var station = pendingStation;
            var commit = pendingCommit;
            var check = pendingCheck;
            station.DockGranted -= OnSwapDockGranted;
            pendingStation = null;
            pendingCommit = null;
            pendingCheck = null;

            var error = check != null ? check() : ShipyardError.None;
            if (error != ShipyardError.None)
            {
                station.Docking.Release(SwapId);
                GameMessages.Post("shipyard." + error);
                PreparingChanged?.Invoke();
                return;
            }
            station.Docking.Release(DockingAutopilot.PlayerId);
            station.Docking.Rename(SwapId, DockingAutopilot.PlayerId);
            station.Docking.MarkDocked(DockingAutopilot.PlayerId);
            commit();
            Player.Docking.MoveToDock(station.Docks[dockIndex]);
            HudNotice.Show("notice.ship_ready", 6f, station.Docks[dockIndex].Number.ToString("00"));
            PreparingChanged?.Invoke();
        }

        // ---------- Montagem da nave ativa ----------

        // A carga da nave que vai ser guardada fica nela.
        void StoreCargo()
        {
            var record = Fleet.Active;
            if (record == null) return;
            record.name = Player.ShipName;
            record.cargo = Snapshot(Player.Ship.Cargo);
        }

        void Activate(ShipRecord record)
        {
            var bp = record.ToBlueprint(Block);
            var cargo = new CargoHold(bp.body.classDef.cargoCapacity);
            foreach (var e in record.cargo)
            {
                var res = Player.Contracts ? Player.Contracts.Res(e.resourceId) : null;
                if (!res) continue;
                cargo.Add(res, e.count);
                cargo.Reserve(res, e.reserved);
            }
            record.cargo = new List<SaveData.CargoEntry>();
            Player.ShipName = record.name;
            Apply(bp, cargo);
        }

        void Apply(ShipBlueprint bp, CargoHold cargo)
        {
            var go = Player.Ship.gameObject;
            ShipAssembler.Rebuild(Player.Visual, bp);
            Player.Ship.Initialize(bp.body.classDef, cargo);

            var box = go.GetComponent<BoxCollider>();
            if (box)
            {
                var bounds = Player.Visual.LocalBounds();
                box.center = bounds.center;
                box.size = bounds.size;
            }
            if (Player.Boost) Player.Boost.ResetTank();
            if (Player.Laser)
            {
                Player.Laser.Loadout = new WeaponLoadout(bp.body.classDef.weaponSlots);
                Player.Laser.Loadout.slots[0].kind = WeaponKind.MiningLaser;
                Player.Laser.Loadout.slots[0].group = FireGroup.Primary;
            }
            Player.CameraRig.Attach(go);
            if (Panels) Panels.Build(Player.Visual);
            Player.RaiseShipChanged();
        }

        public static List<SaveData.CargoEntry> Snapshot(CargoHold hold)
        {
            var list = new List<SaveData.CargoEntry>();
            foreach (var kv in hold.Contents)
                list.Add(new SaveData.CargoEntry { resourceId = kv.Key.id, count = kv.Value, reserved = hold.Reserved(kv.Key) });
            return list;
        }

        static CargoHold CopyCargo(CargoHold from, int capacity)
        {
            var to = new CargoHold(capacity);
            foreach (var kv in from.Contents)
            {
                to.Add(kv.Key, kv.Value);
                to.Reserve(kv.Key, from.Reserved(kv.Key));
            }
            return to;
        }

        void OnDestroy() => CancelPreparing();
    }
}
