using UnityEngine;

namespace PulseFactor
{
    // Aviso temporario em destaque: tarja no topo (terceira pessoa) ou painel central (cockpit).
    public static class HudNotice
    {
        static string key;
        static object[] args;
        static float until;

        public static void Show(string textKey, float seconds = 3.5f, params object[] arguments)
        {
            key = textKey;
            args = arguments;
            until = Time.unscaledTime + seconds;
        }

        public static bool Active => key != null && Time.unscaledTime < until;
        public static string Text => Active ? Loc.T(key, args) : "";
    }
}
