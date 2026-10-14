using System;

namespace AirlinePop.SingleLine.Core
{
    /// <summary>The extra conditions a level can add on top of covering every square.</summary>
    [Flags]
    public enum LevelRule
    {
        None = 0,

        /// <summary>The flight must end on the runway square, and cannot fly on from it.</summary>
        Runway = 1,

        /// <summary>Some squares carry wind: leaving one, the plane must follow its arrow.</summary>
        Wind = 2,

        /// <summary>A night flight: the route is shown once, then the board goes dark but for the plane's lights.</summary>
        Night = 4,

        /// <summary>
        /// Formation flying: the board is two mirror halves and a wingman flies the mirror of
        /// the player's route, so covering one half covers the other.
        /// </summary>
        Formation = 8,

        /// <summary>
        /// Not a condition of its own: the VIP flight closing a chapter, which mixes the
        /// others. Carried here so the screens that explain conditions can explain it too.
        /// </summary>
        Vip = 16,

        /// <summary>Turbulence clouds: squares the plane must fly straight through.</summary>
        Turbulence = 32,

        /// <summary>Beacons: squares the plane must turn on.</summary>
        Beacon = 64,

        /// <summary>Overpasses: squares flown over twice, crossing the route already drawn.</summary>
        Bridge = 128,

        /// <summary>
        /// Not a condition of its own either: the storm flight, the hardest of its city, which
        /// pays double. Carried here so the screens that explain conditions can bill it.
        /// </summary>
        Storm = 256
    }
}
