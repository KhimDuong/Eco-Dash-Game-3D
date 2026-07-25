using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Túi Đồ (bag) screen for the M9 inventory: a slot grid for materials &amp;
/// consumables plus a separate strip for key / quest items, toggled with <b>I</b> or
/// <b>Tab</b>. Built entirely at runtime (see <see cref="UIFactory"/>) and driven by
/// <see cref="Inventory.OnChanged"/> — it never polls. Clicking a consumable slot
/// uses it via <see cref="ItemUse"/>. Drag-to-rearrange is intentionally omitted
/// (a stack list has no meaningful order to curate); the 1–4 hotbar handles
/// quick-use. Drop this component on any GameObject — it finds or makes its Canvas.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] int columns = 6;
    [SerializeField] float cellSize = 96f;
    [SerializeField] float spacing = 10f;

    GameObject panel;
    RectTransform grid;
    RectTransform keyRow;
    TextMeshProUGUI emptyHint;
    bool isOpen;

    void Awake()
    {
        UIFactory.EnsureCanvas(this, sortingOrder: 90);
        Build();
        SetOpen(false);
    }

    void OnEnable() => Inventory.OnChanged += Refresh;
    void OnDisable() => Inventory.OnChanged -= Refresh;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame)
            SetOpen(!isOpen);
    }

    void SetOpen(bool open)
    {
        isOpen = open;
        if (panel != null) panel.SetActive(open);
        if (open) Refresh();
    }

    // --- Build (runtime uGUI) ----------------------------------------------

    void Build()
    {
        panel = new GameObject("BagPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        panel.GetComponent<Image>().color = UIFactory.PanelColor;
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(columns * (cellSize + spacing) + spacing + 40f, 720f);

        var title = UIFactory.Text("Title", panel.transform, "TÚI ĐỒ", 40f);
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -24f); trt.sizeDelta = new Vector2(0f, 56f);

        // Materials + consumables grid.
        grid = UIFactory.Rect("Grid", panel.transform);
        grid.anchorMin = new Vector2(0f, 1f); grid.anchorMax = new Vector2(1f, 1f); grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = new Vector2(0f, -96f);
        grid.offsetMin = new Vector2(20f, grid.offsetMin.y); grid.offsetMax = new Vector2(-20f, grid.offsetMax.y);
        grid.sizeDelta = new Vector2(-40f, 520f);
        var glg = grid.gameObject.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(cellSize, cellSize);
        glg.spacing = new Vector2(spacing, spacing);
        glg.padding = new RectOffset(4, 4, 4, 4);
        glg.childAlignment = TextAnchor.UpperLeft;

        emptyHint = UIFactory.Text("EmptyHint", grid.parent, "Túi trống — hãy dọn rác và thu thập vật phẩm!", 24f);
        var ert = emptyHint.rectTransform;
        ert.anchorMin = new Vector2(0f, 1f); ert.anchorMax = new Vector2(1f, 1f); ert.pivot = new Vector2(0.5f, 1f);
        ert.anchoredPosition = new Vector2(0f, -160f); ert.sizeDelta = new Vector2(-60f, 40f);
        emptyHint.color = new Color(1f, 1f, 1f, 0.6f);

        // Key / quest items strip along the bottom.
        var keyLabel = UIFactory.Text("KeyLabel", panel.transform, "Vật phẩm nhiệm vụ", 22f, TextAlignmentOptions.Left);
        var krt = keyLabel.rectTransform;
        krt.anchorMin = new Vector2(0f, 0f); krt.anchorMax = new Vector2(1f, 0f); krt.pivot = new Vector2(0.5f, 0f);
        krt.anchoredPosition = new Vector2(24f, 132f); krt.sizeDelta = new Vector2(-40f, 30f);
        keyLabel.color = UIFactory.AccentColor;

        keyRow = UIFactory.Rect("KeyRow", panel.transform);
        keyRow.anchorMin = new Vector2(0f, 0f); keyRow.anchorMax = new Vector2(1f, 0f); keyRow.pivot = new Vector2(0.5f, 0f);
        keyRow.anchoredPosition = new Vector2(0f, 24f);
        keyRow.sizeDelta = new Vector2(-40f, 100f);
        var hlg = keyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing; hlg.padding = new RectOffset(20, 20, 4, 4);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
    }

    // --- Refresh ------------------------------------------------------------

    void Refresh()
    {
        if (!isOpen || grid == null) return;
        ClearChildren(grid);
        ClearChildren(keyRow);

        int gridCount = 0, keyCount = 0;
        foreach (var stack in Inventory.Stacks)
        {
            var def = ItemDatabase.Get(stack.id);
            bool isKey = def != null && def.category == ItemCategory.KeyQuest;
            if (isKey) { MakeSlot(keyRow, stack, def, useable: false); keyCount++; }
            else { MakeSlot(grid, stack, def, useable: def != null && def.category == ItemCategory.Consumable); gridCount++; }
        }

        if (emptyHint != null) emptyHint.gameObject.SetActive(gridCount == 0);
        if (keyCount == 0)
        {
            var none = UIFactory.Text("None", keyRow, "—", 22f, TextAlignmentOptions.Left);
            none.color = new Color(1f, 1f, 1f, 0.4f);
        }
    }

    void MakeSlot(Transform parent, ItemStack stack, ItemDef def, bool useable)
    {
        var slot = new GameObject("Slot_" + stack.id, typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(parent, false);
        slot.GetComponent<Image>().color = UIFactory.SlotColor;
        ((RectTransform)slot.transform).sizeDelta = new Vector2(cellSize, cellSize);

        // Icon (or, until art lands, the item's initial on a tinted box).
        var icon = UIFactory.Image("Icon", slot.transform, Color.white, def != null ? def.icon : null);
        UIFactory.Fill(icon.rectTransform);
        icon.rectTransform.offsetMin = new Vector2(8, 8);
        icon.rectTransform.offsetMax = new Vector2(-8, -8);
        if (def == null || def.icon == null)
        {
            icon.color = UIFactory.AccentColor * 0.7f;
            string name = def != null ? def.displayName : stack.id;
            var letter = UIFactory.Text("Letter", icon.transform, name.Length > 0 ? name.Substring(0, 1) : "?", 40f);
            UIFactory.Fill(letter.rectTransform);
        }

        if (stack.count > 1)
        {
            var count = UIFactory.Text("Count", slot.transform, stack.count.ToString(), 24f, TextAlignmentOptions.BottomRight);
            var crt = count.rectTransform;
            UIFactory.Fill(crt);
            crt.offsetMax = new Vector2(-6, crt.offsetMax.y);
            count.fontStyle = FontStyles.Bold;
        }

        if (useable)
        {
            var btn = slot.AddComponent<Button>();
            string id = stack.id;
            btn.onClick.AddListener(() => ItemUse.Use(id));
        }
    }

    static void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
    }
}
