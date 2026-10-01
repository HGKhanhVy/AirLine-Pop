namespace ASTeams.SingleLine.Unity
{
    /// <summary>What a cat is doing on its own (GDD 4 behaviour table).</summary>
    public enum CatActivity
    {
        /// <summary>Standing about, breathing and blinking.</summary>
        Idle,

        /// <summary>Walking to a random point in its space.</summary>
        Wander,

        /// <summary>Walking to a spot (the yarn, the carrier, the seats) to do a routine there.</summary>
        Approach,

        /// <summary>Holding a routine's pose: grooming, stretching, napping, playing, rubbing.</summary>
        Routine,

        /// <summary>Chosen by the player: faces the camera and waits for a care action.</summary>
        Selected,

        /// <summary>Running a care action; nothing else may interrupt it.</summary>
        Busy,
    }
}
