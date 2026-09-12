# Gameplay UI Event Contract

Core gameplay publishes through `Assets/_Game/Events/GameplayEvents.asset`. UI code should reference that ScriptableObject in the Inspector, subscribe in `OnEnable`, and unsubscribe in `OnDisable`. UI must call the public commands on `LevelBootstrap`; it must not access `PathSession`, repositories, save data, or board views.

## Events

| Event | Arguments | Meaning |
| --- | --- | --- |
| `OnLevelLoaded` | `levelNumber`, `levelId`, `difficulty`, `totalCells` | A campaign level is ready. `levelNumber` is 1-based and limited to 1–300. |
| `OnProgressChanged` | `visitedCells`, `totalCells` | The drawn path changed. Use it for progress text or bars. |
| `OnStateChanged` | `previous`, `current` | Gameplay entered `Ready`, `Drawing`, `Stuck`, or `Won`. `Stuck` is a recoverable state, not a loss. |
| `OnInvalidMove` | `cellIndex` | A drag attempted an illegal cell and the board state stayed unchanged. |
| `OnHintResolved` | `HintResult` | Hint search returned steps or requested a restart. |
| `OnLevelWon` | `levelNumber`, `levelId` | The current level completed and next-level progress was saved. |

## Commands

UI receives a serialized reference to `LevelBootstrap` and may call:

- `Restart()` to rewind to the fixed start without reloading the scene.
- `Undo()` to remove one step.
- `HintAsync()` to request up to three highlighted steps.
- `TryLoadLevelNumber(int)` for an authorized level selection.
- `LoadNext()` when a UI-controlled win flow replaces automatic advance.

Core gameplay does not open or close popups. The template `GameResultHandleService` is disabled in the Gameplay scene because it reloads the scene after a win, which conflicts with the GDD.
