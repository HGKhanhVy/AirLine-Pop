namespace ASTeams.SingleLine.Unity
{
    /// <summary>The weather the dark of a night flight is drawn with, each from 0 to 1.</summary>
    public interface INightShade
    {
        void SetFog(float amount);

        void SetRain(float amount);

        /// <summary>How brightly lightning lights up the whole board just now.</summary>
        void SetFlash(float amount);
    }
}
