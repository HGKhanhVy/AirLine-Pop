using System;
using UnityEngine;

namespace ASTeams.Base.Gameplay
{
    public interface IOnboardingAdapter
    {
        event Action TapProgressed;

        bool IsReady { get; }

        bool TryResolveStep(OnboardingConfigSO.StepData step, out OnboardingResolvedTarget resolvedTarget);
        bool TryGetWorldPositionByTargetId(string targetId, out Vector3 worldPosition);
        bool TryTapByTargetId(string targetId);
        bool TryBindStepAction(OnboardingConfigSO.StepData step, OnboardingResolvedTarget resolvedTarget);
        bool IsHoldingResolvedTarget(OnboardingResolvedTarget resolvedTarget);
        void ClearStepAction();
    }
}
