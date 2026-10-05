namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One way a cat answers being touched in the lounge: a roll, a nuzzle, a happy hop.
    /// A new reaction is a new class, not an edit to the reactor that picks one.
    /// </summary>
    public interface ICatReaction
    {
        void Play(CatView cat);
    }
}
