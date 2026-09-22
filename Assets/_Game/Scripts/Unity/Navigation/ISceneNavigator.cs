namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Moves the player between the two screens of the game after boot: the board and
    /// the home screen.
    /// </summary>
    public interface ISceneNavigator
    {
        bool IsInGameplay { get; }

        void GoHome();

        void GoToGameplay();
    }
}
