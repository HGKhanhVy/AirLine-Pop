namespace ASTeams.SingleLine.Data
{
    public interface ILevelProgressStore
    {
        int CurrentLevelNumber { get; }

        void SaveCurrentLevel(int levelNumber);
    }
}
