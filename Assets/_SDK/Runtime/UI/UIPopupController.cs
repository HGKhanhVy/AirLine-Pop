using ASTeams.Base;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.Base.UI
{
    public class UIPopupController : MonoSingleton<UIPopupController>
    {
        [SerializeField] private UIPopupConfig config;
        private Dictionary<string, UIBasePopup> activePopups = new Dictionary<string, UIBasePopup>();

        public T AddPopup<T>() where T : UIBasePopup
        {
            var dialogType = typeof(T).ToString();

            var prefab = config.popups.Find(x => x is T) as T;
            if (prefab == null)
            {
                Debug.LogError($"Prefab cho dialog {dialogType} chưa được cấu hình trong UIDialogConfigs!");
                return null;
            }

            var newDialog = Instantiate(prefab, transform);
            activePopups.Add(dialogType, newDialog);
            newDialog.closeBtns.ForEach(c => c.onClick.AddListener(() => HidePopup<T>()));
            return newDialog;
        }

        public T GetPopup<T>() where T : UIBasePopup
        {
            var dialogType = typeof(T).ToString();
            if (!activePopups.ContainsKey(dialogType)) AddPopup<T>();
            if (!activePopups.TryGetValue(dialogType, out var dialog)) return null;
            return dialog as T;
        }

        public T GetActivePopup<T>() where T : UIBasePopup
        {
            var dialogType = typeof(T).ToString();

            if (!activePopups.ContainsKey(dialogType)) AddPopup<T>();

            var dialog = GetPopup<T>();
            if (dialog != null)
            {
                dialog.transform.SetAsLastSibling();
                return dialog as T;
            }
            return null;
        }

        public void HidePopup<T>() where T : UIBasePopup
        {
            var dialogType = typeof(T).ToString();
            if (activePopups.TryGetValue(dialogType, out var dialog))
            {
                dialog.Hide();
            }
        }

        public bool AnyPopupActive()
        {
            foreach (var item in activePopups)
            {
                if (item.Value.gameObject.activeInHierarchy) return true;
            }
            return false;
        }

        // ✅ Kiểm tra xem dialog nào đó có đang active
        public bool IsActivePopup<T>() where T : UIBasePopup
        {
            var dialogType = typeof(T).ToString();
            return activePopups.ContainsKey(dialogType) && activePopups[dialogType].gameObject.activeInHierarchy;
        }

        public void HideAllPopups()
        {
            foreach (var dialog in activePopups.Values)
            {
                dialog.gameObject.SetActive(false);
            }
        }
    }
}
