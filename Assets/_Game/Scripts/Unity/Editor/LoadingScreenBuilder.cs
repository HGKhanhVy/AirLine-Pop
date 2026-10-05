using ASTeams.Base.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the loading screen onto the SDK's scene change overlay in MANAGERS: on a plain
    /// sky, Captain Bơ stands in the middle of a little track cracking jokes in a speech
    /// bubble while four cats hop and tumble after a ball of yarn round him, under a few small
    /// clouds and paper planes. Replaces the overlay's plain
    /// "Loading.." text. Safe to rerun.
    /// </summary>
    public static class LoadingScreenBuilder
    {
        private const string ManagersPath = "Assets/_SDK/Content/Prefabs/MANAGERS.prefab";
        private const string ScreenName = "Loading Screen";
        private const string MapArt = "Assets/_Game/Art/RouteMap/";
        private const string LoadingArt = "Assets/_Game/Art/Loading/";
        private const string CatMotion = "Assets/_Game/Art/FlatCats/Motion/";
        private const string YarnPath = "Assets/_Game/Art/FlatHome/yarn.png";
        private const float HoldSeconds = 1f;
        private const float CatHeight = 240f;
        private const float CaptainHeight = 300f;

        // Every cat motion frame is 278 x 357 with the feet 25.6 px up from the bottom.
        private const float FrameAspect = 278f / 357f;
        private const float FootPivot = 25.6f / 357f;

        private const string Captain = "bo";
        private const string FlatArt = "Assets/_Game/Art/Flat/";

        // The chasers and how each moves: most hop after the yarn, one tumbles along.
        private static readonly (string cat, string clip, float frameSeconds)[] ChaseCats =
        {
            ("kem", "Jump", 0.07f),
            ("mun", "Roll", 0.05f),
            ("muop", "Jump", 0.07f),
            ("tro", "Jump", 0.07f),
        };

        // Small clouds drifting up and down in the sky: position and width.
        private static readonly (float x, float y, float width)[] SkyClouds =
        {
            (-330f, 640f, 200f), (350f, 560f, 170f), (380f, -700f, 190f), (-360f, -820f, 150f),
        };

        // Small paper planes gliding about: position, size and the way each points.
        private static readonly (float x, float y, float size, float degrees)[] PaperPlanes =
        {
            (330f, 780f, 96f, -60f), (-390f, -650f, 72f, -75f),
        };

        private static readonly string[] TipKeys =
        {
            "loading.tip.wings", "loading.tip.bags", "loading.tip.belts", "loading.tip.snacks",
            "loading.tip.tower", "loading.tip.nap", "loading.tip.yarn", "loading.tip.window",
        };

        [MenuItem("Tools/AirLine Pop/Build Loading Screen")]
        public static void BuildFromMenu()
        {
            Debug.Log(Build());
        }

        public static string Build()
        {
            SpriteImport.Import(LoadingArt + "loading_track.png", 100f);
            SpriteImport.Import(LoadingArt + "loading_bubble.png", 100f);
            GameObject managers = PrefabUtility.LoadPrefabContents(ManagersPath);

            try
            {
                var controller = managers.GetComponentInChildren<UISceneController>(true);

                if (controller == null)
                {
                    return "No scene controller in MANAGERS; loading screen not built.";
                }

                var so = new SerializedObject(controller);
                var overlay = (CanvasGroup)so.FindProperty("root").objectReferenceValue;
                so.FindProperty("minHoldSeconds").floatValue = HoldSeconds;
                so.ApplyModifiedPropertiesWithoutUndo();

                ClearOverlay(overlay.transform);
                BuildScreen(overlay.transform, controller);
                PrefabUtility.SaveAsPrefabAsset(managers, ManagersPath);
                return "Loading screen built.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(managers);
            }
        }

        /// <summary>Drops an earlier build and hides the SDK's own pieces under the new screen.</summary>
        private static void ClearOverlay(Transform overlay)
        {
            for (int i = overlay.childCount - 1; i >= 0; i--)
            {
                Transform child = overlay.GetChild(i);

                if (child.name == ScreenName)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private static void BuildScreen(Transform overlay, UISceneController controller)
        {
            RectTransform screen = UiBuilder.Stretch(UiBuilder.Rect(ScreenName, overlay));
            Image sky = screen.gameObject.AddComponent<Image>();
            sky.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapArt + "map_sky.png");
            sky.raycastTarget = true;

            var parts = new System.Collections.Generic.List<LoadingAnimation>();
            BuildSky(screen, parts);
            // The sky and clouds fill the whole screen; the cats and the bubble keep clear of notches.
            RectTransform safe = UiBuilder.SafeLayer(screen);
            parts.Add(BuildChase(safe));
            BuildBubble(safe, out LoadingBob bob, out LoadingTips tips);
            parts.Add(bob);
            parts.Add(tips);

            screen.gameObject.AddComponent<LoadingScreen>().EditorLink(controller, parts.ToArray());
        }

        /// <summary>Kept light on purpose: a few small clouds bobbing and a couple of paper planes gliding.</summary>
        private static void BuildSky(RectTransform screen, System.Collections.Generic.List<LoadingAnimation> parts)
        {
            for (int i = 0; i < SkyClouds.Length; i++)
            {
                (float x, float y, float width) = SkyClouds[i];
                parts.Add(Floating("Cloud " + i, screen, MapArt + "cloud_c.png", new Vector2(x, y), new Vector2(width, width * 0.49f),
                    10f + 4f * i, 0f, 2.2f + 0.4f * i));
            }

            for (int i = 0; i < PaperPlanes.Length; i++)
            {
                (float x, float y, float size, float degrees) = PaperPlanes[i];

                // Each plane is turned inside its holder, which the bob rocks back and forth.
                LoadingBob glide = Floating("PaperPlane " + i, screen, "", new Vector2(x, y), new Vector2(size, size), 20f, 6f, 1.7f + 0.3f * i);
                Image plane = Centered("Plane", glide.transform, FlatArt + "paper_plane.png", Vector2.zero, new Vector2(size, size));
                plane.rectTransform.localRotation = Quaternion.Euler(0f, 0f, degrees);
                parts.Add(glide);
            }
        }

        /// <summary>A piece that bobs and rocks while the screen is up; an empty sprite path makes a bare holder.</summary>
        private static LoadingBob Floating(string name, RectTransform screen, string spritePath, Vector2 position, Vector2 size,
            float height, float degrees, float seconds)
        {
            RectTransform piece;

            if (spritePath.Length == 0)
            {
                piece = UiBuilder.Rect(name, screen);
                UiBuilder.Place(piece, new Vector2(0.5f, 0.5f), position, size);
                piece.pivot = new Vector2(0.5f, 0.5f);
            }
            else
            {
                piece = Centered(name, screen, spritePath, position, size).rectTransform;
            }

            LoadingBob bob = piece.gameObject.AddComponent<LoadingBob>();
            bob.EditorConfigure(piece, height, degrees, seconds);
            return bob;
        }

        /// <summary>
        /// Captain Bơ's speech bubble, its tail pointing down at him: it floats gently and
        /// says a new line every little while.
        /// </summary>
        private static void BuildBubble(RectTransform screen, out LoadingBob bob, out LoadingTips tips)
        {
            Image bubble = Centered("Bubble", screen, LoadingArt + "loading_bubble.png", new Vector2(0f, 230f), new Vector2(880f, 286f));
            TMP_Text line = UiBuilder.Label("Line", bubble.transform, LocalizationTable.English(TipKeys[0]), 50f, false);
            UiBuilder.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(780f, 170f));
            line.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            line.enableWordWrapping = true;
            line.enableAutoSizing = true;
            line.fontSizeMin = 32f;
            line.fontSizeMax = 50f;

            bob = bubble.gameObject.AddComponent<LoadingBob>();
            bob.EditorConfigure(bubble.rectTransform, 14f, 1.5f, 1.6f);
            tips = bubble.gameObject.AddComponent<LoadingTips>();
            tips.EditorLink(line, TipKeys);
        }

        /// <summary>
        /// A ball of yarn rolling round an oval with the cats chasing it, round the captain
        /// standing in the middle. Feet sit on the oval, so each cat's pivot is at its feet.
        /// </summary>
        private static LoadingCatChase BuildChase(RectTransform screen)
        {
            RectTransform ring = UiBuilder.Rect("CatChase", screen);
            UiBuilder.Place(ring, new Vector2(0.5f, 0.5f), new Vector2(0f, -260f), new Vector2(760f, 240f));
            ring.pivot = new Vector2(0.5f, 0.5f);

            // A grassy oval with a dashed lane marks the track the cats run round.
            Centered("Track", ring, LoadingArt + "loading_track.png", new Vector2(0f, 10f), new Vector2(860f, 226f));

            // The cats get their own holder: the chase reorders its children by depth.
            RectTransform pack = UiBuilder.Stretch(UiBuilder.Rect("Cats", ring));
            var runners = new RectTransform[ChaseCats.Length + 1];

            // The ball spins about its middle inside a holder that sits on the oval.
            RectTransform ball = UiBuilder.Rect("Yarn", pack);
            UiBuilder.Place(ball, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
            ball.pivot = new Vector2(0.5f, 0f);
            Image yarn = Centered("Ball", ball, YarnPath, Vector2.zero, new Vector2(96f, 96f));
            runners[0] = ball;

            var cats = new Image[ChaseCats.Length];
            var frames = new System.Collections.Generic.List<Sprite>();
            var frameCounts = new int[ChaseCats.Length];
            var frameSeconds = new float[ChaseCats.Length];

            for (int i = 0; i < ChaseCats.Length; i++)
            {
                (string name, string clip, float seconds) = ChaseCats[i];
                Sprite[] motion = MotionFrames(name, clip);
                frames.AddRange(motion);
                frameCounts[i] = motion.Length;
                frameSeconds[i] = seconds;
                cats[i] = Cat("Cat " + name, pack, motion[0], CatHeight);
                runners[i + 1] = cats[i].rectTransform;
            }

            Sprite[] idle = MotionFrames(Captain, "Idle");
            Image captain = Cat("Captain " + Captain, pack, idle[0], CaptainHeight);

            LoadingCatChase chase = ring.gameObject.AddComponent<LoadingCatChase>();
            chase.EditorLink(runners, yarn.rectTransform, cats, frames.ToArray(), frameCounts, frameSeconds, captain, idle);
            return chase;
        }

        private static Image Cat(string name, Transform parent, Sprite sprite, float height)
        {
            Image cat = UiBuilder.Image(name, parent, "pill_cream", false);
            cat.sprite = sprite;
            UiBuilder.Place(cat.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(height * FrameAspect, height));
            cat.rectTransform.pivot = new Vector2(0.5f, FootPivot);
            return cat;
        }

        private static Sprite[] MotionFrames(string cat, string clip)
        {
            var sprites = new System.Collections.Generic.List<Sprite>();

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(CatMotion + "cat_" + cat + "_" + clip + ".png"))
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return sprites.ToArray();
        }

        private static Image Centered(string name, Transform parent, string spritePath, Vector2 position, Vector2 size)
        {
            Image image = UiBuilder.Image(name, parent, "pill_cream", false);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            UiBuilder.Place(image.rectTransform, new Vector2(0.5f, 0.5f), position, size);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return image;
        }
    }
}
