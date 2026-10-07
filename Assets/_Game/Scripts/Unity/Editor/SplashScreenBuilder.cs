using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Puts the cat loading screen on the boot splash in place of the template's
    /// "Block Fill Puzzle" logo, so the first thing a player sees is the game's own art.
    /// The template pieces are only switched off, and the consent popup stays above.
    /// Safe to rerun.
    /// </summary>
    public static class SplashScreenBuilder
    {
        private const string ScenePath = "Assets/UXUI/Resources/UI/Splash.unity";
        private const string CanvasName = "Splash";
        private const string ScreenName = "Cat Loading Screen";

        // The template's logo, its own background and its block loading icon.
        private static readonly string[] TemplatePieces = { "BG", "LOGO", "LoadingIcon", "LoadingText" };

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

                HideTemplate(canvas);
                RectTransform screen = UiBuilder.Stretch(UiBuilder.Rect(ScreenName, canvas));
                screen.SetAsFirstSibling();
                LoadingAnimation[] parts = LoadingScreenBuilder.BuildContent(screen);
                screen.gameObject.AddComponent<SplashLoadingScreen>().EditorLink(parts);
                UnlinkTemplateIcon(scene);

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

        private static void HideTemplate(Transform canvas)
        {
            for (int i = canvas.childCount - 1; i >= 0; i--)
            {
                Transform child = canvas.GetChild(i);

                if (child.name == ScreenName)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
                else if (System.Array.IndexOf(TemplatePieces, child.name) >= 0)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>The loader would keep swapping frames on the hidden block icon; the cats replace it.</summary>
        private static void UnlinkTemplateIcon(Scene scene)
        {
            Transform holder = FindRoot(scene, "LoadingManager");

            if (holder == null)
            {
                return;
            }

            LoadingManager loader = holder.GetComponent<LoadingManager>();
            loader.loadingIcon = null;
            loader.loadingFrames = new Sprite[0];
            EditorUtility.SetDirty(loader);
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
