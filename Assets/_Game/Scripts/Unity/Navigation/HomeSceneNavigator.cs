using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Takes the player from the home scene into gameplay.
    ///
    /// The home scene owns nothing about the board, so this only names the scene to open
    /// and states that the player has already asked to play.
    /// </summary>
    public sealed class HomeSceneNavigator : MonoBehaviour
    {
        [SerializeField] private Button playButton;

        [Tooltip("Scene to open on Play. Must be listed in Build Settings.")]
        [SerializeField] private string gameplaySceneName = "SingleLine";

        private void OnEnable()
        {
            playButton.onClick.AddListener(HandlePlay);
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(HandlePlay);
        }

        private void HandlePlay()
        {
            // A second tap during the load would queue a second scene load.
            playButton.interactable = false;

            GameplayEntry.RequestImmediateStart();
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}
