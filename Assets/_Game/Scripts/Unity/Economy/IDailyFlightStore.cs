namespace AirlinePop.SingleLine.Unity
{
    /// <summary>When the daily flight was last flown, and how many days in a row it has been.</summary>
    public interface IDailyFlightStore
    {
        int LastFlownDay { get; }

        int DayStreak { get; }

        void Save(int day, int dayStreak);
    }
}
