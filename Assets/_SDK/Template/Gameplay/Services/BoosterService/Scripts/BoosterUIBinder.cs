using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ASTeams.Base.Data;

namespace ASTeams.Base.Gameplay
{
    public class BoosterUIBinder : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private List<UIBoosterButton> buttons = new();

        [Header("Guide (optional)")]
        [SerializeField] private UIBoosterGuide guideView;

        private BoosterService _service;

        private UnityAction<BoosterType?> _armedListener;
        private BoosterType? _prevArmed;

        public void Bind(BoosterService service)
        {
            if (_service == service) return;

            Unbind();

            _service = service;
            if (_service == null) return;

            if (guideView != null)
            {
                guideView.Bind(_service);
                guideView.Hide();
            }

            _service.OnStateChanged += OnServiceStateChanged;

            _armedListener ??= OnArmedChanged;
            _service.OnArmedChanged.AddListener(_armedListener);

            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] == null) continue;
                buttons[i].Bind(_service);
            }

            _prevArmed = _service.ArmedBooster;
            RefreshAll();
            SyncGuide(_service.ArmedBooster);
        }

        public void Unbind()
        {
            if (_service == null) return;

            _service.OnStateChanged -= OnServiceStateChanged;

            if (_armedListener != null)
                _service.OnArmedChanged.RemoveListener(_armedListener);

            _prevArmed = null;

            if (guideView != null)
                guideView.Hide();

            _service = null;
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnServiceStateChanged(BoosterType type)
        {
            // cooldown end / amount change / etc -> refresh all safe
            RefreshAll();
        }

        private void OnArmedChanged(BoosterType? armed)
        {
            // chỉ cần refresh all để cập nhật armed overlay
            _prevArmed = armed;
            RefreshAll();
            SyncGuide(armed);
        }

        private void SyncGuide(BoosterType? armed)
        {
            if (guideView == null || _service == null) return;

            if (!armed.HasValue)
            {
                guideView.Hide();
                return;
            }

            var cfg = _service.GetBoosterConfig(armed.Value);
            if (cfg == null || cfg.useMode != BoosterUseMode.Interactive)
            {
                guideView.Hide();
                return;
            }

            guideView.Show(cfg);
        }

        public void RefreshAll()
        {
            if (_service == null) return;

            for (int i = 0; i < buttons.Count; i++)
            {
                var b = buttons[i];
                if (b == null) continue;

                var st = _service.GetState(b.Type);

                b.Render(st, lockOthers: false);
            }
        }

        public void Refresh(BoosterType type)
        {
            RefreshAll();
        }
    }
}