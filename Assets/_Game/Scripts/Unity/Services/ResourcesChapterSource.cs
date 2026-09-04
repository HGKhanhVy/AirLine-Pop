using ASTeams.SingleLine.Data;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Reads chapter files shipped under a Resources folder.
    ///
    /// This is the only place in the project that turns a chapter id into a path. Every
    /// other caller goes through <see cref="ILevelRepository"/>, so swapping Resources
    /// for Addressables or a downloaded pack later means replacing this one class.
    ///
    /// It lives outside the assembly definitions on purpose: the bridge layer needs
    /// UnityEngine, while the data layer stays engine free so it can be tested without
    /// opening the editor.
    /// </summary>
    public sealed class ResourcesChapterSource : IChapterSource
    {
        /// <summary>Folder under any Resources root, without a trailing slash.</summary>
        public const string DefaultFolder = "Levels";

        private readonly string folder;

        public ResourcesChapterSource()
            : this(DefaultFolder)
        {
        }

        public ResourcesChapterSource(string folder)
        {
            this.folder = string.IsNullOrEmpty(folder) ? DefaultFolder : folder;
        }

        public bool TryReadChapter(string chapterId, out string json)
        {
            json = null;

            if (string.IsNullOrEmpty(chapterId))
            {
                return false;
            }

            // Resources paths carry no file extension, so the exported ch01.json is
            // addressed as Levels/ch01.
            var asset = Resources.Load<TextAsset>(folder + "/" + chapterId);

            if (asset == null)
            {
                return false;
            }

            json = asset.text;

            // The parsed levels are cached by the repository, so the raw text has no
            // reason to stay resident once it has been read.
            Resources.UnloadAsset(asset);
            return true;
        }
    }
}
