using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Sổ Tay Greenie (codex) screen (M9, K6), toggled with <b>C</b>. Three tabs:
/// <b>Hồ Sơ Quái</b> (bestiary), <b>Mẩu Nhật Ký</b> (lore notes) and <b>Độ Sạch</b>
/// (per-stage cleanliness). Built at runtime (see <see cref="UIFactory"/>) and
/// refreshed from <see cref="Codex.OnChanged"/> / <see cref="Codex.OnCleanlinessChanged"/>.
/// Drop the component on any GameObject under a Canvas.
/// </summary>
public class CodexUI : MonoBehaviour
{
    enum Tab { Bestiary, Lore, Cleanliness }

    GameObject panel;
    RectTransform content;
    readonly Button[] tabButtons = new Button[3];
    Tab tab = Tab.Bestiary;
    bool isOpen;

    void Awake()
    {
        UIFactory.EnsureCanvas(this, sortingOrder: 92);
        Build();
        SetOpen(false);
    }

    void OnEnable() { Codex.OnChanged += Render; Codex.OnCleanlinessChanged += OnClean; }
    void OnDisable() { Codex.OnChanged -= Render; Codex.OnCleanlinessChanged -= OnClean; }
    void OnClean(int stage, float pct) { if (isOpen && tab == Tab.Cleanliness) Render(); }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.cKey.wasPressedThisFrame) SetOpen(!isOpen);
    }

    void SetOpen(bool open)
    {
        isOpen = open;
        if (panel != null) panel.SetActive(open);
        if (open) Render();
    }

    void Build()
    {
        panel = new GameObject("CodexPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        panel.GetComponent<Image>().color = UIFactory.PanelColor;
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(960f, 720f);

        var title = UIFactory.Text("Title", panel.transform, "SỔ TAY GREENIE", 40f);
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -20f); trt.sizeDelta = new Vector2(0f, 52f);

        // Tab bar.
        var bar = UIFactory.Rect("Tabs", panel.transform);
        bar.anchorMin = new Vector2(0f, 1f); bar.anchorMax = new Vector2(1f, 1f); bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -78f); bar.offsetMin = new Vector2(24f, bar.offsetMin.y); bar.offsetMax = new Vector2(-24f, bar.offsetMax.y);
        bar.sizeDelta = new Vector2(-48f, 52f);
        var hlg = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true; hlg.childControlHeight = true;

        string[] names = { "Hồ Sơ Quái", "Mẩu Nhật Ký", "Độ Sạch" };
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            var b = new GameObject("Tab" + i, typeof(RectTransform), typeof(Image), typeof(Button));
            b.transform.SetParent(bar, false);
            b.GetComponent<Image>().color = UIFactory.SlotColor;
            var label = UIFactory.Text("Label", b.transform, names[i], 24f);
            UIFactory.Fill(label.rectTransform);
            var btn = b.GetComponent<Button>();
            btn.onClick.AddListener(() => { tab = (Tab)idx; Render(); });
            tabButtons[i] = btn;
        }

        // Content area.
        content = UIFactory.Rect("Content", panel.transform);
        content.anchorMin = new Vector2(0f, 0f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(28f, 28f); content.offsetMax = new Vector2(-28f, -140f);
        var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12f; vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
    }

    void Render()
    {
        if (!isOpen || content == null) return;
        for (int i = 0; i < 3; i++)
            tabButtons[i].image.color = ((int)tab == i) ? UIFactory.AccentColor * 0.8f : UIFactory.SlotColor;
        for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);

        switch (tab)
        {
            case Tab.Bestiary: RenderBestiary(); break;
            case Tab.Lore: RenderLore(); break;
            default: RenderCleanliness(); break;
        }
    }

    void RenderBestiary()
    {
        foreach (var e in BestiaryCatalog.All)
        {
            bool seen = Codex.HasSeen(e.id);
            Card(seen ? e.name : "? ? ?", seen ? e.description : "Chưa chạm trán.", seen);
        }
    }

    void RenderLore()
    {
        Heading($"Đã tìm: {Codex.LoreNoteCount}/{LoreNoteCatalog.Count}");
        foreach (var n in LoreNoteCatalog.All)
        {
            bool found = Codex.HasLoreNote(n.id);
            Card(found ? n.title : "? ? ?", found ? n.body : "Chưa tìm thấy mẩu nhật ký này.", found);
        }
    }

    void RenderCleanliness()
    {
        Bar("Thung Lũng", Codex.GetCleanliness(Codex.StageValley));
        Bar("Nhà Máy", Codex.GetCleanliness(Codex.StageFactory));
        var hint = UIFactory.Text("Hint", content, "Dọn rác để tăng Độ Sạch. Đạt 50% / 100% nhận thưởng!", 20f, TextAlignmentOptions.Left);
        hint.color = new Color(1f, 1f, 1f, 0.6f);
    }

    // --- Row builders -------------------------------------------------------

    void Heading(string text)
    {
        var t = UIFactory.Text("Heading", content, text, 24f, TextAlignmentOptions.Left);
        t.color = UIFactory.AccentColor;
    }

    void Card(string title, string body, bool unlocked)
    {
        var row = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        row.transform.SetParent(content, false);
        row.GetComponent<Image>().color = new Color(1f, 1f, 1f, unlocked ? 0.06f : 0.02f);
        var vlg = row.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 8, 8); vlg.spacing = 3f;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;

        var h = UIFactory.Text("T", row.transform, title, 26f, TextAlignmentOptions.Left);
        h.fontStyle = FontStyles.Bold;
        h.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        var b = UIFactory.Text("B", row.transform, body, 21f, TextAlignmentOptions.Left);
        b.color = new Color(0.9f, 0.9f, 0.9f, unlocked ? 1f : 0.5f);
    }

    void Bar(string label, float pct)
    {
        var row = new GameObject("Bar", typeof(RectTransform), typeof(LayoutElement));
        row.transform.SetParent(content, false);
        row.GetComponent<LayoutElement>().minHeight = 56f;

        var name = UIFactory.Text("Name", row.transform, label, 24f, TextAlignmentOptions.Left);
        var nrt = name.rectTransform;
        nrt.anchorMin = new Vector2(0f, 0f); nrt.anchorMax = new Vector2(0f, 1f); nrt.pivot = new Vector2(0f, 0.5f);
        nrt.anchoredPosition = new Vector2(6f, 0f); nrt.sizeDelta = new Vector2(220f, 0f);

        var bg = UIFactory.Image("BarBG", row.transform, UIFactory.SlotColor);
        var brt = bg.rectTransform;
        brt.anchorMin = new Vector2(0f, 0.5f); brt.anchorMax = new Vector2(1f, 0.5f); brt.pivot = new Vector2(0f, 0.5f);
        brt.anchoredPosition = new Vector2(240f, 0f); brt.sizeDelta = new Vector2(-280f, 30f);

        var fill = UIFactory.Image("Fill", bg.transform, UIFactory.AccentColor);
        var frt = fill.rectTransform;
        frt.anchorMin = new Vector2(0f, 0f); frt.anchorMax = new Vector2(Mathf.Clamp01(pct / 100f), 1f);
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;

        var val = UIFactory.Text("Val", bg.transform, Mathf.RoundToInt(pct) + "%", 20f);
        UIFactory.Fill(val.rectTransform);
        val.fontStyle = FontStyles.Bold;
    }
}
