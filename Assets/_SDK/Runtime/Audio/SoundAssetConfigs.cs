using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.Base
{
    public enum SoundName
    {
        Gameplay_Music = -1,

        UI_ButtonClick = 0,
        UI_PopupOpen = 1,
        UI_PopupClose = 2,
        UI_LevelStart = 3,
        UI_LevelGoalShow = 4,
        UI_Progress = 5,
        UI_CollectCoin = 6,
        UI_CollectGem = 7,
        UI_BoosterSelect = 8,
        UI_BoosterUse = 9,
        UI_Invalid = 10,
        UI_Warning = 11,
        UI_ClaimReward = 12,
        UI_RewardAds = 13,
        UI_ChestOpen = 14,
        UI_Unlock = 15,
        UI_LevelComplete = 16,
        UI_Win = 17,
        UI_Lose = 18,
        UI_Revive = 19,
        UI_Clock = 20,

        Sorting_PieceSelect = 21,
        Sorting_PiecePlace = 22,
        Sorting_BranchMove = 23,
        Sorting_PictureComplete = 24,
        Sorting_BoardShuffle = 25,
        Sorting_BombExplode = 26,
        Sorting_AlarmResolve = 27,
        Sorting_LockUnlock = 28,
        Sorting_BombTick = 29,
        Sorting_BombThrow = 30
    }

    [System.Serializable]
    public class SoundAsset
    {
        public SoundName soundName;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [CreateAssetMenu(menuName = "ASTeams/SoundAssetConfigs", fileName = "SoundAssetConfigs")]
    public class SoundAssetConfigs : ScriptableObject
    {
        public List<SoundAsset> musics;
        public List<SoundAsset> sounds;

        public SoundAsset GetSound(SoundName soundName)
        {
            return sounds.Find(x => x.soundName == soundName);
        }

        public SoundAsset GetMusic(SoundName soundName)
        {
            return musics.Find(x => x.soundName == soundName);
        }
    }
}
