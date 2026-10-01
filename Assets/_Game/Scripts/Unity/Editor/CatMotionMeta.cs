using System;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// The cat_motion.json written by Tools/flat_art/import_craftpix_cats.py: frame size and
    /// pivot shared by every motion sheet, and the clips each breed has a sheet for.
    /// </summary>
    [Serializable]
    public sealed class CatMotionMeta
    {
        public int frameWidth;
        public int frameHeight;
        public float pivotX;
        public float pivotY;
        public float pixelsPerUnit;
        public CatMotionClipMeta[] clips;
    }
}
