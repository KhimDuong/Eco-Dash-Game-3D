using UnityEngine;

/// <summary>
/// A hidden Mẩu Nhật Ký (lore note) pickup. Press <b>E</b> to read it: the text (from
/// <see cref="LoreNoteCatalog"/>) shows via the <see cref="DialogueRunner"/>, the note
/// is recorded in the <see cref="Codex"/>, and factory notes advance Cô Lan's quest.
/// Already-found notes hide themselves on scene load so revisiting a stage doesn't
/// respawn them. Place 8 across the two stages (4 valley + 4 factory).
///
/// <para>3D port: `Collider2D` → `Collider`.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class LoreNote : MonoBehaviour, IInteractable
{
    [Tooltip("Id from LoreNoteCatalog, e.g. \"ln_valley_1\".")]
    [SerializeField] string noteId = "ln_valley_1";
    [SerializeField] GameObject prompt;
    [SerializeField] AudioClip foundSfx;

    [Header("Bob animation")]
    [SerializeField] float bobHeight = 0.08f;
    [SerializeField] float bobSpeed = 2.5f;

    bool taken;
    Vector3 basePosition;

    public bool CanInteract => !taken && !DialogueRunner.IsActive;
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

    void Start()
    {
        basePosition = transform.position;
        if (Codex.HasLoreNote(noteId)) gameObject.SetActive(false);
    }

    void Update()
    {
        if (taken) return;
        float y = Mathf.Sin(Time.time * bobSpeed + basePosition.x) * bobHeight;
        transform.position = basePosition + new Vector3(0f, y, 0f);
    }

    public void Interact(GameObject interactor)
    {
        if (taken) return;
        taken = true;
        if (prompt != null) prompt.SetActive(false);

        var note = LoreNoteCatalog.Get(noteId);
        bool isNew = Codex.FindLoreNote(noteId);
        if (isNew && note != null && note.stageId == Codex.StageFactory && QuestLog.IsActive(QuestCatalog.LanIntel))
            QuestLog.AddProgress(QuestCatalog.LanIntel);

        if (foundSfx != null) Sfx.Play(foundSfx, transform.position);

        var runner = DialogueRunner.Instance;
        if (runner != null && note != null)
            runner.Begin(new[] { new DialogueLine { speaker = note.title, text = note.body } }, Dismiss);
        else
            Dismiss();
    }

    void Dismiss() => gameObject.SetActive(false);
}
