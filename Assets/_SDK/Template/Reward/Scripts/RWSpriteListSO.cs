using ASTeams.Template;
using UnityEngine;
using ASTeams.Base;
namespace ASTeams.Template
{
    [CreateAssetMenu(fileName = "RewardSpriteListSO", menuName = "ASTeams/Reward Sprite List SO")]
    public class RWSpriteListSO : ScriptableObject
    {
        public RewardSpriteSO[] rewardSpriteSOs;
    }
}

