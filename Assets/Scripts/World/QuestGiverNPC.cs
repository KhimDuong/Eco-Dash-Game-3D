using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QuestGiverNPC : MonoBehaviour, IInteractable
{
    [Header("Dialogues by Stage")]
    [SerializeField] private DialogueLine[] offerLines;
    [SerializeField] private DialogueLine[] herbsInProgressLines;
    [SerializeField] private DialogueLine[] herbsReadyLines;
    [SerializeField] private DialogueLine[] antidoteHeldLines;
    [SerializeField] private DialogueLine[] tiSavedLines;

    [Header("Prompt")]
    [SerializeField] private GameObject prompt;

    private bool talking;

    public bool CanInteract => !talking;
    public Vector3 InteractPosition => transform.position;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (prompt != null) prompt.SetActive(false);
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

        DialogueLine[] linesToPlay = GetCurrentLines();
        runner.Begin(linesToPlay, OnDialogueDone);
    }

    private DialogueLine[] GetCurrentLines()
    {
        switch (QuestProgress.Stage)
        {
            case QuestStage.NotMet:
            case QuestStage.Offered:
                return offerLines;
            case QuestStage.HerbsInProgress:
                return herbsInProgressLines;
            case QuestStage.HerbsReady:
                return herbsReadyLines;
            case QuestStage.AntidoteHeld:
                return antidoteHeldLines;
            case QuestStage.TiSaved:
                return tiSavedLines;
            default:
                return offerLines;
        }
    }

    private void OnDialogueDone()
    {
        talking = false;

        // Advance quest state based on the dialogue we just finished
        switch (QuestProgress.Stage)
        {
            case QuestStage.NotMet:
            case QuestStage.Offered:
                QuestProgress.StartQuest();
                QuestProgress.AcceptQuestAndStartGathering();
                break;
            case QuestStage.HerbsReady:
                QuestProgress.GrantAntidote();
                break;
        }
    }
}
