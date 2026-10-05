using ASTeams.SingleLine.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the card that introduces a level rule the first time the player meets it: the
    /// rule's marker as a picture, its name, one line on how it works and a button.
    /// </summary>
    public static class RuleIntroBuilder
    {
        public const string RunwayArt = "Assets/_Game/Art/Flat/rule_runway.png";
        public const string WindArt = "Assets/_Game/Art/Flat/rule_wind.png";
        public const string NightArt = "Assets/_Game/Art/Flat/rule_night.png";
        public const string FormationArt = "Assets/_Game/Art/Flat/rule_formation.png";
        public const string VipArt = "Assets/_Game/Art/Flat/rule_vip.png";
        private const string HolderName = "RuleIntro";

        public static RuleIntroPresenter Build(Transform canvas)
        {
            Transform old = canvas.Find(HolderName);

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            // The view lives on an always-active holder: the popup itself is switched off while closed.
            RectTransform holder = UiBuilder.Stretch(UiBuilder.Rect(HolderName, canvas));
            RectTransform card = GameplayHudBuilder.Card("RuleIntroPanel", holder, new Vector2(820f, 900f), out ModalPanel modal);

            Image icon = UiBuilder.Image("Icon", card, "pill_cream", false);
            UiBuilder.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(260f, 260f));

            TMP_Text title = UiBuilder.Label("Title", card, "Runway", 80f, false);
            title.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -330f), new Vector2(700f, 110f));

            TMP_Text body = UiBuilder.Label("Body", card, string.Empty, 46f, false);
            body.enableWordWrapping = true;
            body.enableAutoSizing = true;
            body.fontSizeMin = 32f;
            body.fontSizeMax = 46f;
            UiBuilder.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -450f), new Vector2(680f, 220f));

            Button gotIt = UiBuilder.Button("GotItButton", card, "btn_orange", new Vector2(520f, 150f));
            UiBuilder.Place((RectTransform)gotIt.transform, new Vector2(0.5f, 0f), new Vector2(0f, 56f), new Vector2(520f, 150f));
            TMP_Text label = UiBuilder.Text("Label", gotIt.transform, "rule.gotIt", 66f, true);
            UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(480f, 120f));

            RuleIntroView view = holder.gameObject.AddComponent<RuleIntroView>();
            view.EditorLink(modal, icon, title, body, gotIt);

            RuleIntroPresenter presenter = holder.gameObject.AddComponent<RuleIntroPresenter>();
            presenter.EditorLink(view, new[]
            {
                Entry(LevelRule.Runway, RunwayArt, "rule.runway"),
                Entry(LevelRule.Wind, WindArt, "rule.wind"),
                Entry(LevelRule.Night, NightArt, "rule.night"),
                Entry(LevelRule.Formation, FormationArt, "rule.formation"),
                Entry(LevelRule.Vip, VipArt, "rule.vip"),
            });
            return presenter;
        }

        private static RuleIntroEntry Entry(LevelRule rule, string art, string key)
        {
            return new RuleIntroEntry
            {
                rule = rule,
                icon = SpriteImport.Import(art, 256f),
                titleKey = key + ".title",
                bodyKey = key + ".body",
            };
        }
    }
}
