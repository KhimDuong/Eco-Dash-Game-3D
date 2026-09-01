using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Màn hình chớp đỏ khi người chơi bị tấn công / mất máu (Damage Overlay).
/// Tự động khởi tạo Canvas UI ở runtime (xem <see cref="UIFactory"/>) và nhấp nháy màu đỏ
/// khi nhận được tín hiệu bị thương.
/// </summary>
public class DamageOverlayUI : MonoBehaviour
{
    static DamageOverlayUI instance;

    Image flashImage;
    Coroutine fadeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => instance = null;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        UIFactory.EnsureCanvas(this, sortingOrder: 99);
        Build();
    }

    void Build()
    {
        var go = new GameObject("DamageFlashOverlay", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        var rt = (RectTransform)go.transform;
        UIFactory.Fill(rt);

        flashImage = go.GetComponent<Image>();
        flashImage.color = new Color(0.9f, 0.1f, 0.1f, 0f);
        flashImage.raycastTarget = false; // Không cản trở click chuột
    }

    /// <summary>Kích hoạt hiệu ứng chớp đỏ màn hình.</summary>
    public static void FlashRed(float duration = 0.25f, float maxAlpha = 0.35f)
    {
        EnsureInstance();
        if (instance != null) instance.TriggerFlash(duration, maxAlpha);
    }

    static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("~DamageOverlayUI");
        instance = go.AddComponent<DamageOverlayUI>();
        DontDestroyOnLoad(go);
    }

    void TriggerFlash(float duration, float maxAlpha)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeRoutine(duration, maxAlpha));
    }

    IEnumerator FadeRoutine(float duration, float maxAlpha)
    {
        if (flashImage == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Chớp đỏ nhanh rồi mờ dần
            float alpha = Mathf.Lerp(maxAlpha, 0f, t * t);
            flashImage.color = new Color(0.9f, 0.1f, 0.1f, alpha);
            yield return null;
        }

        flashImage.color = new Color(0.9f, 0.1f, 0.1f, 0f);
        fadeRoutine = null;
    }
}
