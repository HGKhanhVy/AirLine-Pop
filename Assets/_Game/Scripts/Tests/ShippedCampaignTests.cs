using System.Collections.Generic;
using System.IO;
using ASTeams.SingleLine.Data;
using NUnit.Framework;
using UnityEngine;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class ShippedCampaignTests
    {
        [Test]
        public void EveryReleaseLevelPassesValidatorAndOnboardingLimits()
        {
            string folder = Path.Combine(Application.dataPath, "_Game/Resources/Levels");
            string[] files = Directory.GetFiles(folder, "ch*.json");
            Assert.That(files.Length, Is.EqualTo(10));
            var ids = new HashSet<string>();
            var validator = new LevelValidator(new WarnsdorffSolver());
            int count = 0;
            int onboardingCount = 0;
            foreach (string file in files)
            {
                List<LevelData> levels = LevelJsonSerializer.DeserializeChapter(File.ReadAllText(file));
                Assert.That(levels.Count, Is.EqualTo(30), file);
                for (int i = 0; i < levels.Count; i++)
                {
                    LevelData level = levels[i];
                    Assert.That(ids.Add(level.Id), Is.True, "Duplicate id: " + level.Id);
                    Assert.That(validator.Validate(level).IsValid, Is.True, level.Id);
                    Assert.That(level.HasSolution, Is.True, level.Id);
                    if (Path.GetFileNameWithoutExtension(file) == "ch01" && i < 10)
                    {
                        Assert.That(level.ActiveCellCount, Is.InRange(3, 10), level.Id);
                        onboardingCount++;
                    }

                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(300));
            Assert.That(onboardingCount, Is.EqualTo(10));
        }
    }
}
