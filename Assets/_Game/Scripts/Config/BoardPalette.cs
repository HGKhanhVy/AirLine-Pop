using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Every colour one level is drawn with.
    ///
    /// The reference game gives each level its own hue and derives the whole board from
    /// it, which is why these arrive together rather than being read one at a time: a
    /// square coloured from level 7 next to a line coloured from level 8 would be worse
    /// than either.
    /// </summary>
    public readonly struct BoardPalette
    {
        /// <summary>A square nobody has reached yet. The same charcoal on every level.</summary>
        public Color Cell { get; }

        public Color Start { get; }

        public Color Visited { get; }

        /// <summary>The square the path ends on, lightened well past the rest.</summary>
        public Color Head { get; }

        public Color Path { get; }

        public Color Won { get; }

        public Color Stuck { get; }

        public BoardPalette(Color cell, Color start, Color visited, Color head, Color path, Color won, Color stuck)
        {
            Cell = cell;
            Start = start;
            Visited = visited;
            Head = head;
            Path = path;
            Won = won;
            Stuck = stuck;
        }
    }
}
