using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AirlinePop.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Puts the cat loading screen on the boot splash, so the first thing a player sees is
    /// the game's own art, and links the boot flow to the consent popup above it. Safe to
    /// rerun.
    /// </summary>
    public static class SplashScreenBuilder
    {
        private const string ScenePath = "Assets/_Game/Scenes/Splash.unity";
        private const string EconomyPath = "Assets/_Game/Config/EconomyConfig.asset";
        private const string CanvasName = "Splash";
        private const string ScreenName = "Cat Loading Screen";

        [MenuItem("Tools/AirLine Pop/Build Splash Screen")]
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
                Transform canvas = FindRoot(scene, CanvasName);

                if (canvas == null)
                {
                    return "No '" + CanvasName + "' canvas in " + ScenePath + "; splash not built.";
                }

                ClearEarlierBuild(canvas);
                RectTransform screen = UiBuilder.Stretch(UiBuilder.Rect(ScreenName, canvas));
                screen.SetAsFirstSibling();
                LoadingAnimation[] parts = LoadingScreenBuilder.BuildContent(screen);
                screen.gameObject.AddComponent<SplashLoadingScreen>().EditorLink(parts);
                LinkBoot(scene);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                return "Splash now shows the cat loading screen.";
            }
            finally
            {
                if (SceneManager.sceneCount > 1)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ClearEarlierBuild(Transform canvas)
        {
            for (int i = canvas.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.GetChild(i);

                if (child.name == ScreenName)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void LinkBoot(Scene scene)
        {
            SplashBoot boot = null;
            ConsentPopup consent = null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                boot = boot != null ? boot : root.GetComponentInChildren<SplashBoot>(true);
                consent = consent != null ? consent : root.GetComponentInChildren<ConsentPopup>(true);
            }

            if (boot == null)
            {
                return;
            }

            boot.EditorLink(consent, AssetDatabase.LoadAssetAtPath<EconomyConfigSO>(EconomyPath));
            EditorUtility.SetDirty(boot);
        }

        private static Transform FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root.transform;
                }
            }

            return null;
        }
    }
}
