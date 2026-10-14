using System;
using AirlinePop.SingleLine.Core;
using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>How the card that introduces one level rule looks: its picture and its words.</summary>
    [Serializable]
    public sealed class RuleIntroEntry
    {
        public LevelRule rule;
        public Sprite icon;
        public string titleKey;
        public string bodyKey;
    }
}
