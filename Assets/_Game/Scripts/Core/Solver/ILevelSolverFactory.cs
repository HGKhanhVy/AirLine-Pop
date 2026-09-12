using System.Threading;

namespace ASTeams.SingleLine.Core
{
    public interface ILevelSolverFactory
    {
        ILevelSolver Create(int nodeBudget, CancellationToken cancellationToken);
    }
}
