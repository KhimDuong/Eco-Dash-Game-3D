using System.Collections;
using UnityEngine;

/// <summary>
/// The generic world pickup for the inventory: grants <see cref="amount"/> of
/// <see cref="itemId"/> into the <see cref="Inventory"/> instead of applying an
/// instant effect. Used for materials, consumables and key items (keycard / portal
/// shard). Two collection styles:
/// <list type="bullet">
/// <item><b>WalkOver</b> — collected on touch (materials, consumables, drops).</item>
/// <item><b>Interact</b> — collected with <b>E</b> via <see cref="IInteractable"/>
/// (deliberate key/quest grabs), mirroring <see cref="Chest"/>.</item>
/// </list>
///
/// <para>3D port: `Collider2D` → `Collider`; the grab flourish shrinks the mesh out
/// instead of fading a sprite's alpha, since URP Lit opaque can't fade.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    public enum PickupMode { WalkOver, Interact }

    [Header("Item")]
    [Tooltip("Item id to grant (must match an ItemDatabase id, e.g. \"bottle\").")]
    [SerializeField] string itemId = "bottle";
    [SerializeField] int amount = 1;

    [Header("Collection")]
    [SerializeField] PickupMode mode = PickupMode.WalkOver;
    [Tooltip("Extra recycling-trash currency granted on pickup (0 = none).")]
    [SerializeField] int trashBonus = 0;
    [SerializeField] AudioClip collectSfx;
    [Tooltip("\"Nhấn E\" prompt shown while targeted (Interact mode only).")]
    [SerializeField] GameObject prompt;

    [Header("Feel")]
    [Tooltip("If set, the mesh pops up and shrinks away on grab; otherwise destroyed at once.")]
    [SerializeField] Transform popVisual;
    [SerializeField] float popHeight = 0.5f;
    [SerializeField] float popDuration = 0.4f;

    [Header("Bob animation")]
    [SerializeField] float bobHeight = 0.1f;
    [SerializeField] float bobSpeed = 2.5f;

    bool taken;
    Vector3 basePosition;

    public bool CanInteract => !taken && mode == PickupMode.Interact;
    public Vector3 InteractPosition => transform.position;
    public void SetHighlighted(bool highlighted)
    {
        if (prompt != null) prompt.SetActive(highlighted && CanInteract);
    }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (prompt != null) prompt.SetActive(false);
        if (popVisual == null)
        {
            var r = GetComponentInChildren<Renderer>();
            if (r != null) popVisual = r.transform;
        }
    }

    void Start()
    {
        // Already taken on a previous visit? Stay gone (M9 save persistence).
        if (SceneProgress.IsConsumed(gameObject)) { Destroy(gameObject); return; }
        basePosition = transform.position;
    }

    void Update()
    {
        if (taken) return;
        float y = Mathf.Sin(Time.time * bobSpeed + basePosition.x) * bobHeight;
        transform.position = basePosition + new Vector3(0f, y, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (mode != PickupMode.WalkOver || taken) return;
        if (other.CompareTag("Player")) Collect();
    }

    public void Interact(GameObject interactor)
    {
        if (mode == PickupMode.Interact) Collect();
    }

    void Collect()
    {
        if (taken) return;
        taken = true;
        if (prompt != null) prompt.SetActive(false);

        SceneProgress.Consume(gameObject);
        Inventory.TryAdd(itemId, amount);
        if (trashBonus > 0 && GameManager.Instance != null) GameManager.Instance.AddTrash(trashBonus);
        if (collectSfx != null) Sfx.Play(collectSfx, transform.position);

        if (popVisual != null) StartCoroutine(PopRoutine());
        else Destroy(gameObject);
    }

    IEnumerator PopRoutine()
    {
        Vector3 from = transform.position;
        Vector3 to = from + Vector3.up * popHeight;
        Vector3 scale = popVisual.localScale;
        float t = 0f;
        while (t < popDuration)
        {
            t += Time.deltaTime;
            float u = t / popDuration;
            transform.position = Vector3.Lerp(from, to, Mathf.Sqrt(u));
            popVisual.localScale = scale * (1f - u);
            yield return null;
        }
        Destroy(gameObject);
    }
}
