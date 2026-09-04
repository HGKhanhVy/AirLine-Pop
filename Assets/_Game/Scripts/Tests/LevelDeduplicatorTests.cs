using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class LevelDeduplicatorTests
    {
        private static LevelDeduplicator CreateDeduplicator()
        {
            return new LevelDeduplicator(new BoardCanonicalizer());
        }

        /// <summary>An L shape, and the same L rotated and mirrored.</summary>
        private static LevelData Upright(string id, int[] solution = null)
        {
            return new LevelData(id, 1, new Grid(3, 3), new[] { 0, 3, 6, 7, 8 }, solution: solution);
        }

        private static LevelData Rotated(string id)
        {
            return new LevelData(id, 1, new Grid(3, 3), new[] { 0, 1, 2, 3, 6 });
        }

        private static LevelData Different(string id)
        {
            return new LevelData(id, 1, new Grid(3, 1), new[] { 0, 1, 2 });
        }

        [Test]
        public void RotationsAndMirrors_LandInOneGroup()
        {
            List<LevelGroup> groups = CreateDeduplicator().Group(new List<LevelData>
            {
                Upright("a"), Rotated("b"), Different("c")
            });

            Assert.AreEqual(2, groups.Count);
            Assert.AreEqual(2, groups[0].Members.Count);
            Assert.AreEqual(1, groups[0].DuplicateCount);
            Assert.IsFalse(groups[1].HasDuplicates);
        }

        [Test]
        public void GroupOrder_FollowsFirstAppearance()
        {
            List<LevelGroup> groups = CreateDeduplicator().Group(new List<LevelData>
            {
                Different("first"), Upright("second")
            });

            Assert.AreEqual("first", groups[0].Representative.Id);
            Assert.AreEqual("second", groups[1].Representative.Id);
        }

        [Test]
        public void Representative_PrefersTheLevelThatShipsASolution()
        {
            // "z" sorts last but carries an authored solution, so it must still win.
            List<LevelGroup> groups = CreateDeduplicator().Group(new List<LevelData>
            {
                Upright("a"), Upright("z", new[] { 0, 3, 6, 7, 8 })
            });

            Assert.AreEqual(1, groups.Count);
            Assert.AreEqual("z", groups[0].Representative.Id);
        }

        [Test]
        public void Representative_BreaksTiesOnTheIdSoTheResultIsStable()
        {
            List<LevelGroup> forwards = CreateDeduplicator().Group(new List<LevelData>
            {
                Upright("m"), Upright("a"), Upright("x")
            });

            List<LevelGroup> backwards = CreateDeduplicator().Group(new List<LevelData>
            {
                Upright("x"), Upright("a"), Upright("m")
            });

            Assert.AreEqual("a", forwards[0].Representative.Id);
            Assert.AreEqual("a", backwards[0].Representative.Id,
                "file enumeration order must not change which level ships");
        }

        [Test]
        public void EmptyInput_ProducesNoGroups()
        {
            Assert.AreEqual(0, CreateDeduplicator().Group(new List<LevelData>()).Count);
        }
    }
}
