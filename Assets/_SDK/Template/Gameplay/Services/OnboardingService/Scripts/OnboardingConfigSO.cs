using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OnboardingConfig", menuName = "Game/Onboarding/Config")]
public class OnboardingConfigSO : ScriptableObject
{
    public enum AdvanceMode
    {
        Tap = 0,
        Hold = 1,
        Waiting = 2,
        Highlight = 3,
    }

    public enum TargetSource
    {
        None = 0,
        RuntimeTargetId = 1,
        WorldPosition = 2,
    }

    public enum FocusShape
    {
        Circle = 0,
        Rectangle = 1,
    }

    [Serializable]
    public class StepData
    {
        [SerializeField] private string title = "Onboarding Step";
        [SerializeField, TextArea(2, 5)] private string description = string.Empty;
        [SerializeField] private AdvanceMode advanceMode = AdvanceMode.Tap;
        [SerializeField] private TargetSource targetSource = TargetSource.RuntimeTargetId;
        [SerializeField] private string runtimeTargetId = string.Empty;
        [SerializeField] private Vector3 worldPosition = Vector3.zero;
        [SerializeField] private Vector3 worldOffsetPosition = Vector3.zero;
        [SerializeField] private Vector3 focusWorldSize = Vector3.one;
        [SerializeField] private FocusShape shape = FocusShape.Circle;
        [SerializeField, Min(0f)] private float delayBeforeShowing;
        [SerializeField, Min(0f)] private float advanceDurationSeconds = 1f;

        public string Title => title;
        public string Description => description;
        public AdvanceMode Advance => advanceMode;
        public TargetSource Target => targetSource;
        public string RuntimeTargetId => runtimeTargetId;
        public Vector3 WorldPosition => worldPosition;
        public Vector3 WorldOffsetPosition => worldOffsetPosition;
        public Vector3 WorldFocusSize => focusWorldSize;
        public FocusShape Shape => shape;
        public float DelayBeforeShowing => Mathf.Max(0f, delayBeforeShowing);
        public float AdvanceDurationSeconds => Mathf.Max(0f, advanceDurationSeconds);
    }

    [Serializable]
    public class LevelOnboarding
    {
        [SerializeField] private int levelIndex;
        [SerializeField] private List<StepData> steps = new List<StepData>();

        public int LevelIndex => Mathf.Max(0, levelIndex);
        public IReadOnlyList<StepData> Steps => steps;
        public int StepCount => steps != null ? steps.Count : 0;

        public bool TryGetStep(int stepIndex, out StepData step)
        {
            if (steps == null || stepIndex < 0 || stepIndex >= steps.Count)
            {
                step = null;
                return false;
            }

            step = steps[stepIndex];
            return step != null;
        }
    }

    [SerializeField] private List<LevelOnboarding> levels = new List<LevelOnboarding>();

    public IReadOnlyList<LevelOnboarding> Levels => levels;

    public bool TryGetLevel(int levelIndex, out LevelOnboarding level)
    {
        if (levels != null)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                LevelOnboarding candidate = levels[i];
                if (candidate == null || candidate.LevelIndex != Mathf.Max(0, levelIndex))
                {
                    continue;
                }

                level = candidate;
                return true;
            }
        }

        level = null;
        return false;
    }
}
