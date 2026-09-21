using System.Collections.Generic;
using System.Text;
using ASTeams.Base.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Gives every Button in the build scenes and in the game's prefabs the shared press
    /// feedback, wires its Button reference so nothing is looked up at runtime, and
    /// centres its pivot so the press shrinks towards the middle.
    ///
    /// Rerunnable: buttons that already carry the effect only get a missing reference
    /// filled in, and settings chosen by hand are left alone. New screens should be run
    /// through this once instead of being wired button by button.
    ///
    /// Skipped on purpose: buttons driven by the SDK's UIBaseButton (it animates and
    /// plays its own click), and buttons inside prefab instances, which are handled in
    /// their own prefab so scenes do not collect overrides.
    /// </summary>
    public static class ButtonEffectInstaller
    {
        private const string PrefabRoot = "Assets/_Game";

        [MenuItem("Tools/Single Line/Add Button Effects")]
        public static void InstallFromMenu()
        {
            EditorUtility.DisplayDialog("Single Line", Install(), "OK");
        }

        public static string Install()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return "Cancelled.";
            }

            var report = new StringBuilder();
            var sceneSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
                {
                    if (!buildScene.enabled)
                    {
                        continue;
                    }

                    var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                    var result = new InstallResult();

                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        Process(root, result);
                    }

                    if (result.HasChanges)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }

                    report.AppendLine(buildScene.path + ": " + result);
                }
            }
            finally
            {
                if (sceneSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                var result = new InstallResult();

                try
                {
                    Process(contents, result);

                    if (result.HasChanges)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, path);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }

                if (result.Total > 0)
                {
                    report.AppendLine(path + ": " + result);
                }
            }

            return report.ToString();
        }

        private static void Process(GameObject root, InstallResult result)
        {
            HashSet<Button> actionButtons = CollectHudButtons(root);

            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (PrefabUtility.IsPartOfPrefabInstance(button))
                {
                    continue;
                }

                result.Total++;

                if (button.GetComponent<UIBaseButton>() != null)
                {
                    result.SdkButtons++;
                    continue;
                }

                var effect = button.GetComponent<UIButtonEffect>();
                bool isNew = effect == null;

                if (isNew)
                {
                    effect = button.gameObject.AddComponent<UIButtonEffect>();
                    result.Added++;
                }

                var serialized = new SerializedObject(effect);
                SerializedProperty buttonProperty = serialized.FindProperty("button");

                if (buttonProperty.objectReferenceValue == null)
                {
                    buttonProperty.objectReferenceValue = button;
                    result.Wired++;
                }

                // Hint, Undo and Restart already announce themselves through the
                // gameplay audio; a click on top of that doubles every press.
                if (isNew && actionButtons.Contains(button))
                {
                    serialized.FindProperty("playsClickSound").boolValue = false;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();

                if (CenterPivot((RectTransform)button.transform))
                {
                    result.Centered++;
                }
            }
        }

        /// <summary>
        /// The press effect scales around the pivot, so a button pivoted on a corner
        /// shrinks into that corner. Moves the pivot to the middle without moving the
        /// button on screen.
        /// </summary>
        private static bool CenterPivot(RectTransform rect)
        {
            var center = new Vector2(0.5f, 0.5f);

            if (rect.pivot == center)
            {
                return false;
            }

            Vector2 shift = Vector2.Scale(center - rect.pivot, rect.rect.size);
            rect.pivot = center;
            rect.anchoredPosition += shift;
            return true;
        }

        private static HashSet<Button> CollectHudButtons(GameObject root)
        {
            var buttons = new HashSet<Button>();

            foreach (GameplayHud hud in root.GetComponentsInChildren<GameplayHud>(true))
            {
                SerializedProperty property = new SerializedObject(hud).GetIterator();

                while (property.NextVisible(true))
                {
                    if (property.propertyType == SerializedPropertyType.ObjectReference &&
                        property.objectReferenceValue is Button button)
                    {
                        buttons.Add(button);
                    }
                }
            }

            return buttons;
        }

        private sealed class InstallResult
        {
            public int Total;
            public int Added;
            public int Wired;
            public int SdkButtons;
            public int Centered;

            public bool HasChanges => Added > 0 || Wired > 0 || Centered > 0;

            public override string ToString()
            {
                return Total + " buttons, " + Added + " added, " + Wired + " wired, " +
                       Centered + " pivots centred, " + SdkButtons + " left to the SDK";
            }
        }
    }
}
