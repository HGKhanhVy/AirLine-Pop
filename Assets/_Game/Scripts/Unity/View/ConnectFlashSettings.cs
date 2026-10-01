namespace ASTeams.SingleLine.Unity
{
    public readonly struct ConnectFlashSettings
    {
        public ConnectFlashSettings(float duration, float peakAlpha, float startScale,
            float endScale, float riseShare)
        {
            Duration = duration;
            PeakAlpha = peakAlpha;
            StartScale = startScale;
            EndScale = endScale;
            RiseShare = riseShare;
        }

        public float Duration { get; }

        public float PeakAlpha { get; }

        public float StartScale { get; }

        public float EndScale { get; }

        public float RiseShare { get; }
    }
}
