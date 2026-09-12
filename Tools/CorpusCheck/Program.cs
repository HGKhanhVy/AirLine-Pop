using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Data;
using ASTeams.SingleLine.Import;

namespace CorpusCheck
{
    /// <summary>
    /// Command line front end for the same <see cref="LevelImportPipeline"/> the editor
    /// window runs, plus the diagnostics that only matter when judging the import rather
    /// than performing it: how the scorer compares against the reference game's own
    /// ordering, and how faithfully the campaign tracks its designed curve.
    ///
    /// Usage: CorpusCheck [nodeBudget] [--export folder] [--curve]
    ///
    /// The campaign ships in source order. Pass --curve to build the generated ordering
    /// instead, which is only useful for comparing the two.
    /// </summary>
    internal static class Program
    {
        private const string LevelsRelativePath = @"Assets\_Game\Asset_Resources\Level Data\oneline";

        private static void Main(string[] args)
        {
            int budget = args.Length > 0 && int.TryParse(args[0], out int parsed)
                ? parsed
                : WarnsdorffSolver.DefaultNodeBudget;

            int exportIndex = Array.IndexOf(args, "--export");
            string exportFolder = exportIndex >= 0 && exportIndex + 1 < args.Length
                ? args[exportIndex + 1]
                : null;

            CampaignMode mode = Array.IndexOf(args, "--curve") >= 0
                ? CampaignMode.DifficultyCurve
                : CampaignMode.SourceOrder;

            List<RawLevelFile> files = ReadSourceFiles(FindLevelsRoot());
            Console.WriteLine("Doc " + files.Count + " file.");
            Console.WriteLine("Che do: " + (mode == CampaignMode.SourceOrder
                ? "thu tu goc theo resources"
                : "duong cong do kho"));

            var timer = Stopwatch.StartNew();
            ImportReport report = LevelImportPipeline
                .Create(mode, ChapterLayout.Default, budget, LevelPackOrder.Default)
                .Run(files);
            timer.Stop();

            PrintPipeline(report, timer.ElapsedMilliseconds);
            PrintDifficultyBands(report);
            PrintReferenceCorrelation(report);
            PrintCurveFidelity(report);
            PrintRoundTrip(report, exportFolder);
            PrintIssues(report);
        }

        private static void PrintPipeline(ImportReport report, long wallClock)
        {
            Console.WriteLine();
            Console.WriteLine("Duong ong:");
            Console.WriteLine("  Parse            : " + report.Parsed.Count + "/" + report.FileCount +
                              "   (" + report.ParseMilliseconds + " ms)");
            Console.WriteLine("  Hinh ban doc nhat: " + report.UniqueBoardCount +
                              "   bo " + report.DuplicateCount + " ban trung");
            Console.WriteLine("  Dung duoc        : " + report.Pool.Levels.Count +
                              "   (" + report.PrepareMilliseconds + " ms)");
            Console.WriteLine("    giai moi       : " + report.Pool.SolvedCount);
            Console.WriteLine("    va lai         : " + report.Pool.RepairedCount);
            Console.WriteLine("    loai bo        : " + report.Pool.DroppedIds.Count);
            Console.WriteLine("  Cham do kho      : " + report.ScoreMilliseconds + " ms");
            Console.WriteLine("  Dung chapter     : " + report.AssembleMilliseconds + " ms");
            Console.WriteLine("  Tong             : " + wallClock + " ms");
            Console.WriteLine();
            Console.WriteLine("Du lieu nguon:");
            Console.WriteLine("  Khong co Toturial_   : " + report.WithoutTutorial);
            Console.WriteLine("  Level_ lech Toturial_: " + report.StartDisagreements);

            foreach (KeyValuePair<PayloadForm, int> entry in report.PayloadForms.OrderByDescending(e => e.Value))
            {
                Console.WriteLine("  Payload " + entry.Key.ToString().PadRight(13) + ": " + entry.Value);
            }
        }

        private static void PrintDifficultyBands(ImportReport report)
        {
            Console.WriteLine();
            Console.WriteLine("Bang do kho tren " + report.Scored.Count + " man:");

            for (int band = DifficultyScorer.MinDifficulty; band <= DifficultyScorer.MaxDifficulty; band++)
            {
                int current = band;
                List<ScoredLevel> inBand = report.Scored.Where(s => s.Difficulty == current).ToList();

                if (inBand.Count == 0)
                {
                    continue;
                }

                Console.WriteLine("  " + band.ToString().PadLeft(2) + " | " + inBand.Count.ToString().PadLeft(3) +
                                  "   o trung binh " + inBand.Average(s => s.Features.CellCount).ToString("00.0") +
                                  "   greedy hong " + inBand.Average(s => s.Features.GreedyFailures).ToString("0.0") + "/6");
            }
        }

        /// <summary>
        /// The beginner pack ships in a hand tuned order, so a scorer that agrees with a
        /// designer should rank it roughly the same way. Spearman rather than Pearson
        /// because only the ordering carries meaning.
        /// </summary>
        private static void PrintReferenceCorrelation(ImportReport report)
        {
            Console.WriteLine();
            Console.WriteLine("Tuong quan voi thu tu goc:");

            foreach (string pack in new[] { "beginner", "hard", "medium" })
            {
                string prefix = pack + "/Level_";

                List<ScoredLevel> inPack = report.Scored
                    .Where(s => s.Level.Id.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(s => new { Level = s, Number = int.Parse(s.Level.Id.Substring(prefix.Length)) })
                    .OrderBy(e => e.Number)
                    .Select(e => e.Level)
                    .ToList();

                if (inPack.Count < 3)
                {
                    continue;
                }

                var shipOrder = new double[inPack.Count];
                var ourScore = new double[inPack.Count];

                for (int i = 0; i < inPack.Count; i++)
                {
                    shipOrder[i] = i;
                    ourScore[i] = inPack[i].Score;
                }

                Console.WriteLine("  " + pack.PadRight(9) + " rho = " + Spearman(shipOrder, ourScore).ToString("0.000") +
                                  "   (" + inPack.Count + " man)");
            }
        }

        private static void PrintCurveFidelity(ImportReport report)
        {
            if (!report.HasCampaign)
            {
                Console.WriteLine();
                Console.WriteLine("Khong dung duoc campaign: " + report.CampaignError);
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Campaign: " + report.Campaign.Chapters.Count + " chapter, " +
                              report.Campaign.LevelCount + " level, ep " + report.Campaign.RelaxedPlacements + " lan");
            Console.WriteLine();
            Console.WriteLine("  chapter  diem tb  lech target  tut sau nhat  nhip nghi");

            double worstDip = 0;

            foreach (Chapter chapter in report.Campaign.Chapters)
            {
                IReadOnlyList<PlacedLevel> placed = chapter.Placements;
                double dip = WorstMovingAverageDip(placed, window: 10);
                worstDip = Math.Max(worstDip, dip);

                int dips = 0;

                for (int i = 1; i < placed.Count; i++)
                {
                    if (placed[i].Score < placed[i - 1].Score)
                    {
                        dips++;
                    }
                }

                Console.WriteLine("  " + chapter.Id + "     " + placed.Average(p => p.Score).ToString("0.000") +
                                  "    " + placed.Average(p => Math.Abs(p.Miss)).ToString("0.0000") +
                                  "       " + dip.ToString("0.0000") +
                                  "        " + dips + "/" + placed.Count);
            }

            Console.WriteLine();
            Console.WriteLine("  Tut sau nhat toan campaign: " + worstDip.ToString("0.0000"));
        }

        /// <summary>
        /// The window has to span one whole rhythm period. A shorter one catches one rest
        /// in some positions and two in others, so it swings with the rhythm instead of
        /// smoothing it.
        /// </summary>
        private static double WorstMovingAverageDip(IReadOnlyList<PlacedLevel> placed, int window)
        {
            double worst = 0;
            double previous = double.MinValue;

            for (int i = 0; i + window <= placed.Count; i++)
            {
                double sum = 0;

                for (int j = i; j < i + window; j++)
                {
                    sum += placed[j].Score;
                }

                double average = sum / window;

                if (previous > double.MinValue && average < previous)
                {
                    worst = Math.Max(worst, previous - average);
                }

                previous = average;
            }

            return worst;
        }

        /// <summary>
        /// Writes the campaign and reads it back through the shipping repository, then
        /// replays every solution through the rules. Field equality would not catch a
        /// board that survives the trip but no longer plays.
        /// </summary>
        private static void PrintRoundTrip(ImportReport report, string exportFolder)
        {
            if (!report.HasCampaign)
            {
                return;
            }

            string folder = exportFolder ?? Path.Combine(Path.GetTempPath(), "single-line-export");
            Directory.CreateDirectory(folder);

            var writeTimer = Stopwatch.StartNew();
            long bytes = 0;

            foreach (Chapter chapter in report.Campaign.Chapters)
            {
                string json = LevelJsonSerializer.SerializeChapter(chapter.Id, chapter.Levels);
                File.WriteAllText(Path.Combine(folder, chapter.Id + ".json"), json);
                bytes += json.Length;
            }

            writeTimer.Stop();

            var source = new InMemoryChapterSource();

            foreach (Chapter chapter in report.Campaign.Chapters)
            {
                source.Add(chapter.Id, File.ReadAllText(Path.Combine(folder, chapter.Id + ".json")));
            }

            var repository = new ChapterLevelRepository(source);

            var loadTimer = Stopwatch.StartNew();
            repository.TryPreloadChapter(report.Campaign.Chapters[0].Id);
            loadTimer.Stop();

            int playable = 0;

            foreach (Chapter chapter in report.Campaign.Chapters)
            {
                foreach (string id in repository.GetLevelIds(chapter.Id))
                {
                    if (Replays(repository.Get(id)))
                    {
                        playable++;
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Xuat va nap lai:");
            Console.WriteLine("  Thu muc        : " + folder);
            Console.WriteLine("  Ghi            : " + writeTimer.ElapsedMilliseconds + " ms, " + (bytes / 1024) + " KB");
            Console.WriteLine("  Nap 1 chapter  : " + loadTimer.Elapsed.TotalMilliseconds.ToString("0.0") +
                              " ms   (ngan sach GDD 15.4: 300 ms)");
            Console.WriteLine("  Giai lai duoc  : " + playable + "/" + report.Campaign.LevelCount);
        }

        private static bool Replays(LevelData level)
        {
            if (!level.HasSolution)
            {
                return false;
            }

            var session = new PathSession(level);

            foreach (int cell in level.Solution)
            {
                if (session.Move(cell) == MoveResult.Rejected)
                {
                    return false;
                }
            }

            return session.State == PathState.Won;
        }

        private static void PrintIssues(ImportReport report)
        {
            if (report.ParseFailures.Count == 0 && report.Pool.DroppedIds.Count == 0)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Van de:");

            foreach (string failure in report.ParseFailures)
            {
                Console.WriteLine("  parse  " + failure);
            }

            foreach (string dropped in report.Pool.DroppedIds)
            {
                Console.WriteLine("  bo     " + dropped + ": khong tim duoc loi giai");
            }
        }

        private static double Spearman(double[] a, double[] b)
        {
            double[] ra = DifficultyScorer.PercentileRanks(a);
            double[] rb = DifficultyScorer.PercentileRanks(b);

            double meanA = ra.Average();
            double meanB = rb.Average();
            double covariance = 0;
            double varianceA = 0;
            double varianceB = 0;

            for (int i = 0; i < ra.Length; i++)
            {
                double da = ra[i] - meanA;
                double db = rb[i] - meanB;
                covariance += da * db;
                varianceA += da * da;
                varianceB += db * db;
            }

            return covariance / Math.Sqrt(varianceA * varianceB);
        }

        private static List<RawLevelFile> ReadSourceFiles(string root)
        {
            var files = new List<RawLevelFile>();

            foreach (string packDirectory in Directory.GetDirectories(root))
            {
                string pack = Path.GetFileName(packDirectory);

                foreach (string levelPath in Directory.GetFiles(packDirectory, "Level_*.txt"))
                {
                    string name = Path.GetFileNameWithoutExtension(levelPath);
                    string tutorialPath = Path.Combine(packDirectory, name.Replace("Level_", "Toturial_") + ".txt");

                    files.Add(new RawLevelFile(
                        pack + "/" + name,
                        File.ReadAllText(levelPath),
                        File.Exists(tutorialPath) ? File.ReadAllText(tutorialPath) : null));
                }
            }

            return files;
        }

        private static string FindLevelsRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, LevelsRelativePath);

                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Khong tim thay " + LevelsRelativePath);
        }
    }
}
