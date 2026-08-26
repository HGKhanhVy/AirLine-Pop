using System;
using System.Collections;
using ASTeams.Base;
using ASTeams.Base.Data;
using ASTeams.Base.UI;
using ASTeams.Base.UI.Template;
using ASTeams.Base.Gameplay;
using TMPro;
using UnityEngine;
using DG.Tweening;

namespace ASTeams.Template
{
    public class UIGameWinPopup : UIBasePopup
    {
        [SerializeField] private TextMeshProUGUI levelTxt;
        [SerializeField] private TextMeshProUGUI coinTxt;
        [SerializeField] private UIBaseButton continueBtn;

        [Header("Coin Collect FX")]
        [Tooltip("Spawner coin bay lên header (đã set headerCoinTarget trong spawner).")]
        [SerializeField] private UICoinHeader coinHeader;

        [Tooltip("Điểm spawn coin trên popup. Nếu null -> dùng coinTxt.")]
        [SerializeField] private RectTransform coinFlyStart;

        [SerializeField] private float afterCollectDelay = 0.15f;

        private int coinReward;
        private LevelService _levelService;
        private GameResultHandleService _resultHandleService;
        private bool _isFinalizing;
        private bool _isPlayingCoinCollect;

        public Action OnNext;

        protected override void Awake()
        {
            base.Awake();
            if (GameController.Instance == null)
            {
                return;
            }

            GameController.Instance.Services.TryGet(out _levelService);
            GameController.Instance.Services.TryGet(out _resultHandleService);
        }

        private void OnEnable()
        {
            if (continueBtn != null)
            {
                continueBtn.onClick.AddListener(OnClickContinue);
            }
        }

        private void OnDisable()
        {
            if (continueBtn != null)
            {
                continueBtn.onClick.RemoveListener(OnClickContinue);
            }

            _isFinalizing = false;
            _isPlayingCoinCollect = false;
        }

        public override void Show()
        {
            int level1Based = _levelService != null
                ? _levelService.CurrentLevelNumber
                : (UserProfileController.Instance != null ? UserProfileController.Instance.LEVEL : 1);

            if (levelTxt != null)
            {
                levelTxt.text = $"Level {level1Based}";
            }

            coinReward = 0;
            if (ConfigController.Instance != null && ConfigController.Instance.GameConfig != null)
            {
                coinReward = ConfigController.Instance.GameConfig.levelReward;
            }

            if (coinTxt != null)
            {
                coinTxt.text = $"+ {coinReward}";
            }

            if (continueBtn != null)
            {
                continueBtn.gameObject.SetActive(true);
            }

            _isFinalizing = false;
            _isPlayingCoinCollect = false;

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlaySound(SoundName.UI_LevelComplete);
            }

            if (VibrationController.Instance != null)
            {
                VibrationController.Instance.PlayMedium();
            }

            canvasGroup.alpha = 0;
            panel.transform.localScale = Vector3.zero;
            gameObject.SetActive(true);

            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Join(canvasGroup.DOFade(1, ANIM_DURATION).SetUpdate(true));
            seq.AppendInterval(0.5f);
            seq.AppendCallback(() =>
            {
                if (AudioController.Instance != null)
                {
                    AudioController.Instance.PlaySound(SoundName.UI_Win);
                }

                if (VibrationController.Instance != null)
                {
                    VibrationController.Instance.PlayHeavy();
                }

                panel.transform.localScale = Vector3.one * 0.25f;
            });
            seq.Append(panel.DOScale(Vector3.one, ANIM_DURATION).SetEase(Ease.OutBack).SetUpdate(true));
            seq.OnComplete(ShowCompleted);
        }

        public void RequestContinue()
        {
            OnClickContinue();
        }

        private void OnClickContinue()
        {
            if (_isFinalizing || _isPlayingCoinCollect)
            {
                return;
            }

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlaySound(SoundName.UI_ClaimReward);
            }

            if (VibrationController.Instance != null)
            {
                VibrationController.Instance.PlayLight();
            }

            if (continueBtn != null)
            {
                continueBtn.gameObject.SetActive(false);
            }

            StartCoroutine(CoPlayCoinFxThenFinalize());
        }

        private IEnumerator CoPlayCoinFxThenFinalize()
        {
            _isPlayingCoinCollect = true;

            if (coinReward > 0)
            {
                bool collectFinished = false;

                if (coinHeader != null && TryResolveCoinFlyStart(out var flyStart))
                {
                    Vector2 startInTarget = ConvertRectToTargetLocal(flyStart, coinHeader);
                    coinHeader.CollectCoinsFromUI(startInTarget, coinReward, () => collectFinished = true);
                    _resultHandleService?.MarkWinCoinsGranted();

                    while (!collectFinished)
                    {
                        yield return null;
                    }
                }
                else
                {
                    _resultHandleService?.MarkWinCoinsGranted();
                    if (UserProfileController.Instance != null)
                    {
                        UserProfileController.Instance.AddCoin(coinReward);
                    }
                }
            }

            if (afterCollectDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(afterCollectDelay);
            }

            _isPlayingCoinCollect = false;
            TryFinalizeWinFlow();
        }

        private bool TryResolveCoinFlyStart(out RectTransform flyStart)
        {
            flyStart = coinFlyStart;
            if (flyStart != null)
            {
                return true;
            }

            if (coinTxt != null)
            {
                flyStart = coinTxt.rectTransform;
                return true;
            }

            if (continueBtn != null)
            {
                flyStart = continueBtn.GetComponent<RectTransform>();
                return flyStart != null;
            }

            return false;
        }

        private void TryFinalizeWinFlow()
        {
            if (_isFinalizing)
            {
                return;
            }

            _isFinalizing = true;

            if (_resultHandleService != null)
            {
                _resultHandleService.FinalizeWinAndReload();
                return;
            }

            if (GameController.Instance != null
                && GameController.Instance.Services.TryGet(out GameResultHandleService service))
            {
                service.FinalizeWinAndReload();
                return;
            }

            OnNext?.Invoke();
        }

        private static Vector2 ConvertRectToTargetLocal(RectTransform startRect, UICoinHeader header)
        {
            if (startRect == null || header == null)
            {
                return Vector2.zero;
            }

            RectTransform targetRect = header.transform as RectTransform;
            if (targetRect == null)
            {
                return Vector2.zero;
            }

            Vector3 world = startRect.TransformPoint(startRect.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRect, screen, null, out var local);
            return local;
        }
    }
}
