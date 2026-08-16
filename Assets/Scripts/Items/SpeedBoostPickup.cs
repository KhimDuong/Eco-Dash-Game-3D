using UnityEngine;

/// <summary>
/// Nước Tăng Lực Mầm Xanh (Green Sprout Energy Drink) — a support item that
/// grants a temporary move-speed boost when Greenie walks over it. Bobs gently.
/// </summary>
/// <remarks>
/// <b>Superseded by <see cref="ItemPickup"/>:</b> pickups now go into the bag
/// (<c>energy_drink</c>) and are used from the hotbar, instead of applying on touch.
/// Kept for legacy placements — prefer ItemPickup for new ones.
/// </remarks>
[RequireComponent(typeof(Collider))]
public class SpeedBoostPickup : MonoBehaviour
{
    [Header("Boost")]
    [Tooltip("Move-speed multiplier. Design doc: +50% => 1.5.")]
    [SerializeField] float speedMultiplier = 1.5f;
    [Tooltip("Seconds the boost lasts. Design doc: 8s.")]
    [SerializeField] float duration = 8f;
    [SerializeField] AudioClip collectSfx;

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
        if (!other.TryGetComponent<PlayerController>(out var player)) return;

        player.ApplySpeedBoost(speedMultiplier, duration);
        if (collectSfx != null) Sfx.Play(collectSfx, transform.position);
        Destroy(gameObject);
    }
}
