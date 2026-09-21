using UnityEngine;
using UnityEngine.UI;
public class UiTheme : Uibase
{
    [SerializeField] private Button closeButton;

    private void Start()
    {
        closeButton.onClick.AddListener(Hide);
    }

    protected override void OnDestroy()
    {
        closeButton.onClick.RemoveListener(Hide);
        base.OnDestroy();
    }
}
