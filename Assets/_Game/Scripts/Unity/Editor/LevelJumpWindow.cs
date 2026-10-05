using System.Collections.Generic;
using System.IO;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Data;
using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Opens any flight straight away while the Gameplay scene is playing, with the night,
    /// formation and VIP flights listed by number so each kind can be tried at once. Only
    /// the board changes: the player's saved progress is left alone.
    /// </summary>
    public sealed class LevelJumpWindow : EditorWindow
    {
        private const string LevelFolder = "Assets/_Game/Resources/Levels";
        private const int ButtonsPerRow = 6;

        private readonly List<int> night = new List<int>();
        private readonly List<int> formation = new List<int>();
        private readonly List<int> vip = new List<int>();
        private int levelNumber = 1;
        private Vector2 scroll;

        [MenuItem("Tools/AirLine Pop/Level Jump")]
        public static void Open()
        {
            GetWindow<LevelJumpWindow>("Level Jump").ReadCampaign();
        }

        private void OnEnable()
        {
            ReadCampaign();
        }

        private void ReadCampaign()
        {
            night.Clear();
            formation.Clear();
            vip.Clear();
            string[] files = Directory.GetFiles(LevelFolder, "ch*.json");
            System.Array.Sort(files);
            int number = 0;

            foreach (string file in files)
            {
                foreach (LevelData level in LevelJsonSerializer.DeserializeChapter(File.ReadAllText(file)))
                {
                    number++;
                    Collect(level.IsNight, night, number);
                    Collect(level.IsFormation, formation, number);
                    Collect(level.IsVip, vip, number);
                }
            }
        }

        private static void Collect(bool matches, List<int> list, int number)
        {
            if (matches)
            {
                list.Add(number);
            }
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Open the Gameplay scene (Assets/_SDK/Template/Scenes/Gameplay.unity) and press Play, then pick a flight.",
                    MessageType.Info);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                levelNumber = EditorGUILayout.IntSlider("Flight", levelNumber, 1, CampaignLevelAddress.MaxLevelNumber);

                if (GUILayout.Button("Go", GUILayout.Width(50f)))
                {
                    Jump(levelNumber);
                }
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            Section("Night flights", night);
            Section("Formation flights", formation);
            Section("VIP flights", vip);
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Show rule cards again"))
            {
                ForgetRuleCards();
            }
        }

        private void Section(string title, List<int> numbers)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(title + " (" + numbers.Count + ")", EditorStyles.boldLabel);

            for (int row = 0; row < numbers.Count; row += ButtonsPerRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int i = row; i < Mathf.Min(row + ButtonsPerRow, numbers.Count); i++)
                    {
                        if (GUILayout.Button(numbers[i].ToString()))
                        {
                            levelNumber = numbers[i];
                            Jump(numbers[i]);
                        }
                    }
                }
            }
        }

        private static void Jump(int number)
        {
            LevelBootstrap bootstrap = EditorApplication.isPlaying ? FindFirstObjectByType<LevelBootstrap>() : null;

            if (bootstrap == null)
            {
                Debug.LogWarning("Level Jump: play the Gameplay scene first.");
                return;
            }

            bootstrap.TryLoadLevelNumber(number);
        }

        /// <summary>Lets the cards explaining night, formation and VIP flights show again, to see them once more.</summary>
        private static void ForgetRuleCards()
        {
            var profile = ASTeams.Base.Data.UserProfileController.Instance;

            if (profile == null)
            {
                Debug.LogWarning("Level Jump: the rule cards are remembered in the profile, which only loads in Play mode.");
                return;
            }

            foreach (LevelRule rule in new[] { LevelRule.Runway, LevelRule.Wind, LevelRule.Night, LevelRule.Formation, LevelRule.Vip })
            {
                profile.SetParam("rule_seen_" + rule, false);
            }
        }
    }
}
