using System;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>One drawn clip in cat_motion.json: its frames, laid out row by row on one sheet.</summary>
    [Serializable]
    public sealed class CatMotionClipMeta
    {
        public string name;
        public int count;
        public int columns;
        public float seconds;
        public bool loop;
    }
}
