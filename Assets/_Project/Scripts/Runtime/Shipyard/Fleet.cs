using System;
using System.Collections.Generic;

namespace PulseFactor
{
    // Uma nave do jogador: pecas (ids), pintura, nome, onde esta guardada e a carga que ficou nela.
    [Serializable]
    public class ShipRecord
    {
        public string id;
        public string name;
        public string cockpit, body, tail, wing;
        public bool customPaint;
        public PaintScheme paint;
        public string stationId; // estacao onde esta guardada (vazio = em uso)
        public List<SaveData.CargoEntry> cargo = new();

        public static ShipRecord From(ShipBlueprint bp, string id, string name) => new ShipRecord
        {
            id = id, name = name,
            cockpit = bp.cockpit ? bp.cockpit.id : null,
            body = bp.body ? bp.body.id : null,
            tail = bp.tail ? bp.tail.id : null,
            wing = bp.wing ? bp.wing.id : null,
            customPaint = bp.paint != null,
            paint = bp.paint,
        };

        public ShipBlueprint ToBlueprint(Func<string, BlockDef> block) => new ShipBlueprint
        {
            cockpit = block(cockpit), body = block(body), tail = block(tail),
            wing = string.IsNullOrEmpty(wing) ? null : block(wing),
            paint = customPaint ? paint : null,
        };

        public DockSize Size(Func<string, BlockDef> block) => block(body) is { } b ? b.size : DockSize.P;
    }

    public enum FleetError { None, FleetFull, NotHere, Unknown }

    // Frota: uma nave em uso e as guardadas em estacoes (ate 15 no total).
    // So da para guardar uma nave quando se passa a usar outra.
    public class Fleet
    {
        public const int MaxShips = 15;

        public ShipRecord Active { get; private set; }
        readonly List<ShipRecord> stored = new();
        int nextId = 1;

        public IReadOnlyList<ShipRecord> Stored => stored;
        public int Count => stored.Count + (Active != null ? 1 : 0);
        public bool CanAdd => Count < MaxShips;

        public event Action Changed;

        public string NewId() => "ship_" + nextId++;

        public void SetActive(ShipRecord ship)
        {
            Active = ship;
            ship.stationId = null;
            Bump(ship.id);
            Changed?.Invoke();
        }

        // Nave nova passa a ser a ativa; a atual fica guardada na estacao.
        public FleetError Add(ShipRecord ship, string stationId)
        {
            if (!CanAdd) return FleetError.FleetFull;
            StoreActive(stationId);
            SetActive(ship);
            return FleetError.None;
        }

        // Troca pela nave guardada nesta estacao; a atual fica guardada aqui.
        public FleetError Swap(string shipId, string stationId)
        {
            var ship = stored.Find(s => s.id == shipId);
            if (ship == null) return FleetError.Unknown;
            if (ship.stationId != stationId) return FleetError.NotHere;
            stored.Remove(ship);
            StoreActive(stationId);
            SetActive(ship);
            return FleetError.None;
        }

        public IEnumerable<ShipRecord> StoredAt(string stationId) => stored.FindAll(s => s.stationId == stationId);

        public void Restore(ShipRecord active, IEnumerable<ShipRecord> storedShips)
        {
            stored.Clear();
            if (storedShips != null)
                foreach (var s in storedShips) { stored.Add(s); Bump(s.id); }
            if (active != null) SetActive(active);
            else Changed?.Invoke();
        }

        void StoreActive(string stationId)
        {
            if (Active == null) return;
            Active.stationId = stationId;
            stored.Add(Active);
            Active = null;
        }

        // Garante ids unicos depois de carregar o save.
        void Bump(string id)
        {
            if (id != null && id.StartsWith("ship_") && int.TryParse(id.Substring(5), out int n) && n >= nextId) nextId = n + 1;
        }
    }
}
