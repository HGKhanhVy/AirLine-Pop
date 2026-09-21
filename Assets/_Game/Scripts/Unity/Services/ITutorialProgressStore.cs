namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Which tutorial steps the player has already been through (GDD 9.3), so each one
    /// is taught once and never again.
    /// </summary>
    public interface ITutorialProgressStore
    {
        bool IsDone(string stepId);

        void MarkDone(string stepId);
    }
}
