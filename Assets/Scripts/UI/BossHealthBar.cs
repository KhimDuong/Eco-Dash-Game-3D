using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh máu Boss — the fight bar across the top of the screen. Hidden until a boss
/// engages, then tracks its health and hides again on defeat. It <b>listens</b> to
/// <see cref="IBoss"/> events and never polls.
///
/// <para><b>Tier 2, and the shape is the point.</b> The 2D bar was authored into the Level 2
/// scene with a hard <c>MegaSmogBoss</c> reference, which meant Level 1's Slime King could
/// only ever have a bar if Dev B remembered to drop a second one in and re-drag it. Here the
/// bar <b>builds itself in code</b> — the same self-contained approach as
/// <see cref="InventoryUI"/> and <see cref="Hotbar"/> — and a boss calls
/// <see cref="Bind"/> as it wakes. Nothing is wired in any scene, so a boss dropped into a
/// level brings its own HP bar with it, and both bosses share exactly one implementation.</para>
///
/// <para>The fill is driven through the RectTransform's anchor rather than
/// <c>Image.fillAmount</c>: a Filled image needs a sprite to fill, and a bar built from bare
/// <c>Image</c>s has none.</para>
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    // Above the hotbar (80), below the bag / codex / quest panels (90-93) and the
    // tutorial popup (100) — a boss fight must not draw over an open menu.
    const int SortingOrder = 85;

    static readonly Color TrackColor = new Color(0.06f, 0.07f, 0.07f, 0.92f);
    static readonly Color FullColor = new Color(0.55f, 0.9f, 0.3f, 1f);
    static readonly Color LowColor = new Color(0.95f, 0.3f, 0.25f, 1f);

    static BossHealthBar instance;

    IBoss boss;
    RectTransform bar, fill;
    TMP_Text label;

    /// <summary>
    /// Show the bar for <paramref name="boss"/>, creating it on first use. Called by a boss
    /// the moment it engages; safe to call more than once.
    /// </summary>
    public static void Bind(IBoss boss)
    {
        if (boss == null) return;

        if (instance == null)
        {
            var host = HostCanvas();
            var go = new GameObject("BossHealthBar", typeof(RectTransform));
            if (host != null) go.transform.SetParent(host.transform, false);

            instance = go.AddComponent<BossHealthBar>();
            if (host == null) UIFactory.EnsureCanvas(instance, SortingOrder);
        }
        instance.Attach(boss);
    }

    /// <summary>The HUD's own canvas if this scene has one, so the bar shares its scaler.</summary>
    static Canvas HostCanvas()
    {
        var hud = FindFirstObjectByType<HudController>(FindObjectsInactive.Include);
        if (hud == null) return null;
        var canvas = hud.GetComponentInParent<Canvas>();
        return canvas != null ? canvas.rootCanvas : null;
    }

    void Awake()
    {
        Build();
        bar.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        Unsubscribe();
        if (instance == this) instance = null;
    }

    void Attach(IBoss next)
    {
        if (ReferenceEquals(boss, next))
        {
            Show();
            return;
        }

        Unsubscribe();
        boss = next;
        boss.OnEngaged += Show;
        boss.OnHealthChanged += UpdateBar;
        boss.OnDefeated += Hide;

        if (label != null) label.text = boss.DisplayName;
        UpdateBar(boss.CurrentHealth, boss.MaxHealth);
        if (boss.IsEngaged) Show();
    }

    void Unsubscribe()
    {
        if (boss == null) return;
        boss.OnEngaged -= Show;
        boss.OnHealthChanged -= UpdateBar;
        boss.OnDefeated -= Hide;
        boss = null;
    }

    void Show()
    {
        if (bar != null) bar.gameObject.SetActive(true);
    }

    void Hide()
    {
        if (bar != null) bar.gameObject.SetActive(false);
    }

    void UpdateBar(int current, int max)
    {
        if (fill == null) return;
        float frac = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;

        // Anchor-driven, not Image.fillAmount: a Filled image needs a sprite, and these
        // are bare Images. anchorMin stays at 0 so the bar always empties leftward.
        fill.anchorMax = new Vector2(frac, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = Color.Lerp(LowColor, FullColor, frac);
    }

    // --- construction ------------------------------------------------------------------

    void Build()
    {
        var root = (RectTransform)transform;
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -24f);
        root.sizeDelta = new Vector2(900f, 78f);

        bar = UIFactory.Rect("Bar", root);
        UIFactory.Fill(bar);

        label = UIFactory.Text("Name", bar, "Boss", 30f);
        var lr = label.rectTransform;
        lr.anchorMin = new Vector2(0f, 1f);
        lr.anchorMax = new Vector2(1f, 1f);
        lr.pivot = new Vector2(0.5f, 1f);
        lr.sizeDelta = new Vector2(0f, 36f);
        lr.anchoredPosition = Vector2.zero;
        label.color = new Color(1f, 0.86f, 0.55f);
        label.fontStyle = FontStyles.Bold;

        var track = UIFactory.Image("Track", bar, TrackColor);
        var tr = track.rectTransform;
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 0f);
        tr.pivot = new Vector2(0.5f, 0f);
        tr.sizeDelta = new Vector2(0f, 30f);
        tr.anchoredPosition = Vector2.zero;
        track.raycastTarget = false;

        var fillImage = UIFactory.Image("Fill", track.transform, FullColor);
        fill = fillImage.rectTransform;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fillImage.raycastTarget = false;
    }
}
