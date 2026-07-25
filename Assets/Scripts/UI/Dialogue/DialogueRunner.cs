using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Reusable dialogue box (M7 narrative layer). NPCs and story beats call
/// <see cref="Begin"/> with a set of <see cref="DialogueLine"/>s; the panel shows the
/// speaker + line and the player advances with E / Space / Enter. While a line is up
/// <see cref="IsActive"/> is true, and player movement / shooting / interaction pause
/// (those scripts check this flag). One runner per scene — reach it via <see cref="Instance"/>.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    public static DialogueRunner Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Root object of the dialogue box; toggled on/off as dialogue starts/ends.")]
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text lineText;
    [Tooltip("Optional portrait image (hidden when a line has no portrait).")]
    [SerializeField] Image portraitImage;

    [Header("Audio (optional)")]
    [Tooltip("Short blip played as each line appears.")]
    [SerializeField] AudioClip blipSfx;

    /// <summary>
    /// True while a dialogue is showing — and also for the single frame it closes on, so
    /// the dismissing keypress isn't re-read by <see cref="PlayerInteractor"/> and used to
    /// instantly re-open the same NPC. Gameplay input scripts early-out while this is true.
    /// </summary>
    public static bool IsActive =>
        Instance != null && (Instance.running || Time.frameCount == Instance.closedFrame);

    readonly List<DialogueLine> queue = new();
    Action onComplete;
    int index;
    bool running;
    int openedFrame = -1;
    int closedFrame = -1;
    AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (panel != null) panel.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Show the given lines. <paramref name="onDone"/> fires after the last line is
    /// dismissed (use it to flip an objective, open a shop, advance a beat, etc.).
    /// </summary>
    public void Begin(IList<DialogueLine> lines, Action onDone = null)
    {
        if (lines == null || lines.Count == 0) { onDone?.Invoke(); return; }
        queue.Clear();
        queue.AddRange(lines);
        onComplete = onDone;
        index = 0;
        running = true;
        openedFrame = Time.frameCount;
        Time.timeScale = 0f; // Dừng hoàn toàn quái vật và thế giới khi nói chuyện
        if (panel != null) panel.SetActive(true);
        Show(queue[0]);
    }

    void Update()
    {
        if (!running) return;
        if (Time.frameCount == openedFrame) return; // swallow the keypress that opened us

        var kb = Keyboard.current;
        bool advance = kb != null &&
            (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
        if (advance) Advance();
    }

    void Advance()
    {
        index++;
        if (index >= queue.Count) { Close(); return; }
        Show(queue[index]);
    }

    void Show(DialogueLine line)
    {
        if (speakerText != null)
        {
            bool hasName = !string.IsNullOrEmpty(line.speaker);
            speakerText.text = line.speaker;
            speakerText.gameObject.SetActive(hasName);
        }
        if (lineText != null) lineText.text = line.text;
        if (portraitImage != null)
        {
            bool hasPortrait = line.portrait != null;
            portraitImage.sprite = line.portrait;
            portraitImage.gameObject.SetActive(hasPortrait); // hide the box entirely when unused
        }
        if (blipSfx != null && audioSource != null) audioSource.PlayOneShot(blipSfx);
    }

    void Close()
    {
        running = false;
        closedFrame = Time.frameCount;
        Time.timeScale = 1f; // Khôi phục lại thời gian cho map
        if (panel != null) panel.SetActive(false);
        var cb = onComplete;
        onComplete = null;
        cb?.Invoke();
    }
}
