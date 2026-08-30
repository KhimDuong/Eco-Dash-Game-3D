using UnityEngine;

/// <summary>
/// Enemy ordnance — a toxic Smog Orb (Quả cầu khói độc). Flies straight, damages
/// the Player on contact (with knockback), and fizzles on walls or after its
/// lifetime. A mirror of <see cref="SeedProjectile"/> aimed at Greenie; reused by
/// the Fly-Bot and the Mega-Smog boss.
///
/// <para><b>Tier 1 (mechanical port).</b> Every number is the 2D one; only the
/// physics API changed. Like the Seed, the orb is <b>flattened onto XZ</b> — the
/// shooter may hover above Greenie, but a shot that visibly arced downward would
/// be unreadable under the fixed ¾ camera, and gravity is never a mechanic here.
/// The fly-bot's fire point hangs below its body so a flat orb still meets the
/// player capsule; see <see cref="PollutionFlyBot"/>.</para>
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] float speed = 6f;
    [SerializeField] int damage = 1;
    [SerializeField] float lifeTime = 4f;

    [Header("Impact")]
    [Tooltip("Impulse pushed onto the player on hit (0 = no knockback). The shove " +
             "duration is owned by PlayerHealth.")]
    [SerializeField] float knockbackForce = 6f;

    Rigidbody rb;
    Vector3 travelDir = Vector3.forward;
    float clearance;              // metres above the ground, held for the whole flight (B8)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;   // orbs fly flat; gravity is never a mechanic
    }

    /// <summary>Send the orb flying in a world-space direction (flattened onto XZ).</summary>
    public void Launch(Vector3 direction)
    {
        direction.y = 0f;
        travelDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        rb.linearVelocity = travelDir * speed;
        // Measured, not assumed: on Level 1's relief the muzzle is already at the hill's
        // height, so this comes out as the same ~0.60 m it has always been on flat ground.
        clearance = GroundHeight.ClearanceOf(transform.position);
        Destroy(gameObject, lifeTime);
    }

    /// <summary>Launch with a per-shot speed (the boss varies orb speed by pattern).</summary>
    public void Launch(Vector3 direction, float speedOverride)
    {
        speed = speedOverride;
        Launch(direction);
    }

    /// <summary>Follow the ground's rise and fall without ever leaving the XZ line it was
    /// fired along. A no-op in every flat scene — see <see cref="GroundHeight.Hug"/>.</summary>
    void FixedUpdate() => GroundHeight.Hug(rb, clearance);

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")) return; // never hit fellow minions / the boss

        if (other.TryGetComponent<PlayerHealth>(out var hp))
        {
            hp.TakeDamage(damage, transform.position, knockbackForce);
            Destroy(gameObject);
            return;
        }

        // Hit a solid wall/pipe → fizzle (ignore other triggers like pickups).
        if (!other.isTrigger) Destroy(gameObject);
    }
}
