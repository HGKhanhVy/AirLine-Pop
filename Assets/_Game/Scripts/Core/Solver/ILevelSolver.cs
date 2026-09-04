using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Finds a Hamiltonian path over the active cells of a level. Abstracted so the
    /// validator, the editor tooling and the hint system depend on the contract rather
    /// than on a concrete search strategy.
    ///
    /// Implementations write into a caller supplied buffer instead of returning a new
    /// array, so batch validation over the whole level set stays allocation free.
    /// </summary>
    public interface ILevelSolver
    {
        /// <summary>Nodes expanded by the most recent search, for profiling level difficulty.</summary>
        long LastNodeCount { get; }

        /// <summary>
        /// True when the last search stopped on its own budget rather than proving the
        /// level unsolvable. Callers must not read that as "no solution exists".
        /// </summary>
        bool LastRunHitBudget { get; }

        /// <summary>
        /// Searches for a solution that begins at <paramref name="startCell"/>.
        /// </summary>
        /// <param name="destination">
        /// Buffer of at least <see cref="LevelData.ActiveCellCount"/> entries, filled
        /// with the solution on success.
        /// </param>
        /// <param name="length">Number of entries written, zero on failure.</param>
        bool TrySolve(LevelData level, int startCell, int[] destination, out int length);

        /// <summary>
        /// Searches from the pinned start when the level has one, otherwise tries every
        /// active cell until a solution is found.
        /// </summary>
        /// <param name="startCell">
        /// The start the solution begins at, or <see cref="LevelData.NoCell"/> on failure.
        /// </param>
        bool TrySolveAny(LevelData level, int[] destination, out int length, out int startCell);

        /// <summary>
        /// Finds a way to finish a level from a path already drawn, which is what a hint
        /// needs: the question is never "how is this level solved" but "what should I do
        /// from here", and the two can have different answers once the player has
        /// committed to a route.
        /// </summary>
        /// <param name="pathSoFar">Cells already visited, in the order they were entered.</param>
        /// <param name="destination">
        /// Filled with the whole path, the drawn part first, so the next cell to play is
        /// at index <c>pathSoFar.Count</c>.
        /// </param>
        bool TryContinue(LevelData level, IReadOnlyList<int> pathSoFar, int[] destination, out int length);
    }
}
