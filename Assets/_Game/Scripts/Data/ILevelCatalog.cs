namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// Answers which level a campaign number refers to.
    ///
    /// The number is what the player and the save file talk about; the id is what the
    /// repository loads. Keeping the mapping behind an interface lets the shipped build
    /// take it from a designer owned config while a test or an isolated scene derives it
    /// from the numbering rule, without either knowing about the other.
    /// </summary>
    public interface ILevelCatalog
    {
        /// <summary>How many levels the catalog can address, 0 when it is empty.</summary>
        int LevelCount { get; }

        bool TryGetLevelId(int levelNumber, out string levelId);
    }
}
