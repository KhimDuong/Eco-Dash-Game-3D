using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hiệu ứng viền đỏ máu màn hình (Bloody Red Vignette & Damage Overlay).
/// Khởi tạo Canvas uGUI phẳng toàn màn hình ở runtime và bật sáng viền đỏ máu khi bị quái đánh.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
public class DamageOverlayUI : MonoBehaviour
{
    static DamageOverlayUI instance;
    static Sprite vignetteSprite;

    Image flashImage;
    Coroutine fadeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        instance = null;
        vignetteSprite = null;
    }

    void Awake()
    {
        instance = this;
        flashImage = GetComponent<Image>();

        if (vignetteSprite == null)
            vignetteSprite = CreateVignetteSprite();

        flashImage.sprite = vignetteSprite;
        flashImage.type = Image.Type.Simple;
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        flashImage.raycastTarget = false; // Không cản trở gameplay
    }

    /// <summary>Kích hoạt hiệu ứng viền đỏ máu lóe lên trên màn hình.</summary>
    public static void FlashRed(float duration = 0.45f, float maxAlpha = 0.95f)
    {
        var ui = EnsureInstance();
        if (ui != null) ui.TriggerFlash(duration, maxAlpha);
    }

    public static DamageOverlayUI EnsureInstance()
    {
        if (instance != null) return instance;

        // Tạo Canvas Overlay 1920x1080
        var canvasGo = new GameObject("~DamageCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99; // Trên hết các UI ngoại trừ Modal/Tutorial

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Tạo GameObject đại diện cho lớp phủ tràn màn hình
        var overlayGo = new GameObject("DamageBloodOverlay", typeof(RectTransform), typeof(Image), typeof(DamageOverlayUI));
        overlayGo.transform.SetParent(canvasGo.transform, false);

        var rt = (RectTransform)overlayGo.transform;
        UIFactory.Fill(rt);

        instance = overlayGo.GetComponent<DamageOverlayUI>();
        DontDestroyOnLoad(canvasGo);
        return instance;
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
            // Giữ độ sáng ban đầu một chút rồi giảm mượt mà về 0
            float alpha = Mathf.Lerp(maxAlpha, 0f, t * t);
            flashImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        flashImage.color = new Color(1f, 1f, 1f, 0f);
        fadeRoutine = null;
    }

    /// <summary>Tạo Texture2D hiệu ứng viền đỏ máu (Vignette) bo tròn xung quanh màn hình.</summary>
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

                float factor = Mathf.Lerp(distBox, distRadial, 0.4f);

                // Tâm trong suốt hoàn toàn, viền màn hình và 4 góc lên màu đỏ thẫm
                float alpha = Mathf.Clamp01((factor - 0.25f) / 0.75f);
                alpha = Mathf.Pow(alpha, 1.4f);

                Color bloodColor = Color.Lerp(
                    new Color(0.95f, 0.03f, 0.03f, alpha), // Viền đỏ tươi
                    new Color(0.40f, 0.00f, 0.00f, alpha), // Góc đỏ thẫm
                    factor * 0.7f
                );

                pixels[y * width + x] = bloodColor;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}
