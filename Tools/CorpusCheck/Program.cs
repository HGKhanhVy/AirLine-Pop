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
    // Acceptance harness for the importer. Drives the real LevelTextParser and
    // LevelValidator over every ripped file, so the numbers it prints are the numbers
    // the shipping editor tool will produce.
    internal static class Program
    {
        private static string exportDirectory;

        private const string LevelsRelativePath = @"Assets\_Game\Asset_Resources\Level Data\oneline";

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

        private static void Main(string[] args)
        {
            int budget = args.Length > 0 && int.TryParse(args[0], out int b) ? b : WarnsdorffSolver.DefaultNodeBudget;
            string filter = args.Length > 1 ? args[1] : null;
            int exportIndex = Array.IndexOf(args, "--export");
            exportDirectory = exportIndex >= 0 && exportIndex + 1 < args.Length ? args[exportIndex + 1] : null;

            if (filter == "--export")
            {
                filter = null;
            }

            string root = FindLevelsRoot();
            var parser = new LevelTextParser();
            var solver = new WarnsdorffSolver(budget);
            var validator = new LevelValidator(solver);

            int total = 0;
            int parsed = 0;
            int valid = 0;
            int startDisagreements = 0;
            int withoutTutorial = 0;
            long maxNodes = 0;
            string worstLevel = "";

            var parseErrors = new Dictionary<LevelParseError, int>();
            var forms = new Dictionary<PayloadForm, int>();
            var issues = new Dictionary<LevelIssueCode, int>();
            var failures = new List<string>();
            var parsedLevels = new List<LevelData>();

            var stopwatch = Stopwatch.StartNew();

            foreach (string packDir in Directory.GetDirectories(root).OrderBy(d => d))
            {
                string pack = Path.GetFileName(packDir);

                foreach (string file in Directory.GetFiles(packDir, "Level_*.txt").OrderBy(f => f))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    string id = pack + "/" + name;

                    if (filter != null && !id.Contains(filter))
                    {
                        continue;
                    }

                    total++;

                    string tutorialPath = Path.Combine(packDir, name.Replace("Level_", "Toturial_") + ".txt");
                    string tutorialText = File.Exists(tutorialPath) ? File.ReadAllText(tutorialPath) : null;

                    LevelParseResult result = parser.Parse(id, File.ReadAllText(file), tutorialText);

                    if (!result.IsSuccess)
                    {
                        parseErrors.TryGetValue(result.Error, out int errorCount);
                        parseErrors[result.Error] = errorCount + 1;
                        failures.Add(id + ": parse -> " + result.Error);
                        continue;
                    }

                    parsed++;
                    parsedLevels.Add(result.Level);
                    forms.TryGetValue(result.Form, out int formCount);
                    forms[result.Form] = formCount + 1;

                    if (result.StartDisagreed)
                    {
                        startDisagreements++;
                    }

                    if (!result.HasTutorial)
                    {
                        withoutTutorial++;
                    }

                    LevelValidationResult validation = validator.Validate(result.Level);

                    if (solver.LastNodeCount > maxNodes)
                    {
                        maxNodes = solver.LastNodeCount;
                        worstLevel = id;
                    }

                    if (validation.IsValid)
                    {
                        valid++;
                        continue;
                    }

                    foreach (LevelIssue issue in validation.Issues)
                    {
                        issues.TryGetValue(issue.Code, out int issueCount);
                        issues[issue.Code] = issueCount + 1;
                    }

                    failures.Add(id + ": " + string.Join(", ", validation.Issues.Select(i => i.Code.ToString())));
                }
            }

            stopwatch.Stop();

            Console.WriteLine("File quet duoc       : " + total);
            Console.WriteLine("Parse thanh cong     : " + parsed);
            Console.WriteLine("Qua validator        : " + valid);
            Console.WriteLine("Khong co Toturial_   : " + withoutTutorial + "  (solver tu cap loi giai)");
            Console.WriteLine("Level_ lech Toturial_: " + startDisagreements);
            Console.WriteLine("Thoi gian            : " + stopwatch.ElapsedMilliseconds + " ms");
            Console.WriteLine("Search nang nhat     : " + maxNodes + " node (" + worstLevel + ")");

            Print("Bien the payload", forms);
            Print("Loi parse", parseErrors);
            Print("Loi validate", issues);
            ReportDeduplication(parsedLevels);
            ReportDifficulty(parsedLevels, budget);

            if (failures.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Chi tiet " + failures.Count + " file:");

                foreach (string failure in failures)
                {
                    Console.WriteLine("  " + failure);
                }
            }
        }

        private static void ReportDeduplication(List<LevelData> levels)
        {
            var deduplicator = new LevelDeduplicator(new BoardCanonicalizer());
            var stopwatch = Stopwatch.StartNew();
            List<LevelGroup> groups = deduplicator.Group(levels);
            stopwatch.Stop();

            int duplicates = levels.Count - groups.Count;

            Console.WriteLine();
            Console.WriteLine("Khu trung (8 phep doi xung, chi so hinh ban):");
            Console.WriteLine("  " + levels.Count + " level -> " + groups.Count + " hinh ban doc nhat");
            Console.WriteLine("  Ban trung bi bo   : " + duplicates);
            Console.WriteLine("  Nhom co trung lap : " + groups.Count(g => g.HasDuplicates));
            Console.WriteLine("  Thoi gian         : " + stopwatch.ElapsedMilliseconds + " ms");

            Console.WriteLine();
            Console.WriteLine("  Ba nhom trung nhieu nhat:");

            foreach (LevelGroup group in groups.OrderByDescending(g => g.Members.Count).Take(3))
            {
                Console.WriteLine("    " + group.Members.Count + " ban, giu " + group.Representative.Id +
                                  "  [" + group.Key.Width + "x" + group.Key.Height + ", " +
                                  group.Representative.ActiveCellCount + " o]");
                Console.WriteLine("      " + string.Join(", ", group.Members.Select(m => m.Id)));
            }

            Console.WriteLine();
            Console.WriteLine("  Phan bo so o tren " + groups.Count + " hinh ban doc nhat:");

            var bands = new (string Label, int Low, int High, int Need)[]
            {
                ("<=12  ", 0, 12, 30),
                ("13-20 ", 13, 20, 60),
                ("21-30 ", 21, 30, 90),
                ("31-45 ", 31, 45, 60),
                (">45   ", 46, int.MaxValue, 60)
            };

            foreach ((string label, int low, int high, int need) in bands)
            {
                int count = groups.Count(g => g.Representative.ActiveCellCount >= low &&
                                              g.Representative.ActiveCellCount <= high);
                Console.WriteLine("    " + label + " co " + count.ToString().PadLeft(4) +
                                  "  can " + need.ToString().PadLeft(3) +
                                  "  du " + (count / (double)need).ToString("0.0") + "x");
            }
        }

        private static void ReportDifficulty(List<LevelData> levels, int budget)
        {
            var deduplicator = new LevelDeduplicator(new BoardCanonicalizer());
            List<LevelData> unique = deduplicator.Group(levels)
                .Select(g => g.Representative)
                .ToList();

            // Every level must carry a checked solution before it can be scored: the
            // packs without tutorial data ship none at all, and a few ship a broken one.
            var preparer = new LevelPreparer(new LevelValidator(new WarnsdorffSolver(budget)));
            var prepareTimer = Stopwatch.StartNew();
            PreparedPool pool = preparer.Prepare(unique);
            prepareTimer.Stop();

            Console.WriteLine();
            Console.WriteLine("Chuan bi kho (" + prepareTimer.ElapsedMilliseconds + " ms):");
            Console.WriteLine("  Dung duoc       : " + pool.Levels.Count + "/" + unique.Count);
            Console.WriteLine("  Solver cap moi  : " + pool.SolvedCount + " loi giai");
            Console.WriteLine("  Va lai loi giai : " + pool.RepairedCount);
            Console.WriteLine("  Loai bo         : " + pool.DroppedIds.Count +
                              (pool.DroppedIds.Count > 0 ? "  (" + string.Join(", ", pool.DroppedIds) + ")" : ""));

            var scorer = new DifficultyScorer(new WarnsdorffSolver(budget));
            var stopwatch = Stopwatch.StartNew();
            List<ScoredLevel> scored = scorer.Score(pool.Levels);
            stopwatch.Stop();
            levels = new List<LevelData>(pool.Levels);

            Console.WriteLine();
            Console.WriteLine("Cham do kho (" + stopwatch.ElapsedMilliseconds + " ms):");

            for (int band = DifficultyScorer.MinDifficulty; band <= DifficultyScorer.MaxDifficulty; band++)
            {
                int currentBand = band;
                List<ScoredLevel> inBand = scored.Where(s => s.Difficulty == currentBand).ToList();

                if (inBand.Count == 0)
                {
                    Console.WriteLine("  " + band.ToString().PadLeft(2) + " |  0");
                    continue;
                }

                double meanCells = inBand.Average(s => s.Features.CellCount);
                double meanGreedy = inBand.Average(s => s.Features.GreedyFailures);
                string bar = new string('#', (int)Math.Round(inBand.Count / (double)levels.Count * 120));

                Console.WriteLine("  " + band.ToString().PadLeft(2) + " | " +
                                  inBand.Count.ToString().PadLeft(3) + "  " + bar.PadRight(14) +
                                  "  o trung binh " + meanCells.ToString("00.0") +
                                  "  greedy hong " + meanGreedy.ToString("0.0") + "/6");
            }

            ReportBeginnerCorrelation(scored);
            ReportCampaign(scored);
        }

        /// <summary>
        /// Builds the campaign and measures how faithfully the real pool follows the
        /// designed curve. The pool is finite, so the assembler takes the nearest unused
        /// level rather than one at the exact target, and the drift that causes is the
        /// number worth watching.
        /// </summary>
        private static void ReportCampaign(List<ScoredLevel> scored)
        {
            Campaign campaign;

            try
            {
                campaign = new ChapterAssembler().Assemble(scored);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine();
                Console.WriteLine("Dung chapter that bai: " + e.Message);
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Dung campaign: " + campaign.Chapters.Count + " chapter, " +
                              campaign.LevelCount + " level, noi long " + campaign.RelaxedPlacements + " lan");
            Console.WriteLine();
            Console.WriteLine("  chapter   diem tb   lech target tb   tut sau nhat   nhip nghi/10");

            double worstDip = 0;

            foreach (Chapter chapter in campaign.Chapters)
            {
                IReadOnlyList<PlacedLevel> placed = chapter.Placements;
                double mean = placed.Average(x => x.Score);
                double meanMiss = placed.Average(x => Math.Abs(x.Miss));

                double dip = 0;
                double previous = double.MinValue;

                for (int i = 0; i + 10 <= placed.Count; i++)
                {
                    double average = 0;

                    for (int j = i; j < i + 10; j++)
                    {
                        average += placed[j].Score;
                    }

                    average /= 10;

                    if (previous > double.MinValue && average < previous)
                    {
                        dip = Math.Max(dip, previous - average);
                    }

                    previous = average;
                }

                worstDip = Math.Max(worstDip, dip);

                int dips = 0;

                for (int i = 1; i < placed.Count; i++)
                {
                    if (placed[i].Score < placed[i - 1].Score)
                    {
                        dips++;
                    }
                }

                Console.WriteLine("  " + chapter.Id + "      " + mean.ToString("0.000") +
                                  "     " + meanMiss.ToString("0.0000") +
                                  "          " + dip.ToString("0.0000") +
                                  "        " + dips + "/30");
            }

            Console.WriteLine();
            Console.WriteLine("  Tut sau nhat tren toan bo campaign: " + worstDip.ToString("0.0000"));

            ExportAndReload(campaign);
        }

        /// <summary>
        /// Writes the campaign the way the editor tool will, then reads it back through
        /// the shipping repository. Round tripping through real files is the only way to
        /// know the exported data is actually loadable, and it puts a number on the
        /// 300 ms board load budget in GDD 15.4.
        /// </summary>
        private static void ExportAndReload(Campaign campaign)
        {
            string directory = exportDirectory ?? Path.Combine(Path.GetTempPath(), "single-line-export");
            Directory.CreateDirectory(directory);

            var writeTimer = Stopwatch.StartNew();
            long totalBytes = 0;

            foreach (Chapter chapter in campaign.Chapters)
            {
                string json = LevelJsonSerializer.SerializeChapter(chapter.Id, chapter.Levels);
                string path = Path.Combine(directory, chapter.Id + ".json");
                File.WriteAllText(path, json);
                totalBytes += json.Length;
            }

            writeTimer.Stop();

            var source = new InMemoryChapterSource();

            foreach (Chapter chapter in campaign.Chapters)
            {
                source.Add(chapter.Id, File.ReadAllText(Path.Combine(directory, chapter.Id + ".json")));
            }

            var repository = new ChapterLevelRepository(source);

            var loadTimer = Stopwatch.StartNew();
            repository.TryPreloadChapter("ch01");
            loadTimer.Stop();

            var readTimer = Stopwatch.StartNew();
            int reloaded = 0;

            foreach (Chapter chapter in campaign.Chapters)
            {
                foreach (string id in repository.GetLevelIds(chapter.Id))
                {
                    LevelData level = repository.Get(id);

                    if (level.ActiveCellCount > 0)
                    {
                        reloaded++;
                    }
                }
            }

            readTimer.Stop();

            Console.WriteLine();
            Console.WriteLine("Xuat va nap lai:");
            Console.WriteLine("  Thu muc         : " + directory);
            Console.WriteLine("  Ghi 10 chapter  : " + writeTimer.ElapsedMilliseconds + " ms, " +
                              (totalBytes / 1024) + " KB");
            Console.WriteLine("  Nap 1 chapter   : " + loadTimer.Elapsed.TotalMilliseconds.ToString("0.0") +
                              " ms  (ngan sach GDD 15.4 la 300 ms)");
            Console.WriteLine("  Doc lai         : " + reloaded + "/300 level trong " +
                              readTimer.ElapsedMilliseconds + " ms");

            VerifyRoundTrip(campaign, repository);
        }

        /// <summary>Replays every reloaded solution through the rules, not just field equality.</summary>
        private static void VerifyRoundTrip(Campaign campaign, ILevelRepository repository)
        {
            int playable = 0;
            int missingSolution = 0;

            foreach (Chapter chapter in campaign.Chapters)
            {
                foreach (LevelData original in chapter.Levels)
                {
                    LevelData reloaded = repository.Get(original.Id);

                    if (!reloaded.HasSolution)
                    {
                        missingSolution++;
                        continue;
                    }

                    var session = new PathSession(reloaded);
                    bool rejected = false;

                    foreach (int cell in reloaded.Solution)
                    {
                        if (session.Move(cell) == MoveResult.Rejected)
                        {
                            rejected = true;
                            break;
                        }
                    }

                    if (!rejected && session.State == PathState.Won)
                    {
                        playable++;
                    }
                }
            }

            Console.WriteLine("  Giai lai duoc   : " + playable + "/300" +
                              (missingSolution > 0 ? "  (thieu loi giai: " + missingSolution + ")" : ""));
        }

        /// <summary>
        /// The beginner pack ships in a hand tuned order, so a scorer that agrees with a
        /// designer should rank it roughly the same way. Spearman rather than Pearson
        /// because only the ordering is meaningful.
        /// </summary>
        private static void ReportBeginnerCorrelation(List<ScoredLevel> scored)
        {
            foreach (string pack in new[] { "beginner", "hard", "medium" })
            {
                List<ScoredLevel> inPack = scored
                    .Where(s => s.Level.Id.StartsWith(pack + "/Level_", StringComparison.Ordinal))
                    .Select(s => new { Scored = s, Number = int.Parse(s.Level.Id.Substring((pack + "/Level_").Length)) })
                    .OrderBy(e => e.Number)
                    .Select(e => e.Scored)
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

                double rho = Spearman(shipOrder, ourScore);
                Console.WriteLine("  Tuong quan voi thu tu goc cua pack " + pack.PadRight(9) +
                                  " : rho = " + rho.ToString("0.000") + "  (" + inPack.Count + " man)");
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

        private static void Print<TKey>(string title, Dictionary<TKey, int> counts)
        {
            if (counts.Count == 0)
            {
                return;
            }

            Console.WriteLine();
            Console.WriteLine(title + ":");

            foreach (KeyValuePair<TKey, int> entry in counts.OrderByDescending(e => e.Value))
            {
                Console.WriteLine("  " + entry.Key + " : " + entry.Value);
            }
        }
    }
}
