using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Core.Tests
{
    /// <summary>
    /// Boards shared by several test fixtures. RippedLevel442 is real shipped data from
    /// the reference game (pack medium, file Level_442.txt, shown in game as level 441),
    /// kept here so the rules are exercised against a board a player actually solved
    /// rather than only against hand written toys.
    /// </summary>
    internal static class LevelSamples
    {
        /// <summary>6x5 board, 25 active cells, five holes at 3, 5, 11, 13, 19.</summary>
        internal static readonly int[] Level442Cells =
        {
            0, 1, 2, 4,
            6, 7, 8, 9, 10,
            12, 14, 15, 16, 17,
            18, 20, 21, 22, 23,
            24, 25, 26, 27, 28, 29
        };

        /// <summary>The solution shipped in Toturial_442.txt.</summary>
        internal static readonly int[] Level442Solution =
        {
            7, 8, 2, 1, 0, 6, 12, 18, 24, 25, 26, 27, 28,
            29, 23, 17, 16, 22, 21, 20, 14, 15, 9, 10, 4
        };

        internal const int Level442Start = 7;

        internal static LevelData CreateLevel442(bool withSolution = true)
        {
            return new LevelData(
                "medium_442",
                1,
                new Grid(6, 5),
                Level442Cells,
                solution: withSolution ? Level442Solution : null,
                difficulty: 4);
        }

        /// <summary>Every cell of a width by height grid is active.</summary>
        internal static LevelData CreateFullBoard(int width, int height, int fixedStart = LevelData.NoCell)
        {
            var grid = new Grid(width, height);
            var cells = new int[grid.CellCount];

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i] = i;
            }

            return new LevelData("full_" + width + "x" + height, 1, grid, cells, fixedStart);
        }
    }
}
