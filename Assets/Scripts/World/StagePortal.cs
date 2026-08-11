using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A hub Portal Nexus gate to a stage (M9, K7). Travels to <see cref="targetScene"/>
/// on use. Optionally <b>gated by Portal Shards</b>: a broken gate (Stage 2) needs
/// <see cref="shardCost"/> Mảnh Cổng to power up — the first time the player interacts
/// with enough shards they are consumed, the gate is marked powered (persisted via a
/// <see cref="QuestLog"/> flag), and travel proceeds; afterwards it's simply open.
/// Default gates (<see cref="shardCost"/> = 0) are always open. Use E-interact for
/// gated gates so shards aren't spent by accident.
///
/// <para>3D port (Tier 1): <c>Collider2D</c> → <see cref="Collider"/>, nothing else.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class StagePortal : MonoBehaviour, IInteractable
{
    [Header("Destination")]
    [SerializeField] string targetScene = "Level1_BarrenFarm";

    [Header("Shard gate (0 = always open)")]
    [SerializeField] int shardCost = 0;
    [SerializeField] string shardItemId = "portal_shard";
    [Tooltip("QuestLog flag persisting the powered state once paid (required for gated gates).")]
    [SerializeField] string poweredFlag = "portal_stage2_powered";

    [Header("Interaction")]
    [Tooltip("If true, travels on touch; otherwise needs E. Gated gates should use E.")]
    [SerializeField] bool walkOver = false;
    [SerializeField] GameObject prompt;
    [SerializeField] GameObject lockedVisual;
    [SerializeField] GameObject openVisual;
    [SerializeField] AudioClip travelSfx;
    [SerializeField] AudioClip powerSfx;

    bool sessionPowered;

    /// <summary>True if the gate is open (free, already paid this session, or flagged).</summary>
    public bool Powered => shardCost <= 0 || sessionPowered ||
        (!string.IsNullOrEmpty(poweredFlag) && QuestLog.HasFlag(poweredFlag));

    public bool CanInteract => !walkOver && !DialogueRunner.IsActive;
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

    void Start() => RefreshVisual();

    void OnTriggerEnter(Collider other)
    {
        if (!walkOver) return;
        if (other.CompareTag("Player")) TryUse();
    }

    public void Interact(GameObject interactor)
    {
        if (!walkOver) TryUse();
    }

    void TryUse()
    {
        if (Powered) { Travel(); return; }

        // Try to power the broken gate with shards.
        if (Inventory.Has(shardItemId, shardCost))
        {
            Inventory.TryRemove(shardItemId, shardCost);
            sessionPowered = true;
            if (!string.IsNullOrEmpty(poweredFlag)) QuestLog.SetFlag(poweredFlag);
            if (powerSfx != null) AudioSource.PlayClipAtPoint(powerSfx, transform.position);
            RefreshVisual();
            Travel();
        }
        // Otherwise not enough shards — the locked visual / prompt already says so.
    }

    void Travel()
    {
        if (travelSfx != null) AudioSource.PlayClipAtPoint(travelSfx, transform.position);
        Time.timeScale = 1f;
        SceneManager.LoadScene(targetScene);
    }

    void RefreshVisual()
    {
        bool p = Powered;
        if (lockedVisual != null) lockedVisual.SetActive(!p);
        if (openVisual != null) openVisual.SetActive(p);
    }
}
