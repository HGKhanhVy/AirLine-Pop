using Cysharp.Threading.Tasks;
using UnityEngine;


namespace ASTeams.Base.Gameplay.Example
{
    public class DemoAutoStartGame : MonoBehaviour
    {
        private async void Start()
        {
            await UniTask.Delay(1000);

            AudioController.Instance.PlaySound(SoundName.UI_LevelStart);
            GameController.Instance.Services.Get<GameStateService>().Play();
        }
    }
}
