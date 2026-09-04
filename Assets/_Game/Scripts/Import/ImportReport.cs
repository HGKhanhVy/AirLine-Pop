using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Everything one import run learned, kept so the editor window can explain the
    /// result instead of only announcing it. A number without the levels behind it is
    /// not much use when a chapter looks wrong.
    /// </summary>
    public sealed class ImportReport
    {
        public int FileCount { get; set; }

        public IReadOnlyList<LevelData> Parsed { get; set; }

        public Dictionary<LevelParseError, int> ParseErrors { get; } = new Dictionary<LevelParseError, int>();

        public Dictionary<PayloadForm, int> PayloadForms { get; } = new Dictionary<PayloadForm, int>();

        /// <summary>Source ids that failed to parse, with the reason, for the issue list.</summary>
        public List<string> ParseFailures { get; } = new List<string>();

        /// <summary>Files whose level and tutorial disagreed about the start cell.</summary>
        public int StartDisagreements { get; set; }

        /// <summary>Files from packs that ship no tutorial, so the solver had to supply one.</summary>
        public int WithoutTutorial { get; set; }

        public IReadOnlyList<LevelGroup> Groups { get; set; }

        public PreparedPool Pool { get; set; }

        public IReadOnlyList<ScoredLevel> Scored { get; set; }

        /// <summary>Null when the pool was too small to fill the campaign.</summary>
        public Campaign Campaign { get; set; }

        /// <summary>Why no campaign was built, when there is none.</summary>
        public string CampaignError { get; set; }

        public long ParseMilliseconds { get; set; }

        public long PrepareMilliseconds { get; set; }

        public long ScoreMilliseconds { get; set; }

        public long AssembleMilliseconds { get; set; }

        public long TotalMilliseconds =>
            ParseMilliseconds + PrepareMilliseconds + ScoreMilliseconds + AssembleMilliseconds;

        public int UniqueBoardCount => Groups == null ? 0 : Groups.Count;

        public int DuplicateCount => Parsed == null || Groups == null ? 0 : Parsed.Count - Groups.Count;

        public bool HasCampaign => Campaign != null;
    }
}
