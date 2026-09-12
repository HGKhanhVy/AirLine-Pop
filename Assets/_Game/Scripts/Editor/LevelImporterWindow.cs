using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Editor
{
    /// <summary>
    /// One window that runs the whole import and shows what it did, so producing the
    /// campaign is a button rather than a command line.
    /// </summary>
    public sealed class LevelImporterWindow : EditorWindow
    {
        private enum Tab
        {
            Summary = 0,
            Curve = 1,
            Chapters = 2,
            Issues = 3
        }

        private static readonly string[] TabLabels = { "Summary", "Curve", "Chapters", "Issues" };

        private string sourceFolder = LevelSourceReader.DefaultSourceFolder;
        private string outputFolder = CampaignExporter.DefaultOutputFolder;
        private int solverNodeBudget = WarnsdorffSolver.DefaultNodeBudget;
        private CampaignMode campaignMode = CampaignMode.SourceOrder;

        private ImportReport report;
        private ExportResult lastExport;
        private bool hasExported;

        private Tab tab = Tab.Summary;
        private Vector2 scroll;
        private int selectedChapter;
        private int selectedLevel;
        private bool showSolution = true;

        [MenuItem("Tools/Single Line/Level Importer")]
        public static void Open()
        {
            LevelImporterWindow window = GetWindow<LevelImporterWindow>("Level Importer");
            window.minSize = new Vector2(560f, 480f);
        }

        private void OnGUI()
        {
            DrawSettings();
            EditorGUILayout.Space(4f);
            DrawActions();
            EditorGUILayout.Space(6f);

            if (report == null)
            {
                EditorGUILayout.HelpBox(
                    "Run the import to read the level files, collapse duplicate boards, check every " +
                    "solution, score difficulty and lay out the chapters.",
                    MessageType.Info);
                return;
            }

            tab = (Tab)GUILayout.Toolbar((int)tab, TabLabels);
            EditorGUILayout.Space(4f);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            switch (tab)
            {
                case Tab.Curve:
                    DrawCurve();
                    break;

                case Tab.Chapters:
                    DrawChapters();
                    break;

                case Tab.Issues:
                    DrawIssues();
                    break;

                default:
                    DrawSummary();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            sourceFolder = EditorGUILayout.TextField("Level files", sourceFolder);
            outputFolder = EditorGUILayout.TextField("Export to", outputFolder);

            solverNodeBudget = EditorGUILayout.IntField(
                new GUIContent("Solver budget", "Nodes the solver may expand per level before giving up."),
                solverNodeBudget);

            campaignMode = (CampaignMode)EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Order",
                    "Source order ships the reference packs as they are numbered, which is what release uses. " +
                    "Difficulty curve rebuilds the run and is for comparison only."),
                campaignMode);

            if (solverNodeBudget < 1000)
            {
                solverNodeBudget = 1000;
            }
        }

        private void DrawActions()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run import", GUILayout.Height(24f)))
                {
                    RunImport();
                }

                using (new EditorGUI.DisabledScope(report == null || !report.HasCampaign))
                {
                    if (GUILayout.Button("Export chapters", GUILayout.Height(24f)))
                    {
                        RunExport();
                    }
                }
            }

            if (!hasExported)
            {
                return;
            }

            if (lastExport.IsSuccess)
            {
                EditorGUILayout.HelpBox(
                    "Wrote " + lastExport.ChapterCount + " chapters, " + (lastExport.Bytes / 1024) + " KB, to " +
                    outputFolder +
                    (lastExport.RemovedStaleFiles > 0
                        ? ". Removed " + lastExport.RemovedStaleFiles + " chapter files left by an earlier run."
                        : "."),
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(lastExport.Error, MessageType.Error);
            }
        }

        private void RunImport()
        {
            hasExported = false;
            selectedChapter = 0;
            selectedLevel = 0;

            List<RawLevelFile> files = LevelSourceReader.Read(sourceFolder);

            if (files.Count == 0)
            {
                report = null;
                EditorUtility.DisplayDialog(
                    "Level Importer",
                    "No Level_*.txt files under " + sourceFolder + ".",
                    "OK");
                return;
            }

            LevelImportPipeline pipeline = LevelImportPipeline.Create(
                campaignMode, ChapterLayout.Default, solverNodeBudget, LevelPackOrder.Default);

            try
            {
                report = pipeline.Run(files, (step, progress) =>
                    EditorUtility.DisplayProgressBar("Importing levels", step, progress));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void RunExport()
        {
            lastExport = CampaignExporter.Export(report.Campaign, outputFolder);
            hasExported = true;
        }

        private void DrawSummary()
        {
            Row("Files read", report.FileCount.ToString());
            Row("Parsed", report.Parsed.Count.ToString());
            Row("Unique boards", report.Groups == null
                ? "not collapsed  (source order keeps duplicates)"
                : report.UniqueBoardCount + "  (" + report.DuplicateCount + " duplicates dropped)");
            Row("Usable after checks", report.Pool.Levels.Count.ToString());
            Row("Solutions solved fresh", report.Pool.SolvedCount.ToString());
            Row("Solutions repaired", report.Pool.RepairedCount.ToString());
            Row("Dropped", report.Pool.DroppedIds.Count.ToString());

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Source data", EditorStyles.boldLabel);
            Row("Without a tutorial file", report.WithoutTutorial.ToString());
            Row("Start disagreed with tutorial", report.StartDisagreements.ToString());

            foreach (KeyValuePair<PayloadForm, int> entry in report.PayloadForms)
            {
                Row("Payload " + entry.Key, entry.Value.ToString());
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Campaign", EditorStyles.boldLabel);

            if (report.HasCampaign)
            {
                Row("Chapters", report.Campaign.Chapters.Count.ToString());
                Row("Levels", report.Campaign.LevelCount.ToString());
                Row("Forced placements", report.Campaign.RelaxedPlacements.ToString());
            }
            else
            {
                EditorGUILayout.HelpBox(report.CampaignError, MessageType.Error);
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            Row("Parse", report.ParseMilliseconds + " ms");
            Row("Check and repair", report.PrepareMilliseconds + " ms");
            Row("Score", report.ScoreMilliseconds + " ms");
            Row("Assemble", report.AssembleMilliseconds + " ms");
            Row("Total", report.TotalMilliseconds + " ms");
        }

        /// <summary>
        /// Plots the placed score, not the 1 to 10 difficulty. One difficulty band spans
        /// roughly one chapter, so plotting that would draw a flat staircase and hide the
        /// rests and peaks the curve is built around.
        /// </summary>
        private void DrawCurve()
        {
            if (!report.HasCampaign)
            {
                EditorGUILayout.HelpBox(report.CampaignError, MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField(
                "Difficulty across the campaign. Each bar is one level; dips are the rests.",
                EditorStyles.miniLabel);

            Rect area = GUILayoutUtility.GetRect(10f, 10000f, 160f, 160f);
            EditorGUI.DrawRect(area, EditorGUIUtility.isProSkin
                ? new Color(0.16f, 0.17f, 0.20f)
                : new Color(0.92f, 0.93f, 0.95f));

            var placements = new List<PlacedLevel>(report.Campaign.LevelCount);

            foreach (Chapter chapter in report.Campaign.Chapters)
            {
                placements.AddRange(chapter.Placements);
            }

            float barWidth = area.width / placements.Count;
            Color bar = EditorGUIUtility.isProSkin
                ? new Color(0.33f, 0.71f, 0.78f)
                : new Color(0.09f, 0.44f, 0.49f);

            for (int i = 0; i < placements.Count; i++)
            {
                float height = Mathf.Max(1f, (float)placements[i].Score * area.height);
                var rect = new Rect(
                    area.x + i * barWidth,
                    area.yMax - height,
                    Mathf.Max(1f, barWidth - 0.5f),
                    height);

                EditorGUI.DrawRect(rect, bar);
            }

            // Chapter boundaries, so the landing at the start of each one is visible.
            Color divider = EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.18f)
                : new Color(0f, 0f, 0f, 0.18f);
            int offset = 0;

            foreach (Chapter chapter in report.Campaign.Chapters)
            {
                offset += chapter.LevelCount;
                float x = area.x + offset * barWidth;
                EditorGUI.DrawRect(new Rect(x, area.y, 1f, area.height), divider);
            }

            EditorGUILayout.Space(4f);

            foreach (Chapter chapter in report.Campaign.Chapters)
            {
                double sum = 0;

                foreach (PlacedLevel placed in chapter.Placements)
                {
                    sum += placed.Score;
                }

                Row(chapter.Id, "mean score " + (sum / chapter.LevelCount).ToString("0.000"));
            }
        }

        private void DrawChapters()
        {
            if (!report.HasCampaign)
            {
                EditorGUILayout.HelpBox(report.CampaignError, MessageType.Error);
                return;
            }

            IReadOnlyList<Chapter> chapters = report.Campaign.Chapters;
            var chapterNames = new string[chapters.Count];

            for (int i = 0; i < chapters.Count; i++)
            {
                chapterNames[i] = chapters[i].Id;
            }

            selectedChapter = Mathf.Clamp(selectedChapter, 0, chapters.Count - 1);
            selectedChapter = EditorGUILayout.Popup("Chapter", selectedChapter, chapterNames);

            Chapter chapter = chapters[selectedChapter];
            selectedLevel = Mathf.Clamp(selectedLevel, 0, chapter.LevelCount - 1);
            selectedLevel = EditorGUILayout.IntSlider("Level", selectedLevel + 1, 1, chapter.LevelCount) - 1;
            showSolution = EditorGUILayout.Toggle("Show solution order", showSolution);

            PlacedLevel placed = chapter.Placements[selectedLevel];
            LevelData level = placed.Level;

            EditorGUILayout.Space(4f);
            Row("Id", level.Id);
            Row("Board", level.Grid + ", " + level.ActiveCellCount + " cells");
            Row("Difficulty", level.Difficulty + "  (score " + placed.Score.ToString("0.000") +
                              ", target " + placed.Target.ToString("0.000") + ")");
            Row("Has solution", level.HasSolution ? "yes" : "no");

            EditorGUILayout.Space(6f);
            float width = EditorGUIUtility.currentViewWidth - 40f;
            float height = LevelBoardPreview.MeasureHeight(level, width);
            Rect area = GUILayoutUtility.GetRect(width, height);
            LevelBoardPreview.Draw(area, level, showSolution);
        }

        private void DrawIssues()
        {
            if (report.ParseFailures.Count == 0 && report.Pool.DroppedIds.Count == 0)
            {
                EditorGUILayout.HelpBox("Every source file parsed and every board was solvable.", MessageType.Info);
                return;
            }

            if (report.ParseFailures.Count > 0)
            {
                EditorGUILayout.LabelField(
                    "Failed to parse (" + report.ParseFailures.Count + ")", EditorStyles.boldLabel);

                foreach (string failure in report.ParseFailures)
                {
                    EditorGUILayout.LabelField("    " + failure, EditorStyles.miniLabel);
                }

                EditorGUILayout.Space(6f);
            }

            if (report.Pool.DroppedIds.Count == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(
                "No solution found (" + report.Pool.DroppedIds.Count + ")", EditorStyles.boldLabel);

            foreach (string dropped in report.Pool.DroppedIds)
            {
                EditorGUILayout.LabelField("    " + dropped, EditorStyles.miniLabel);
            }
        }

        private static void Row(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(210f));
                EditorGUILayout.LabelField(value);
            }
        }
    }
}
