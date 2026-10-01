using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The one localization service for the whole game, like the SDK's audio and save
    /// services: every scene and every label reads the same language. Created on first use
    /// from the table in Resources, loaded once.
    /// </summary>
    public static class Localization
    {
        private static ILocalizationService service;

        public static ILocalizationService Service =>
            service ??= new LocalizationService(Resources.Load<LocalizationSO>(LocalizationSO.ResourcePath));

        public static Language Current => Service.Current;

        public static string Get(string key)
        {
            return Service.Get(key);
        }

        /// <summary>Fills a format line, such as "Flight {0}", in the current language.</summary>
        public static string Format(string key, params object[] args)
        {
            return string.Format(Service.Get(key), args);
        }
    }
}
