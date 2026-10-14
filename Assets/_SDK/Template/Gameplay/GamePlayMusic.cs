using AirlinePop.Base;
using UnityEngine;

namespace AirlinePop.Template
{
    public class GamePlayMusic : MonoBehaviour
    {
        [SerializeField] private SoundName musicName = SoundName.Gameplay_Music;

        private void Start()
        {
            AudioController.Instance.PlayMusic(musicName);
        }
    }
}
