using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Data;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class ChapterLevelRepositoryTests
    {
        private static string ChapterJson(string chapterId, int levelCount)
        {
            var levels = new List<LevelData>(levelCount);

            for (int i = 1; i <= levelCount; i++)
            {
                levels.Add(new LevelData(
                    chapterId + "_" + i.ToString("000"), 1, new Grid(3, 1), new[] { 0, 1, 2 }, difficulty: 2));
            }

            return LevelJsonSerializer.SerializeChapter(chapterId, levels);
        }

        private static CountingSource CreateSource()
        {
            var source = new CountingSource();
            source.Add("ch01", ChapterJson("ch01", 30));
            source.Add("ch02", ChapterJson("ch02", 30));
            return source;
        }

        [Test]
        public void GetsALevelByIdWithoutBeingToldItsChapter()
        {
            var repository = new ChapterLevelRepository(CreateSource());

            LevelData level = repository.Get("ch02_017");

            Assert.AreEqual("ch02_017", level.Id);
        }

        [Test]
        public void ReadsEachChapterOnce()
        {
            CountingSource source = CreateSource();
            var repository = new ChapterLevelRepository(source);

            repository.Get("ch01_001");
            repository.Get("ch01_015");
            repository.Get("ch01_030");

            Assert.AreEqual(1, source.ReadCount, "the chapter must be cached after the first level");
        }

        [Test]
        public void DoesNotReadAChapterNobodyAskedFor()
        {
            CountingSource source = CreateSource();
            var repository = new ChapterLevelRepository(source);

            repository.Get("ch01_001");

            Assert.AreEqual(1, source.ReadCount, "loading is lazy, one chapter at a time");
        }

        [Test]
        public void AMissingLevelIsAnEmptyAnswerNotAnException()
        {
            var repository = new ChapterLevelRepository(CreateSource());

            Assert.IsFalse(repository.TryGet("ch01_099", out LevelData _));
            Assert.IsFalse(repository.TryGet("ch99_001", out LevelData _));
        }

        [Test]
        public void GetThrowsOnAnIdThatDoesNotExist()
        {
            var repository = new ChapterLevelRepository(CreateSource());

            Assert.Throws<KeyNotFoundException>(() => repository.Get("ch99_001"));
        }

        [Test]
        public void AMissingChapterIsRememberedInsteadOfRetried()
        {
            CountingSource source = CreateSource();
            var repository = new ChapterLevelRepository(source);

            repository.TryGet("ch99_001", out LevelData _);
            repository.TryGet("ch99_002", out LevelData _);
            repository.TryGet("ch99_003", out LevelData _);

            Assert.AreEqual(1, source.ReadCount, "walking past the last chapter must not hammer the source");
        }

        [Test]
        public void ListsLevelIdsInPlayOrder()
        {
            var repository = new ChapterLevelRepository(CreateSource());

            IReadOnlyList<string> ids = repository.GetLevelIds("ch01");

            Assert.AreEqual(30, ids.Count);
            Assert.AreEqual("ch01_001", ids[0]);
            Assert.AreEqual("ch01_030", ids[29]);
        }

        [Test]
        public void ListingAnUnknownChapterGivesAnEmptyList()
        {
            var repository = new ChapterLevelRepository(CreateSource());

            Assert.AreEqual(0, repository.GetLevelIds("ch99").Count);
        }

        [Test]
        public void PreloadReportsWhetherTheChapterExists()
        {
            var repository = new ChapterLevelRepository(CreateSource());

            Assert.IsTrue(repository.TryPreloadChapter("ch01"));
            Assert.IsFalse(repository.TryPreloadChapter("ch99"));
        }

        [Test]
        public void ChapterIdIsDerivedFromTheLevelId()
        {
            Assert.AreEqual("ch01", ChapterLevelRepository.GetChapterId("ch01_007"));
            Assert.AreEqual("ch10", ChapterLevelRepository.GetChapterId("ch10_030"));
            Assert.IsNull(ChapterLevelRepository.GetChapterId("nounderscore"));
            Assert.IsNull(ChapterLevelRepository.GetChapterId("_leading"));
            Assert.IsNull(ChapterLevelRepository.GetChapterId(null));
        }
    }
}
