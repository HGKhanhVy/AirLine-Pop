namespace ASTeams.SingleLine.Import
{
    /// <summary>How an import decides the order the shipped levels are played in.</summary>
    public enum CampaignMode
    {
        /// <summary>
        /// Keep the reference packs exactly as they are numbered. This is what ships.
        /// </summary>
        SourceOrder = 0,

        /// <summary>
        /// Rebuild the run onto the designed difficulty curve. Kept for comparing a
        /// generated ordering against the reference one, not for release.
        /// </summary>
        DifficultyCurve = 1
    }
}
