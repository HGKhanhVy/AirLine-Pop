using System.Collections.Generic;
using System.IO;
using System.Text;
using AirlinePop.SingleLine.Core;
using AirlinePop.SingleLine.Editor;
using AirlinePop.SingleLine.Import;
using UnityEditor;
using UnityEngine;

namespace AirlinePop.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Rebuilds the shipped campaign in one step: boards drawn by our own generator, measured
    /// and laid flight by flight onto absolute difficulty targets with their conditions on, the
    /// hand-drawn picture boards pinned to their places, then the chapter files and the level
    /// config written out. One level per gate on the route map. No board from any other game
    /// is used.
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
            if (!TryReadSpecials(out List<SpecialLevel> specials, out string specialError))
            {
                return specialError;
            }

            Campaign campaign;
            int generatedCount;
            List<LevelData> dailyFlights;

            try
            {
                EditorUtility.DisplayProgressBar("Rebuilding campaign", "Drawing boards", 0f);

                // The reference packs are read only so that no drawn board, turned or mirrored,
                // happens to match one of them; none of their boards is ever shipped.
                List<LevelData> pool = new GeneratedBoards().Generate(ReadReferenceBoards());
                generatedCount = pool.Count;

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

        private static List<LevelData> ReadReferenceBoards()
        {
            var parser = new LevelTextParser();
            var boards = new List<LevelData>();

            foreach (RawLevelFile file in LevelSourceReader.Read(LevelSourceReader.DefaultSourceFolder))
            {
                LevelParseResult result = parser.Parse(file.Id, file.LevelText, file.TutorialText);

                if (result.IsSuccess)
                {
                    boards.Add(result.Level);
                }
            }

            return boards;
        }

        /// <summary>The daily flights sit in a file of their own, outside the campaign's chapter folder.</summary>
        private static void WriteDailyFlights(List<LevelData> days)
        {
            string folder = Path.GetDirectoryName(DailyFlightFile);
            Directory.CreateDirectory(folder);
            File.WriteAllText(DailyFlightFile, AirlinePop.SingleLine.Data.LevelJsonSerializer.SerializeChapter(DailyFlightPlanner.ChapterId, days));
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
