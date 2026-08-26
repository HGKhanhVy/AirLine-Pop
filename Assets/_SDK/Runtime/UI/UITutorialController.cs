namespace ASTeams.Base.UI
{
    public class UITutorialController : MonoSingleton<UITutorialController>
    {
#if TUTORIAL
        [SerializeField] private GameObject bg;
        [SerializeField] private GameObject hand;

        [SerializeField] private GameObject interacable;

        private GameObject temp;

        private UnityEvent tempEvent;
        private UnityAction callback;

        //public string CurrentTutorial;


        [Obsolete("ATTENTION: Not right when parrent include auto layout component")]
        public void ShowByButton(GameObject button, UnityAction cb, float bgFade=0.8f)
        {
            Show();
            bg.GetComponent<Image>().color = new Vector4(0, 0, 0, bgFade);
            temp = Instantiate(button, bg.transform);
            temp.transform.position = button.transform.position;

            tempEvent = button.GetComponent<UIBaseButton>().onClick;
            temp.GetComponent<UIBaseButton>().onClick.AddListener(InvokeTemp);

            var rectTemp = temp.GetComponent<RectTransform>();
            hand.transform.position = temp.transform.position + new Vector3(rectTemp.sizeDelta.x * 0.25f, -rectTemp.sizeDelta.y * 0.5f);

            callback = cb;
        }

        private void InvokeTemp()
        {
            tempEvent?.Invoke();
            callback?.Invoke();
            Hide();
        }

        public void ShowByBoxCollider(GameObject go, UnityAction cb, float bgFade = 0.8f)
        {
            Show();

            bg.GetComponent<Image>().color = new Vector4(0, 0, 0, bgFade);
            interacable.SetActive(true);

            var boxCollider = go.GetComponent<BoxCollider2D>();
            Vector3 viewPos = Camera.main.WorldToViewportPoint(boxCollider.transform.position);
            Vector3 screenPos = new Vector2(viewPos.x * Screen.width, viewPos.y * Screen.height);
            
            Vector3 boxColliderSize = boxCollider.bounds.size;
            Vector3 screenSize = Camera.main.WorldToScreenPoint(boxCollider.transform.position + boxColliderSize)
                                 - Camera.main.WorldToScreenPoint(boxCollider.transform.position);
            RectTransform rectTransform = interacable.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(screenSize.x, screenSize.y);

            var rectTemp = interacable.GetComponent<RectTransform>();
            hand.transform.position = screenPos + new Vector3(rectTemp.sizeDelta.x * 0.25f, -rectTemp.sizeDelta.y * 0.25f);
         
            interacable.GetComponent<RectTransform>().position = screenPos;
            interacable.GetComponent<UIBaseButton>().onClick.AddListener(InvokeTemp);

            callback = cb;
        }

        public void ShowDrag(GameObject from, GameObject to, UnityAction cb)
        {
            Show();

            Vector3 fromViewPos = Camera.main.WorldToViewportPoint(from.transform.position);
            Vector3 toViewPos = Camera.main.WorldToViewportPoint(to.transform.position);

            Vector3 fromScreenPos = new Vector3(fromViewPos.x * Screen.width, fromViewPos.y * Screen.height);
            Vector3 toScreenPos = new Vector3(toViewPos.x * Screen.width, toViewPos.y * Screen.height);

            hand.transform.position = fromScreenPos - Vector3.up * 2f;
     

            DOTween.Kill(transform.GetInstanceID());

            var seq = DOTween.Sequence();
            seq.AppendInterval(0.2f);
            seq.Append(hand.transform.DOScale(1, 0.2f)); 
            seq.Append(hand.transform.DOMove(toScreenPos - Vector3.up * 2f, 0.5f)); 
            seq.SetLoops(-1, LoopType.Restart);
            seq.SetId(transform.GetInstanceID());

            interacable.GetComponent<RectTransform>().position = fromScreenPos;
            interacable.GetComponent<UIBaseButton>().onPointerEnter.AddListener(InvokeTemp);
            interacable.SetActive(false);

            Invoke("ActiveInteracable", 1f);

            callback = cb;
        }

        private void ActiveInteracable()
        {
            interacable.SetActive(true);
        }

        private void Show()
        {
            bg.SetActive(true);
            hand.SetActive(true);
            interacable.gameObject.SetActive(true);

            DOTween.Kill(transform.GetInstanceID());

            var seq = DOTween.Sequence();
            seq.AppendInterval(0.2f);
            seq.Append(hand.transform.DOScale(1, 0.2f));
            seq.SetId(transform.GetInstanceID());
        }

        public void Hide()
        {
            bg.SetActive(false);
            interacable.SetActive(false);
            if (temp) Destroy(temp.gameObject);
            temp = null;
            tempEvent = null;

            interacable.GetComponent<UIBaseButton>().onClick.RemoveAllListeners();
            interacable.GetComponent<UIBaseButton>().onPointerEnter.RemoveAllListeners();

            DOTween.Kill(transform.GetInstanceID());

            var seq = DOTween.Sequence();
            seq.AppendInterval(0.2f);
            seq.Append(hand.transform.DOScale(0, 0.2f).OnComplete(() =>
            {
                hand.SetActive(false);
            }));
            seq.SetId(transform.GetInstanceID());
        }
#endif
    }
}

