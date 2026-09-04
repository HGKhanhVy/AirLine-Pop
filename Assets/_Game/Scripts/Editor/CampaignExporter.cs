using System.Collections.Generic;
using System.IO;
using ASTeams.SingleLine.Data;
using ASTeams.SingleLine.Import;
using UnityEditor;

namespace ASTeams.SingleLine.Editor
{
    /// <summary>
    /// Writes an assembled campaign out as one json file per chapter.
    ///
    /// Stale chapter files are removed first. An import that produces eight chapters
    /// where the last run produced ten must not leave the ninth and tenth behind for the
    /// repository to keep loading.
    /// </summary>
    public static class CampaignExporter
    {
        public const string DefaultOutputFolder = "Assets/_Game/Resources/Levels";

        private const string Extension = ".json";

        public static ExportResult Export(Campaign campaign, string outputFolder)
        {
            if (campaign == null)
            {
                return new ExportResult(0, 0, 0, "No campaign to export.");
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                return new ExportResult(0, 0, 0, "Output folder is empty.");
            }

            Directory.CreateDirectory(outputFolder);

            int removed = RemoveStaleChapters(campaign, outputFolder);
            long bytes = 0;

            foreach (Chapter chapter in campaign.Chapters)
            {
                string json = LevelJsonSerializer.SerializeChapter(chapter.Id, chapter.Levels);
                File.WriteAllText(Path.Combine(outputFolder, chapter.Id + Extension), json);
                bytes += json.Length;
            }

            AssetDatabase.Refresh();
            return new ExportResult(campaign.Chapters.Count, bytes, removed, null);
        }

        private static int RemoveStaleChapters(Campaign campaign, string outputFolder)
        {
            var current = new HashSet<string>();

            foreach (Chapter chapter in campaign.Chapters)
            {
                current.Add(chapter.Id + Extension);
            }

            int removed = 0;

            foreach (string path in Directory.GetFiles(outputFolder, "*" + Extension))
            {
                if (current.Contains(Path.GetFileName(path)))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(ToAssetPath(path));
                removed++;
            }

            return removed;
        }

        private static string ToAssetPath(string path)
        {
            string normalized = path.Replace('\\', '/');
            int index = normalized.IndexOf("Assets/", System.StringComparison.Ordinal);
            return index < 0 ? normalized : normalized.Substring(index);
        }
    }
}
