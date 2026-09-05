namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The hand-off between the home scene and the gameplay scene.
    ///
    /// A scene load carries no arguments, so the one bit gameplay needs to know — whether
    /// the player already pressed Play elsewhere — has to survive the load on its own. It
    /// is read once and cleared, so opening the gameplay scene directly in the Editor
    /// still shows its own entry panel.
    /// </summary>
    public static class GameplayEntry
    {
        private static bool startsImmediately;

        /// <summary>Asks the gameplay scene to skip its entry panel and play at once.</summary>
        public static void RequestImmediateStart()
        {
            startsImmediately = true;
        }

        /// <summary>Reads the pending request and clears it.</summary>
        public static bool ConsumeImmediateStart()
        {
            bool requested = startsImmediately;
            startsImmediately = false;
            return requested;
        }
    }
}
