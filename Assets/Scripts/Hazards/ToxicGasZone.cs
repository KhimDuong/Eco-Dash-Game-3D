using System.Collections;
using UnityEngine;

/// <summary>
/// Vùng khí độc (toxic-gas zone) — a telegraphed area attack used by the Mega-Smog
/// boss. Spawns harmless, pulses a warning while it telegraphs, then becomes active
/// gas that damages the player on a tick while they stand in it, then fades out and
/// destroys itself. The trigger collider is off during the telegraph so the player
/// has time to clear the marked ground.
///
/// <para>3D port (Tier 1 logic, 3D presentation). Every timing and number is the 2D
/// one. The cloud is a flat disc on the ground rather than a sprite, because under the
/// fixed ¾ camera what the player must read is <em>which floor is about to be unsafe</em>
/// — a billowing volume would hide exactly that. The 2D warning pulse was an alpha
/// ramp; opaque greybox meshes have none, so the telegraph pulses the disc's
/// <b>scale</b> as well, and colour still goes through <see cref="MaterialTint"/> so
/// the alpha returns for free with a transparent material in the P3 art pass.</para>
/// </summary>
[RequireComponent(typeof(Collider))]
public class ToxicGasZone : MonoBehaviour
{
    [Header("Timing (seconds)")]
    [SerializeField] float telegraphTime = 0.9f;
    [SerializeField] float activeTime = 2.5f;
    [SerializeField] float fadeTime = 0.6f;

    [Header("Effect")]
    [SerializeField] int damage = 1;
    [SerializeField] float damageInterval = 0.7f;
    [Tooltip("World radius of the gas cloud.")]
    [SerializeField] float radius = 1.4f;

    [Header("Visual")]
    [Tooltip("Flat disc marking the affected floor.")]
    [SerializeField] Transform cloud;
    [SerializeField] Renderer cloudRenderer;
    [SerializeField] Color telegraphColor = new Color(0.75f, 0.35f, 0.95f, 0.3f);
    [SerializeField] Color activeColor = new Color(0.6f, 0.9f, 0.3f, 0.65f); // toxic green
    [Tooltip("Height of the damaging volume — enough to catch Greenie's 1.15 m capsule.")]
    [SerializeField] float volumeHeight = 1.4f;

    Collider col;
    bool active;
    bool playerInside;
    float nextDamageTime;
    PlayerHealth hp;

    /// <summary>True once the telegraph has elapsed and the gas actually bites.</summary>
    public bool IsActive => active;

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
        col.enabled = false; // harmless while telegraphing

        // Size the damaging volume in world units; the disc is scaled to match below.
        if (col is CapsuleCollider capsule)
        {
            capsule.direction = 1;                 // Y
            capsule.radius = radius;
            capsule.height = Mathf.Max(volumeHeight, radius * 2f);
            capsule.center = Vector3.up * volumeHeight * 0.5f;
        }
        else if (col is SphereCollider sphere)
        {
            sphere.radius = radius;
            sphere.center = Vector3.up * volumeHeight * 0.5f;
        }

        if (cloudRenderer == null && cloud != null) cloudRenderer = cloud.GetComponentInChildren<Renderer>();
        if (cloud != null) cloud.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
        MaterialTint.Apply(cloudRenderer, telegraphColor);
        StartCoroutine(Life());
    }

    IEnumerator Life()
    {
        Vector3 discScale = cloud != null ? cloud.localScale : Vector3.one;

        float t = 0f;
        while (t < telegraphTime)
        {
            t += Time.deltaTime;
            float pulse = Mathf.PingPong(t * 4f, 1f);
            var c = telegraphColor;
            c.a = 0.15f + 0.25f * pulse;                       // pulsing warning
            MaterialTint.Apply(cloudRenderer, c);
            if (cloud != null) cloud.localScale = discScale * (0.85f + 0.15f * pulse);
            yield return null;
        }

        active = true;
        col.enabled = true;
        if (cloud != null) cloud.localScale = discScale;
        MaterialTint.Apply(cloudRenderer, activeColor);
        yield return new WaitForSeconds(activeTime);

        active = false;
        col.enabled = false;
        float f = 0f;
        while (f < fadeTime)
        {
            f += Time.deltaTime;
            float u = f / fadeTime;
            var c = activeColor;
            c.a = Mathf.Lerp(activeColor.a, 0f, u);
            MaterialTint.Apply(cloudRenderer, c);
            if (cloud != null) cloud.localScale = discScale * (1f - u);
            yield return null;
        }
        Destroy(gameObject);
    }

    void Update()
    {
        if (active && playerInside && Time.time >= nextDamageTime && hp != null)
        {
            nextDamageTime = Time.time + damageInterval;
            hp.TakeDamage(damage, transform.position, 0f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        other.TryGetComponent(out hp);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }
}
