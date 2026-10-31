using UnityEngine;

namespace PulseFactor
{
    // Algo com afiliacao (corporacao): estacoes e naves NPC.
    public interface IAffiliated
    {
        string CorpId { get; }
    }

    // Nave NPC (patrulha ou comercio): alvo selecionavel, aparece no HUD so dentro do scanner.
    [RequireComponent(typeof(WorldAnchor))]
    public class NpcShip : MonoBehaviour, ITargetable, IAffiliated, INamed
    {
        public string Id;
        public string Name;     // nome proprio da nave (NpcNames)
        public string Role;     // "npc_patrol" ou "npc_trader" (chave do nome no HUD)
        public string Corp;

        WorldAnchor anchor;

        public WorldPosition Position => anchor.World;
        public string DisplayId => Role;
        public TargetKind Kind => TargetKind.Ship;
        public string CorpId => Corp;
        public string DisplayName => Name;

        void Awake() => anchor = GetComponent<WorldAnchor>();
        void OnEnable() => TargetRegistry.Register(this);
        void OnDisable() => TargetRegistry.Unregister(this);
    }
}
