using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    public sealed class HintResult
    {
        public static readonly HintResult RestartRequired = new HintResult(0, Array.Empty<int>(), true);

        public int BacktrackCount { get; }
        public IReadOnlyList<int> Steps { get; }
        public bool NeedsRestart { get; }

        public HintResult(int backtrackCount, int[] steps, bool needsRestart = false)
        {
            BacktrackCount = backtrackCount;
            Steps = Array.AsReadOnly(steps);
            NeedsRestart = needsRestart;
        }
    }
}
