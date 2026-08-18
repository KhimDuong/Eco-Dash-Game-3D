using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The 1–4 quick-use hotbar (M9). A fixed strip of four slots along the bottom of
/// the screen that auto-fills with the consumables currently in the bag (first four
/// distinct ids, in <see cref="Inventory"/> order) — no manual assignment or drag
/// needed. Pressing <b>1</b>–<b>4</b> uses that slot's consumable via
/// <see cref="ItemUse"/>. Built at runtime (see <see cref="UIFactory"/>) and driven
/// by <see cref="Inventory.OnChanged"/>; drop the component on any GameObject.
/// </summary>
public class Hotbar : MonoBehaviour
{
    const int Slots = 4;

    [SerializeField] float cellSize = 88f;
    [SerializeField] float spacing = 12f;

    readonly Image[] slotIcons = new Image[Slots];
    readonly TextMeshProUGUI[] slotLetters = new TextMeshProUGUI[Slots];
    readonly TextMeshProUGUI[] slotCounts = new TextMeshProUGUI[Slots];
    readonly string[] slotIds = new string[Slots];

    void Awake()
    {
        UIFactory.EnsureCanvas(this, sortingOrder: 80);
        Build();
    }

    void OnEnable() { Inventory.OnChanged += Refresh; Refresh(); }
    void OnDisable() => Inventory.OnChanged -= Refresh;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) UseSlot(0);
        else if (kb.digit2Key.wasPressedThisFrame) UseSlot(1);
        else if (kb.digit3Key.wasPressedThisFrame) UseSlot(2);
        else if (kb.digit4Key.wasPressedThisFrame) UseSlot(3);
    }

    void UseSlot(int i)
    {
        if (i < 0 || i >= Slots || string.IsNullOrEmpty(slotIds[i])) return;
        ItemUse.Use(slotIds[i]);
    }

    // --- Build (runtime uGUI) ----------------------------------------------

    void Build()
    {
        var root = UIFactory.Rect("HotbarRoot", transform);
        root.anchorMin = new Vector2(0.5f, 0f); root.anchorMax = new Vector2(0.5f, 0f); root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 24f);
        root.sizeDelta = new Vector2(Slots * cellSize + (Slots - 1) * spacing, cellSize);
        var hlg = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing; hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
        // childControl* defaults to TRUE, which would size each slot to its PREFERRED
        // size — and a sprite-less Image prefers 0, so the explicit cellSize below was
        // being scrubbed every layout pass and the slots rendered 0x0 (QA E1).
        hlg.childControlWidth = false; hlg.childControlHeight = false;

        for (int i = 0; i < Slots; i++)
        {
            var slot = new GameObject("Slot" + (i + 1), typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(root, false);
            slot.GetComponent<Image>().color = UIFactory.SlotEmptyColor;
            ((RectTransform)slot.transform).sizeDelta = new Vector2(cellSize, cellSize);

            var num = UIFactory.Text("Num", slot.transform, (i + 1).ToString(), 22f, TextAlignmentOptions.TopLeft);
            UIFactory.Fill(num.rectTransform);
            num.rectTransform.offsetMin = new Vector2(6, num.rectTransform.offsetMin.y);
            num.color = UIFactory.AccentColor;

            var icon = UIFactory.Image("Icon", slot.transform, Color.white);
            UIFactory.Fill(icon.rectTransform);
            icon.rectTransform.offsetMin = new Vector2(10, 10);
            icon.rectTransform.offsetMax = new Vector2(-10, -10);
            icon.enabled = false;
            slotIcons[i] = icon;

            var letter = UIFactory.Text("Letter", icon.transform, "", 34f);
            UIFactory.Fill(letter.rectTransform);
            slotLetters[i] = letter;

            var count = UIFactory.Text("Count", slot.transform, "", 22f, TextAlignmentOptions.BottomRight);
            UIFactory.Fill(count.rectTransform);
            count.rectTransform.offsetMax = new Vector2(-6, count.rectTransform.offsetMax.y);
            count.fontStyle = FontStyles.Bold;
            slotCounts[i] = count;
        }
    }

    // --- Refresh ------------------------------------------------------------

    void Refresh()
    {
        if (slotIcons[0] == null) return;

        // First four distinct consumable ids currently in the bag.
        var ids = new List<string>(Slots);
        var seen = new HashSet<string>();
        foreach (var stack in Inventory.Stacks)
        {
            if (ids.Count >= Slots) break;
            var def = ItemDatabase.Get(stack.id);
            if (def == null || def.category != ItemCategory.Consumable) continue;
            if (seen.Add(stack.id)) ids.Add(stack.id);
        }

        for (int i = 0; i < Slots; i++)
        {
            if (i < ids.Count)
            {
                string id = ids[i];
                var def = ItemDatabase.Get(id);
                slotIds[i] = id;
                slotIcons[i].enabled = true;
                if (def != null && def.icon != null)
                {
                    slotIcons[i].sprite = def.icon;
                    slotIcons[i].color = Color.white;
                    slotLetters[i].text = "";
                }
                else
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].color = UIFactory.AccentColor * 0.7f;
                    string name = def != null ? def.displayName : id;
                    slotLetters[i].text = name.Length > 0 ? name.Substring(0, 1) : "?";
                }
                slotCounts[i].text = Inventory.Count(id).ToString();
            }
            else
            {
                slotIds[i] = null;
                slotIcons[i].enabled = false;
                slotLetters[i].text = "";
                slotCounts[i].text = "";
            }
        }
    }
}
