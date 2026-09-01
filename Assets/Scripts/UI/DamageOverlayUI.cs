using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng viền đỏ máu màn hình (Bloody Red Vignette & Damage Overlay).
/// Tạo kết cấu Vignette bo tròn viền đỏ tối/máu ở runtime (không cần file ảnh bên ngoài)
/// và nhấp nháy mạnh ở các mép màn hình khi người chơi bị quái vật đánh trúng.
/// </summary>
public class DamageOverlayUI : MonoBehaviour
{
    static DamageOverlayUI instance;

    Image flashImage;
    Coroutine fadeRoutine;
    static Sprite vignetteSprite;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        vignetteSprite = null;
    }

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
        var go = new GameObject("DamageBloodVignetteOverlay", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        var rt = (RectTransform)go.transform;
        UIFactory.Fill(rt);

        if (vignetteSprite == null)
            vignetteSprite = CreateVignetteSprite();

        flashImage = go.GetComponent<Image>();
        flashImage.sprite = vignetteSprite;
        flashImage.type = Image.Type.Simple;
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        flashImage.raycastTarget = false; // Không cản trở click chuột/gameplay
    }

    /// <summary>Kích hoạt hiệu ứng viền đỏ máu nhấp nháy khi trúng đạn/bị đánh.</summary>
    public static void FlashRed(float duration = 0.4f, float maxAlpha = 0.85f)
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
            // Bùng lên viền đỏ máu và mờ dần mượt mà
            float alpha = Mathf.Lerp(maxAlpha, 0f, Mathf.SmoothStep(0f, 1f, t));
            flashImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        flashImage.color = new Color(1f, 1f, 1f, 0f);
        fadeRoutine = null;
    }

    /// <summary>Tạo Texture2D hiệu ứng viền đỏ máu (Vignette) ở 4 góc và viền màn hình.</summary>
    static Sprite CreateVignetteSprite()
    {
        int width = 256;
        int height = 256;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            float v = (y / (float)height) - 0.5f;
            for (int x = 0; x < width; x++)
            {
                float u = (x / (float)width) - 0.5f;

                // Khoảng cách từ tâm màn hình
                float distRadial = Mathf.Sqrt(u * u * 4f + v * v * 4f);
                float distBox = Mathf.Max(Mathf.Abs(u * 2f), Mathf.Abs(v * 2f));
                
                // Kết hợp giữa dạng hình tròn và dạng hộp viền màn hình
                float factor = Mathf.Lerp(distBox, distRadial, 0.45f);

                // Ở giữa màn hình trong suốt (alpha = 0), ở viền tăng dần lên 1
                float alpha = Mathf.Clamp01((factor - 0.3f) / 0.7f);
                alpha = Mathf.Pow(alpha, 1.6f);

                // Tông màu đỏ máu sẫm (Crimson Red) ở viền và tối thẫm ở 4 góc
                Color bloodEdgeColor = Color.Lerp(
                    new Color(0.85f, 0.05f, 0.05f, alpha), 
                    new Color(0.35f, 0.01f, 0.01f, alpha), 
                    factor * 0.75f
                );

                pixels[y * width + x] = bloodEdgeColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}
