using UnityEngine;

/// <summary>
/// Rác thải (litter / waste) scattered across the barren farm. Greenie cleans it
/// just by walking over it, feeding the trash counter
/// (<see cref="GameManager.AddTrash"/>) — the start of the recycling economy that
/// Mr. Bear's shop spends later. Bobs slightly so it reads as a pickup, not an
/// obstacle.
///
/// <para>3D port: trigger callbacks lose their `2D` suffix; the bob was already on
/// Y and stays there.</para>
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

    void Start()
    {
        // Already cleaned on a previous visit? Stay gone (M9 save persistence).
        if (SceneProgress.IsConsumed(gameObject)) { Destroy(gameObject); return; }
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
        if (!other.CompareTag("Player")) return;
        SceneProgress.Consume(gameObject);
        if (GameManager.Instance != null) GameManager.Instance.AddTrash(trashValue);
        // Cleaning trash yields a crafting material.
        if (dropPool != null && dropPool.Length > 0 && Random.value <= dropChance)
            Inventory.TryAdd(dropPool[Random.Range(0, dropPool.Length)], 1);
        if (cleanSfx != null) AudioSource.PlayClipAtPoint(cleanSfx, transform.position);
        Destroy(gameObject);
    }
}
