using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AirlinePop.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// The gameplay HUD's pieces for what a landing earns on top: the notice that bills a
    /// storm, and the bonus line on the win card. Called by <see cref="GameplayHudBuilder"/>.
    /// </summary>
    public static class FlightBonusUiBuilder
    {
        public const string StormArt = "Assets/_Game/Art/Flat/icon_storm.png";
        public const string DailyArt = "Assets/_Game/Art/Flat/icon_daily.png";

        private const string EconomyPath = "Assets/_Game/Config/EconomyConfig.asset";

        /// <summary>The notice that slides down under the top bar, below the passenger count.</summary>
        public static HudNoticePresenter BuildNotice(RectTransform safe)
        {
            Image pill = UiBuilder.Image("HudNotice", safe, "pill_cream", true);
            UiBuilder.Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(760f, 110f));
            CanvasGroup group = UiBuilder.Group(pill.rectTransform);
            group.blocksRaycasts = false;
            group.interactable = false;

            Image icon = ArtImage("Icon", pill.transform, StormArt);
            UiBuilder.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, 6f), new Vector2(92f, 92f));

            TMP_Text label = UiBuilder.Label("Text", pill.transform, "Storm ahead!", 44f, false);
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            label.enableAutoSizing = true;
            label.fontSizeMin = 30f;
            label.fontSizeMax = 44f;
            UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(46f, 4f), new Vector2(620f, 90f));

            HudNoticeView view = pill.gameObject.AddComponent<HudNoticeView>();
            view.EditorLink(group, pill.rectTransform, icon, label);
            HudNoticePresenter presenter = safe.gameObject.AddComponent<HudNoticePresenter>();
            presenter.EditorLink(view, SpriteImport.Import(StormArt, 128f));
            return presenter;
        }

        /// <summary>The line under the coins naming what was paid on top.</summary>
        public static void BuildWinBonus(RectTransform card, WinPanelView view)
        {
            TMP_Text bonus = UiBuilder.Label("BonusNote", card, string.Empty, 36f, false);
            bonus.enableAutoSizing = true;
            bonus.fontSizeMin = 26f;
            bonus.fontSizeMax = 36f;
            bonus.color = new Color32(214, 96, 70, 255);
            UiBuilder.Place(bonus.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -506f), new Vector2(780f, 100f));
            bonus.gameObject.SetActive(false);
            view.EditorLinkBonus(bonus);
        }

        /// <summary>Pays the bonuses; lives on the HUD root so it runs whatever the board is doing.</summary>
        public static void AddPayer(GameObject root)
        {
            root.AddComponent<WinBonusPayer>().EditorLink(AssetDatabase.LoadAssetAtPath<EconomyConfigSO>(EconomyPath));
        }

        /// <summary>Links the daily flight runner into the gameplay prefab's bootstrap.</summary>
        public static void InstallDailyRunner(GameObject gameplayPrefab, EconomyConfigSO economy)
        {
            foreach (LevelBootstrap bootstrap in gameplayPrefab.GetComponentsInChildren<LevelBootstrap>(true))
            {
                DailyFlightRunner runner = bootstrap.GetComponent<DailyFlightRunner>();

                if (runner == null)
                {
                    runner = bootstrap.gameObject.AddComponent<DailyFlightRunner>();
                }

                runner.EditorLink(economy);
                var data = new SerializedObject(bootstrap);
                data.FindProperty("dailyFlight").objectReferenceValue = runner;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Image ArtImage(string name, Transform parent, string path)
        {
            RectTransform rect = UiBuilder.Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = SpriteImport.Import(path, 128f);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }
    }
}
