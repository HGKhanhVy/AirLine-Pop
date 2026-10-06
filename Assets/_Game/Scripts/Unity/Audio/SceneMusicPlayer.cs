using ASTeams.Base;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Starts a scene's background music. The track is held here rather than looked up in
    /// the SDK's sound config, so it plays even where that config is missing, such as a
    /// scene opened on its own in the editor. Asking for the track already playing leaves
    /// it running, so music shared by two scenes carries on across the switch.
    /// </summary>
    public sealed class SceneMusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioClip music;
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

        private void Start()
        {
            AudioController audioController = AudioController.Instance;

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
