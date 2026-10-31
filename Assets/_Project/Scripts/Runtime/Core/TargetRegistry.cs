using System.Collections.Generic;

namespace PulseFactor
{
    // Tudo que pode ser selecionado como alvo (corpos, estacoes, asteroides, naves).
    public static class TargetRegistry
    {
        static readonly List<ITargetable> all = new();
        public static IReadOnlyList<ITargetable> All => all;

        public static void Register(ITargetable t) { if (!all.Contains(t)) all.Add(t); }
        public static void Unregister(ITargetable t) => all.Remove(t);
    }

    // Mensagens curtas para o HUD (chave da tabela de localizacao + argumentos).
    public static class GameMessages
    {
        public static event System.Action<string, object[]> Posted;
        public static void Post(string key, params object[] args) => Posted?.Invoke(key, args);
    }
}
