using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Rebuilds the privacy popup on the boot splash in the AirLine Pop kit: a cream card with
    /// Captain Bơ standing on its top edge, plain-words facts about the player's data, the
    /// policy link and one Agree button. The game shows no ads yet, so the personalized ads
    /// switch is left out. Replaces the template's dark panel; the popup's behaviour stays in
    /// <see cref="UiConsent"/>. Safe to rerun.
    /// </summary>
    public static class ConsentPopupBuilder
    {
        private const string ScenePath = "Assets/UXUI/Resources/UI/Splash.unity";
        private const string ConfigPath = "Assets/_Game/Config/SettingsConfig.asset";
        private const string CaptainPath = "Assets/_Game/Art/FlatCats/passenger_bo_happy.png";

        private static readonly Vector2 CardSize = new Vector2(840f, 860f);

        // How far Captain Bơ rises above the card; the fit holder makes room for him.
        private const float CaptainRise = 230f;
        private const float CaptainSize = 300f;

        private static readonly Color HintBrown = new Color32(150, 112, 96, 255);

        private static readonly (string icon, string key)[] Facts =
        {
            ("lock", "consent.local"),
            ("paw", "consent.noAccount"),
        };

        [MenuItem("Tools/AirLine Pop/Build Consent Popup")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Debug.Log(Build());
        }

        public static string Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

            try
            {
                UiConsent popup = FindPopup(scene);

                if (popup == null)
                {
                    return "No UiConsent in " + ScenePath + "; popup not built.";
                }

                BuildContent(popup);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return "Consent popup rebuilt in the AirLine Pop kit.";
            }
            finally
            {
                if (SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void BuildContent(UiConsent popup)
        {
            var root = (RectTransform)popup.transform;
            ClearChildren(root);
            UiBuilder.Stretch(root);

            CanvasGroup group = popup.GetComponent<CanvasGroup>();
            UiBuilder.Backdrop(root, UiBuilder.ModalDim);

            // Same as every kit popup: the wash fills the screen, the card keeps clear of
            // notches and shrinks to fit short screens inside a holder, so the card's own
            // spring-in scale stays free.
            RectTransform safe = UiBuilder.SafeLayer(root);
            RectTransform fit = UiBuilder.Rect("Fit", safe);
            Vector2 fitSize = CardSize + new Vector2(0f, CaptainRise);
            UiBuilder.Place(fit, new Vector2(0.5f, 0.5f), Vector2.zero, fitSize);
            safe.gameObject.AddComponent<ContentScaleFitter>().EditorLink(safe, fit, GameplayHudBuilder.FitMargin);

            Image card = UiBuilder.Image("Card", fit, "panel_cream", true);
            UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, CardSize);
            card.raycastTarget = true;

            BuildCaptain(card.rectTransform);
            BuildHeader(card.rectTransform);
            BuildFacts(card.rectTransform);
            Button policy = BuildPolicyButton(card.rectTransform);
            Button agree = BuildAgreeButton(card.rectTransform);

            SettingsConfigSO config = AssetDatabase.LoadAssetAtPath<SettingsConfigSO>(ConfigPath);
            popup.EditorLink(group, card.rectTransform, null, policy, agree, null, config);
            EditorUtility.SetDirty(popup);
        }

        /// <summary>The airline's mascot stands on the card's top edge, half out of it.</summary>
        private static void BuildCaptain(RectTransform card)
        {
            Image captain = UiBuilder.Image("Captain", card, "pill_cream", false);
            captain.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CaptainPath);
            UiBuilder.Place(captain.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, CaptainRise - 25f), new Vector2(CaptainSize, CaptainSize));
        }

        private static void BuildHeader(RectTransform card)
        {
            TMP_Text title = UiBuilder.Text("Title", card, "consent.title", 84f, false);
            title.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(760f, 110f));

            TMP_Text intro = UiBuilder.Text("Intro", card, "consent.intro", 40f, false);
            intro.color = HintBrown;
            FitLine(intro, 30f);
            UiBuilder.Place(intro.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(740f, 60f));
        }

        private static void BuildFacts(RectTransform card)
        {
            Image panel = UiBuilder.Image("Facts", card, "panel_sky", true);
            UiBuilder.Place(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(740f, 46f + Facts.Length * 88f));

            for (int i = 0; i < Facts.Length; i++)
            {
                FactRow(panel.rectTransform, Facts[i].icon, Facts[i].key, -30f - i * 88f);
            }
        }

        private static void FactRow(RectTransform panel, string icon, string key, float y)
        {
            RectTransform row = UiBuilder.Rect("Fact_" + icon, panel);
            UiBuilder.Place(row, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(660f, 80f));

            Image badge = UiBuilder.Image("Badge", row, "btn_round_cream", false);
            UiBuilder.Place(badge.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(76f, 76f));
            Image glyph = UiBuilder.Icon("Icon", badge.transform, icon, 48f);
            glyph.color = UiBuilder.IconBlue;
            glyph.rectTransform.anchoredPosition = new Vector2(0f, 3f);

            TMP_Text text = UiBuilder.Text("Label", row, key, 40f, false);
            text.alignment = TextAlignmentOptions.Left;
            FitLine(text, 28f);
            UiBuilder.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(100f, 2f), new Vector2(540f, 70f));
        }

        private static Button BuildPolicyButton(RectTransform card)
        {
            var size = new Vector2(540f, 96f);
            Button button = UiBuilder.Button("PrivacyPolicyButton", card, "btn_sky", size);
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0f, -500f), size);

            Image glyph = UiBuilder.Icon("Icon", button.transform, "globe", 56f);
            glyph.color = Color.white;
            UiBuilder.Place(glyph.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 4f), new Vector2(56f, 56f));

            TMP_Text label = UiBuilder.Text("Label", button.transform, "consent.policy", 40f, true);
            FitLine(label, 28f);
            UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(36f, 5f), new Vector2(400f, 76f));
            return button;
        }

        private static Button BuildAgreeButton(RectTransform card)
        {
            var size = new Vector2(520f, 150f);
            // Orange is the one primary action colour, the same as Home's Play.
            Button button = UiBuilder.Button("AgreeButton", card, "btn_orange", size);
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0f, 48f), size);

            TMP_Text label = UiBuilder.Text("Label", button.transform, "consent.agree", 66f, true);
            FitLine(label, 36f);
            UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), size - new Vector2(40f, 40f));
            return button;
        }

        // Vietnamese lines run longer than English; they shrink rather than spill.
        private static void FitLine(TMP_Text label, float minSize)
        {
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            label.fontSizeMin = minSize;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static UiConsent FindPopup(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                UiConsent popup = root.GetComponentInChildren<UiConsent>(true);

                if (popup != null)
                {
                    return popup;
                }
            }

            return null;
        }
    }
}
