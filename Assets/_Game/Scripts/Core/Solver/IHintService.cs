using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    public interface IHintService
    {
        Task<HintResult> FindHintAsync(LevelData level, IReadOnlyList<int> path, int steps,
            int nodeBudget, int timeBudgetMilliseconds, CancellationToken cancellationToken);
    }
}
