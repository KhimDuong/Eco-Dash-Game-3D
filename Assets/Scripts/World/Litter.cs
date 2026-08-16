using UnityEngine;

/// <summary>
/// Rác thải (litter / waste) scattered across the barren farm and the factory floor.
/// Greenie cleans it just by walking over it, feeding the trash counter
/// (<see cref="GameManager.AddTrash"/>) — the start of the recycling economy that
/// Mr. Bear's shop spends later. Bobs slightly so it reads as a pickup, not an
/// obstacle.
///
/// <para>3D port: trigger callbacks lose their `2D` suffix; the bob was already on
/// Y and stays there.</para>
///
/// <para><b>C4:</b> clearing a piece now also drives the cleaning loop —
/// <see cref="GroundCleanser"/> greens the ground around it and raises the stage's
/// Độ Sạch. The cleanup itself moved into <see cref="Clean"/> so the Seed Bomb can
/// trigger it at a distance, which is the AoE's "clears trash" half.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class Litter : MonoBehaviour
{
    [Header("Cleanup")]
    [SerializeField] int trashValue = 1;
    [SerializeField] AudioClip cleanSfx;

    [Header("Material drop")]
    [Tooltip("Item ids this trash can drop into the bag when cleaned.")]
    [SerializeField] string[] dropPool = { "bottle", "scrap" };
    [Tooltip("Chance (0–1) to drop one material on cleanup.")]
    [Range(0f, 1f)][SerializeField] float dropChance = 1f;

    [Header("Bob animation")]
    [SerializeField] float bobHeight = 0.06f;
    [SerializeField] float bobSpeed = 3f;

    Vector3 basePosition;
    bool cleaned;

    // Registering here rather than in Start is what makes the stage's 100% mean "all of it":
    // an already-cleaned piece deletes itself in Start, and a count taken after that would
    // only ever see the leftovers. See GroundCleanser's class remarks.
    void Awake() => GroundCleanser.Register(this);

    void OnDestroy() => GroundCleanser.Unregister(this);

    void Start()
    {
        // Already cleaned on a previous visit? Stay gone (M9 save persistence) — but hand the
        // cleanser the position on the way out, so the count stays honest and the ground it
        // cleared last time is still green when the player walks back over it.
        if (SceneProgress.IsConsumed(gameObject))
        {
            GroundCleanser.RestoreCleaned(transform.position);
            Destroy(gameObject);
            return;
        }
        basePosition = transform.position;
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * bobSpeed + basePosition.x) * bobHeight;
        transform.position = basePosition + new Vector3(0f, y, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) Clean();
    }

    /// <summary>
    /// Clear this piece: bank it, pay out trash and a material, run the cleanse, and go away.
    /// Public because <see cref="GroundCleanser.CleanRadius"/> calls it for the Seed Bomb —
    /// the same cleanup either way, so a bombed piece pays exactly what a walked-over one does.
    /// </summary>
    public void Clean()
    {
        if (cleaned) return;      // a bomb and a footstep in the same frame must not pay twice
        cleaned = true;

        SceneProgress.Consume(gameObject);
        if (GameManager.Instance != null) GameManager.Instance.AddTrash(trashValue);
        // Cleaning trash yields a crafting material.
        if (dropPool != null && dropPool.Length > 0 && Random.value <= dropChance)
            Inventory.TryAdd(dropPool[Random.Range(0, dropPool.Length)], 1);
        if (cleanSfx != null) Sfx.Play(cleanSfx, transform.position);

        GroundCleanser.Clean(transform.position);
        Destroy(gameObject);
    }
}
