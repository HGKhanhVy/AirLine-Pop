using System.Linq;
using ASTeams.Base;
using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Wires the game's own sounds: the tune the path plays note by note, on soft bubble plips, the
    /// passenger meow and the background music, and sets each clip's import settings for
    /// mobile. The night flight's sounds are wired by the night installer with the rest of
    /// the night. Safe to rerun.
    /// </summary>
    public static class GameAudioInstaller
    {
        public const string AudioFolder = "Assets/_Game/Audio/";
        public const string StepNotesFolder = AudioFolder + "StepNotes";
        public const string MusicPath = AudioFolder + "Music/music_gameplay_loop.ogg";
        public const string RainPath = AudioFolder + "Ambience/rain_loop.ogg";
        public const string WindPath = AudioFolder + "Ambience/wind_loop.ogg";
        public const string ThunderPath = AudioFolder + "Ambience/thunder.ogg";
        public const string TorchPath = AudioFolder + "Sfx/flashlight_on.ogg";
        public const string MeowPath = AudioFolder + "Sfx/meow.ogg";

        private const string PrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string SoundConfigPath = "Assets/_SDK/Runtime/Audio/SoundAssetConfigs.asset";
        private const int StepVoiceCount = 4;

        // The tune the path plays, as note names: arpeggios over C, G, Am and F, four notes
        // to a chord, four times round with a different shape each time (rising, leaping,
        // falling from the top, climbing high), 64 steps before it comes round again. Each
        // level starts a round further on.
        private static readonly string[] Tune =
        {
            "C5", "E5", "G5", "C6", "B4", "D5", "G5", "B5", "A4", "C5", "E5", "A5", "F4", "A4", "C5", "F5",
            "C5", "G5", "E5", "C6", "B4", "G5", "D5", "B5", "A4", "E5", "C5", "A5", "F4", "C5", "A4", "F5",
            "C6", "G5", "E5", "G5", "B5", "G5", "D5", "G5", "A5", "E5", "C5", "E5", "F5", "C5", "A4", "C5",
            "E5", "G5", "C6", "E6", "D5", "G5", "B5", "D6", "C5", "E5", "A5", "C6", "F5", "A5", "C6", "F6"
        };

        [MenuItem("Tools/AirLine Pop/Install Game Audio")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            ApplyImportSettings();
            AudioClip[] tune = LoadTune();
            var meow = AssetDatabase.LoadAssetAtPath<AudioClip>(MeowPath);
            var music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);

            if (tune.Length == 0 || meow == null || music == null)
            {
                return "Audio missing under " + AudioFolder + "; nothing installed.";
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                var presenter = contents.GetComponentInChildren<GameplayAudioPresenter>(true);

                if (presenter == null)
                {
                    return "No GameplayAudioPresenter in " + PrefabPath + ".";
                }

                var so = new SerializedObject(presenter);
                SetArray(so.FindProperty("stepTones"), tune);
                SetArray(so.FindProperty("stepVoices"), StepVoices(presenter.gameObject));

                so.FindProperty("passengerClip").objectReferenceValue = meow;
                so.ApplyModifiedPropertiesWithoutUndo();
                AddMusic(presenter.gameObject, music);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            SetGameplayMusic(music);
            return "Game audio installed: a " + tune.Length + " step tune, meow, music.";
        }

        /// <summary>The background music for a scene, on the given object; reused if it is already there.</summary>
        public static void AddMusic(GameObject owner, AudioClip music)
        {
            SceneMusicPlayer player = owner.GetComponent<SceneMusicPlayer>();

            if (player == null)
            {
                player = owner.AddComponent<SceneMusicPlayer>();
            }

            player.EditorLink(music);
        }

        /// <summary>The note clip for each step of the tune; empty if any note is missing.</summary>
        private static AudioClip[] LoadTune()
        {
            AudioClip[] clips = Tune
                .Select(name => AssetDatabase.LoadAssetAtPath<AudioClip>(StepNotesFolder + "/step_" + name + ".ogg"))
                .ToArray();
            return clips.Any(clip => clip == null) ? new AudioClip[0] : clips;
        }

        /// <summary>The sources the tune is played on, reusing any already on the object.</summary>
        private static AudioSource[] StepVoices(GameObject owner)
        {
            var voices = owner.GetComponents<AudioSource>().ToList();

            while (voices.Count < StepVoiceCount)
            {
                voices.Add(owner.AddComponent<AudioSource>());
            }

            foreach (AudioSource voice in voices)
            {
                voice.playOnAwake = false;
                voice.loop = false;
                voice.spatialBlend = 0f;
            }

            return voices.ToArray();
        }

        private static void SetArray(SerializedProperty array, Object[] values)
        {
            array.arraySize = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }

        private static void SetGameplayMusic(AudioClip music)
        {
            var config = AssetDatabase.LoadAssetAtPath<SoundAssetConfigs>(SoundConfigPath);
            SoundAsset entry = config != null ? config.GetMusic(SoundName.Gameplay_Music) : null;

            if (entry == null)
            {
                return;
            }

            entry.clip = music;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssetIfDirty(config);
        }

        /// <summary>
        /// Long loops stream from disk instead of sitting decoded in memory; the short notes
        /// and effects are decoded once on load so a fast drag never waits on one.
        /// </summary>
        private static void ApplyImportSettings()
        {
            Import(MusicPath, AudioClipLoadType.Streaming, false);
            Import(RainPath, AudioClipLoadType.CompressedInMemory, false);
            Import(WindPath, AudioClipLoadType.CompressedInMemory, false);
            Import(ThunderPath, AudioClipLoadType.CompressedInMemory, false);
            Import(TorchPath, AudioClipLoadType.DecompressOnLoad, true);
            Import(MeowPath, AudioClipLoadType.DecompressOnLoad, true);

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { StepNotesFolder }))
            {
                Import(AssetDatabase.GUIDToAssetPath(guid), AudioClipLoadType.DecompressOnLoad, true);
            }
        }

        private static void Import(string path, AudioClipLoadType loadType, bool isMono)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;

            if (importer == null)
            {
                return;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;

            if (settings.loadType == loadType && importer.forceToMono == isMono)
            {
                return;
            }

            settings.loadType = loadType;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = isMono;
            importer.SaveAndReimport();
        }
    }
}
