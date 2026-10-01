namespace ASTeams.SingleLine.Unity
{
    /// <summary>The current calendar day, for daily care limits.</summary>
    public interface IDayClock
    {
        int Today { get; }
    }
}
