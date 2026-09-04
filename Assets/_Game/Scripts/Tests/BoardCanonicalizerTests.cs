using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class BoardCanonicalizerTests
    {
        /// <summary>
        /// Builds a level from an ASCII sketch, so the tests read as the shapes they are
        /// about. A '#' is an active cell, anything else is a hole.
        /// </summary>
        private static LevelData FromSketch(string id, params string[] rows)
        {
            int height = rows.Length;
            int width = rows[0].Length;
            var cells = new List<int>();

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    if (rows[row][column] == '#')
                    {
                        cells.Add(row * width + column);
                    }
                }
            }

            return new LevelData(id, 1, new Grid(width, height), cells.ToArray());
        }

        private static BoardKey KeyOf(LevelData level)
        {
            return new BoardCanonicalizer().GetKey(level);
        }

        [Test]
        public void QuarterTurn_ProducesTheSameKey()
        {
            // An L shape and the same L rotated 90 degrees clockwise.
            LevelData upright = FromSketch("upright",
                "#..",
                "#..",
                "###");

            LevelData turned = FromSketch("turned",
                "###",
                "#..",
                "#..");

            Assert.AreEqual(KeyOf(upright), KeyOf(turned));
        }

        [Test]
        public void Mirror_ProducesTheSameKey()
        {
            LevelData original = FromSketch("original",
                "##.",
                "#..",
                "#..");

            LevelData mirrored = FromSketch("mirrored",
                ".##",
                "..#",
                "..#");

            Assert.AreEqual(KeyOf(original), KeyOf(mirrored));
        }

        [Test]
        public void NonSquareBoard_MatchesItsRotation()
        {
            LevelData wide = FromSketch("wide",
                "####",
                "#..#");

            LevelData tall = FromSketch("tall",
                "##",
                ".#",
                ".#",
                "##");

            BoardKey wideKey = KeyOf(wide);
            BoardKey tallKey = KeyOf(tall);

            Assert.AreEqual(wideKey, tallKey);
            Assert.AreEqual(wideKey.Width, tallKey.Width, "both must settle on one orientation");
        }

        [Test]
        public void DifferentShapes_ProduceDifferentKeys()
        {
            LevelData square = FromSketch("square",
                "##",
                "##");

            LevelData line = FromSketch("line", "####");

            Assert.AreNotEqual(KeyOf(square), KeyOf(line));
        }

        [Test]
        public void ShapesThatDifferByOneCell_ProduceDifferentKeys()
        {
            LevelData full = FromSketch("full",
                "###",
                "###",
                "###");

            LevelData notched = FromSketch("notched",
                "###",
                "###",
                "##.");

            Assert.AreNotEqual(KeyOf(full), KeyOf(notched));
        }

        [Test]
        public void ADuplicatedCellIndex_DoesNotChangeTheKey()
        {
            var clean = new LevelData("clean", 1, new Grid(3, 1), new[] { 0, 1, 2 });
            var duplicated = new LevelData("dup", 1, new Grid(3, 1), new[] { 0, 1, 1, 2 });

            Assert.AreEqual(KeyOf(clean), KeyOf(duplicated));
        }

        [Test]
        public void StartCell_IsNotPartOfTheIdentity()
        {
            var cells = new[] { 0, 1, 2, 3 };
            var startsAtZero = new LevelData("a", 1, new Grid(4, 1), cells, fixedStart: 0);
            var startsAtThree = new LevelData("b", 1, new Grid(4, 1), cells, fixedStart: 3);

            Assert.AreEqual(KeyOf(startsAtZero), KeyOf(startsAtThree));
        }

        [Test]
        public void ReusingOneCanonicalizer_GivesStableKeys()
        {
            var canonicalizer = new BoardCanonicalizer();
            LevelData small = FromSketch("small", "##");
            LevelData big = FromSketch("big", "####", "####");

            BoardKey firstSmall = canonicalizer.GetKey(small);
            canonicalizer.GetKey(big);
            BoardKey secondSmall = canonicalizer.GetKey(small);

            Assert.AreEqual(firstSmall, secondSmall);
        }

        [Test]
        public void AllEightSymmetriesOfAnAsymmetricBoard_CollapseToOneKey()
        {
            // A shape with no symmetry of its own, so all eight renderings differ and the
            // canonical choice is doing real work.
            LevelData original = FromSketch("original",
                "###",
                "#..",
                "#.#");

            var keys = new HashSet<BoardKey>();
            var canonicalizer = new BoardCanonicalizer();
            keys.Add(canonicalizer.GetKey(original));

            foreach (LevelData variant in AllSymmetries(original))
            {
                keys.Add(canonicalizer.GetKey(variant));
            }

            Assert.AreEqual(1, keys.Count, "every symmetry must land on the same key");
        }

        /// <summary>Rebuilds a level under each of the eight symmetries, independently of the class under test.</summary>
        private static IEnumerable<LevelData> AllSymmetries(LevelData level)
        {
            Grid grid = level.Grid;

            for (int rotations = 0; rotations < 4; rotations++)
            {
                for (int mirrorPass = 0; mirrorPass < 2; mirrorPass++)
                {
                    bool mirror = mirrorPass == 1;
                    int width = (rotations & 1) == 1 ? grid.Height : grid.Width;
                    int height = (rotations & 1) == 1 ? grid.Width : grid.Height;
                    var cells = new List<int>();

                    foreach (int cell in level.DeclaredActiveCells)
                    {
                        int row = cell / grid.Width;
                        int column = cell % grid.Width;
                        int currentWidth = grid.Width;
                        int currentHeight = grid.Height;

                        if (mirror)
                        {
                            column = currentWidth - 1 - column;
                        }

                        for (int turn = 0; turn < rotations; turn++)
                        {
                            int nextRow = column;
                            int nextColumn = currentHeight - 1 - row;
                            row = nextRow;
                            column = nextColumn;
                            int swap = currentWidth;
                            currentWidth = currentHeight;
                            currentHeight = swap;
                        }

                        cells.Add(row * currentWidth + column);
                    }

                    yield return new LevelData("variant", 1, new Grid(width, height), cells.ToArray());
                }
            }
        }
    }
}
