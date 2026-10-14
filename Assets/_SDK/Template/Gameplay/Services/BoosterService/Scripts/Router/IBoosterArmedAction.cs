using UnityEngine;
using AirlinePop.Base.Data;

namespace AirlinePop.Base.Gameplay
{
    public interface IBoosterArmedAction
    {
        BoosterType Type { get; }

        // target là object gameplay player click (cell/block/item...). Bạn quyết định truyền gì.
        bool IsValidTarget(Component target);
        bool TryApplyTo(Component target);
    }
}