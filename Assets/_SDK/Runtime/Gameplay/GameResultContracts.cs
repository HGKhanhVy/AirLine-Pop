using UnityEngine;

namespace ASTeams.Base.Gameplay
{
    public struct WinPopupData
    {
        public int level;
        public int coinReward;
    }

    public struct LosePopupData
    {
        public int level;
        public FailType failType;
    }

    public struct RevivePopupData
    {
        public int level;
        public FailType failType;
        public string reviveText;
        public string reviveDscText;
        public long coinPrice;
        public string rewardedPlacement;
        public Sprite reviveSprite;
    }

    public struct GameFailPopupData
    {
        public int level;
        public FailType failType;
        public Sprite loseSprite;
        public string loseText;
        public Sprite reviveSprite;
        public string reviveText;
        public string reviveDscText;
    }
}
