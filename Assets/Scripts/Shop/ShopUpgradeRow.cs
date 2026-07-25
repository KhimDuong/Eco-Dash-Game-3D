using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One upgrade line in Mr. Bear's shop: shows the upgrade name, current tier and
/// next-tier cost, and a Buy button. Buying routes through
/// <see cref="PlayerProgress.TryBuy"/>; the resulting OnChanged event re-refreshes
/// the whole shop, so this only has to render its own state.
/// </summary>
public class ShopUpgradeRow : MonoBehaviour
{
    [SerializeField] PlayerProgress.Upgrade upgrade;
    [SerializeField] string displayName = "Nâng cấp";
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text tierText;
    [SerializeField] TMP_Text costText;
    [SerializeField] Button buyButton;

    ShopController shop;

    void Awake()
    {
        shop = GetComponentInParent<ShopController>();
        if (nameText != null) nameText.text = displayName;
        if (buyButton != null) buyButton.onClick.AddListener(OnBuy);
    }

    void OnBuy()
    {
        if (PlayerProgress.TryBuy(upgrade) && shop != null) shop.PlayBuyFeedback();
    }

    public void Refresh()
    {
        int lvl = PlayerProgress.GetLevel(upgrade);
        if (tierText != null) tierText.text = $"Cấp {lvl}/{PlayerProgress.MaxTier}";

        bool maxed = PlayerProgress.IsMaxed(upgrade);
        if (costText != null) costText.text = maxed ? "TỐI ĐA" : $"{PlayerProgress.CostFor(upgrade)} rác";
        if (buyButton != null) buyButton.interactable = !maxed && PlayerProgress.CanAfford(upgrade);
    }
}
