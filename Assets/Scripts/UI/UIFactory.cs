using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Small helpers for building uGUI at runtime (M9). The inventory bag and hotbar
/// construct themselves in code rather than as authored prefabs — this keeps them
/// self-contained (drop the component on any GameObject and it works) and avoids
/// the fragile MCP prefab-UI editing path. Styling matches the existing dark HUD.
/// </summary>
public static class UIFactory
{
    public static readonly Color PanelColor = new Color(0.08f, 0.10f, 0.09f, 0.94f);
    public static readonly Color SlotColor = new Color(0.18f, 0.22f, 0.20f, 1f);
    public static readonly Color SlotEmptyColor = new Color(0.14f, 0.16f, 0.15f, 0.6f);
    public static readonly Color AccentColor = new Color(0.45f, 0.78f, 0.42f, 1f);

    /// <summary>Find an overlay Canvas to build under, creating one if none exists.</summary>
    public static Canvas EnsureCanvas(Component owner, int sortingOrder)
    {
        EnsureEventSystem();
        var existing = owner.GetComponentInParent<Canvas>();
        if (existing != null) return existing.rootCanvas;

        var go = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        owner.transform.SetParent(go.transform, false);
        EnsureEventSystem();
        return canvas;
    }

    /// <summary>Make sure the scene has an EventSystem (new Input System module).</summary>
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static Image Image(string name, Transform parent, Color color, Sprite sprite = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        if (sprite != null) img.sprite = sprite;
        return img;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, string text, float size,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Stretch a RectTransform to fill its parent.</summary>
    public static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
