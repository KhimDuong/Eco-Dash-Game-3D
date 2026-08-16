using UnityEngine;

/// <summary>
/// Lõi Năng Lượng Sạch (Clean Energy Core) — the Level 1 objective pickup.
/// Hidden in old wooden chests; collecting 3 purifies the water and opens the
/// gate to Level 2 (tracked by GameManager). Gently bobs and spins to draw the eye.
///
/// <para>3D port: trigger callback loses its `2D` suffix and the core also turns on
/// Y, which a flat sprite couldn't do.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class EnergyCore : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] AudioClip collectSfx;

    [Header("Bob animation")]
    [SerializeField] float bobHeight = 0.12f;
    [SerializeField] float bobSpeed = 2f;
    [SerializeField] float spinSpeed = 60f;

    Vector3 basePosition;

    void Start()
    {
        // Already collected on a previous visit? Stay gone (M9 save persistence).
        if (SceneProgress.IsConsumed(gameObject)) { Destroy(gameObject); return; }
        basePosition = transform.position;
        GetComponent<Collider>().isTrigger = true;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = basePosition + new Vector3(0f, y, 0f);
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        SceneProgress.Consume(gameObject);
        if (GameManager.Instance != null) GameManager.Instance.CollectCore();
        if (collectSfx != null) Sfx.Play(collectSfx, transform.position);
        Destroy(gameObject);
    }
}
