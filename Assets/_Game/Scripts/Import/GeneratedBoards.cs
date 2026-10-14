using System;
using System.Collections.Generic;
using AirlinePop.SingleLine.Core;

namespace AirlinePop.SingleLine.Import
{
    /// <summary>
    /// Every board the campaign is built from: tiny ones for the first flights, a batch per
    /// grid size, some swept tidily and some wandering, and a batch carrying overpasses. All
    /// drawn by <see cref="BoardGenerator"/>, so the game ships no board taken from elsewhere.
    /// Seeded, so a rebuild draws the same boards; a board identical to one already drawn,
    /// turned or mirrored, is dropped.
    /// </summary>
    public sealed class GeneratedBoards
    {
        public const int DefaultSeed = 20261013;

        /// <summary>The first flights grow from three squares to ten, so these grids are drawn whole.</summary>
        private static readonly (int Width, int Height)[] SmallSizes =
            { (3, 1), (4, 1), (2, 2), (3, 2), (2, 3), (4, 2), (3, 3), (2, 4), (3, 4), (4, 3), (4, 4) };

        private const int SmallAttempts = 60;

        private static readonly (int Width, int Height)[] PlainSizes =
            { (4, 5), (5, 5), (5, 6), (6, 6), (6, 7), (6, 8), (7, 7), (7, 8) };

        private static readonly (int Width, int Height)[] BridgeSizes =
            { (3, 3), (4, 3), (3, 4), (4, 4), (5, 6), (6, 6), (6, 7), (6, 8), (7, 8) };

        private static readonly double[] CornerBiases = { 0.0, 1.5, 4.0 };
        private static readonly double[] Fills = { 0.75, 1.0 };

        private readonly int seed;
        private readonly int attemptsPerBatch;

        public GeneratedBoards(int seed = DefaultSeed, int attemptsPerBatch = 120)
        {
            this.seed = seed;
            this.attemptsPerBatch = Math.Max(1, attemptsPerBatch);
        }

        public List<LevelData> Generate(IReadOnlyList<LevelData> existing)
        {
            var canonicalizer = new BoardCanonicalizer();
            var seen = new HashSet<BoardKey>();

            if (existing != null)
            {
                foreach (LevelData level in existing)
                {
                    seen.Add(canonicalizer.GetKey(level));
                }
            }

            var random = new Random(seed);
            var boards = new List<LevelData>();

            foreach ((int width, int height) in SmallSizes)
            {
                var settings = new BoardGeneratorSettings(width, height, 3, width * height, 1.0, 1.5);
                Draw(settings, "gens", random, canonicalizer, seen, boards, SmallAttempts);
            }

            foreach ((int width, int height) in PlainSizes)
            {
                foreach (double bias in CornerBiases)
                {
                    foreach (double fill in Fills)
                    {
                        var settings = new BoardGeneratorSettings(width, height, width * height / 2, width * height, fill, bias);
                        Draw(settings, "gen", random, canonicalizer, seen, boards, attemptsPerBatch);
                    }
                }
            }

            foreach ((int width, int height) in BridgeSizes)
            {
                foreach (double fill in Fills)
                {
                    var settings = new BoardGeneratorSettings(width, height, width * height / 2, width * height, fill, 1.5, 1, 2, 0.3);
                    Draw(settings, "genx", random, canonicalizer, seen, boards, attemptsPerBatch);
                }
            }

            return boards;
        }

        private void Draw(BoardGeneratorSettings settings, string prefix, Random random, BoardCanonicalizer canonicalizer,
            HashSet<BoardKey> seen, List<LevelData> boards, int attempts)
        {
            var generator = new BoardGenerator(settings);

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                string id = prefix + "_" + settings.Width + "x" + settings.Height + "_" + boards.Count.ToString("00000");
                LevelData board = generator.TryGenerate(id, random);

                if (board == null)
                {
                    continue;
                }

                // Overpass boards may share a shape with each other, but never with a plain board
                // already drawn or with a board from the reference packs.
                BoardKey key = canonicalizer.GetKey(board);

                if (board.BridgeCount == 0 ? !seen.Add(key) : seen.Contains(key))
                {
                    continue;
                }

                boards.Add(board);
            }
        }
    }
}
