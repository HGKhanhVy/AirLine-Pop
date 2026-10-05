using System;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The daily gift: a regular from Bronze up brings the player some coins once a day,
    /// more the closer it has grown.
    /// </summary>
    public interface ICatGiftService
    {
        /// <summary>Raised after a gift is handed over: the cat's id and the coins it brought.</summary>
        event Action<string, int> OnGiftCollected;

        /// <summary>True when the cat has today's gift waiting.</summary>
        bool HasGift(string catId);

        /// <summary>Hands over today's gift and pays its coins; zero when there was none.</summary>
        int Collect(string catId);
    }
}
