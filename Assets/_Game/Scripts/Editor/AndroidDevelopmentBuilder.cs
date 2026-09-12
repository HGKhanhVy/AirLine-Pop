#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ASTeams.SingleLine.Editor
{
    public static class AndroidDevelopmentBuilder
    {
        private const string OutputPath = "Builds/SingleLine-development.apk";

        [MenuItem("Tools/Single Line/Build/Android APK")]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                throw new InvalidOperationException("Switch the active build target to Android before building.");
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string absoluteOutputPath = Path.Combine(projectRoot, OutputPath);
            string outputDirectory = Path.GetDirectoryName(absoluteOutputPath);

            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new InvalidOperationException("The Android output directory is invalid.");
            }

            Directory.CreateDirectory(outputDirectory);
            EditorUserBuildSettings.buildAppBundle = false;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = absoluteOutputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    "Android build failed with " + summary.totalErrors + " errors and " +
                    summary.totalWarnings + " warnings.");
            }

            Debug.Log(
                "Android APK built: " + absoluteOutputPath +
                " (" + summary.totalSize + " bytes)");
        }

        private static string[] GetEnabledScenes()
        {
            EditorBuildSettingsScene[] configuredScenes = EditorBuildSettings.scenes;
            var enabledScenes = new List<string>(configuredScenes.Length);

            for (int i = 0; i < configuredScenes.Length; i++)
            {
                if (configuredScenes[i].enabled)
                {
                    enabledScenes.Add(configuredScenes[i].path);
                }
            }

            if (enabledScenes.Count == 0)
            {
                throw new BuildFailedException("Build Settings contain no enabled scenes.");
            }

            return enabledScenes.ToArray();
        }
    }
}
#endif