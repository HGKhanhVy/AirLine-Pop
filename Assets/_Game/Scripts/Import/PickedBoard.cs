using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>A board handed out by <see cref="BoardPicker"/>, conditions on, with its measured wrong turns.</summary>
    public readonly struct PickedBoard
    {
        public LevelData Level { get; }

        public double Trials { get; }

        public PickedBoard(LevelData level, double trials)
        {
            Level = level;
            Trials = trials;
        }
    }
}
