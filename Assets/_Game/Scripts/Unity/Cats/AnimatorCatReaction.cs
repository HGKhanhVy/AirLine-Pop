namespace ASTeams.SingleLine.Unity
{
    /// <summary>A reaction the cat's own animation already holds: rolling over, purring, hopping with joy.</summary>
    public sealed class AnimatorCatReaction : ICatReaction
    {
        private readonly int trigger;

        public AnimatorCatReaction(int trigger)
        {
            this.trigger = trigger;
        }

        public void Play(CatView cat)
        {
            cat.Trigger(trigger);
        }
    }
}
