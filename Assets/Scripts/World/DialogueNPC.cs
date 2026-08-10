using UnityEngine;

/// <summary>
/// A talkable NPC (M7). An <see cref="IInteractable"/> that hands its lines to the
/// scene <see cref="DialogueRunner"/> when Greenie presses E — reusing the chest/shop
/// prompt pattern (a "Nhấn E" world label shown while it's the nearest target).
///
/// Quest-giver options: <see cref="autoBriefOnStart"/> plays the lines once when the
/// level loads (Bà Tư's opening mission briefing), and <see cref="swapOnAllCores"/>
/// switches to <see cref="postCompletionLines"/> when every core/keycard is collected
/// (her closing "the poison flows from the factory" beat).
/// </summary>
[RequireComponent(typeof(Collider))]
public class DialogueNPC : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] DialogueLine[] lines;

    [Tooltip("Lines shown after every core/keycard is collected (if Swap On All Cores is on).")]
    [SerializeField] DialogueLine[] postCompletionLines;

    [Header("Behaviour")]
    [Tooltip("Play the lines automatically once when the level loads (mission briefing).")]
    [SerializeField] bool autoBriefOnStart = false;
    [Tooltip("Delay before the auto-briefing, so the scene/HUD settle first.")]
    [SerializeField] float briefDelay = 0.4f;
    [Tooltip("Swap to Post Completion Lines once GameManager raises OnAllCoresCollected.")]
    [SerializeField] bool swapOnAllCores = false;
    [Tooltip("If true, the NPC stops being interactable after the player reads it once.")]
    [SerializeField] bool talkOnce = false;

    [Header("Prompt")]
    [Tooltip("Optional 'Nhấn E' world-space label shown when Greenie is the nearest target.")]
    [SerializeField] GameObject prompt;

    /// <summary>Raised after the player finishes reading this NPC's current lines.</summary>
    public event System.Action OnTalked;

    bool talking;
    bool spent;

    public bool CanInteract => !spent && !talking && lines != null && lines.Length > 0;
    public Vector3 InteractPosition => transform.position;

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (prompt != null) prompt.SetActive(false);
    }

    void Start()
    {
        if (swapOnAllCores && GameManager.Instance != null)
        {
            GameManager.Instance.OnAllCoresCollected += SwapToPostCompletion;
            if (GameManager.Instance.AllCoresCollected) SwapToPostCompletion(); // safety on re-entry
        }
        if (autoBriefOnStart) Invoke(nameof(Talk), briefDelay);
    }

    void OnDestroy()
    {
        if (swapOnAllCores && GameManager.Instance != null)
            GameManager.Instance.OnAllCoresCollected -= SwapToPostCompletion;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (prompt != null) prompt.SetActive(highlighted && CanInteract);
    }

    public void Interact(GameObject interactor) => Talk();

    /// <summary>Begin this NPC's dialogue (used by E and by the auto-briefing).</summary>
    void Talk()
    {
        var runner = DialogueRunner.Instance;
        if (runner == null || !CanInteract) return;
        talking = true;
        if (prompt != null) prompt.SetActive(false);
        runner.Begin(lines, OnDialogueDone);
    }

    void OnDialogueDone()
    {
        talking = false;
        if (talkOnce) spent = true;
        OnTalked?.Invoke();
    }

    void SwapToPostCompletion()
    {
        if (postCompletionLines != null && postCompletionLines.Length > 0)
            SetLines(postCompletionLines);
    }

    /// <summary>Swap the lines shown on the next talk (e.g. Bà Tư's post-mission line).</summary>
    public void SetLines(DialogueLine[] newLines)
    {
        lines = newLines;
        spent = false;
    }
}
