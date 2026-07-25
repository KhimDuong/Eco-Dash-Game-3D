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
            target.TakeDamage(damage);
            if (knockbackForce > 0f && other.TryGetComponent<IKnockbackable>(out var shovable))
                shovable.ApplyKnockback(travelDir * knockbackForce, knockbackDuration);
            Destroy(gameObject);
            return;
        }

        // Hit a solid obstacle (rusty debris / pipes) → fizzle out.
        if (!other.isTrigger)
            Destroy(gameObject);
    }
}
