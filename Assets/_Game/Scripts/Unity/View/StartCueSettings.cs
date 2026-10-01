namespace ASTeams.SingleLine.Unity
{
    public readonly struct StartCueSettings
    {
        public StartCueSettings(float pulseDuration, float pulseScale, float pulseAlpha,
            float reminderDelay, float reminderInterval)
        {
            PulseDuration = pulseDuration;
            PulseScale = pulseScale;
            PulseAlpha = pulseAlpha;
            ReminderDelay = reminderDelay;
            ReminderInterval = reminderInterval;
        }

        public float PulseDuration { get; }

        public float PulseScale { get; }

        public float PulseAlpha { get; }

        public float ReminderDelay { get; }

        public float ReminderInterval { get; }
    }
}
