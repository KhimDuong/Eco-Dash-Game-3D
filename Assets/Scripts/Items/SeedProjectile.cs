using UnityEngine;

/// <summary>
/// The player's weapon shot: an eco "Seed" (Hạt mầm). Flies in a straight line
/// across the ground plane, damages the first IDamageable it hits (enemies), and
/// dies on impact with obstacles or after its lifetime expires.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SeedProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] float speed = 10f;
    [SerializeField] int damage = 1;
    [SerializeField] float lifeTime = 3f;

    [Header("Impact")]
    [Tooltip("Impulse applied to a knocked-back target on hit (0 = no knockback).")]
    [SerializeField] float knockbackForce = 6f;
    [SerializeField] float knockbackDuration = 0.18f;

    /// <summary>Chip colour when a Seed splashes on scenery rather than an enemy.</summary>
    static readonly Color FizzleColor = new Color(0.62f, 0.82f, 0.45f);

    Rigidbody rb;
    Vector3 travelDir = Vector3.forward;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;   // seeds fly flat; gravity is never a mechanic
    }

    /// <summary>Send the seed flying in a world-space direction (flattened onto XZ).</summary>
    public void Launch(Vector3 direction)
    {
        direction.y = 0f;
        travelDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        rb.linearVelocity = travelDir * speed;
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return; // never hit the shooter

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            // Chips in the target's own colour, thrown before the hit lands — TakeDamage can
            // destroy the enemy outright, and there would be nothing left to read a colour off.
            Vfx.Impact(transform.position, travelDir, Vfx.ColorOf(other.gameObject, Color.white));
            target.TakeDamage(damage);
            if (knockbackForce > 0f && other.TryGetComponent<IKnockbackable>(out var shovable))
                shovable.ApplyKnockback(travelDir * knockbackForce, knockbackDuration);
            Destroy(gameObject);
            return;
        }

        // Hit a solid obstacle (rusty debris / pipes) → fizzle out.
        if (!other.isTrigger)
        {
            Vfx.Impact(transform.position, travelDir, FizzleColor);
            Destroy(gameObject);
        }
    }
}
