using System;
namespace ASTeams.Base.Gameplay
{
    public interface IReviveAction
    {
        FailType FailType { get; }
        void Execute();
    }
}
