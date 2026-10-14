using System;
namespace AirlinePop.Base.Gameplay
{
    public interface IReviveAction
    {
        FailType FailType { get; }
        void Execute();
    }
}
