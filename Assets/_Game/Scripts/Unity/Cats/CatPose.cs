namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A held pose the cat's animator loops until the brain changes its mind. None is the
    /// ordinary stand and walk. The values are the animator's Action parameter.
    /// </summary>
    public enum CatPose
    {
        None = 0,

        /// <summary>Standing still like a passenger waiting to board; also the pose for being looked at.</summary>
        Wait = 1,

        /// <summary>Rolling about with a toy.</summary>
        Play = 2,

        /// <summary>Hopping on the spot, too excited to keep still.</summary>
        Hop = 3,

        /// <summary>Seeing stars after spinning round once too often.</summary>
        Dizzy = 4,
    }
}
