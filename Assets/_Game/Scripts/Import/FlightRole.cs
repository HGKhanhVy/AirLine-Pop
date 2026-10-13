namespace ASTeams.SingleLine.Import
{
    /// <summary>The part a flight plays in its city's rhythm of ten.</summary>
    public enum FlightRole
    {
        /// <summary>The first flights of the game, one small step bigger each time.</summary>
        Onboarding = 0,

        /// <summary>The steady climb that makes up most of a city.</summary>
        Climb = 1,

        /// <summary>A breather: smaller and kinder, often a night or formation flight.</summary>
        Rest = 2,

        /// <summary>The city's hardest flight, billed in advance and paying double.</summary>
        Storm = 3,

        /// <summary>The flight that lands in the city: a picture board or a solid closer.</summary>
        Landing = 4
    }
}
