using AirlinePop.Template;
using UnityEngine;
using AirlinePop.Base;
namespace AirlinePop.Template
{
    [CreateAssetMenu(fileName = "RewardSpriteListSO", menuName = "AirlinePop/Reward Sprite List SO")]
    public class RWSpriteListSO : ScriptableObject
    {
        public RewardSpriteSO[] rewardSpriteSOs;
    }
}

