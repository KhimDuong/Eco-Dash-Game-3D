using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Plays a sequence of full-screen story slides — a caption + optional image —
/// advanced by E / Space / Enter / left-click. After the last slide it loads
/// <see cref="nextScene"/> by NAME (so adding scenes can't break the flow by shifting
/// build indices). Drives the Intro_Story (before Level 1) and Ending_Story (after the
/// boss) scenes of the M7 narrative layer.
/// </summary>
public class StorySequence : MonoBehaviour
{
    [System.Serializable]
    public class Slide
    {
        [TextArea(2, 5)] public string text;
        public Sprite image;
    }

    [Header("Slides")]
    [SerializeField] Slide[] slides;
    [Tooltip("Optional slide appended at the end if the M8 side quest (Rescue Ti) was completed.")]
    [SerializeField] Slide bonusRescueSlide;

    [Header("UI")]
    [SerializeField] TMP_Text captionText;
    [Tooltip("Optional image shown above/behind the caption (hidden when a slide has none).")]
    [SerializeField] Image imageDisplay;

    [Header("Flow")]
    [Tooltip("Scene to load by name after the last slide (e.g. Level1_BarrenFarm, MainMenu).")]
    [SerializeField] string nextScene = "Level1_BarrenFarm";

    [Header("Audio (optional)")]
    [SerializeField] AudioClip advanceSfx;

    int index;
    bool transitioning;
    AudioSource audioSource;

    void Awake() => audioSource = GetComponent<AudioSource>();

    void Start()
    {
        Time.timeScale = 1f; // clear any freeze inherited from a previous scene
        
        // Append bonus slide if Ti was saved
        if (QuestProgress.Stage == QuestStage.TiSaved && bonusRescueSlide != null && !string.IsNullOrEmpty(bonusRescueSlide.text))
        {
            var list = new System.Collections.Generic.List<Slide>(slides != null ? slides : new Slide[0]);
            list.Add(bonusRescueSlide);
            slides = list.ToArray();
        }

        index = 0;
        if (slides != null && slides.Length > 0) ShowSlide(slides[0]);
    }

    void Update()
    {
        if (transitioning) return;
        var kb = Keyboard.current;
        bool advance =
            (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame ||
                            kb.enterKey.wasPressedThisFrame)) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        if (advance) Advance();
    }

    void Advance()
    {
        if (advanceSfx != null && audioSource != null) audioSource.PlayOneShot(advanceSfx);
        index++;
        if (slides == null || index >= slides.Length) { LoadNext(); return; }
        ShowSlide(slides[index]);
    }

    void ShowSlide(Slide slide)
    {
        if (captionText != null) captionText.text = slide.text;
        if (imageDisplay != null)
        {
            imageDisplay.sprite = slide.image;
            imageDisplay.enabled = slide.image != null;
        }
    }

    void LoadNext()
    {
        transitioning = true;
        if (!string.IsNullOrEmpty(nextScene)) SceneManager.LoadScene(nextScene);
    }
}
