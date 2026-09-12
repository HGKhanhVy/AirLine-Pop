using System.Collections.Generic;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Turns a scored pool into the ordered campaign that ships.
    ///
    /// Two orderings are in use and they answer different questions, so neither can be
    /// expressed as a setting on the other: <see cref="SourceOrderAssembler"/> keeps the
    /// reference packs exactly as their authors numbered them, while
    /// <see cref="ChapterAssembler"/> rebuilds the run onto a designed difficulty curve.
    /// Adding a third ordering means adding a class here, not editing either of them.
    /// </summary>
    public interface ICampaignAssembler
    {
        Campaign Assemble(IReadOnlyList<ScoredLevel> pool);
    }
}
