using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;

namespace CorpusCheck
{
    // Acceptance harness for the importer. Drives the real LevelTextParser and
    // LevelValidator over every ripped file, so the numbers it prints are the numbers
    // the shipping editor tool will produce.
    internal static class Program
    {
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
