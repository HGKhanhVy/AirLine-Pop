#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

[InitializeOnLoad]
public static class SceneToolbarSwitcher
{
    static string[] scenePaths;
    static string[] sceneNames;
    static int currentIndex;

    static SceneToolbarSwitcher()
    {
        ToolbarExtender.RightToolbarGUI.Add(Draw);
        LoadScenes();
        UpdateCurrentSceneIndex();
    }

    static void LoadScenes()
    {
        scenePaths = AssetDatabase
            .FindAssets("t:Scene")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.StartsWith("Assets/"))   // chỉ scene trong project
            .OrderBy(p => p)
            .ToArray();

        sceneNames = scenePaths
            .Select(p => System.IO.Path.GetFileNameWithoutExtension(p))
            .ToArray();
    }

    static void UpdateCurrentSceneIndex()
    {
        string currentScene = SceneManager.GetActiveScene().path;

        for (int i = 0; i < scenePaths.Length; i++)
        {
            if (scenePaths[i] == currentScene)
            {
                currentIndex = i;
                return;
            }
        }
    }

    static void Draw()
    {
        if (sceneNames == null || sceneNames.Length == 0)
            return;

        GUILayout.Space(15);

        int newIndex = EditorGUILayout.Popup(currentIndex, sceneNames, GUILayout.Width(140));

        if (newIndex != currentIndex)
        {
            currentIndex = newIndex;
            OpenScene(scenePaths[currentIndex]);
        }
    }

    static void OpenScene(string path)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(path);
    }
}

#endif