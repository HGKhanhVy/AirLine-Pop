using System;
using ASTeams.Base.Gameplay;
using UnityEngine;

public sealed class OnboardingGameplayAdapter : MonoBehaviour, IOnboardingAdapter
{
    [SerializeField] private Vector3 defaultFocusSize = new(1.6f, 1.6f, 1f);

    public event Action TapProgressed;

    public bool IsReady => true;

    public bool TryResolveStep(OnboardingConfigSO.StepData step, out OnboardingResolvedTarget resolvedTarget)
    {
        resolvedTarget = default;
        if (step == null)
        {
            return false;
        }

        if (step.Target != OnboardingConfigSO.TargetSource.WorldPosition)
        {
            return false;
        }

        var focusSize = defaultFocusSize.sqrMagnitude > 0.001f ? defaultFocusSize : Vector3.one;
        resolvedTarget = new OnboardingResolvedTarget(
            "world_position",
            "World Position",
            "WorldPoint",
            null,
            null,
            step.WorldPosition,
            step.WorldFocusSize.sqrMagnitude > 0.001f ? step.WorldFocusSize : focusSize,
            default,
            hasGridPosition: false);
        return true;
    }

    public bool TryGetWorldPositionByTargetId(string targetId, out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;
        return false;
    }

    public bool TryTapByTargetId(string targetId)
    {
        return false;
    }

    public bool TryBindStepAction(OnboardingConfigSO.StepData step, OnboardingResolvedTarget resolvedTarget)
    {
        return step != null;
    }

    public bool IsHoldingResolvedTarget(OnboardingResolvedTarget resolvedTarget)
    {
        return false;
    }

    public void ClearStepAction()
    {
    }
}
