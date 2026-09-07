namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// How hard a buzz should be. Named by weight rather than by the event that asks for
    /// it, so the mapping from game events to strengths lives in one place instead of
    /// being spread through the service.
    /// </summary>
    public enum HapticStrength
    {
        Light = 0,
        Medium = 1,
        Heavy = 2
    }
}
