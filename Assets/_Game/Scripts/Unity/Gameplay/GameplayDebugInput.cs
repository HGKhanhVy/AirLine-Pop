using UnityEngine;
using UnityEngine.InputSystem;

namespace ASTeams.SingleLine.Unity
{
    public sealed class GameplayDebugInput : MonoBehaviour
    {
        [SerializeField] private LevelBootstrap bootstrap;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private InputActionMap actions;

        private void Awake()
        {
            actions = new InputActionMap("GameplayDebug");
            actions.AddAction("Undo", InputActionType.Button, "<Keyboard>/z").performed += UndoHandler;
            actions.AddAction("Restart", InputActionType.Button, "<Keyboard>/r").performed += RestartHandler;
            actions.AddAction("Next", InputActionType.Button, "<Keyboard>/n").performed += NextHandler;
            actions.AddAction("Hint", InputActionType.Button, "<Keyboard>/h").performed += HintHandlerAsync;
        }

        private void OnEnable()
        {
            actions.Enable();
        }

        private void OnDisable()
        {
            actions.Disable();
        }

        private void OnDestroy()
        {
            actions.Dispose();
        }

        private void UndoHandler(InputAction.CallbackContext context)
        {
            bootstrap.Undo();
        }

        private void RestartHandler(InputAction.CallbackContext context)
        {
            bootstrap.Restart();
        }

        private void NextHandler(InputAction.CallbackContext context)
        {
            bootstrap.LoadNext();
        }

        private async void HintHandlerAsync(InputAction.CallbackContext context)
        {
            await bootstrap.HintAsync();
        }
#endif
    }
}
