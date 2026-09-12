# Gameplay UI Event Contract

Core gameplay publishes through `Assets/_Game/Events/GameplayEvents.asset`. UI code should reference that ScriptableObject in the Inspector, subscribe in `OnEnable`, and unsubscribe in `OnDisable`. UI must not access `PathSession`, repositories, save data, or board views.

Commands now travel on the same asset, so UI needs **no reference into gameplay at all**: a button raises a request on the channel and `LevelBootstrap` carries it out. Keeping both directions on one asset is what stops two people reassigning the same Inspector fields in the same scene.

## Events

| Event | Arguments | Meaning |
| --- | --- | --- |
| `OnLevelLoaded` | `levelNumber`, `levelId`, `difficulty`, `totalCells` | A campaign level is ready. `levelNumber` is 1-based and limited to 1–300. |
| `OnProgressChanged` | `visitedCells`, `totalCells` | The drawn path changed. Use it for progress text or bars. |
| `OnStateChanged` | `previous`, `current` | Gameplay entered `Ready`, `Drawing`, `Stuck`, or `Won`. `Stuck` is a recoverable state, not a loss. |
| `OnInvalidMove` | `cellIndex` | A drag attempted an illegal cell and the board state stayed unchanged. |
| `OnHintStarted` | none | A hint search began. Use it to show a busy state on the hint button. |
| `OnHintResolved` | `HintResult` | Hint search returned steps or requested a restart. |
| `OnLevelWon` | `levelNumber`, `levelId` | The current level completed and next-level progress was saved. |

## Commands

Raise these on the channel asset. Nothing else is needed on the UI side.

| Call | Effect |
| --- | --- |
| `RequestUndo()` | Removes one step. |
| `RequestRestart()` | Rewinds to the fixed start without reloading the scene. |
| `RequestHint()` | Asks for up to three highlighted steps. The answer arrives on `OnHintStarted` then `OnHintResolved`. |

A screen that also drives level selection or replaces the automatic advance can take a serialized reference to `LevelBootstrap` and call `TryLoadLevelNumber(int)` or `LoadNext()`. Those two are deliberately not on the channel: they change which level is being played, and that has one owner.

## Level numbering and progress

The campaign order lives in `Assets/_Game/Config/SingleLineLevelConfig.asset`, which lists all 300 levels in the order the reference packs number them. `LevelService` from the template owns the current level number and writes it to `UserProfileController.LEVEL`, so the win popup, the background and gameplay all read the same value. Rebuild the config with **Tools > Single Line > Rebuild Level Config** after re-importing levels.

Core gameplay does not open or close popups. The template `GameResultHandleService` is disabled in the Gameplay scene because it reloads the scene after a win, which conflicts with the GDD.
