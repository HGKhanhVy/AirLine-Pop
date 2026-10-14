using System.Threading;

namespace AirlinePop.SingleLine.Core
{
    public sealed class WarnsdorffSolverFactory : ILevelSolverFactory
    {
        public ILevelSolver Create(int nodeBudget, CancellationToken cancellationToken)
        {
            return new WarnsdorffSolver(nodeBudget, cancellationToken);
        }
    }
}
