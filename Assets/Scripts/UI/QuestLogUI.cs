using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Nhật Ký Nhiệm Vụ (quest log) screen (M9, K5), toggled with <b>Q</b>. Lists the
/// M8 antidote quest (read from <see cref="QuestProgress"/>) plus every side quest
/// the player has seen (from <see cref="QuestLog"/>), with giver, description,
/// counter and status. Built at runtime (see <see cref="UIFactory"/>) and refreshed
/// from <see cref="QuestLog.OnChanged"/> / <see cref="QuestProgress.OnChanged"/> —
/// never polled. Drop the component on any GameObject under a Canvas.
/// </summary>
public class QuestLogUI : MonoBehaviour
{
    GameObject panel;
    RectTransform list;
    bool isOpen;

    void Awake()
    {
        UIFactory.EnsureCanvas(this, sortingOrder: 91);
        Build();
        SetOpen(false);
    }

    void OnEnable() { QuestLog.OnChanged += Refresh; QuestProgress.OnChanged += Refresh; }
    void OnDisable() { QuestLog.OnChanged -= Refresh; QuestProgress.OnChanged -= Refresh; }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.qKey.wasPressedThisFrame) SetOpen(!isOpen);
    }

    void SetOpen(bool open)
    {
        isOpen = open;
        if (panel != null) panel.SetActive(open);
        if (open) Refresh();
    }

    void Build()
    {
        panel = new GameObject("QuestLogPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        panel.GetComponent<Image>().color = UIFactory.PanelColor;
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(900f, 720f);

        var title = UIFactory.Text("Title", panel.transform, "NHẬT KÝ NHIỆM VỤ", 40f);
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f); trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -24f); trt.sizeDelta = new Vector2(0f, 56f);

        list = UIFactory.Rect("List", panel.transform);
        list.anchorMin = new Vector2(0f, 0f); list.anchorMax = new Vector2(1f, 1f); list.pivot = new Vector2(0.5f, 1f);
        list.offsetMin = new Vector2(28f, 28f); list.offsetMax = new Vector2(-28f, -96f);
        var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f; vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
    }

    void Refresh()
    {
        if (!isOpen || list == null) return;
        for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);

        int shown = 0;

        // M8 antidote quest (still owned by QuestProgress).
        if (QuestProgress.Stage != QuestStage.NotMet)
        {
            string status = QuestProgress.Stage == QuestStage.TiSaved ? "Hoàn thành" : "Đang làm";
            string detail = QuestProgress.Stage < QuestStage.HerbsReady
                ? $"Thu thập lá thuốc ({Mathf.Min(QuestProgress.HerbCount, 3)}/3)"
                : (QuestProgress.Stage == QuestStage.TiSaved ? "Đã cứu được Tí." : "Mang thuốc giải về cứu Tí.");
            AddRow("Liều Thuốc Giải", "Ông Sáu", detail, status, QuestProgress.Stage == QuestStage.TiSaved);
            shown++;
        }

        // New side quests the player has seen.
        foreach (var id in QuestLog.KnownQuestIds())
        {
            var def = QuestCatalog.Get(id);
            if (def == null) continue;
            bool done = QuestLog.IsCompleted(id);
            string status = done ? "Hoàn thành" : "Đang làm";
            string detail = def.description;
            if (def.target > 0 && !done) detail += $"  ({Mathf.Min(QuestLog.GetProgress(id), def.target)}/{def.target})";
            AddRow(def.title, def.giver, detail, status, done);
            shown++;
        }

        if (shown == 0)
        {
            var none = UIFactory.Text("None", list, "Chưa có nhiệm vụ nào. Hãy trò chuyện với dân làng!", 24f, TextAlignmentOptions.TopLeft);
            none.color = new Color(1f, 1f, 1f, 0.6f);
        }
    }

    void AddRow(string title, string giver, string detail, string status, bool done)
    {
        var row = new GameObject("Quest_" + title, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        row.transform.SetParent(list, false);
        row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
        var vlg = row.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 10, 10); vlg.spacing = 4f;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true; vlg.childControlHeight = true;

        var head = UIFactory.Text("Head", row.transform, $"{title}  —  {giver}", 28f, TextAlignmentOptions.Left);
        head.fontStyle = FontStyles.Bold;
        head.color = done ? new Color(0.5f, 1f, 0.55f) : UIFactory.AccentColor;

        var body = UIFactory.Text("Body", row.transform, detail, 22f, TextAlignmentOptions.Left);
        body.color = new Color(0.92f, 0.92f, 0.92f);

        var st = UIFactory.Text("Status", row.transform, "Trạng thái: " + status, 20f, TextAlignmentOptions.Left);
        st.color = done ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.85f, 0.4f);
    }
}
