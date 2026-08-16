using UnityEngine;

/// <summary>
/// Nước Suối Tinh Khiết (Pure Spring Water) — a support item that instantly
/// restores HP when Greenie walks over it. Bobs gently to catch the eye.
/// </summary>
/// <remarks>
/// <b>Superseded by <see cref="ItemPickup"/>:</b> pickups now go into the bag
/// (<c>spring_water</c>) and are used from the hotbar, instead of healing on touch.
/// Kept for legacy placements — prefer ItemPickup for new ones.
/// </remarks>
[RequireComponent(typeof(Collider))]
public class HealthPickup : MonoBehaviour
{
    [Header("Pickup")]
    [Tooltip("HP restored on contact. Design doc: Pure Spring Water = +2 HP.")]
    [SerializeField] int healAmount = 2;
    [SerializeField] AudioClip collectSfx;
    [Tooltip("If true, the player must be missing HP to pick this up.")]
    [SerializeField] bool skipIfFull = true;

    [Header("Bob animation")]
    [SerializeField] float bobHeight = 0.1f;
    [SerializeField] float bobSpeed = 2.5f;

    Vector3 basePosition;

    void Start()
    {
        basePosition = transform.position;
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = basePosition + new Vector3(0f, y, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!other.TryGetComponent<PlayerHealth>(out var hp)) return;
        if (skipIfFull && hp.CurrentHealth >= hp.MaxHealth) return;

        hp.Heal(healAmount);
        if (collectSfx != null) Sfx.Play(collectSfx, transform.position);
        Destroy(gameObject);
    }
}
