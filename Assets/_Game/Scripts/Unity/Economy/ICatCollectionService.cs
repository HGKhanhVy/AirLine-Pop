using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Which cats the player owns, which ones live in the room, and buying more.</summary>
    public interface ICatCollectionService
    {
        event Action OnCollectionChanged;

        IReadOnlyList<CatBreedSO> Breeds { get; }

        bool IsOwned(CatBreedSO breed);

        CatSave GetSave(string catId);

        /// <summary>Owned cats shown in the room, at most the configured limit.</summary>
        IReadOnlyList<string> VisibleCatIds { get; }

        string CompanionCatId { get; }

        /// <summary>
        /// Welcomes every cat whose arrival flight has been flown and who is not a regular
        /// yet, and returns them so the lounge can announce them. Empty when nobody is new.
        /// </summary>
        IReadOnlyList<CatBreedSO> WelcomeArrivals(int nextFlight);

        /// <summary>Shows or hides an owned cat in the room; false when the room is full.</summary>
        bool SetVisible(string catId, bool isVisible);

        void SetCompanion(string catId);
    }
}
