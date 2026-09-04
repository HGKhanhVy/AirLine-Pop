using System.Collections.Generic;
using System.IO;
using ASTeams.SingleLine.Import;

namespace ASTeams.SingleLine.Editor
{
    /// <summary>
    /// Collects the reference game's level files off disk into the shape the pipeline
    /// takes. Reading files is kept out of the pipeline itself so the pipeline stays
    /// engine free and testable.
    /// </summary>
    public static class LevelSourceReader
    {
        public const string DefaultSourceFolder = "Assets/_Game/Asset_Resources/Level Data/oneline";

        private const string LevelPrefix = "Level_";
        private const string TutorialPrefix = "Toturial_";

        /// <summary>
        /// Walks every pack folder under <paramref name="rootFolder"/>. Ids come out as
        /// pack and file name, which is what the issue lists show and what makes an
        /// import reproducible.
        /// </summary>
        public static List<RawLevelFile> Read(string rootFolder)
        {
            var files = new List<RawLevelFile>();

            if (string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder))
            {
                return files;
            }

            foreach (string packDirectory in Directory.GetDirectories(rootFolder))
            {
                string pack = Path.GetFileName(packDirectory);

                foreach (string levelPath in Directory.GetFiles(packDirectory, LevelPrefix + "*.txt"))
                {
                    string name = Path.GetFileNameWithoutExtension(levelPath);
                    string tutorialPath = Path.Combine(
                        packDirectory,
                        name.Replace(LevelPrefix, TutorialPrefix) + ".txt");

                    files.Add(new RawLevelFile(
                        pack + "/" + name,
                        File.ReadAllText(levelPath),
                        File.Exists(tutorialPath) ? File.ReadAllText(tutorialPath) : null));
                }
            }

            return files;
        }

        public static int CountPacks(string rootFolder)
        {
            return string.IsNullOrEmpty(rootFolder) || !Directory.Exists(rootFolder)
                ? 0
                : Directory.GetDirectories(rootFolder).Length;
        }
    }
}
