using UnityEngine;
using UnityEngine.UI;

public class UiSetting : Uibase
{
    [Header("Sound")]
    [SerializeField] private Toggle soundToggle;
    [SerializeField] private GameObject soundOffBtn;
    [SerializeField] private GameObject soundOnBtn;

    [Header("Haptic")]
    [SerializeField] private Toggle hapticToggle;
    [SerializeField] private GameObject hapticOffBtn;
    [SerializeField] private GameObject hapticOnBtn;


    private void Start()
    {
        // Cập nhật giao diện ban đầu
        UpdateSoundUI(soundToggle.isOn);
        UpdateHapticUI(hapticToggle.isOn);

        //  khi Toggle thay đổi
        soundToggle.onValueChanged.AddListener(UpdateSoundUI);
        hapticToggle.onValueChanged.AddListener(UpdateHapticUI);
    }

    private void UpdateSoundUI(bool isOn)
    {
        soundOffBtn.SetActive(!isOn);
        soundOnBtn.SetActive(isOn);
    }

    private void UpdateHapticUI(bool isOn)
    {
        hapticOffBtn.SetActive(!isOn);
        hapticOnBtn.SetActive(isOn);
    }
}