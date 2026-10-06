namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// What makes a win worth stopping for. The card after an ordinary flight moves on to the
    /// next by itself; after any of the others it waits for the player.
    /// </summary>
    public enum WinMilestone
    {
        None,

        /// <summary>The chapter's VIP flight: His Majesty comes out to say well done.</summary>
        Vip,

        /// <summary>A new regular joins the lounge after this flight.</summary>
        Arrival,

        /// <summary>The city's last flight: its stamp is complete and its postcard is earned.</summary>
        Postcard,

        /// <summary>The last flight of a chapter.</summary>
        ChapterEnd
    }
}
