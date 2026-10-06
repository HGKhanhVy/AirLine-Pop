using ASTeams.Base.Data;
using ASTeams.Base;
using System.Collections.Generic;
using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ASTeams.Base
{
    public class AudioController : MonoSingleton<AudioController>
    {
        [SerializeField] private SoundAssetConfigs assets;
        [SerializeField] private int maxSoundSources = 10;

        private List<AudioSource> soundSources = new List<AudioSource>();
        private Dictionary<SoundName, float> soundCooldowns = new Dictionary<SoundName, float>();
        private float soundCooldownDuration = 0.1f;

        [HideInInspector] public bool IsMuteSound;
        [HideInInspector] public bool IsMuteMusic;

        private AudioSource musicSource;
        private const string SETTING_MUTEMUSIC_KEY = "settingMuteMusic";
        private const string SETTING_MUTESOUND_KEY = "settingMuteSound";
        private float musicVolume;

        public override void Init()
        {
            base.Init();

            musicSource = CreateSource("Music Source", true);
            for (int i = 0; i < maxSoundSources; i++)
            {
                soundSources.Add(CreateSource("Sound Source " + i, false));
            }

            LoadSetting();
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            var src = obj.AddComponent<AudioSource>();
            src.loop = loop;
            return src;
        }

        private void LoadSetting()
        {
            IsMuteMusic = UserProfileController.Instance.GetParam<bool>(SETTING_MUTEMUSIC_KEY);
            IsMuteSound = UserProfileController.Instance.GetParam<bool>(SETTING_MUTESOUND_KEY);

            musicSource.volume = IsMuteMusic ? 0f : musicVolume;
            if (!IsMuteMusic && !musicSource.isPlaying) musicSource.Play();

            foreach (var src in soundSources)
                src.volume = IsMuteSound ? 0f : 1f;
        }

        public void ToggleSound()
        {
            UserProfileController.Instance.SetParam(SETTING_MUTESOUND_KEY, !IsMuteSound);
            LoadSetting();
        }

        public void ToggleMusic()
        {
            UserProfileController.Instance.SetParam(SETTING_MUTEMUSIC_KEY, !IsMuteMusic);
            LoadSetting();
        }

        public void PlayMusic(SoundName soundName)
        {
            if (assets == null)
            {
                return;
            }

            var musicAsset = assets.GetMusic(soundName);
            if (musicAsset == null || musicAsset.clip == null) return;

            if (musicSource.isPlaying)
                musicSource.Stop();

            musicSource.clip = musicAsset.clip;
            musicSource.Play();

            musicVolume = musicAsset.volume;
            musicSource.volume = IsMuteMusic ? 0f : musicVolume;
        }

        // =========================
        // Overloads: Play by AudioClip
        // =========================

        public void PlayMusic(AudioClip clip, float volume = 1f, bool restartIfSameClip = false)
        {
            if (clip == null) return;

            // Nếu đang phát đúng clip đó và không muốn restart thì thôi
            if (!restartIfSameClip && musicSource.isPlaying && musicSource.clip == clip)
            {
                musicVolume = volume;
                musicSource.volume = IsMuteMusic ? 0f : musicVolume;
                return;
            }

            if (musicSource.isPlaying) musicSource.Stop();

            musicSource.clip = clip;
            musicVolume = Mathf.Clamp01(volume);
            musicSource.volume = IsMuteMusic ? 0f : musicVolume;

            // Nếu mute thì vẫn có thể Play (volume=0) để sau unmute nghe tiếp
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource.isPlaying)
                musicSource.Stop();
        }

        public void PlaySound(SoundName soundName)
        {
            if (assets == null || soundSources.Count == 0)
            {
                return;
            }

            var soundAsset = assets.GetSound(soundName);
            if (soundAsset == null || soundAsset.clip == null) return;

            // Cooldown check
            if (soundCooldowns.TryGetValue(soundName, out float lastTime))
            {
                if (Time.time - lastTime < soundCooldownDuration) return;
            }
            soundCooldowns[soundName] = Time.time;

            // Find available AudioSource
            foreach (var source in soundSources)
            {
                if (!source.isPlaying)
                {
                    source.PlayOneShot(soundAsset.clip, soundAsset.volume);
                    return;
                }
            }

            // All busy? Play on first source (optional)
            soundSources[0].PlayOneShot(soundAsset.clip, soundAsset.volume);
        }

        public void PlaySound(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;

            float vol = Mathf.Clamp01(volume);

            // Find available AudioSource
            foreach (var source in soundSources)
            {
                if (!source.isPlaying)
                {
                    float muteScale = IsMuteSound ? 0f : 1f;
                    source.PlayOneShot(clip, vol * muteScale);
                    return;
                }
            }

            // All busy? Play on first source (optional)
            {
                float muteScale = IsMuteSound ? 0f : 1f;
                soundSources[0].PlayOneShot(clip, vol * muteScale);
            }
        }

        public async UniTaskVoid PlaySound(SoundName soundName, float delayTime)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(delayTime));
            PlaySound(soundName);
        }
    }

}
