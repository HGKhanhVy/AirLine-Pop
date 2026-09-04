using System;
using System.Collections.Generic;
using System.Diagnostics;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Runs the whole import in one place: parse, collapse duplicates, check and repair
    /// solutions, score, then lay the campaign onto the difficulty curve.
    ///
    /// The editor window and the command line harness both go through here, so there is
    /// one definition of what an import is. Wiring the steps up twice is how the two
    /// drift apart and how a step gets forgotten, which already happened once with the
    /// solution repair.
    /// </summary>
    public sealed class LevelImportPipeline
    {
        private readonly ChapterLayout layout;
        private readonly int solverNodeBudget;

        public LevelImportPipeline()
            : this(ChapterLayout.Default, WarnsdorffSolver.DefaultNodeBudget)
        {
        }

        public LevelImportPipeline(ChapterLayout layout, int solverNodeBudget)
        {
            this.layout = layout;
            this.solverNodeBudget = solverNodeBudget;
        }

        /// <summary>
        /// <paramref name="onProgress"/> receives a step name and a fraction from 0 to 1,
        /// so a caller with a progress bar can show one and a caller without can pass null.
        /// </summary>
        public ImportReport Run(IReadOnlyList<RawLevelFile> files, Action<string, float> onProgress = null)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            var report = new ImportReport { FileCount = files.Count };

            List<RawLevelFile> ordered = SortById(files);

            Report(onProgress, "Reading level files", 0f);
            List<LevelData> parsed = Parse(ordered, report);
            report.Parsed = parsed;

            Report(onProgress, "Collapsing duplicate boards", 0.25f);
            var deduplicator = new LevelDeduplicator(new BoardCanonicalizer());
            IReadOnlyList<LevelGroup> groups = deduplicator.Group(parsed);
            report.Groups = groups;

            var representatives = new List<LevelData>(groups.Count);

            for (int i = 0; i < groups.Count; i++)
            {
                representatives.Add(groups[i].Representative);
            }

            Report(onProgress, "Checking and repairing solutions", 0.35f);
            var prepareTimer = Stopwatch.StartNew();
            var preparer = new LevelPreparer(new LevelValidator(new WarnsdorffSolver(solverNodeBudget)));
            PreparedPool pool = preparer.Prepare(representatives);
            prepareTimer.Stop();
            report.Pool = pool;
            report.PrepareMilliseconds = prepareTimer.ElapsedMilliseconds;

            Report(onProgress, "Scoring difficulty", 0.6f);
            var scoreTimer = Stopwatch.StartNew();
            var scorer = new DifficultyScorer(new WarnsdorffSolver(solverNodeBudget));
            IReadOnlyList<ScoredLevel> scored = scorer.Score(pool.Levels);
            scoreTimer.Stop();
            report.Scored = scored;
            report.ScoreMilliseconds = scoreTimer.ElapsedMilliseconds;

            Report(onProgress, "Building chapters", 0.9f);
            var assembleTimer = Stopwatch.StartNew();

            try
            {
                report.Campaign = new ChapterAssembler(layout).Assemble(scored);
            }
            catch (ArgumentException e)
            {
                report.CampaignError = e.Message;
            }

            assembleTimer.Stop();
            report.AssembleMilliseconds = assembleTimer.ElapsedMilliseconds;

            Report(onProgress, "Done", 1f);
            return report;
        }

        private List<LevelData> Parse(List<RawLevelFile> files, ImportReport report)
        {
            var timer = Stopwatch.StartNew();
            var parser = new LevelTextParser();
            var parsed = new List<LevelData>(files.Count);

            for (int i = 0; i < files.Count; i++)
            {
                RawLevelFile file = files[i];
                LevelParseResult result = parser.Parse(file.Id, file.LevelText, file.TutorialText);

                if (!result.IsSuccess)
                {
                    Increment(report.ParseErrors, result.Error);
                    report.ParseFailures.Add(file.Id + ": " + result.Error);
                    continue;
                }

                Increment(report.PayloadForms, result.Form);

                if (result.StartDisagreed)
                {
                    report.StartDisagreements++;
                }

                if (!result.HasTutorial)
                {
                    report.WithoutTutorial++;
                }

                parsed.Add(result.Level);
            }

            timer.Stop();
            report.ParseMilliseconds = timer.ElapsedMilliseconds;
            return parsed;
        }

        /// <summary>
        /// Sorting here rather than trusting the caller keeps the result identical whether
        /// the files arrived from a directory listing, an asset database query or a test.
        /// </summary>
        private static List<RawLevelFile> SortById(IReadOnlyList<RawLevelFile> files)
        {
            var ordered = new List<RawLevelFile>(files);
            ordered.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return ordered;
        }

        private static void Increment<TKey>(Dictionary<TKey, int> counts, TKey key)
        {
            counts.TryGetValue(key, out int count);
            counts[key] = count + 1;
        }

        private static void Report(Action<string, float> onProgress, string step, float progress)
        {
            onProgress?.Invoke(step, progress);
        }
    }
}
