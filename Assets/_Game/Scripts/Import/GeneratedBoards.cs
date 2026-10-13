using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// The fresh boards the campaign draws on next to the reference packs: a batch per grid
    /// size, some swept tidily and some wandering, plus a batch carrying overpasses, which no
    /// reference board has. Seeded, so a rebuild draws the same boards; a board identical to
    /// one already in the pool, turned or mirrored, is dropped.
    /// </summary>
    public sealed class GeneratedBoards
    {
        public const int DefaultSeed = 20261013;

        private static readonly (int Width, int Height)[] PlainSizes =
            { (4, 5), (5, 5), (5, 6), (6, 6), (6, 7), (6, 8), (7, 7), (7, 8) };

        private static readonly (int Width, int Height)[] BridgeSizes = { (5, 6), (6, 6), (6, 7), (6, 8), (7, 8) };

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

            foreach ((int width, int height) in PlainSizes)
            {
                foreach (double bias in CornerBiases)
                {
                    foreach (double fill in Fills)
                    {
                        var settings = new BoardGeneratorSettings(width, height, width * height / 2, width * height, fill, bias);
                        Draw(settings, "gen", random, canonicalizer, seen, boards);
                    }
                }
            }

            foreach ((int width, int height) in BridgeSizes)
            {
                foreach (double fill in Fills)
                {
                    var settings = new BoardGeneratorSettings(width, height, width * height / 2, width * height, fill, 1.5, 1, 2, 0.3);
                    Draw(settings, "genx", random, canonicalizer, seen, boards);
                }
            }

            return boards;
        }

        private void Draw(BoardGeneratorSettings settings, string prefix, Random random, BoardCanonicalizer canonicalizer,
            HashSet<BoardKey> seen, List<LevelData> boards)
        {
            var generator = new BoardGenerator(settings);

            for (int attempt = 0; attempt < attemptsPerBatch; attempt++)
            {
                string id = prefix + "_" + settings.Width + "x" + settings.Height + "_" + boards.Count.ToString("00000");
                LevelData board = generator.TryGenerate(id, random);

                // Overpass boards share a shape with plain ones, so only plain boards are deduplicated.
                if (board == null || (board.BridgeCount == 0 && !seen.Add(canonicalizer.GetKey(board))))
                {
                    continue;
                }

                boards.Add(board);
            }
        }
    }
}
