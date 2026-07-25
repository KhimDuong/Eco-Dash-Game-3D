using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bàn Chế Tạo (crafting bench) screen (M9, K4), opened by interacting with a
/// <see cref="CraftingBench"/> (not a hotkey — it's a hub station). Lists every
/// recipe from <see cref="Crafting"/> with its ingredients (have/need) and a craft
/// button; locked recipes show a hint. Built at runtime (see <see cref="UIFactory"/>)
/// and refreshed from <see cref="Inventory.OnChanged"/> / <see cref="QuestLog.OnChanged"/>.
/// </summary>
public class CraftingUI : MonoBehaviour
{
    GameObject panel;
    RectTransform list;
    bool isOpen;

    public bool IsOpen => isOpen;

    void Awake()
    {
        UIFactory.EnsureCanvas(this, sortingOrder: 93);
        Build();
        SetOpen(false);
    }

    void OnEnable() { Inventory.OnChanged += RefreshIfOpen; QuestLog.OnChanged += RefreshIfOpen; }
    void OnDisable() { Inventory.OnChanged -= RefreshIfOpen; QuestLog.OnChanged -= RefreshIfOpen; }
    void RefreshIfOpen() { if (isOpen) Refresh(); }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    void SetOpen(bool open)
    {
        isOpen = open;
        if (panel != null) panel.SetActive(open);
        if (open) Refresh();
    }

    void Build()
    {
        panel = new GameObject("CraftingPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        panel.GetComponent<Image>().color = UIFactory.PanelColor;
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(900f, 720f);

        var title = UIFactory.Text("Title", panel.transform, "BÀN CHẾ TẠO", 40f);
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -22f); trt.sizeDelta = new Vector2(0f, 54f);

        // Close button (top-right).
        var close = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        close.transform.SetParent(panel.transform, false);
        close.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f, 1f);
        var crt = (RectTransform)close.transform;
        crt.anchorMin = crt.anchorMax = new Vector2(1f, 1f); crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-16f, -16f); crt.sizeDelta = new Vector2(48f, 48f);
        var clabel = UIFactory.Text("X", close.transform, "✕", 28f);
        UIFactory.Fill(clabel.rectTransform);
        close.GetComponent<Button>().onClick.AddListener(Close);

        list = UIFactory.Rect("List", panel.transform);
        list.anchorMin = new Vector2(0f, 0f); list.anchorMax = new Vector2(1f, 1f); list.pivot = new Vector2(0.5f, 1f);
        list.offsetMin = new Vector2(28f, 28f); list.offsetMax = new Vector2(-28f, -92f);
        var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f; vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
    }

    void Refresh()
    {
        if (list == null) return;
        for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
        foreach (var r in Crafting.Recipes)
            AddRecipeRow(r);
    }

    void AddRecipeRow(CraftingRecipe r)
    {
        bool unlocked = Crafting.IsUnlocked(r);
        bool canCraft = Crafting.CanCraft(r);

        var row = new GameObject("Recipe_" + r.outputId, typeof(RectTransform), typeof(Image));
        row.transform.SetParent(list, false);
        row.GetComponent<Image>().color = new Color(1f, 1f, 1f, unlocked ? 0.06f : 0.02f);
        var rrt = (RectTransform)row.transform;
        var le = row.AddComponent<LayoutElement>(); le.minHeight = 92f;

        // Output name + count.
        string outName = ItemDatabase.DisplayName(r.outputId);
        var head = UIFactory.Text("Head", row.transform, r.outputCount > 1 ? $"{outName} ×{r.outputCount}" : outName, 28f, TextAlignmentOptions.Left);
        var hrt = head.rectTransform;
        hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f); hrt.pivot = new Vector2(0f, 1f);
        hrt.anchoredPosition = new Vector2(16f, -10f); hrt.sizeDelta = new Vector2(-220f, 32f);
        head.fontStyle = FontStyles.Bold;
        head.color = unlocked ? UIFactory.AccentColor : new Color(1f, 1f, 1f, 0.5f);

        // Ingredients line (or locked hint).
        var sb = new StringBuilder();
        if (!unlocked)
        {
            sb.Append("🔒 Chưa mở khóa — hoàn thành nhiệm vụ liên quan.");
        }
        else
        {
            sb.Append("Cần: ");
            for (int i = 0; i < r.inputs.Length; i++)
            {
                var ing = r.inputs[i];
                int have = Inventory.Count(ing.id);
                sb.Append($"{ItemDatabase.DisplayName(ing.id)} {Mathf.Min(have, ing.count)}/{ing.count}");
                if (i < r.inputs.Length - 1) sb.Append(", ");
            }
        }
        var cost = UIFactory.Text("Cost", row.transform, sb.ToString(), 21f, TextAlignmentOptions.Left);
        var crt = cost.rectTransform;
        crt.anchorMin = new Vector2(0f, 0f); crt.anchorMax = new Vector2(1f, 0f); crt.pivot = new Vector2(0f, 0f);
        crt.anchoredPosition = new Vector2(16f, 12f); crt.sizeDelta = new Vector2(-220f, 32f);
        cost.color = !unlocked ? new Color(1f, 1f, 1f, 0.5f) : (canCraft ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.85f, 0.4f));

        // Craft button.
        var btnGo = new GameObject("Craft", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(row.transform, false);
        var brt = (RectTransform)btnGo.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(1f, 0.5f); brt.pivot = new Vector2(1f, 0.5f);
        brt.anchoredPosition = new Vector2(-16f, 0f); brt.sizeDelta = new Vector2(170f, 56f);
        var img = btnGo.GetComponent<Image>();
        img.color = canCraft ? UIFactory.AccentColor * 0.8f : new Color(0.3f, 0.3f, 0.3f, 1f);
        var blabel = UIFactory.Text("L", btnGo.transform, "Chế Tạo", 24f);
        UIFactory.Fill(blabel.rectTransform);
        var btn = btnGo.GetComponent<Button>();
        btn.interactable = canCraft;
        btn.onClick.AddListener(() => Crafting.TryCraft(r));
    }
}
