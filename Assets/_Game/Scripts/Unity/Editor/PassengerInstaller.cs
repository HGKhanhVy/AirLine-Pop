using ASTeams.SingleLine.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Installs the pickup flight: waiting cats on the board and the "cats aboard" pill
    /// on the HUD. Sprites come from Tools/flat_art/import_craftpix_cats.py. Safe to rerun.
    /// </summary>
    public static class PassengerInstaller
    {
        private const string CatFolder = "Assets/_Game/Art/FlatCats";
        private const string ShadowPath = "Assets/_Game/Art/FlatHome/prop_shadow.png";
        private const string IconPath = "Assets/_Game/Art/UIKit/icon_cat.png";
        private const string LooksPath = "Assets/_Game/Config/PassengerLooks.asset";
        private const string GameplayPrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string HudPrefabPath = "Assets/_Game/UI/AirlineHud.prefab";

        private const string PassengersName = "Passengers";
        private const string CounterName = "PassengerCounter";

        private static readonly string[] Breeds = { "bo", "kem", "mun", "muop", "tro" };

        // Feet on the passenger canvas: 492 of 512 px down, as import_craftpix_cats.py places them.
        private static readonly Vector2 FeetPivot = new Vector2(0.5f, 1f - 492f / 512f);

        [MenuItem("Tools/AirLine Pop/Install Passenger Cats")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            PassengerLookSO looks = BuildLooks();
            Sprite shadow = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowPath);
            UiKitInstaller.ImportKitSprite(IconPath);

            string board = WireBoard(looks, shadow);
            string hud = WireHud();
            AssetDatabase.SaveAssets();
            return "Passenger cats installed.\n" + board + "\n" + hud;
        }

        private static PassengerLookSO BuildLooks()
        {
            var looks = AssetDatabase.LoadAssetAtPath<PassengerLookSO>(LooksPath);

            if (looks == null)
            {
                looks = ScriptableObject.CreateInstance<PassengerLookSO>();
                AssetDatabase.CreateAsset(looks, LooksPath);
            }

            var entries = new PassengerLook[Breeds.Length];

            for (int i = 0; i < Breeds.Length; i++)
            {
                entries[i] = new PassengerLook(
                    ImportPassenger(CatFolder + "/passenger_" + Breeds[i] + "_sit.png"),
                    ImportPassenger(CatFolder + "/passenger_" + Breeds[i] + "_happy.png"));
            }

            looks.EditorSetLooks(entries);
            EditorUtility.SetDirty(looks);
            return looks;
        }

        private static Sprite ImportPassenger(string path)
        {
            // One unit tall at scale one; the view sizes it to the square.
            SpriteImport.Import(path, 256f);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = FeetPivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------ board

        private static string WireBoard(PassengerLookSO looks, Sprite shadow)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);

            try
            {
                var board = contents.GetComponentInChildren<BoardView>(true);
                var gameplay = contents.GetComponentInChildren<GameplayController>(true);

                if (board == null || gameplay == null)
                {
                    return "Board or gameplay controller missing in " + GameplayPrefabPath + "; prefab left unchanged.";
                }

                Transform old = board.transform.Find(PassengersName);

                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var root = new GameObject(PassengersName).transform;
                root.SetParent(board.transform, false);

                var seats = new PassengerCatView[PassengerPlanner.MaxPassengers];

                for (int i = 0; i < seats.Length; i++)
                {
                    seats[i] = BuildSeat(root, "Seat " + (i + 1), shadow);
                }

                var view = root.gameObject.AddComponent<PassengerBoardView>();
                view.EditorLink(board, looks, seats);

                var controller = root.gameObject.AddComponent<PassengerController>();
                controller.EditorLink(gameplay, view);

                PrefabUtility.SaveAsPrefabAsset(contents, GameplayPrefabPath);
                return GameplayPrefabPath + ": " + seats.Length + " passenger seats under the board.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static PassengerCatView BuildSeat(Transform parent, string name, Sprite shadowSprite)
        {
            var seat = new GameObject(name).transform;
            seat.SetParent(parent, false);

            SpriteRenderer shadow = Renderer(seat, "Shadow", shadowSprite, BoardSortingOrder.PassengerShadow);
            shadow.color = new Color(1f, 1f, 1f, 0.8f);
            SpriteRenderer body = Renderer(seat, "Body", null, BoardSortingOrder.Passenger);

            var view = seat.gameObject.AddComponent<PassengerCatView>();
            view.EditorLink(body, shadow);
            return view;
        }

        private static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }

        // ------------------------------------------------------------------ HUD

        private static string WireHud()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(HudPrefabPath);

            try
            {
                Transform topBar = FindDeep(contents.transform, "TopBar");

                if (topBar == null)
                {
                    return "No TopBar in " + HudPrefabPath + "; HUD left unchanged.";
                }

                Transform old = topBar.Find(CounterName);

                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                BuildCounter((RectTransform)topBar);
                PrefabUtility.SaveAsPrefabAsset(contents, HudPrefabPath);
                return HudPrefabPath + ": cats-aboard pill under the level readout.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>
        /// The cats-aboard pill, hanging just under the level readout. Shared with
        /// <see cref="GameplayHudBuilder"/> so a rebuilt HUD gets the same pill.
        /// </summary>
        public static PassengerCounterView BuildCounter(RectTransform topBar)
        {
            Image pill = UiBuilder.Image(CounterName, topBar, "pill_cream", true);
            UiBuilder.Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -76f), new Vector2(210f, 84f));

            Image icon = UiBuilder.Icon("Icon", pill.transform, "cat", 66f);
            UiBuilder.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 3f), new Vector2(66f, 66f));

            TMP_Text label = UiBuilder.Label("Count", pill.transform, "0/0", 50f, false);
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(34f, 5f), new Vector2(130f, 70f));

            CanvasGroup group = UiBuilder.Group(pill.rectTransform);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            var view = pill.gameObject.AddComponent<PassengerCounterView>();
            view.EditorLink(group, label, pill.rectTransform);
            return view;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
