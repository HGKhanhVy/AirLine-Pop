using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The motion things make as they arrive on screen.
    /// </summary>
    public static class EntranceCurves
    {
        /// <summary>
        /// The drop of the reference game's logo letters (BlockDropingAnimation), as
        /// progress from 0 to 1: a fast fall that runs 6% past the mark, one small bounce
        /// back, then rest. The clip itself cannot be reused because it is keyed to the
        /// old logo's hierarchy, so its keyframes are carried over here.
        /// </summary>
        public static AnimationCurve CreateDrop()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 2.6f),
                new Keyframe(0.567f, 1.06f, 0f, 0f),
                new Keyframe(0.833f, 0.987f, 0f, 0f),
                new Keyframe(1f, 1f, 0f, 0f));
        }
    }
}
