using System;
using UnityEngine;

/// <summary>
/// A generic NPC quest-giver for the M9 side quests (K9) — Bé Mây, Ông Tài, Cô Lan,
/// Ông Bear — driving <see cref="QuestLog"/> by a <see cref="questId"/> from
/// <see cref="QuestCatalog"/>. It shows state-appropriate dialogue and advances the
/// quest on talk:
/// <list type="bullet">
/// <item><b>Hidden</b> → offer lines, then the quest becomes Active.</item>
/// <item><b>Active</b> → in-progress lines; for a <see cref="Completion.TurnInItems"/>
/// quest, if the bag holds the required items it plays the ready lines and, on close,
/// consumes them and completes the quest (granting its unlock flag + reward).</item>
/// <item><b>Completed</b> → done lines.</item>
/// </list>
/// Externally-completed quests (pet found / pond cleaned) use
/// <see cref="QuestCompleteTrigger"/>; this NPC just narrates their state — and that
/// trigger was never placed in any 2D scene, so <c>may_pet</c> and <c>tai_pond</c> are
/// offerable but not completable in the original either. Ported as-is; see roadmap.md.
///
/// <para>3D port (Tier 1): <c>Collider2D</c> → <see cref="Collider"/>, nothing else.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class SideQuestNPC : MonoBehaviour, IInteractable
{
    public enum Completion { External, TurnInItems }

    [Header("Quest")]
    [Tooltip("Quest id from QuestCatalog, e.g. \"bear_recycle\".")]
    [SerializeField] string questId = QuestCatalog.BearRecycle;
    [SerializeField] Completion completion = Completion.External;

    [Header("Turn-in (Completion = TurnInItems)")]
    [Tooltip("Item ids required to turn in (paired with Turn In Counts).")]
    [SerializeField] string[] turnInIds;
    [SerializeField] int[] turnInCounts;

    [Header("Reward on completion (optional)")]
    [SerializeField] string rewardItemId;
    [SerializeField] int rewardItemCount = 0;

    [Header("Dialogue by state")]
    [SerializeField] DialogueLine[] offerLines;
    [SerializeField] DialogueLine[] inProgressLines;
    [Tooltip("Shown when an item turn-in is ready (Completion = TurnInItems).")]
    [SerializeField] DialogueLine[] readyLines;
    [SerializeField] DialogueLine[] doneLines;

    [Header("Prompt")]
    [SerializeField] GameObject prompt;

    bool talking;

    public bool CanInteract => !talking && !DialogueRunner.IsActive;
    public Vector3 InteractPosition => transform.position;
    public void SetHighlighted(bool highlighted)
    {
        if (prompt != null) prompt.SetActive(highlighted && CanInteract);
    }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (prompt != null) prompt.SetActive(false);
    }

    public void Interact(GameObject interactor)
    {
        var runner = DialogueRunner.Instance;
        if (runner == null || talking) return;

        DialogueLine[] lines;
        Action after = null;

        switch (QuestLog.GetState(questId))
        {
            case QuestState.Hidden:
                lines = offerLines;
                after = () => QuestLog.Offer(questId);
                break;
            case QuestState.Completed:
                lines = doneLines;
                break;
            default: // Active
                if (completion == Completion.TurnInItems && HasTurnIn())
                {
                    lines = readyLines;
                    after = CompleteTurnIn;
                }
                else lines = inProgressLines;
                break;
        }

        talking = true;
        if (prompt != null) prompt.SetActive(false);

        if (lines == null || lines.Length == 0)
        {
            talking = false;
            after?.Invoke();
            return;
        }
        runner.Begin(lines, () => { talking = false; after?.Invoke(); });
    }

    bool HasTurnIn()
    {
        if (turnInIds == null) return false;
        for (int i = 0; i < turnInIds.Length; i++)
        {
            int need = (turnInCounts != null && i < turnInCounts.Length) ? turnInCounts[i] : 1;
            if (!Inventory.Has(turnInIds[i], need)) return false;
        }
        return turnInIds.Length > 0;
    }

    void CompleteTurnIn()
    {
        if (!HasTurnIn()) return;
        for (int i = 0; i < turnInIds.Length; i++)
        {
            int need = (turnInCounts != null && i < turnInCounts.Length) ? turnInCounts[i] : 1;
            Inventory.TryRemove(turnInIds[i], need);
        }
        QuestLog.Complete(questId);
        if (!string.IsNullOrEmpty(rewardItemId) && rewardItemCount > 0)
            Inventory.TryAdd(rewardItemId, rewardItemCount);
    }
}
