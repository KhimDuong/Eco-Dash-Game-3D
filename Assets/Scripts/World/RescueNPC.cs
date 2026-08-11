using UnityEngine;

/// <summary>
/// Tí, the worker trapped in the Factory. Unconscious until Greenie brings the
/// antidote brewed from Ông Sáu's herbs (Level 1's M8 quest chain); rescuing him
/// grants the mandatory **third keycard** directly, which is what lets Level 2's
/// boss door open at all.
///
/// <para>3D port (Tier 1): <c>Collider2D</c> → <see cref="Collider"/>, and the
/// unconscious/awake sprite swap becomes a swap between two child objects — a mesh
/// has no "sprite" to reassign, and a slumped vs standing pose reads far better under
/// the fixed ¾ camera than a texture change would. The quest logic is untouched.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class RescueNPC : MonoBehaviour, IInteractable
{
    [Header("Dialogues")]
    [SerializeField] private DialogueLine[] unconsciousLines;
    [SerializeField] private DialogueLine[] rescueLines;
    [SerializeField] private DialogueLine[] alreadySavedLines;

    [Header("Visuals & Rewards")]
    [Tooltip("Slumped pose, shown until Tí is revived.")]
    [SerializeField] private GameObject unconsciousVisual;
    [Tooltip("Standing pose, shown once the antidote is used.")]
    [SerializeField] private GameObject awakeVisual;

    [Header("Prompt")]
    [SerializeField] private GameObject prompt;

    private bool talking;
    private bool hasDroppedReward;

    public bool CanInteract => !talking;
    public Vector3 InteractPosition => transform.position;

    /// <summary>True once Tí is on his feet (drives the probe and the objective list).</summary>
    public bool IsSaved => QuestProgress.Stage == QuestStage.TiSaved;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (prompt != null) prompt.SetActive(false);
    }

    private void Start()
    {
        UpdateVisuals();
    }

    public void SetHighlighted(bool highlighted)
    {
        if (prompt != null) prompt.SetActive(highlighted && CanInteract);
    }

    public void Interact(GameObject interactor)
    {
        var runner = DialogueRunner.Instance;
        if (runner == null || !CanInteract) return;

        talking = true;
        if (prompt != null) prompt.SetActive(false);

        DialogueLine[] linesToPlay;

        if (QuestProgress.Stage == QuestStage.TiSaved)
        {
            linesToPlay = alreadySavedLines;
        }
        else if (QuestProgress.HasAntidote)
        {
            // We have the antidote!
            linesToPlay = rescueLines;
        }
        else
        {
            // Don't have the antidote yet
            linesToPlay = unconsciousLines;
        }

        runner.Begin(linesToPlay, OnDialogueDone);
    }

    private void OnDialogueDone()
    {
        talking = false;

        // If we just used the antidote
        if (QuestProgress.HasAntidote && QuestProgress.Stage == QuestStage.AntidoteHeld)
        {
            QuestProgress.ConsumeAntidoteAndSaveTi();
            UpdateVisuals();

            // Saving Tí grants the mandatory 3rd keycard directly (M9). Crediting the
            // objective (rather than dropping a physical card that could be left behind
            // and lost on scene reload) guarantees Level 2 can't soft-lock, and it
            // persists via SceneProgress like the other two keycards.
            if (!hasDroppedReward)
            {
                if (GameManager.Instance != null) GameManager.Instance.CollectCore();
                hasDroppedReward = true;
            }
        }
    }

    private void UpdateVisuals()
    {
        bool saved = QuestProgress.Stage == QuestStage.TiSaved;
        if (unconsciousVisual != null) unconsciousVisual.SetActive(!saved);
        if (awakeVisual != null) awakeVisual.SetActive(saved);
    }
}
