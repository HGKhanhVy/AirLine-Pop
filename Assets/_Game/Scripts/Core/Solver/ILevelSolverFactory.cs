using System.Threading;

namespace AirlinePop.SingleLine.Core
{
    public interface ILevelSolverFactory
    {
        ILevelSolver Create(int nodeBudget, CancellationToken cancellationToken);
    }
}
