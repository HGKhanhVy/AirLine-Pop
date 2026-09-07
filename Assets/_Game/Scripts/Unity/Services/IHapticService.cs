namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Buzzes the device.
    ///
    /// Gameplay asks for a strength and nothing else. Whether the phone can buzz, whether
    /// the player has turned it off, and how often it is allowed to fire are all decided
    /// behind this, so the rules never have to care.
    /// </summary>
    public interface IHapticService
    {
        void Play(HapticStrength strength);
    }
}
