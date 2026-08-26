using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.Base.Gameplay
{
    public sealed class UICombo : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text starTotalText;
        [SerializeField] private Image timerFill;
        [SerializeField] private GameObject root;

        [Header("Star Fly")]
        [SerializeField] private UIStarFlyEmitter starFlyEmitter;

        private ComboService service;

        public void Bind(ComboService comboService)
        {
            if (service == comboService)
            {
                return;
            }

            Unbind();
            service = comboService;

            if (service == null)
            {
                return;
            }

            service.OnChanged += OnChanged;
            service.OnStarFly += OnStarFly;
            OnChanged(service.Combo, service.TimeLeft, 0f, service.StarTotal);
        }

        public void Unbind()
        {
            if (service == null)
            {
                return;
            }

            service.OnChanged -= OnChanged;
            service.OnStarFly -= OnStarFly;
            service = null;
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnChanged(int combo, float timeLeft, float window, int starTotal)
        {
            if (root != null)
            {
                root.SetActive(combo > 0);
            }

            if (comboText != null)
            {
                comboText.text = combo > 0 ? $"COMBO x{combo}" : string.Empty;
            }

            if (timerFill != null)
            {
                var progress = window <= 0f ? 0f : Mathf.Clamp01(timeLeft / window);
                timerFill.fillAmount = progress;
            }

            if (starTotalText != null)
            {
                starTotalText.text = starTotal.ToString();
            }
        }

        private void OnStarFly(int count, Vector3 from)
        {
            if (starFlyEmitter == null)
            {
                return;
            }

            starFlyEmitter.Emit(count, from, null);
        }
    }
}
