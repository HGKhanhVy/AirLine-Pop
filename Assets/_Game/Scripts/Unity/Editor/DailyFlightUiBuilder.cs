using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// The daily flight's button on the airport, on the right above the boarding pass, in the
    /// gap between the ticket and the baggage train. <see cref="HomeSceneBuilder"/> builds it
    /// with the rest of Home; <see cref="Install"/> adds it to the Home scene already built.
    /// </summary>
    public static class DailyFlightUiBuilder
    {
        private const string HolderName = "DailyFlight";
        private const string AirportPanelName = "AirportPanel";
        private static readonly Color RewardCoral = new Color32(226, 104, 82, 255);

        [MenuItem("Tools/AirLine Pop/Install Daily Flight Button")]
        public static void InstallFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Debug.Log(Install());
        }

        public static string Install()
        {
            LocalizationInstaller.Install();
            Scene scene = EditorSceneManager.OpenScene(HomeSceneBuilder.ScenePath, OpenSceneMode.Single);
            RectTransform panel = FindPanel(scene);
            HomeBootstrap bootstrap = FindBootstrap(scene);

            if (panel == null || bootstrap == null)
            {
                return "Home scene has no airport panel or bootstrap; build Home first.";
            }

            bootstrap.EditorLinkDailyFlight(Build(panel));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Daily flight button installed on Home.";
        }

        public static DailyFlightPresenter Build(RectTransform airportPanel)
        {
            Transform old = airportPanel.Find(HolderName);

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            Button button = UiBuilder.Button(HolderName, airportPanel, "btn_cream", new Vector2(340f, 150f));
            UiBuilder.Place((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(-40f, 496f), new Vector2(340f, 150f));

            Image icon = UiBuilder.Rect("Icon", button.transform).gameObject.AddComponent<Image>();
            icon.sprite = SpriteImport.Import(FlightBonusUiBuilder.DailyArt, 128f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            UiBuilder.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(20f, 8f), new Vector2(104f, 104f));

            TMP_Text status = UiBuilder.Label("Status", button.transform, "Daily flight", 30f, false);
            status.enableAutoSizing = true;
            status.fontSizeMin = 20f;
            status.fontSizeMax = 30f;
            UiBuilder.Place(status.rectTransform, new Vector2(1f, 0.5f), new Vector2(-14f, 30f), new Vector2(196f, 50f));

            TMP_Text reward = UiBuilder.Label("Reward", button.transform, "+100", 60f, false);
            reward.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            reward.color = RewardCoral;
            UiBuilder.Place(reward.rectTransform, new Vector2(1f, 0.5f), new Vector2(-18f, -22f), new Vector2(200f, 70f));

            Image done = UiBuilder.Icon("Done", button.transform, "check", 64f);
            done.color = UiBuilder.IconBlue;
            UiBuilder.Place(done.rectTransform, new Vector2(1f, 0.5f), new Vector2(-84f, -22f), new Vector2(64f, 64f));
            done.gameObject.SetActive(false);

            DailyFlightButtonView view = button.gameObject.AddComponent<DailyFlightButtonView>();
            view.EditorLink(button, reward, status, done.gameObject);
            DailyFlightPresenter presenter = airportPanel.gameObject.GetComponent<DailyFlightPresenter>();

            if (presenter == null)
            {
                presenter = airportPanel.gameObject.AddComponent<DailyFlightPresenter>();
            }

            presenter.EditorLink(view);
            return presenter;
        }

        private static RectTransform FindPanel(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (AirportPanelView airport in root.GetComponentsInChildren<AirportPanelView>(true))
                {
                    if (airport.name == AirportPanelName)
                    {
                        return (RectTransform)airport.transform;
                    }
                }
            }

            return null;
        }

        private static HomeBootstrap FindBootstrap(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                HomeBootstrap bootstrap = root.GetComponentInChildren<HomeBootstrap>(true);

                if (bootstrap != null)
                {
                    return bootstrap;
                }
            }

            return null;
        }
    }
}
