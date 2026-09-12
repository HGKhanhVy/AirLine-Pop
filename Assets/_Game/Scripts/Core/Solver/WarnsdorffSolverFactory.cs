using System.Threading;

namespace ASTeams.SingleLine.Core
{
    public sealed class WarnsdorffSolverFactory : ILevelSolverFactory
    {
        public ILevelSolver Create(int nodeBudget, CancellationToken cancellationToken)
        {
            return new WarnsdorffSolver(nodeBudget, cancellationToken);
        }
    }
}
