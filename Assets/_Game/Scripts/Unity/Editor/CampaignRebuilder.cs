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
    /// Rebuilds the shipped campaign in one step: every reference pack scored and laid onto
    /// the difficulty curve (hard and easy flights taking turns, each city closing on its
    /// hardest flight), the hand-drawn picture boards pinned to their places, then the
    /// chapter files and the level config written out. One level per gate on the route map.
    /// </summary>
    public static class CampaignRebuilder
    {
        public const string SpecialFolder = "Assets/_Game/Asset_Resources/Level Data/special";

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

            LevelImportPipeline pipeline = LevelImportPipeline.Create(CampaignMode.DifficultyCurve, ChapterLayout.FullRoute,
                WarnsdorffSolver.DefaultNodeBudget, LevelPackOrder.Default, specials);
            ImportReport report;

            try
            {
                report = pipeline.Run(files, (step, progress) => EditorUtility.DisplayProgressBar("Rebuilding campaign", step, progress));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (report.Campaign == null)
            {
                return "Campaign not rebuilt: " + report.CampaignError;
            }

            // The runway and the wind go on last, onto the finished order, from each level's own solution.
            Campaign campaign = CampaignRules.Default.Apply(report.Campaign);
            ExportResult export = CampaignExporter.Export(campaign, CampaignExporter.DefaultOutputFolder);

            if (!export.IsSuccess)
            {
                return "Campaign not exported: " + export.Error;
            }

            int count = LevelConfigBuilder.Rebuild(CampaignExporter.DefaultOutputFolder, LevelConfigBuilder.DefaultLevelFolder,
                LevelConfigBuilder.DefaultConfigPath);
            return "Campaign rebuilt: " + count + " levels in " + export.ChapterCount + " chapters, " + specials.Count +
                " picture boards, " + report.Campaign.RelaxedPlacements + " relaxed placements.\n" + Describe(campaign);
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

                    number++;
                }
            }

            return text.ToString();
        }
    }
}
