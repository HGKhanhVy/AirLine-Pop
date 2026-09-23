using ASTeams.Base.Data;
using ASTeams.Base.UI;
using ASTeams.SingleLine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Skins screen (GDD 9.1 Theme Shop): one look at a time, paged through with the arrows,
/// bought with coins and worn straight away. Only shows and forwards; owning and paying
/// belong to the service.
/// </summary>
public class UiSkins : Uibase
{
    [SerializeField] private Button closeButton;
    [SerializeField] private SkinCardView card;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Tooltip("One dot per skin, in catalogue order.")]
    [SerializeField] private Image[] dots;

    [SerializeField] private Color dotOnColor = new Color32(46, 204, 140, 255);
    [SerializeField] private Color dotOffColor = new Color32(78, 90, 102, 255);
    [SerializeField] private TMP_Text coinLabel;

    private ISkinService skins;
    private UserProfileController profile;
    private int index;

    public void Initialize(ISkinService skinService, UserProfileController userProfile)
    {
        skins = skinService;
        profile = userProfile;
    }

    public override void Show()
    {
        base.Show();

        // Open on the look the player is wearing, not on the first in the list.
        index = IndexOfWorn();
        Refresh();
    }

    private void OnEnable()
    {
        closeButton.onClick.AddListener(Hide);
        previousButton.onClick.AddListener(ShowPrevious);
        nextButton.onClick.AddListener(ShowNext);
        card.OnClicked += HandleCardClicked;
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(Hide);
        previousButton.onClick.RemoveListener(ShowPrevious);
        nextButton.onClick.RemoveListener(ShowNext);
        card.OnClicked -= HandleCardClicked;
    }

    private int Count => skins == null ? 0 : skins.Skins.Count;

    private int IndexOfWorn()
    {
        for (int i = 0; i < Count; i++)
        {
            if (skins.Skins[i] == skins.Equipped)
            {
                return i;
            }
        }

        return 0;
    }

    private void ShowPrevious()
    {
        Step(-1);
    }

    private void ShowNext()
    {
        Step(1);
    }

    /// <summary>Wraps around, so the arrows never dead-end.</summary>
    private void Step(int direction)
    {
        if (Count == 0)
        {
            return;
        }

        index = (index + direction + Count) % Count;
        Refresh();
    }

    private void Refresh()
    {
        if (Count > 0)
        {
            card.SetSkin(skins.Skins[Mathf.Clamp(index, 0, Count - 1)]);
            card.Refresh(skins);
        }

        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].color = i == index ? dotOnColor : dotOffColor;
        }

        if (coinLabel != null && profile != null && profile.userData != null)
        {
            coinLabel.text = profile.userData.coin.ToString("N0");
        }
    }

    private void HandleCardClicked(SkinCardView card)
    {
        if (skins == null || card.Skin == null)
        {
            return;
        }

        if (skins.IsOwned(card.Skin))
        {
            skins.Equip(card.Skin);
            Refresh();
            return;
        }

        if (!skins.TryBuy(card.Skin))
        {
            UIMessageController.Instance?.ShowNoti("Not enough coins.", "top");
            return;
        }

        // Bought skins go on at once: nobody buys a look to leave it in the drawer.
        skins.Equip(card.Skin);
        UIMessageController.Instance?.ShowNoti(card.Skin.DisplayName + " equipped.", "top");
        Refresh();
    }
}
