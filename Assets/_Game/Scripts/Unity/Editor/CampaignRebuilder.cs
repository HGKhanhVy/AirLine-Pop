using System.Collections.Generic;
using System.IO;
using System.Text;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Editor;
using ASTeams.SingleLine.Import;
using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Rebuilds the shipped campaign in one step: the reference packs and a batch of freshly
    /// drawn boards measured and laid flight by flight onto absolute difficulty targets with
    /// their conditions on, the hand-drawn picture boards pinned to their places, then the
    /// chapter files and the level config written out. One level per gate on the route map.
    /// </summary>
    public static class CampaignRebuilder
    {
        public const string SpecialFolder = "Assets/_Game/Asset_Resources/Level Data/special";
        public const string DailyFlightFile = "Assets/_Game/Resources/" + DailyFlightCatalog.ResourcePath + ".json";

        [MenuItem("Tools/Single Line/Rebuild Campaign")]
        public static void RebuildFromMenu()
        {
            string result = Rebuild();
            Debug.Log(result);
            EditorUtility.DisplayDialog("Single Line", result, "OK");
        }

        public static string Rebuild()
        {
            List<RawLevelFile> files = LevelSourceReader.Read(LevelSourceReader.DefaultSourceFolder);

            if (!TryReadSpecials(out List<SpecialLevel> specials, out string specialError))
            {
                return specialError;
            }

            Campaign campaign;
            int generatedCount;
            List<LevelData> dailyFlights;

            try
            {
                EditorUtility.DisplayProgressBar("Rebuilding campaign", "Checking the reference packs", 0f);
                LevelImportPipeline pipeline = LevelImportPipeline.Create(CampaignMode.DifficultyCurve, ChapterLayout.FullRoute,
                    WarnsdorffSolver.DefaultNodeBudget, LevelPackOrder.Default, specials);
                var pool = new List<LevelData>(pipeline.Prepare(files, new ImportReport()).Levels);

                EditorUtility.DisplayProgressBar("Rebuilding campaign", "Drawing new boards", 0.1f);
                List<LevelData> generated = new GeneratedBoards().Generate(pool);
                generatedCount = generated.Count;
                pool.AddRange(generated);

                EditorUtility.DisplayProgressBar("Rebuilding campaign", "Measuring every board", 0.15f);
                var picker = new BoardPicker(BoardPicker.DefaultVariants, new ThoughtfulPlayer());
                picker.Add(pool);

                // Every flight is measured against an absolute target once its conditions are on.
                campaign = CampaignPlanner.Default.Plan(picker, specials,
                    progress => EditorUtility.DisplayProgressBar("Rebuilding campaign", "Placing flights", 0.3f + progress * 0.65f));
                dailyFlights = new DailyFlightPlanner().Plan(picker);
            }
            catch (System.InvalidOperationException e)
            {
                return "Campaign not rebuilt: " + e.Message;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            ExportResult export = CampaignExporter.Export(campaign, CampaignExporter.DefaultOutputFolder);

            if (!export.IsSuccess)
            {
                return "Campaign not exported: " + export.Error;
            }

            WriteDailyFlights(dailyFlights);
            int count = LevelConfigBuilder.Rebuild(CampaignExporter.DefaultOutputFolder, LevelConfigBuilder.DefaultLevelFolder,
                LevelConfigBuilder.DefaultConfigPath);
            return "Campaign rebuilt: " + count + " levels in " + export.ChapterCount + " chapters, " + specials.Count +
                " picture boards, " + generatedCount + " boards drawn, " + dailyFlights.Count + " daily flights.\n" + Describe(campaign);
        }

        /// <summary>The daily flights sit in a file of their own, outside the campaign's chapter folder.</summary>
        private static void WriteDailyFlights(List<LevelData> days)
        {
            string folder = Path.GetDirectoryName(DailyFlightFile);
            Directory.CreateDirectory(folder);
            File.WriteAllText(DailyFlightFile, ASTeams.SingleLine.Data.LevelJsonSerializer.SerializeChapter(DailyFlightPlanner.ChapterId, days));
            AssetDatabase.ImportAsset(DailyFlightFile);
        }

        /// <summary>Reads every picture board and has the solver prove it can be flown in one line.</summary>
        private static bool TryReadSpecials(out List<SpecialLevel> specials, out string error)
        {
            specials = new List<SpecialLevel>();
            error = null;

            if (!Directory.Exists(SpecialFolder))
            {
                return true;
            }

            var preparer = new LevelPreparer(new LevelValidator(new WarnsdorffSolver()));

            foreach (string path in Directory.GetFiles(SpecialFolder, "*.txt"))
            {
                SpecialLevel special;

                try
                {
                    special = ShapeLevelParser.Parse(File.ReadAllText(path));
                }
                catch (System.FormatException e)
                {
                    error = Path.GetFileName(path) + ": " + e.Message;
                    return false;
                }

                PreparedPool solved = preparer.Prepare(new[] { special.Level });

                if (solved.Levels.Count == 0)
                {
                    error = Path.GetFileName(path) + ": no one-line path covers this picture from its start square.";
                    return false;
                }

                specials.Add(new SpecialLevel(special.LevelNumber, solved.Levels[0]));
            }

            return true;
        }

        /// <summary>Each city's ten flights as cell counts, to eyeball the rhythm in the console.</summary>
        private static string Describe(Campaign campaign)
        {
            var text = new StringBuilder();
            int number = 0;

            foreach (Chapter chapter in campaign.Chapters)
            {
                foreach (PlacedLevel placed in chapter.Placements)
                {
                    if (number % 10 == 0)
                    {
                        text.Append('\n').Append("city ").Append(number / 10 + 1).Append(':');
                    }

                    text.Append(' ').Append(placed.Level.ActiveCellCount);

                    if (placed.Level.Tags.Count > 0)
                    {
                        text.Append('*');
                    }

                    if (placed.Level.HasFixedEnd)
                    {
                        text.Append('R');
                    }

                    if (placed.Level.HasWind)
                    {
                        text.Append('W');
                    }

                    if (placed.Level.Marks.Count > 0)
                    {
                        text.Append('M');
                    }

                    if (placed.Level.IsStorm)
                    {
                        text.Append('!');
                    }

                    number++;
                }
            }

            return text.ToString();
        }
    }
}
