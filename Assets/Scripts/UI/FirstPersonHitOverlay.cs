using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Red hit flash and low-HP vignette overlay for first-person mode.
/// Built dynamically at runtime by <see cref="PerspectiveRig"/> / <see cref="Ensure"/>.
/// </summary>
public class FirstPersonHitOverlay : MonoBehaviour
{
    const float FlashDuration = 0.35f;

    PlayerHealth playerHealth;
    CanvasGroup canvasGroup;
    float flashTimer;

    /// <summary>Find the scene's hit overlay, or make one. Safe to call repeatedly.</summary>
    public static FirstPersonHitOverlay Ensure()
    {
        var existing = FindFirstObjectByType<FirstPersonHitOverlay>();
        if (existing != null) return existing;
        return new GameObject("FirstPersonHitOverlay").AddComponent<FirstPersonHitOverlay>();
    }

    void Awake()
    {
        // Under modal panels (100), above reticle (70)
        UIFactory.EnsureCanvas(this, sortingOrder: 75);
        BuildOverlay();
    }

    void OnEnable()
    {
        BindPlayerHealth();
    }

    void OnDisable()
    {
        UnbindPlayerHealth();
    }

    void Update()
    {
        if (playerHealth == null) BindPlayerHealth();

        if (!PerspectiveMode.IsFirstPerson)
        {
            SetAlpha(0f);
            return;
        }

        float alpha = 0f;

        // Active hit flash decay
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            alpha = Mathf.Clamp01(flashTimer / FlashDuration) * 0.5f;
        }

        // Low HP pulse effect when health <= 2 HP
        if (playerHealth != null && playerHealth.CurrentHealth > 0 && playerHealth.CurrentHealth <= 2)
        {
            float pulse = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f * 0.15f;
            alpha = Mathf.Max(alpha, 0.12f + pulse);
        }

        SetAlpha(alpha);
    }

    public void TriggerHitFlash()
    {
        flashTimer = FlashDuration;
    }

    void BindPlayerHealth()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.TryGetComponent<PlayerHealth>(out playerHealth))
        {
            playerHealth.OnDamaged -= TriggerHitFlash;
            playerHealth.OnDamaged += TriggerHitFlash;
        }
    }

    void UnbindPlayerHealth()
    {
        if (playerHealth != null)
            playerHealth.OnDamaged -= TriggerHitFlash;
    }

    void BuildOverlay()
    {
        var go = new GameObject("RedFlashOverlay", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);
        var rt = (RectTransform)go.transform;
        UIFactory.Fill(rt);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.85f, 0.05f, 0.05f, 1f);
        img.raycastTarget = false;

        canvasGroup = go.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }
}
