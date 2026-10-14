using System;
using AirlinePop.SingleLine.Core;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>The language the game is shown in, and its text in that language.</summary>
    public interface ILocalizationService
    {
        Language Current { get; }

        event Action OnLanguageChanged;

        string Get(string key);

        void SetLanguage(Language language);
    }
}
