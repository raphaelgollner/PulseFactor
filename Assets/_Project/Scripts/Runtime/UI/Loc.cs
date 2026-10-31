using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace PulseFactor
{
    // Acesso curto a tabela de textos "UI" (PT-BR / EN).
    public static class Loc
    {
        public const string Table = "UI";

        public static event Action LanguageChanged;
        static bool hooked;

        public static string T(string key, params object[] args)
        {
            Hook();
            try
            {
                var s = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key, args);
                if (!string.IsNullOrEmpty(s)) return s;
            }
            catch (Exception) { /* tabela ainda nao carregada: cai no fallback */ }
            return args != null && args.Length > 0 ? $"{key} {string.Join(" ", args)}" : key;
        }

        public static void SetLanguage(string code)
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale != null) LocalizationSettings.SelectedLocale = locale;
        }

        public static string CurrentLanguage =>
            LocalizationSettings.SelectedLocale ? LocalizationSettings.SelectedLocale.Identifier.Code : "pt-BR";

        static void Hook()
        {
            if (hooked) return;
            hooked = true;
            LocalizationSettings.SelectedLocaleChanged += _ => LanguageChanged?.Invoke();
        }
    }
}
