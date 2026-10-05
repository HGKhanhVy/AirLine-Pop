namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The level the route map last showed the plane heading for, so the next visit can fly
    /// it on from there instead of jumping.
    /// </summary>
    public interface IRouteMapMemory
    {
        int LastShownLevel { get; }

        void Remember(int levelNumber);
    }
}
