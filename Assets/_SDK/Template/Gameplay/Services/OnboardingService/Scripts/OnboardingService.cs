using System;
using ASTeams.Base.Gameplay;
using UnityEngine;
using UnityEngine.Serialization;

public class OnboardingService : GameplayServiceBehaviour
{
    private const string CompletedLevelPlayerPrefKeyPrefix = "onboarding.level.completed.";

    public event Action TutorialStarted;
    public event Action TutorialCompleted;

    [Header("Config")]
    [SerializeField] private OnboardingConfigSO config;
    [FormerlySerializedAs("autoPlayFirstOnLevelLoadedStep")]
    [SerializeField] private bool autoPlayFirstStep = true;
    [SerializeField] private bool tryBootstrapFromCurrentRuntime = true;
    [SerializeField] private bool playEachLevelOnlyOnce = true;
    [SerializeField] private bool verboseLogging;
    [SerializeField] private bool forceRuntimeDiagnostics = true;
    [SerializeField, Min(0.05f)] private float unresolvedStepRetryDelaySeconds = 0.25f;
    [SerializeField, Min(0)] private int unresolvedStepRetryMaxAttempts;

    [Header("Runtime")]
    [FormerlySerializedAs("adapterSource")]
    [FormerlySerializedAs("targetResolverSource")]
    [FormerlySerializedAs("runtimeBridgeSource")]
    [SerializeField] private MonoBehaviour targetAdapterSource;

    [Header("Presentation")]
    [SerializeField] private OnboardingMask tutorialMask;
    [SerializeField] private OnboardingPanelTap panelTap;

    private LevelService _levelService;
    private IOnboardingAdapter _adapter;
    private bool _isSubscribedToAdapter;
    private bool _awaitingRuntimeBootstrap;
    private int _pendingLevelIndex = -1;
    private int _scheduledStepIndex = -1;
    private float _scheduledStepDelayRemaining;

    private OnboardingConfigSO.LevelOnboarding _activeLevel;
    private int _activeLevelIndex = -1;
    private OnboardingResolvedTarget _currentResolvedTarget;
    private OnboardingConfigSO.StepData _currentStep;
    private int _currentStepIndex = -1;
    private float _advanceTimerRemaining;
    private float _holdProgressSeconds;
    private bool _isTutorialRunning;
    private int _pendingRetryStepIndex = -1;
    private int _pendingRetryAttempts;

    public override void OnRegister(GameplayServices services)
    {
        _levelService = services.Get<LevelService>();
    }

    public override void OnStart()
    {
        OnboardingRuntimeSettings.EnabledChanged += HandleOnboardingEnabledChanged;
        LogRuntimeState("OnStart");

        if (_levelService != null)
        {
            _levelService.OnLevelLoaded += HandleServiceLevelLoaded;
        }

        TryResolveAdapter(forceResubscribe: true);
        ResolveTutorialMaskIfNeeded();
        SubscribeToMaskEvents(true);
        ClearPresentation();
        DisablePanelCompletely();
        ClearActiveState();

        if (!OnboardingRuntimeSettings.IsEnabled)
        {
            Log("Onboarding is disabled via cheat toggle.");
            return;
        }

        if (tryBootstrapFromCurrentRuntime)
        {
            int currentLevelIndex = GetCurrentKnownLevelIndex();
            if (currentLevelIndex >= 0)
            {
                _pendingLevelIndex = currentLevelIndex;
                _awaitingRuntimeBootstrap = true;
            }

            TryActivatePendingFlow();
        }
    }

    public override void Tick(float dt)
    {
        if (!OnboardingRuntimeSettings.IsEnabled)
        {
            return;
        }

        if (_adapter == null)
        {
            TryResolveAdapter(forceResubscribe: true);
        }

        if (_awaitingRuntimeBootstrap)
        {
            TryActivatePendingFlow();
        }

        TickScheduledStep(dt);
        TickStepAdvance(dt);
    }

    public override void OnStop()
    {
        OnboardingRuntimeSettings.EnabledChanged -= HandleOnboardingEnabledChanged;

        if (_levelService != null)
        {
            _levelService.OnLevelLoaded -= HandleServiceLevelLoaded;
        }

        SubscribeToAdapter(false);
        SubscribeToMaskEvents(false);
        ClearPresentation();
        ClearActiveState();
    }

    private void HandleServiceLevelLoaded(int levelNumber, ASTeams.Base.Level.LevelConfig _)
    {
        _pendingLevelIndex = Mathf.Max(0, levelNumber - 1);
        _awaitingRuntimeBootstrap = true;
        LogRuntimeState($"LevelLoaded:{levelNumber}");
        Log($"LevelService loaded level {_pendingLevelIndex}.");
    }

    private void HandleRuntimeTapProgressed()
    {
        if (_activeLevel == null || _currentStep == null)
        {
            return;
        }

        if (_currentStep.Advance != OnboardingConfigSO.AdvanceMode.Tap)
        {
            return;
        }

        Log($"Runtime tap completed for step '{ResolveStepLabel(_currentStep)}'. Advancing to next step.");
        NextStep();
    }

    private void TryResolveAdapter(bool forceResubscribe)
    {
        MonoBehaviour source = targetAdapterSource;
        if (source == null)
        {
            source = FindAdapterSource();
            targetAdapterSource = source;
        }

        if (source == null)
        {
            Log("No onboarding adapter found.");
            return;
        }

        if (!(source is IOnboardingAdapter resolved))
        {
            Log($"Adapter '{source.GetType().Name}' does not implement {nameof(IOnboardingAdapter)}.");
            return;
        }

        if (ReferenceEquals(_adapter, resolved) && !forceResubscribe)
        {
            return;
        }

        SubscribeToAdapter(false);
        _adapter = resolved;
        SubscribeToAdapter(true);
        Log($"Resolved onboarding adapter: {source.GetType().Name}.");
    }

    private MonoBehaviour FindAdapterSource()
    {
        MonoBehaviour[] localBehaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < localBehaviours.Length; i++)
        {
            if (localBehaviours[i] is IOnboardingAdapter)
            {
                return localBehaviours[i];
            }
        }

#if UNITY_2023_1_OR_NEWER
        MonoBehaviour[] sceneBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        MonoBehaviour[] sceneBehaviours = FindObjectsOfType<MonoBehaviour>(true);
#endif
        for (int i = 0; i < sceneBehaviours.Length; i++)
        {
            if (sceneBehaviours[i] is IOnboardingAdapter)
            {
                return sceneBehaviours[i];
            }
        }

        return null;
    }

    private void SubscribeToAdapter(bool subscribe)
    {
        if (_adapter == null)
        {
            _isSubscribedToAdapter = false;
            return;
        }

        if (subscribe)
        {
            if (_isSubscribedToAdapter)
            {
                return;
            }

            _adapter.TapProgressed += HandleRuntimeTapProgressed;
            _isSubscribedToAdapter = true;
            return;
        }

        if (!_isSubscribedToAdapter)
        {
            return;
        }

        _adapter.TapProgressed -= HandleRuntimeTapProgressed;
        _isSubscribedToAdapter = false;
    }

    private void TryActivatePendingFlow()
    {
        if (!_awaitingRuntimeBootstrap)
        {
            return;
        }

        if (!OnboardingRuntimeSettings.IsEnabled)
        {
            return;
        }

        if (_adapter == null || !_adapter.IsReady)
        {
            LogRuntimeState("WaitingAdapterReady");
            return;
        }

        int runtimeLevelIndex = GetCurrentKnownLevelIndex();
        if (runtimeLevelIndex < 0)
        {
            LogRuntimeState("WaitingCurrentLevel");
            return;
        }

        int effectiveLevelIndex = _pendingLevelIndex >= 0 ? _pendingLevelIndex : runtimeLevelIndex;
        if (effectiveLevelIndex != runtimeLevelIndex)
        {
            Log($"Waiting: pending level {effectiveLevelIndex} but current runtime reports {runtimeLevelIndex}.");
            effectiveLevelIndex = runtimeLevelIndex;
        }

        _awaitingRuntimeBootstrap = false;
        ActivateLevel(effectiveLevelIndex);
    }

    private void HandleOnboardingEnabledChanged(bool isEnabled)
    {
        if (!isEnabled)
        {
            Log("Onboarding disabled via cheat toggle.");
            ClearPresentation();
            ClearActiveState();
            return;
        }

        int currentLevelIndex = GetCurrentKnownLevelIndex();
        if (currentLevelIndex < 0)
        {
            Log("Onboarding enabled via cheat toggle, but current level is not available yet.");
            return;
        }

        _pendingLevelIndex = currentLevelIndex;
        _awaitingRuntimeBootstrap = true;
        Log($"Onboarding enabled via cheat toggle. Rebootstrapping level {currentLevelIndex}.");
        TryActivatePendingFlow();
    }

    private int GetCurrentKnownLevelIndex()
    {
        if (_levelService == null || _levelService.CurrentLevelNumber <= 0)
        {
            return -1;
        }

        return Mathf.Max(0, _levelService.CurrentLevelNumber - 1);
    }

    private void ActivateLevel(int levelIndex)
    {
        LogRuntimeState($"ActivateLevel:{levelIndex}");
        if (playEachLevelOnlyOnce && IsLevelCompleted(levelIndex))
        {
            Log($"Skip onboarding for level {levelIndex} because it already completed.");
            ClearPresentation();
            ClearActiveState();
            return;
        }

        if (config == null || !config.TryGetLevel(levelIndex, out OnboardingConfigSO.LevelOnboarding level))
        {
            Log($"No onboarding level bound for level {levelIndex}. Clearing active onboarding state.");
            ClearPresentation();
            ClearActiveState();
            return;
        }

        ResetStepState();
        _activeLevel = level;
        _activeLevelIndex = levelIndex;
        StartTutorialFlow();
        Log($"Activated onboarding level {levelIndex}.");

        if (!autoPlayFirstStep)
        {
            return;
        }

        ScheduleFirstStep();
    }

    private void ScheduleFirstStep()
    {
        int stepIndex = FindNextValidStepIndex(-1);
        if (stepIndex < 0)
        {
            Log("Active onboarding level does not contain any valid step.");
            ClearPresentation();
            ResetStepRuntimeState();
            return;
        }

        ScheduleStep(stepIndex);
    }

    private int FindNextValidStepIndex(int fromIndexExclusive)
    {
        if (_activeLevel == null || _activeLevel.StepCount <= 0)
        {
            return -1;
        }

        for (int i = fromIndexExclusive + 1; i < _activeLevel.StepCount; i++)
        {
            if (_activeLevel.TryGetStep(i, out _))
            {
                return i;
            }
        }

        return -1;
    }

    private void ScheduleStep(int stepIndex)
    {
        if (_activeLevel == null || !_activeLevel.TryGetStep(stepIndex, out OnboardingConfigSO.StepData step))
        {
            CompleteFlow();
            return;
        }

        float delaySeconds = step.DelayBeforeShowing;
        if (delaySeconds <= 0f)
        {
            ShowStep(stepIndex);
            return;
        }

        _scheduledStepIndex = stepIndex;
        _scheduledStepDelayRemaining = delaySeconds;
        ClearPresentation();
        ResetStepRuntimeState();
        Log($"Scheduled step {stepIndex} with delay {delaySeconds:0.##}s.");
    }

    private void TickScheduledStep(float dt)
    {
        if (_scheduledStepIndex < 0)
        {
            return;
        }

        _scheduledStepDelayRemaining -= Mathf.Max(0f, dt);
        if (_scheduledStepDelayRemaining > 0f)
        {
            return;
        }

        int stepIndex = _scheduledStepIndex;
        CancelScheduledStep();
        ShowStep(stepIndex);
    }

    private void CancelScheduledStep()
    {
        _scheduledStepIndex = -1;
        _scheduledStepDelayRemaining = 0f;
    }

    private void ShowStep(int stepIndex)
    {
        _pendingRetryStepIndex = -1;
        _pendingRetryAttempts = 0;
        CancelScheduledStep();

        if (_activeLevel == null)
        {
            ClearPresentation();
            ResetStepRuntimeState();
            return;
        }

        if (!_activeLevel.TryGetStep(stepIndex, out OnboardingConfigSO.StepData step))
        {
            CompleteFlow();
            return;
        }

        _currentStep = step;
        _currentStepIndex = stepIndex;
        _currentResolvedTarget = default;
        ResetStepAdvanceState();
        bool isFinalStep = FindNextValidStepIndex(stepIndex) < 0;

        ResolveTutorialMaskIfNeeded();
        tutorialMask?.SetIsFinalStep(isFinalStep);
        tutorialMask?.SetShape(step.Shape);
        DisablePanelCompletely();

        if (step.Target == OnboardingConfigSO.TargetSource.None)
        {
            _adapter?.ClearStepAction();
            ApplyStepPresentation(default, step);
            return;
        }

        if (_adapter == null)
        {
            HideMask();
            Log($"Step '{ResolveStepLabel(step)}' could not be shown because onboarding adapter is missing.");
            TryScheduleRetryStep(stepIndex, "adapter missing");
            return;
        }

        if (!_adapter.TryResolveStep(step, out OnboardingResolvedTarget resolvedTarget))
        {
            HideMask();
            _adapter.ClearStepAction();
            Log($"Step '{ResolveStepLabel(step)}' could not resolve its runtime target.");
            TryScheduleRetryStep(stepIndex, "runtime target unresolved");
            return;
        }

        _currentResolvedTarget = resolvedTarget;
        ApplyStepPresentation(resolvedTarget, step);
        ApplyStepAction(step, resolvedTarget);
    }

    private void ApplyStepPresentation(OnboardingResolvedTarget resolvedTarget, OnboardingConfigSO.StepData step)
    {
        RenderPanel(step);

        if (tutorialMask == null)
        {
            Log($"Tutorial mask is missing; step '{ResolveStepLabel(step)}' will not render focus.");
            return;
        }

        if (step.Target == OnboardingConfigSO.TargetSource.None)
        {
            tutorialMask.ShowOverlayOnly();
            panelTap?.ResetHandAnchorToDefault();
            Log($"Step '{ResolveStepLabel(step)}' shows overlay only (no target source).");
            return;
        }

        Vector3 configuredFocusSize = step.WorldFocusSize;
        bool hasConfiguredFocusSize = configuredFocusSize.sqrMagnitude > 0.0001f;
        bool hasWorldOffset = step.WorldOffsetPosition.sqrMagnitude > 0.0001f;
        Vector3 focusCenter = resolvedTarget.WorldPosition + step.WorldOffsetPosition;

        if (resolvedTarget.HasTransform)
        {
            if (hasConfiguredFocusSize || hasWorldOffset)
            {
                Vector3 targetFocusSize = hasConfiguredFocusSize ? configuredFocusSize : resolvedTarget.FocusWorldSize;
                if (targetFocusSize.sqrMagnitude <= 0.0001f)
                {
                    targetFocusSize = Vector3.one;
                }

                tutorialMask.ShowForWorldFocus(focusCenter, targetFocusSize, refreshNow: true);
            }
            else
            {
                tutorialMask.ShowForTarget(resolvedTarget.TargetTransform, refreshNow: true);
            }

            UpdateHandAnchorFromMask();
            if (step.Advance == OnboardingConfigSO.AdvanceMode.Tap)
            {
                panelTap?.PlayHandStepStartAnimation();
            }
            Log(BuildResolvedTargetLogMessage(step, resolvedTarget, "transform"));
            return;
        }

        Vector3 focusSize = hasConfiguredFocusSize ? configuredFocusSize : resolvedTarget.FocusWorldSize;
        if (focusSize.sqrMagnitude <= 0.0001f)
        {
            focusSize = Vector3.one;
        }

        tutorialMask.ShowForWorldFocus(focusCenter, focusSize, refreshNow: true);
        UpdateHandAnchorFromMask();
        if (step.Advance == OnboardingConfigSO.AdvanceMode.Tap)
        {
            panelTap?.PlayHandStepStartAnimation();
        }
        Log($"Step '{ResolveStepLabel(step)}' focusing world area for target '{resolvedTarget.Id}'.");
    }

    private void RenderPanel(OnboardingConfigSO.StepData step)
    {
        if (panelTap == null || step == null)
        {
            return;
        }

        if (!panelTap.gameObject.activeSelf)
        {
            panelTap.gameObject.SetActive(true);
        }

        string message = ResolveStepMessage(step);
        bool showMessage = !string.IsNullOrWhiteSpace(message);
        bool hasFocusableTarget = step.Target != OnboardingConfigSO.TargetSource.None;
        bool showHandIcon = hasFocusableTarget && step.Advance == OnboardingConfigSO.AdvanceMode.Tap;
        panelTap.Render(new OnboardingPanelTap.ViewData
        {
            Message = message,
            ShowMessage = showMessage,
            ShowHandIcon = showHandIcon
        });
        panelTap.SetVisible(showMessage || showHandIcon);
    }

    private static string ResolveStepMessage(OnboardingConfigSO.StepData step)
    {
        if (!string.IsNullOrWhiteSpace(step.Description))
        {
            return step.Description;
        }

        return step.Title;
    }

    private void ApplyStepAction(OnboardingConfigSO.StepData step, OnboardingResolvedTarget resolvedTarget)
    {
        if (_adapter == null)
        {
            return;
        }

        if (_adapter.TryBindStepAction(step, resolvedTarget))
        {
            return;
        }

        _adapter.ClearStepAction();
        Log(
            $"Step '{ResolveStepLabel(step)}' uses runtime action but target '{resolvedTarget.Id}' " +
            "does not expose a valid interaction binding.");
    }

    private void NextStep()
    {
        if (_activeLevel == null)
        {
            return;
        }

        if (_currentStep != null && _currentStep.Advance == OnboardingConfigSO.AdvanceMode.Tap)
        {
            panelTap?.PlayHandStepCompleteAnimation();
        }

        if (_adapter == null || !_adapter.IsReady)
        {
            Log("Cannot advance onboarding step because onboarding adapter is not ready.");
            return;
        }

        int nextIndex = FindNextValidStepIndex(_currentStepIndex);
        if (nextIndex < 0)
        {
            CompleteFlow();
            return;
        }

        ScheduleStep(nextIndex);
    }

    private void CompleteFlow()
    {
        MarkActiveLevelAsCompletedIfNeeded();
        CancelScheduledStep();
        ClearPresentation();
        ResetStepRuntimeState();
        _activeLevel = null;
        _activeLevelIndex = -1;
        CompleteTutorialFlow();
        Log("Completed onboarding flow for current level.");
    }

    private void ResetStepRuntimeState()
    {
        _adapter?.ClearStepAction();
        _currentResolvedTarget = default;
        _currentStep = null;
        _currentStepIndex = -1;
        ResetStepAdvanceState();
    }

    private void ResetStepState()
    {
        CancelScheduledStep();
        ResetStepRuntimeState();
        _pendingRetryStepIndex = -1;
        _pendingRetryAttempts = 0;
    }

    private string BuildResolvedTargetLogMessage(OnboardingConfigSO.StepData step, OnboardingResolvedTarget resolvedTarget, string focusType)
    {
        if (resolvedTarget.HasGridPosition)
        {
            return
                $"Step '{ResolveStepLabel(step)}' focusing {focusType} target '{resolvedTarget.Id}' " +
                $"at grid ({resolvedTarget.GridPosition.x}, {resolvedTarget.GridPosition.y}).";
        }

        return
            $"Step '{ResolveStepLabel(step)}' focusing {focusType} target '{resolvedTarget.Id}' " +
            $"at world {resolvedTarget.WorldPosition}.";
    }

    private void ResolveTutorialMaskIfNeeded()
    {
        if (tutorialMask != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        tutorialMask = FindFirstObjectByType<OnboardingMask>(FindObjectsInactive.Include);
#else
        tutorialMask = FindObjectOfType<OnboardingMask>(true);
#endif
    }

    private void SubscribeToMaskEvents(bool subscribe)
    {
        if (tutorialMask == null)
        {
            return;
        }

        if (subscribe)
        {
            tutorialMask.HoleTapped += HandleMaskHoleTapped;
            return;
        }

        tutorialMask.HoleTapped -= HandleMaskHoleTapped;
    }

    private void HandleMaskHoleTapped()
    {
        if (_currentStep == null || _adapter == null)
        {
            return;
        }

        if (_currentStep.Advance != OnboardingConfigSO.AdvanceMode.Tap)
        {
            return;
        }

        if (_currentStep.Target != OnboardingConfigSO.TargetSource.RuntimeTargetId)
        {
            return;
        }

        panelTap?.PlayHandClickAnimation();
        _adapter.TryTapByTargetId(_currentStep.RuntimeTargetId);
    }

    private void UpdateHandAnchorFromMask()
    {
        if (tutorialMask == null || panelTap == null)
        {
            return;
        }

        if (!tutorialMask.TryGetFocusCenterScreenPoint(out Vector2 centerScreenPoint, out Camera eventCamera))
        {
            panelTap.ResetHandAnchorToDefault();
            return;
        }

        panelTap.SetHandAnchorScreenPosition(centerScreenPoint, eventCamera);
    }

    private void HideMask()
    {
        if (tutorialMask != null)
        {
            tutorialMask.HideMask(clearTarget: true);
        }
    }

    private void HidePanel()
    {
        if (panelTap != null)
        {
            panelTap.SetVisible(false);
        }
    }

    private void DisablePanelCompletely()
    {
        if (panelTap == null)
        {
            return;
        }

        panelTap.SetVisible(false);
        if (panelTap.gameObject.activeSelf)
        {
            panelTap.gameObject.SetActive(false);
        }
    }

    private void ClearPresentation()
    {
        HideMask();
        HidePanel();
    }

    private void ClearActiveState()
    {
        ResetStepState();
        _activeLevel = null;
        _activeLevelIndex = -1;
        _isTutorialRunning = false;
    }

    private void MarkActiveLevelAsCompletedIfNeeded()
    {
        if (!playEachLevelOnlyOnce || _activeLevelIndex < 0)
        {
            return;
        }

        PlayerPrefs.SetInt(GetCompletedLevelPlayerPrefKey(_activeLevelIndex), 1);
        PlayerPrefs.Save();
    }

    private static bool IsLevelCompleted(int levelIndex)
    {
        return PlayerPrefs.GetInt(GetCompletedLevelPlayerPrefKey(levelIndex), 0) == 1;
    }

    private static string GetCompletedLevelPlayerPrefKey(int levelIndex)
    {
        return $"{CompletedLevelPlayerPrefKeyPrefix}{Mathf.Max(0, levelIndex)}";
    }

    private void StartTutorialFlow()
    {
        if (_isTutorialRunning)
        {
            return;
        }

        _isTutorialRunning = true;
        TutorialStarted?.Invoke();
    }

    private void CompleteTutorialFlow()
    {
        if (!_isTutorialRunning)
        {
            return;
        }

        _isTutorialRunning = false;
        TutorialCompleted?.Invoke();
    }

    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[{nameof(OnboardingService)}] {message}", this);
        }
    }

    private void LogRuntimeState(string context)
    {
        if (!verboseLogging && !forceRuntimeDiagnostics)
        {
            return;
        }

        var currentLevelIndex = GetCurrentKnownLevelIndex();
        var completedCurrent = currentLevelIndex >= 0 && IsLevelCompleted(currentLevelIndex);
        var enabledFlag = OnboardingRuntimeSettings.IsEnabled;
        Debug.Log(
            $"[{nameof(OnboardingService)}][{context}] " +
            $"enabled={enabledFlag}, currentLevelIndex={currentLevelIndex}, " +
            $"completedCurrent={completedCurrent}, pendingLevel={_pendingLevelIndex}, " +
            $"awaitBootstrap={_awaitingRuntimeBootstrap}, adapterReady={_adapter != null && _adapter.IsReady}",
            this);
    }

    private void TryScheduleRetryStep(int stepIndex, string reason)
    {
        if (_activeLevel == null || stepIndex < 0)
        {
            return;
        }

        if (_pendingRetryStepIndex != stepIndex)
        {
            _pendingRetryStepIndex = stepIndex;
            _pendingRetryAttempts = 0;
        }

        if (HasExceededRetryAttempts())
        {
            Log($"Step retry reached max attempts for index {stepIndex}. Reason: {reason}.");
            return;
        }

        _pendingRetryAttempts++;
        _scheduledStepIndex = stepIndex;
        _scheduledStepDelayRemaining = Mathf.Max(0.05f, unresolvedStepRetryDelaySeconds);
        Log(
            $"Retry step index {stepIndex} in {_scheduledStepDelayRemaining:0.##}s " +
            $"(attempt {_pendingRetryAttempts}/{ResolveRetryLimitLabel()}). Reason: {reason}.");
    }

    private bool HasExceededRetryAttempts()
    {
        if (unresolvedStepRetryMaxAttempts <= 0)
        {
            return false;
        }

        return _pendingRetryAttempts >= unresolvedStepRetryMaxAttempts;
    }

    private string ResolveRetryLimitLabel()
    {
        return unresolvedStepRetryMaxAttempts <= 0
            ? "unlimited"
            : unresolvedStepRetryMaxAttempts.ToString();
    }

    private void TickStepAdvance(float dt)
    {
        if (_activeLevel == null || _currentStep == null)
        {
            return;
        }

        switch (_currentStep.Advance)
        {
            case OnboardingConfigSO.AdvanceMode.Waiting:
            case OnboardingConfigSO.AdvanceMode.Highlight:
                TickWaitingAdvance(dt);
                break;

            case OnboardingConfigSO.AdvanceMode.Hold:
                TickHoldAdvance(dt);
                break;

            case OnboardingConfigSO.AdvanceMode.Tap:
            default:
                break;
        }
    }

    private void TickWaitingAdvance(float dt)
    {
        if (_advanceTimerRemaining <= 0f)
        {
            _advanceTimerRemaining = ResolveAdvanceDuration(_currentStep);
        }

        _advanceTimerRemaining -= Mathf.Max(0f, dt);
        if (_advanceTimerRemaining > 0f)
        {
            return;
        }

        Log($"Waiting completed for step '{ResolveStepLabel(_currentStep)}'. Advancing to next step.");
        NextStep();
    }

    private void TickHoldAdvance(float dt)
    {
        if (_adapter == null || _currentResolvedTarget.InteractionTarget == null)
        {
            return;
        }

        if (!_adapter.IsHoldingResolvedTarget(_currentResolvedTarget))
        {
            _holdProgressSeconds = 0f;
            return;
        }

        _holdProgressSeconds += Mathf.Max(0f, dt);
        if (_holdProgressSeconds < ResolveAdvanceDuration(_currentStep))
        {
            return;
        }

        Log($"Hold completed for step '{ResolveStepLabel(_currentStep)}'. Advancing to next step.");
        NextStep();
    }

    private void ResetStepAdvanceState()
    {
        _advanceTimerRemaining = 0f;
        _holdProgressSeconds = 0f;
    }

    private static float ResolveAdvanceDuration(OnboardingConfigSO.StepData step)
    {
        if (step == null)
        {
            return 0f;
        }

        return Mathf.Max(0f, step.AdvanceDurationSeconds);
    }

    private static string ResolveStepLabel(OnboardingConfigSO.StepData step)
    {
        if (step == null)
        {
            return "Unknown";
        }

        if (!string.IsNullOrWhiteSpace(step.Title))
        {
            return step.Title;
        }

        if (!string.IsNullOrWhiteSpace(step.Description))
        {
            return step.Description;
        }

        return "Unnamed Step";
    }
}

public static class OnboardingRuntimeSettings
{
    private const string EnabledPlayerPrefsKey = "cheat.onboarding.enabled";

    public static event Action<bool> EnabledChanged;

    public static bool IsEnabled
    {
        get => PlayerPrefs.GetInt(EnabledPlayerPrefsKey, 1) != 0;
        set
        {
            bool normalizedValue = value;
            if (normalizedValue == IsEnabled)
            {
                return;
            }

            PlayerPrefs.SetInt(EnabledPlayerPrefsKey, normalizedValue ? 1 : 0);
            PlayerPrefs.Save();
            EnabledChanged?.Invoke(normalizedValue);
        }
    }
}
