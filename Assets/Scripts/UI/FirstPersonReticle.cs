using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A small centre dot, shown only while first person is live (B6).
///
/// <para>It exists because of an asymmetry the perspective toggle creates: under the ¾ camera
/// Greenie's body <i>is</i> the aim indicator — you can see which way he faces — and at eye
/// height there is nothing on screen to say where a Seed will go. Seeds still fly flat on XZ
/// (golden rule 1), so the dot marks the horizontal aim, not a 3D crosshair: looking down at
/// the ground does not make a Seed hit the ground.</para>
///
/// <para>Built at runtime and created by <see cref="PerspectiveRig"/>, so no scene or prefab
/// needs to know about it — same self-contained pattern as the bag and the codex.</para>
/// </summary>
public class FirstPersonReticle : MonoBehaviour
{
    const float DotSize = 7f;
    const float RingSize = 21f;

    GameObject root;

    /// <summary>Find the scene's reticle, or make one. Safe to call repeatedly.</summary>
    public static FirstPersonReticle Ensure()
    {
        var existing = FindFirstObjectByType<FirstPersonReticle>();
        if (existing != null) return existing;
        return new GameObject("FirstPersonReticle").AddComponent<FirstPersonReticle>();
    }

    void Awake()
    {
        // Under the hotbar (80) and every panel: the reticle is world-reading, not a screen.
        UIFactory.EnsureCanvas(this, sortingOrder: 70);
        Build();
    }

    void Update()
    {
        bool show = PerspectiveMode.IsFirstPerson && !UiModal.AnyOpen;
        if (root != null && root.activeSelf != show) root.SetActive(show);
    }

    void Build()
    {
        root = new GameObject("Reticle", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        Centre((RectTransform)root.transform, 0f);

        // A dark ring behind a light dot, so it stays readable against both the bleached farm
        // sky and the factory's crushed blacks.
        Centre(Dot("Ring", new Color(0f, 0f, 0f, 0.35f)), RingSize);
        Centre(Dot("Dot", new Color(1f, 1f, 1f, 0.85f)), DotSize);

        root.SetActive(false);
    }

    RectTransform Dot(string name, Color colour)
    {
        var img = UIFactory.Image(name, root.transform, colour);
        img.raycastTarget = false;
        return img.rectTransform;
    }

    static void Centre(RectTransform rt, float size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
    }
}
