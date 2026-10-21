using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The one place music and one-shot sounds play from, so the player's two switches
    /// apply to everything. Lives on the persistent app root; music therefore carries on
    /// across a scene change when the next scene asks for the same track.
    /// </summary>
    public sealed class GameAudio : MonoBehaviour
    {
        private const string MusicOffKey = "music_off";
        private const string SoundOffKey = "sound_off";

        [SerializeField, Min(1)] private int soundVoices = 8;

        [Tooltip("The click every button makes when it is pressed.")]
        [SerializeField] private UiSoundConfigSO uiSounds;

        private AudioSource musicSource;
        private AudioSource[] voices;
        private float musicVolume = 1f;

        public static GameAudio Instance { get; private set; }

        public bool IsMusicOn { get; private set; } = true;

        public bool IsSoundOn { get; private set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            musicSource = CreateSource("Music", true);
            voices = new AudioSource[soundVoices];

            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = CreateSource("Voice " + i, false);
            }

            PlayerProfile profile = PlayerProfile.Instance;
            IsMusicOn = !profile.Get<bool>(MusicOffKey);
            IsSoundOn = !profile.Get<bool>(SoundOffKey);
            ApplyMusicVolume();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetMusicOn(bool isOn)
        {
            IsMusicOn = isOn;
            PlayerProfile.Instance.Set(MusicOffKey, !isOn);
            ApplyMusicVolume();
        }

        public void SetSoundOn(bool isOn)
        {
            IsSoundOn = isOn;
            PlayerProfile.Instance.Set(SoundOffKey, !isOn);
        }

        /// <summary>Asking for the track already playing only updates its volume, so shared music never restarts.</summary>
        public void PlayMusic(AudioClip clip, float volume)
        {
            if (clip == null)
            {
                return;
            }

            musicVolume = Mathf.Clamp01(volume);
            ApplyMusicVolume();

            if (musicSource.isPlaying && musicSource.clip == clip)
            {
                return;
            }

            musicSource.clip = clip;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void PlaySound(AudioClip clip, float volume)
        {
            if (clip == null || !IsSoundOn)
            {
                return;
            }

            FreeVoice().PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public void PlayButtonClick()
        {
            if (uiSounds != null)
            {
                PlaySound(uiSounds.ButtonClick, uiSounds.ButtonClickVolume);
            }
        }

        /// <summary>A voice that is not playing; when all are busy the first one takes the sound on top.</summary>
        private AudioSource FreeVoice()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying)
                {
                    return voices[i];
                }
            }

            return voices[0];
        }

        private void ApplyMusicVolume()
        {
            // Muted music keeps playing silently, so switching it back on resumes the
            // track where it is instead of starting it over.
            musicSource.volume = IsMusicOn ? musicVolume : 0f;
        }

        private AudioSource CreateSource(string sourceName, bool loops)
        {
            var holder = new GameObject(sourceName);
            holder.transform.SetParent(transform, false);
            AudioSource source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loops;
            return source;
        }

#if UNITY_EDITOR
        public void EditorLink(UiSoundConfigSO linkedUiSounds)
        {
            uiSounds = linkedUiSounds;
        }
#endif
    }
}
