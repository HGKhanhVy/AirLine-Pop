namespace ASTeams.SingleLine.Unity
{
    /// <summary>Drives a set of loading screen parts together, whichever screen holds them.</summary>
    public static class LoadingAnimations
    {
        public static void Restart(LoadingAnimation[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].Stop();
                parts[i].Play();
            }
        }

        public static void Complete(LoadingAnimation[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].Complete();
            }
        }

        public static void Stop(LoadingAnimation[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].Stop();
            }
        }
    }
}
