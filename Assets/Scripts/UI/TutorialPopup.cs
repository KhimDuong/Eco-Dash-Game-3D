using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Bảng Hướng Dẫn Chơi — the multi-page how-to-play tutorial popup (mirrors
/// <c>HOW_TO_PLAY.md</c>, the source of truth). A modal, page-switching overlay
/// built entirely at runtime (see <see cref="UIFactory"/>, like the bag/codex UI)
/// so it works by dropping this one component on any GameObject — no prefab wiring.
///
/// <para>It <b>auto-opens the first time the player enters a fresh game</b> (gated
/// by <see cref="SeenKey"/> in PlayerPrefs, which <see cref="SaveSystem.ResetNewGame"/>
/// clears so every "Chơi Mới" re-arms it) and can be re-opened <b>anytime with H</b>.
/// While open the game pauses (Time.timeScale = 0); <see cref="PauseController"/>
/// checks <see cref="IsOpen"/> so Esc closes the tutorial instead of double-pausing.</para>
/// </summary>
public class TutorialPopup : MonoBehaviour
{
    /// <summary>PlayerPrefs flag: set once the tutorial has auto-shown this playthrough.</summary>
    public const string SeenKey = "EcoDash_SeenTutorial";

    /// <summary>True while the popup is showing — read by <see cref="PauseController"/>.</summary>
    public static bool IsOpen { get; private set; }

    [Tooltip("Show automatically on the first fresh run. Off = H-key only.")]
    [SerializeField] bool autoShowFirstTime = true;

    // --- Page content (Vietnamese — matches HOW_TO_PLAY.md) -------------------

    readonly (string heading, string body)[] pages =
    {
        ("Chào mừng!",
         "Bạn là <b>Greenie</b> — chú robot dọn dẹp môi trường.\n\n" +
         "• Nhiệm vụ: giải cứu thung lũng khỏi tập đoàn hóa chất <b>Khói Đen</b>.\n" +
         "• Dọn rác, hạ quái ô nhiễm, thu thập <b>Lõi Năng Lượng</b> và cứu dân làng.\n" +
         "• Nhấn nút <b>Tiếp theo</b> để xem hướng dẫn. Mở lại bất cứ lúc nào bằng phím <b>H</b>."),

        ("Di chuyển & Chiến đấu",
         "• <b>W A S D</b> — di chuyển 4 hướng.\n" +
         "• <b>Click chuột trái</b> (Góc nhìn 1st) / <b>J</b> (Góc nhìn ¾) — bắn hạt mầm.\n" +
         "• <b>E</b> — tương tác: mở rương, nói chuyện, dùng cổng, bàn chế tạo.\n" +
         // No arrow glyph here: LiberationSans' fallback atlas has no U+2194, so a nice-looking
         // "top-down <-> first person" renders as a tofu box in the one screen that teaches the key.
         "• <b>P</b> — đổi góc nhìn: trên xuống hoặc góc nhìn thứ nhất. Ở góc nhìn " +
         "thứ nhất, di <b>chuột</b> để nhìn quanh; hạt mầm vẫn bay ngang mặt đất.\n" +
         "• <b>Esc</b> — tạm dừng trò chơi."),

        ("Túi đồ & Bảng thông tin",
         "• <b>1 – 4</b> — dùng nhanh vật phẩm ở thanh dưới màn hình.\n" +
         "• <b>I</b> hoặc <b>Tab</b> — mở Túi Đồ (vật phẩm & nguyên liệu).\n" +
         "• <b>Q</b> — Nhật Ký Nhiệm Vụ.\n" +
         "• <b>C</b> — Sổ Tay Greenie (hồ sơ quái · nhật ký · độ sạch).\n" +
         "• <b>H</b> — mở lại bảng hướng dẫn này."),

        ("Màn 1 — Nông Trại Hoang Hóa",
         "• Tìm <b>3 Lõi Năng Lượng</b> trong các rương gỗ (đến gần, nhấn <b>E</b>).\n" +
         "• Coi chừng <b>Quái Rác Nhựa</b> và <b>Slime Chúa</b> (mini-boss ở góc bản đồ).\n" +
         "• Dọn rác để nhận <b>nguyên liệu</b> và tăng <b>Độ Sạch Thung Lũng</b>.\n" +
         "• Đủ 3 lõi → <b>cổng phía Bắc</b> mở → về <b>Trạm Trung Tâm</b>."),

        ("Nhiệm vụ & Dân làng",
         "• Tìm <b>Ông Sáu</b> nhận nhiệm vụ: gom <b>3 Lá Thuốc</b>, mang về đổi <b>Thuốc Giải</b>.\n" +
         "• Ở Màn 2, dùng Thuốc Giải để <b>cứu bé Tí</b> — nhận Thẻ Từ thứ 3.\n" +
         "• Giúp <b>Bé Mây, Ông Tài, Cô Lan</b> làm nhiệm vụ phụ để nhận thưởng.\n" +
         "• Theo dõi mọi nhiệm vụ trong <b>Nhật Ký (Q)</b>."),

        ("Trạm Trung Tâm & Cổng dịch chuyển",
         "• <b>Trạm Tái Chế Của Ông Bear</b> là trung tâm: Cửa Hàng · Bàn Chế Tạo · Sổ Tay.\n" +
         "• Dùng <b>Cổng Nexus</b> để đi tới Màn 1 hoặc Màn 2.\n" +
         "• <b>Cổng Về Trạm</b> trong mỗi màn đưa bạn quay lại (đi hai chiều).\n" +
         "• Chế tạo vật phẩm từ nguyên liệu tại <b>Bàn Chế Tạo</b> (nhấn <b>E</b>)."),

        ("Màn 2 — Nhà Máy & Boss",
         "• Thu thập <b>3 Thẻ Từ</b> để mở <b>Cửa Boss</b> (2 thẻ trong mê cung + 1 khi cứu Tí).\n" +
         "• Né <b>Fly-Bot Ô Nhiễm</b>, khí độc, tia laser và bẫy hố ga.\n" +
         "• Đánh bại boss <b>“Mega-Smog”</b> — né đạn 8 hướng và vùng khí độc.\n" +
         "• Thắng boss = <b>giải cứu thung lũng!</b> Chúc may mắn!"),
    };

    GameObject overlay;
    TextMeshProUGUI headingText;
    TextMeshProUGUI bodyText;
    TextMeshProUGUI pageLabel;
    TextMeshProUGUI backLabel;
    TextMeshProUGUI nextLabel;
    readonly System.Collections.Generic.List<Image> dots = new System.Collections.Generic.List<Image>();
    int page;

    void Awake()
    {
        UIFactory.EnsureCanvas(this, sortingOrder: 100); // above the bag/codex/quest panels
        Build();
        overlay.SetActive(false);
    }

    void Start()
    {
        if (autoShowFirstTime && PlayerPrefs.GetInt(SeenKey, 0) == 0)
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            Open();
        }
    }

    void OnDisable()
    {
        if (IsOpen) { IsOpen = false; Time.timeScale = 1f; }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (IsOpen)
        {
            if (kb.escapeKey.wasPressedThisFrame || kb.hKey.wasPressedThisFrame) { Close(); return; }
            if (kb.rightArrowKey.wasPressedThisFrame) Go(1);
            else if (kb.leftArrowKey.wasPressedThisFrame) Go(-1);
        }
        else if (kb.hKey.wasPressedThisFrame && !DialogueRunner.IsActive)
        {
            Open();
        }
    }

    // --- Open / close -------------------------------------------------------

    public void Open()
    {
        page = 0;
        overlay.SetActive(true);
        IsOpen = true;
        Time.timeScale = 0f;
        Render();
    }

    public void Close()
    {
        overlay.SetActive(false);
        IsOpen = false;
        Time.timeScale = 1f;
    }

    void Go(int delta)
    {
        int last = pages.Length - 1;
        if (page >= last && delta > 0) { Close(); return; } // "Bắt đầu chơi!" on the final page
        page = Mathf.Clamp(page + delta, 0, last);
        Render();
    }

    // --- Build (runtime uGUI) ----------------------------------------------

    void Build()
    {
        // Full-screen dark scrim that blocks clicks to the game behind it.
        overlay = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(transform, false);
        UIFactory.Fill((RectTransform)overlay.transform);
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        // Centered card.
        var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(overlay.transform, false);
        card.GetComponent<Image>().color = UIFactory.PanelColor;
        var crt = (RectTransform)card.transform;
        crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
        crt.anchoredPosition = Vector2.zero;
        crt.sizeDelta = new Vector2(1120f, 760f);

        // Accent title bar.
        var bar = UIFactory.Image("TitleBar", card.transform, UIFactory.AccentColor * 0.85f);
        var brt = bar.rectTransform;
        brt.anchorMin = new Vector2(0f, 1f); brt.anchorMax = new Vector2(1f, 1f); brt.pivot = new Vector2(0.5f, 1f);
        brt.sizeDelta = new Vector2(0f, 92f); brt.anchoredPosition = Vector2.zero;

        var title = UIFactory.Text("Title", bar.transform, "HƯỚNG DẪN CHƠI", 40f, TextAlignmentOptions.Left);
        UIFactory.Fill(title.rectTransform);
        title.rectTransform.offsetMin = new Vector2(32f, 0f);
        title.rectTransform.offsetMax = new Vector2(-180f, 0f);
        title.fontStyle = FontStyles.Bold;

        // Close ("Bỏ qua") button, top-right of the title bar.
        MakeButton(card.transform, "Bỏ qua", new Vector2(1f, 1f), new Vector2(-16f, -18f),
            new Vector2(150f, 56f), Close, out _);

        // Page sub-heading, on its own line just under the title bar.
        headingText = UIFactory.Text("Heading", card.transform, "", 30f, TextAlignmentOptions.Left);
        var hrt = headingText.rectTransform;
        hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f); hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = new Vector2(0f, -104f); hrt.sizeDelta = new Vector2(0f, 44f);
        hrt.offsetMin = new Vector2(44f, hrt.offsetMin.y); hrt.offsetMax = new Vector2(-44f, hrt.offsetMax.y);
        headingText.fontStyle = FontStyles.Bold;
        headingText.color = UIFactory.AccentColor;

        // Body text.
        bodyText = UIFactory.Text("Body", card.transform, "", 27f, TextAlignmentOptions.TopLeft);
        var body = bodyText.rectTransform;
        body.anchorMin = new Vector2(0f, 0f); body.anchorMax = new Vector2(1f, 1f); body.pivot = new Vector2(0.5f, 0.5f);
        body.offsetMin = new Vector2(44f, 130f); body.offsetMax = new Vector2(-44f, -170f);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.lineSpacing = 6f;

        // Page dots.
        var dotRow = UIFactory.Rect("Dots", card.transform);
        dotRow.anchorMin = new Vector2(0.5f, 0f); dotRow.anchorMax = new Vector2(0.5f, 0f); dotRow.pivot = new Vector2(0.5f, 0f);
        dotRow.anchoredPosition = new Vector2(0f, 92f); dotRow.sizeDelta = new Vector2(400f, 20f);
        var dlg = dotRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        dlg.spacing = 12f; dlg.childAlignment = TextAnchor.MiddleCenter;
        dlg.childForceExpandWidth = false; dlg.childForceExpandHeight = false;
        for (int i = 0; i < pages.Length; i++)
        {
            var dot = UIFactory.Image("Dot" + i, dotRow, UIFactory.SlotColor);
            dot.rectTransform.sizeDelta = new Vector2(14f, 14f);
            dots.Add(dot);
        }

        // Footer nav buttons.
        MakeButton(card.transform, "Quay lại", new Vector2(0f, 0f), new Vector2(28f, 24f),
            new Vector2(240f, 60f), () => Go(-1), out backLabel);
        MakeButton(card.transform, "Tiếp theo", new Vector2(1f, 0f), new Vector2(-28f, 24f),
            new Vector2(240f, 60f), () => Go(1), out nextLabel);

        pageLabel = UIFactory.Text("PageNum", card.transform, "", 20f);
        var plt = pageLabel.rectTransform;
        plt.anchorMin = new Vector2(0.5f, 0f); plt.anchorMax = new Vector2(0.5f, 0f); plt.pivot = new Vector2(0.5f, 0f);
        plt.anchoredPosition = new Vector2(0f, 40f); plt.sizeDelta = new Vector2(200f, 28f);
        pageLabel.color = new Color(1f, 1f, 1f, 0.55f);
    }

    void MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size,
        UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI labelText)
    {
        var go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = UIFactory.SlotColor;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        labelText = UIFactory.Text("Label", go.transform, label, 24f);
        UIFactory.Fill(labelText.rectTransform);
        labelText.fontStyle = FontStyles.Bold;

        go.GetComponent<Button>().onClick.AddListener(onClick);
    }

    // --- Render -------------------------------------------------------------

    void Render()
    {
        int last = pages.Length - 1;
        headingText.text = pages[page].heading;
        bodyText.text = pages[page].body;
        pageLabel.text = $"{page + 1} / {pages.Length}";
        nextLabel.text = page >= last ? "Bắt đầu chơi!" : "Tiếp theo";

        // Dim the Back button on the first page.
        backLabel.color = page == 0 ? new Color(1f, 1f, 1f, 0.35f) : Color.white;

        for (int i = 0; i < dots.Count; i++)
            dots[i].color = i == page ? UIFactory.AccentColor : new Color(1f, 1f, 1f, 0.25f);
    }
}
