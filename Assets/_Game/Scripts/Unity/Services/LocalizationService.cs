using System;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Text in the player's chosen language. The choice is kept in PlayerPrefs; the first
    /// time, the game follows the device: Vietnamese on a Vietnamese phone, English elsewhere.
    /// </summary>
    public sealed class LocalizationService : ILocalizationService
    {
        private const string LanguageKey = "settingLanguage";

        private readonly BilingualTextCatalog catalog;

        public LocalizationService(LocalizationSO table)
        {
            catalog = new BilingualTextCatalog(table != null ? table.Entries : Array.Empty<BilingualTextEntry>());
            Current = Load();
        }

        public Language Current { get; private set; }

        public event Action OnLanguageChanged;

        public string Get(string key)
        {
            return catalog.Get(key, Current);
        }

        public void SetLanguage(Language language)
        {
            if (language == Current)
            {
                return;
            }

            Current = language;
            PlayerPrefs.SetInt(LanguageKey, (int)language);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke();
        }

        private static Language Load()
        {
            if (PlayerPrefs.HasKey(LanguageKey))
            {
                return (Language)PlayerPrefs.GetInt(LanguageKey);
            }

            return Application.systemLanguage == SystemLanguage.Vietnamese ? Language.Vietnamese : Language.English;
        }
    }
}
