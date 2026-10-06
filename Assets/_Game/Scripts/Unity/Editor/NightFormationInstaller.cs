using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Fits the gameplay prefab for night and formation flights: a wingman with its own
    /// mirrored line, copied from the player's line and plane so it always matches them,
    /// and the dark of a night flight with the lights cut through it and the weather it
    /// flies through. Safe to rerun; the weather config, once made, is kept as it is.
    /// </summary>
    public static class NightFormationInstaller
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string ArtFolder = "Assets/_Game/Art/Flat/";
        private const string WingmanPathName = "Wingman Path";
        private const string WingmanPlaneName = "Wingman Airplane";
        private const string NightName = "Night";

        private const string ShadeMaterialPath = "Assets/_Game/Art/Flat/NightShade.mat";
        private const string ShadeShaderName = "AirLinePop/Night Shade";
        private const string BoltMaterialPath = "Assets/_Game/Art/Flat/NightBolt.mat";
        private const string BoltShaderName = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        private const string WeatherConfigPath = "Assets/_Game/Config/NightWeather.asset";
        private const int BoltPoints = 12;

        // The first night flights only have mist while the dark is still new; rain and storms come later.
        private const int FirstRainLevel = 87;
        private const int FirstStormLevel = 127;

        [MenuItem("Tools/AirLine Pop/Install Night And Formation")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            Sprite solid = SpriteImport.Import(ArtFolder + "solid_white.png", 100f);
            Sprite guideArt = SpriteImport.Import(ArtFolder + "night_guide.png", 128f);
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                var controller = contents.GetComponentInChildren<GameplayController>(true);
                var board = contents.GetComponentInChildren<BoardView>(true);
                PathView path = FindOwn<PathView>(contents, WingmanPathName);
                AirplaneView airplane = FindOwn<AirplaneView>(contents, WingmanPlaneName);

                if (controller == null || board == null || path == null || airplane == null)
                {
                    return "Controller, board, path or airplane missing in " + PrefabPath + "; night and formation not installed.";
                }

                PathView wingmanPath = Copy(path, WingmanPathName);
                SetBool(wingmanPath, "isMirrored", true);
                RemoveHint(wingmanPath.transform);

                AirplaneView wingman = Copy(airplane, WingmanPlaneName);
                SetObject(wingman, "path", wingmanPath);
                SetBool(wingman, "isWingman", true);

                SpriteRenderer[] bodies = { PlaneBody(airplane), PlaneBody(wingman) };
                NightFlightView night = BuildNight(board, bodies, solid, ShadeMaterial(), guideArt);
                SetObject(controller, "wingmanPath", wingmanPath);
                SetObject(controller, "nightView", night);

                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                return "Night and formation installed.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>The player's own copy of a component, skipping a wingman copy left by an earlier run.</summary>
        private static T FindOwn<T>(GameObject contents, string copyName) where T : Component
        {
            foreach (T found in contents.GetComponentsInChildren<T>(true))
            {
                if (found.gameObject.name != copyName)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>A fresh copy of the object beside the original, replacing any copy from an earlier run.</summary>
        private static T Copy<T>(T original, string name) where T : Component
        {
            Transform parent = original.transform.parent;
            Transform old = parent.Find(name);

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            GameObject copy = Object.Instantiate(original.gameObject, parent);
            copy.name = name;
            return copy.GetComponent<T>();
        }

        /// <summary>The hint rides the player's line only; the copied line drops its own.</summary>
        private static void RemoveHint(Transform wingmanPath)
        {
            foreach (HintGhostView ghost in wingmanPath.GetComponentsInChildren<HintGhostView>(true))
            {
                Object.DestroyImmediate(ghost.gameObject);
            }

            Transform trail = wingmanPath.Find("Hint Trail");

            if (trail != null)
            {
                Object.DestroyImmediate(trail.gameObject);
            }
        }

        /// <summary>The sprite the plane is drawn with, which turns with its heading.</summary>
        private static SpriteRenderer PlaneBody(AirplaneView airplane)
        {
            var rig = new SerializedObject(airplane.GetComponent<SpriteAirplaneRig>());
            return (SpriteRenderer)rig.FindProperty("bodyRenderer").objectReferenceValue;
        }

        /// <summary>The material the dark is drawn with, made from the night shade shader.</summary>
        private static Material ShadeMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ShadeMaterialPath);

            if (material == null)
            {
                material = new Material(Shader.Find(ShadeShaderName));
                AssetDatabase.CreateAsset(material, ShadeMaterialPath);
            }

            return material;
        }

        private static NightFlightView BuildNight(BoardView board, SpriteRenderer[] bodies, Sprite solid, Material shadeMaterial, Sprite guideArt)
        {
            Transform parent = board.transform;
            Transform old = parent.Find(NightName);

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var root = new GameObject(NightName);
            root.transform.SetParent(parent, false);

            SpriteRenderer shade = Sprite(root.transform, "Shade", solid, BoardSortingOrder.NightShade);
            shade.sharedMaterial = shadeMaterial;

            // Far bigger than any view, so the dark reaches every edge of the screen.
            shade.transform.localScale = new Vector3(4000f, 4000f, 1f);
            shade.enabled = false;

            SpriteRenderer guide = Sprite(root.transform, "Guide", guideArt, BoardSortingOrder.HintGuide);
            NightFlightView view = root.AddComponent<NightFlightView>();
            view.EditorLink(board, shade, bodies, guide);
            view.EditorLinkWeather(BuildWeather(root.transform, view));
            return view;
        }

        private static NightWeatherView BuildWeather(Transform parent, NightFlightView night)
        {
            var root = new GameObject("Weather");
            root.transform.SetParent(parent, false);

            var lightningRoot = new GameObject("Lightning");
            lightningRoot.transform.SetParent(root.transform, false);
            NightLightningView lightning = lightningRoot.AddComponent<NightLightningView>();
            lightning.EditorLink(Bolt(lightningRoot));

            NightWeatherView weather = root.AddComponent<NightWeatherView>();
            weather.EditorLink(WeatherConfig(), lightning);
            BuildAudio(root.transform, night, weather, lightning);
            return weather;
        }

        /// <summary>The night's sounds: a source each for the rain, the wind and the thunder.</summary>
        private static void BuildAudio(Transform parent, NightFlightView night, NightWeatherView weather, NightLightningView lightning)
        {
            var root = new GameObject("Audio");
            root.transform.SetParent(parent, false);

            AudioSource rain = Source(root, GameAudioInstaller.RainPath, true);
            AudioSource wind = Source(root, GameAudioInstaller.WindPath, true);
            AudioSource thunder = Source(root, GameAudioInstaller.ThunderPath, false);
            var torch = AssetDatabase.LoadAssetAtPath<AudioClip>(GameAudioInstaller.TorchPath);

            NightAudio audio = root.AddComponent<NightAudio>();
            audio.EditorLink(night, weather, lightning, rain, wind, thunder, torch);
        }

        private static AudioSource Source(GameObject owner, string clipPath, bool isLoop)
        {
            AudioSource source = owner.AddComponent<AudioSource>();
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            source.loop = isLoop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private static LineRenderer Bolt(GameObject owner)
        {
            LineRenderer bolt = owner.AddComponent<LineRenderer>();
            bolt.sharedMaterial = BoltMaterial();
            bolt.useWorldSpace = true;
            bolt.positionCount = BoltPoints;
            bolt.numCornerVertices = 2;
            bolt.numCapVertices = 2;
            bolt.sortingOrder = BoardSortingOrder.NightWeather;
            bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bolt.receiveShadows = false;
            bolt.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.35f));

            var colours = new Gradient();
            colours.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.75f, 0.85f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 1f) });
            bolt.colorGradient = colours;
            bolt.enabled = false;
            return bolt;
        }

        private static Material BoltMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(BoltMaterialPath);

            if (material == null)
            {
                material = new Material(Shader.Find(BoltShaderName));
                AssetDatabase.CreateAsset(material, BoltMaterialPath);
            }

            return material;
        }

        /// <summary>The weather config, made with a first set of weathers if there is none yet.</summary>
        private static NightWeatherConfigSO WeatherConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<NightWeatherConfigSO>(WeatherConfigPath);

            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<NightWeatherConfigSO>();
            config.EditorSetPresets(new[]
            {
                Weather("Night mist", 1, 0.55f, 0f, 0f),
                Weather("Fog bank", 1, 0.85f, 0f, 0f),
                Weather("Rain", FirstRainLevel, 0.25f, 0.8f, 0f),
                Weather("Thunderstorm", FirstStormLevel, 0.2f, 1f, 6f)
            });
            AssetDatabase.CreateAsset(config, WeatherConfigPath);
            return config;
        }

        private static NightWeatherPreset Weather(string name, int fromLevel, float fog, float rain, float lightningEvery)
        {
            return new NightWeatherPreset
            {
                name = name,
                fromLevel = fromLevel,
                fog = fog,
                rain = rain,
                lightningEvery = lightningEvery
            };
        }

        private static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, int order)
        {
            var piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static void SetObject(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string property, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
