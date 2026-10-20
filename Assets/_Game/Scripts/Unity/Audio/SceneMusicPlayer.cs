using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Starts a scene's background music. Asking for the track already playing leaves it
    /// running, so music shared by two scenes carries on across the switch.
    /// </summary>
    public sealed class SceneMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

        private void Start()
        {
            GameAudio audioController = GameAudio.Instance;

            if (audioController != null)
            {
                audioController.PlayMusic(music, volume);
            }
        }

#if UNITY_EDITOR
        public void EditorLink(AudioClip linkedMusic)
        {
            music = linkedMusic;
        }
#endif
    }
}
